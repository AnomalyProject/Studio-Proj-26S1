using System;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using System.Collections;


public class SteamFriendsLobbyBrowser : MonoBehaviour
{
    public bool TestMode;

    private readonly List<JoinableFriendInfo> joinableFriends = new();
    private Callback<FriendRichPresenceUpdate_t> friendRichPresenceUpdateCallback;

    public IReadOnlyList<JoinableFriendInfo> JoinableFriends => joinableFriends;

    public event Action OnFriendsUpdated;
    public event Action<string> OnRefreshFailed;

    private void Start()
    {
        if (!SteamManager.Initialized)
            return;

        friendRichPresenceUpdateCallback =
            Callback<FriendRichPresenceUpdate_t>.Create(OnFriendRichPresenceUpdated);
    }

    public void RefreshJoinableFriends()
    {
        joinableFriends.Clear();

        if (!SteamManager.Initialized)
        {
            OnRefreshFailed?.Invoke("Steam is not initialized.");
            OnFriendsUpdated?.Invoke();
            return;
        }

        int friendCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);

        for (int i = 0; i < friendCount; i++)
        {
            CSteamID friendId = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
            SteamFriends.RequestFriendRichPresence(friendId);
        }

        RebuildJoinableFriendsList();
    }

    private void OnFriendRichPresenceUpdated(FriendRichPresenceUpdate_t callback)
    {
        RebuildJoinableFriendsList();
    }

    private void RebuildJoinableFriendsList()
    {
        joinableFriends.Clear();

        if (!SteamManager.Initialized)
        {
            OnFriendsUpdated?.Invoke();
            return;
        }

        int friendCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);

        for (int i = 0; i < friendCount; i++)
        {
            CSteamID friendId = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);

            if (!SteamFriends.GetFriendGamePlayed(friendId, out FriendGameInfo_t gameInfo)) continue;

            if (gameInfo.m_gameID.AppID() != SteamUtils.GetAppID()) continue;

            string connectString = SteamFriends.GetFriendRichPresence(friendId, "connect");
            if (!TryParseLobbyId(connectString, out ulong lobbyId)) continue;

            joinableFriends.Add(new JoinableFriendInfo(
                friendId.m_SteamID,
                SteamFriends.GetFriendPersonaName(friendId),
                lobbyId));
        }

        if (TestMode)
        {
            joinableFriends.Add(new JoinableFriendInfo(00, "Babis", 00));
            joinableFriends.Add(new JoinableFriendInfo(00, "Mitsos", 00));
            joinableFriends.Add(new JoinableFriendInfo(00, "Takis", 00));
            joinableFriends.Add(new JoinableFriendInfo(00, "Leon Kennedy", 00));
        }

        OnFriendsUpdated?.Invoke();
    }

    public void JoinFriendLobby(ulong lobbyId)
    {
        if (SteamSessionBridge.Instance == null)
        {
            OnRefreshFailed?.Invoke("SteamSessionBridge was not found.");
            return;
        }

        MainMenuManager.Instance?.JoinCoOp(lobbyId);
    }

    private bool TryParseLobbyId(string connectString, out ulong lobbyId)
    {
        lobbyId = 0;

        if (string.IsNullOrWhiteSpace(connectString)) return false;

        string[] parts = connectString.Split(' ');
        if (parts.Length != 2) return false;

        if (parts[0] != "+connect_lobby") return false;

        return ulong.TryParse(parts[1], out lobbyId);
    }
}
