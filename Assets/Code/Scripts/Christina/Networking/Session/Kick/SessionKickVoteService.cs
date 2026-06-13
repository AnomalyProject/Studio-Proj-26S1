using System.Collections.Generic;
using UnityEngine;
using PurrNet;

public enum KickVoteOutcome
{
    None,
    Passed,
    Failed,
    Expired, 
    Cancelled
}

public class SessionKickVoteService
{
    private const float VoteDurationSeconds = 25f;
    private const float SessionVoteCooldownSeconds = 45f;
    private const float PlayerStartCooldownSeconds = 120f;
    private const int MinEligibleVoters = 2;

    private readonly SessionStateStore sessionStore;
    private readonly SessionPlayerRegistry registry;

    private ActiveKickVote activeVote;
    private float nextSessionVoteAllowedAt;

    private readonly Dictionary<ulong, float> nextPlayerVoteStartAllowedAt = new Dictionary<ulong, float>();
    private readonly HashSet<ulong> kickedFromSession = new HashSet<ulong>();
    
    public SessionKickVoteService(SessionStateStore sessionStore, SessionPlayerRegistry registry)
    {
        this.sessionStore = sessionStore;
        this.registry = registry;
    }
    
    public bool IsKickedFromSession(ulong steamID)
    {
        return kickedFromSession.Contains(steamID);
    }

    public void Clear()
    {
        activeVote = null;
        nextSessionVoteAllowedAt = 0f;
        nextPlayerVoteStartAllowedAt.Clear();
        kickedFromSession.Clear();
    }
    
     public SessionCommandResult TryStartVote(PlayerID sender, ulong targetSteamID, SessionKickReason reason, float now, out ClientKickVoteData voteData)
    {
        voteData = new ClientKickVoteData();

        if (!sessionStore.HasSession) return SessionCommandResult.Failed(SessionErrorCode.InvalidState, "There is no active session.");

        if (activeVote != null) return SessionCommandResult.Failed(SessionErrorCode.InvalidState, "A kick vote is already active.");

        if (now < nextSessionVoteAllowedAt) return SessionCommandResult.Failed(SessionErrorCode.InvalidState, "Kick votes are on cooldown.");

        if (!registry.TryGetSteamID(sender, out ulong starterSteamID)) return SessionCommandResult.Failed(SessionErrorCode.PlayerNotFound, "You are not in this session.");

        if (nextPlayerVoteStartAllowedAt.TryGetValue(starterSteamID, out float playerCooldown) && now < playerCooldown) return SessionCommandResult.Failed(SessionErrorCode.InvalidState, "You must wait before starting another kick vote.");

        SessionData session = sessionStore.Current;

        PlayerSessionInfo? target = session.GetPlayer(targetSteamID);
        if (!target.HasValue) return SessionCommandResult.Failed(SessionErrorCode.PlayerNotFound, "Target player was not found.");

        if (!target.Value.IsConnected) return SessionCommandResult.Failed(SessionErrorCode.InvalidState, "Target player is not connected.");

        if (target.Value.IsHost) return SessionCommandResult.Failed(SessionErrorCode.InvalidState, "The host cannot be vote-kicked.");

        if (targetSteamID == starterSteamID) return SessionCommandResult.Failed(SessionErrorCode.InvalidState, "You cannot start a vote against yourself.");

        List<ulong> eligibleVoters = GetEligibleVoters(targetSteamID);

        if (eligibleVoters.Count < MinEligibleVoters) return SessionCommandResult.Failed(SessionErrorCode.InvalidState, "Vote kick requires at least three connected players.");

        if (!eligibleVoters.Contains(starterSteamID)) return SessionCommandResult.Failed(SessionErrorCode.InvalidState, "You are not allowed to vote.");

        activeVote = new ActiveKickVote();
        activeVote.StarterSteamID = starterSteamID;
        activeVote.TargetSteamID = targetSteamID;
        activeVote.TargetDisplayName = target.Value.DisplayName;
        activeVote.Reason = reason;
        activeVote.StartedAt = now;
        activeVote.EndsAt = now + VoteDurationSeconds;
        activeVote.RequiredYesVotes = CalculateRequiredYesVotes(eligibleVoters.Count);
        activeVote.EligibleVoters = new HashSet<ulong>(eligibleVoters);
        activeVote.YesVotes.Add(starterSteamID);

        nextPlayerVoteStartAllowedAt[starterSteamID] = now + PlayerStartCooldownSeconds;

        voteData = BuildClientData(now);
        return SessionCommandResult.Succeeded();
    }
     
     public SessionCommandResult TryCastVote( PlayerID sender, bool voteYes, float now, out ClientKickVoteData voteData, out KickVoteOutcome outcome, out ulong targetSteamID)
    {
        voteData = new ClientKickVoteData();
        outcome = KickVoteOutcome.None;
        targetSteamID = 0;

        if (activeVote == null)
            return SessionCommandResult.Failed(SessionErrorCode.InvalidState, "There is no active kick vote.");

        if (!registry.TryGetSteamID(sender, out ulong voterSteamID))
            return SessionCommandResult.Failed(SessionErrorCode.PlayerNotFound, "You are not in this session.");

        if (!activeVote.EligibleVoters.Contains(voterSteamID))
            return SessionCommandResult.Failed(SessionErrorCode.InvalidState, "You are not allowed to vote.");

        activeVote.YesVotes.Remove(voterSteamID);
        activeVote.NoVotes.Remove(voterSteamID);

        if (voteYes) activeVote.YesVotes.Add(voterSteamID);
        else activeVote.NoVotes.Add(voterSteamID);

        targetSteamID = activeVote.TargetSteamID;

        if (activeVote.YesVotes.Count >= activeVote.RequiredYesVotes)
        {
            outcome = KickVoteOutcome.Passed;
            kickedFromSession.Add(activeVote.TargetSteamID);
            FinishVote(now);
        }
        else if (HasEnoughNoVotesToFail())
        {
            outcome = KickVoteOutcome.Failed;
            FinishVote(now);
        }

        voteData = BuildClientData(now);
        return SessionCommandResult.Succeeded();
    }

