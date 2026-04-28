using System;

[Serializable]
public struct JoinableFriendInfo
{
    public ulong SteamID;
    public string DisplayName;
    public ulong LobbyID;

    public JoinableFriendInfo(ulong steamID, string displayName, ulong lobbyID)
    {
        SteamID = steamID;
        DisplayName = displayName;
        LobbyID = lobbyID;
    }
}