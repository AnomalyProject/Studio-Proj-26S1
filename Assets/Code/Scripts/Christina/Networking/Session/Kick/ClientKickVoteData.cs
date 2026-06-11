using System;

[Serializable]
public struct ClientKickVoteData
{
    public bool HasActiveVote;
    public ulong TargetSteamID;
    public string TargetDisplayName;
    public SessionKickReason Reason;
    public int YesVotes;
    public int NoVotes;
    public int RequiredYesVotes;
    public float RemainingSeconds;
}