    public bool Tick(float now, out ClientKickVoteData voteData, out KickVoteOutcome outcome)
    {
        voteData = new ClientKickVoteData();
        outcome = KickVoteOutcome.None;

        if (activeVote == null) return false;
        if (now < activeVote.EndsAt) return false;

        outcome = KickVoteOutcome.Expired;
        FinishVote(now);
        voteData = BuildClientData(now);
        return true;
    }
    
    /// <summary>
    /// Updates the active vote when a player leaves or disconnects.
    /// Cancels the vote if the target is no longer present, removes departed voters
    /// from the voter pool, recalculates the threshold, and resolves the vote if the
    /// new vote counts now pass or fail.
    /// </summary>
    public bool HandlePlayerUnavailable(ulong steamID, float now, out ClientKickVoteData voteData, out KickVoteOutcome outcome, out ulong targetSteamID)
    {
        voteData = new ClientKickVoteData();
        outcome = KickVoteOutcome.None;
        targetSteamID = 0;

        if (activeVote == null) return false;

        targetSteamID = activeVote.TargetSteamID;

        if (steamID == activeVote.TargetSteamID)
        {
            outcome = KickVoteOutcome.Cancelled;
            FinishVote(now);
            voteData = BuildClientData(now);
            return true;
        }

        if (!activeVote.EligibleVoters.Remove(steamID)) return false;

        activeVote.YesVotes.Remove(steamID);
        activeVote.NoVotes.Remove(steamID);

        if (activeVote.EligibleVoters.Count < MinEligibleVoters)
        {
            outcome = KickVoteOutcome.Cancelled;
            FinishVote(now);
            voteData = BuildClientData(now);
            return true;
        }

        activeVote.RequiredYesVotes = CalculateRequiredYesVotes(activeVote.EligibleVoters.Count);

        if (activeVote.YesVotes.Count >= activeVote.RequiredYesVotes)
        {
            outcome = KickVoteOutcome.Passed;
            kickedFromSession.Add(activeVote.TargetSteamID);
            FinishVote(now);
        }
        else if (HasEnoughNoVotesToFail())
        {
            outcome = KickVoteOutcome.Failed;
            FinishVote(now);
        }

        voteData = BuildClientData(now);
        return true;
    }
    
    /// <summary>
    /// Returns the number of yes votes required to pass a vote kick.
    /// The target is excluded from the eligible voter count before this is called,
    /// so this is a strict majority of the players who are allowed to vote.
    /// </summary>
    private int CalculateRequiredYesVotes(int eligibleVoterCount)
    {
        return eligibleVoterCount / 2 + 1;
    }
    
    /// <summary>
    /// Returns how many no votes make it mathematically impossible for the vote to pass.
    /// Example: 3 eligible voters, 2 yes required. Once 2 people vote no, the vote has failed.
    /// </summary>
    private int CalculateBlockingNoVotes(int eligibleVoterCount, int requiredYesVotes)
    {
        return eligibleVoterCount - requiredYesVotes + 1;
    }
    
    /// <summary>
    /// Checks whether the active vote has enough no votes to fail immediately.
    /// </summary>
    private bool HasEnoughNoVotesToFail()
    {
        int blockingNoVotes = CalculateBlockingNoVotes(
            activeVote.EligibleVoters.Count,
            activeVote.RequiredYesVotes
        );

        return activeVote.NoVotes.Count >= blockingNoVotes;
    }

    private List<ulong> GetEligibleVoters(ulong targetSteamID)
    {
        List<ulong> voters = new List<ulong>();

        foreach (PlayerSessionInfo player in sessionStore.Current.Players)
        {
            if (!player.IsConnected) continue;
            if (player.SteamID == targetSteamID) continue;

            voters.Add(player.SteamID);
        }

        return voters;
    }

    private ClientKickVoteData BuildClientData(float now)
    {
        ClientKickVoteData data = new ClientKickVoteData();

        if (activeVote == null)
            return data;

        data.HasActiveVote = true;
        data.TargetSteamID = activeVote.TargetSteamID;
        data.TargetDisplayName = activeVote.TargetDisplayName;
        data.Reason = activeVote.Reason;
        data.YesVotes = activeVote.YesVotes.Count;
        data.NoVotes = activeVote.NoVotes.Count;
        data.RequiredYesVotes = activeVote.RequiredYesVotes;
        data.RemainingSeconds = Mathf.Max(0f, activeVote.EndsAt - now);

        return data;
    }

    private void FinishVote(float now)
    {
        activeVote = null;
        nextSessionVoteAllowedAt = now + SessionVoteCooldownSeconds;
    }

    private class ActiveKickVote
    {
        public ulong StarterSteamID;
        public ulong TargetSteamID;
        public string TargetDisplayName;
        public SessionKickReason Reason;
        public float StartedAt;
        public float EndsAt;
        public int RequiredYesVotes;
        public HashSet<ulong> EligibleVoters = new HashSet<ulong>();
        public HashSet<ulong> YesVotes = new HashSet<ulong>();
        public HashSet<ulong> NoVotes = new HashSet<ulong>();
    }
    
}
