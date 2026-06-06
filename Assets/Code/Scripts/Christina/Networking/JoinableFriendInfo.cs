using Steamworks;
using System;
using System.Runtime.CompilerServices;

[Serializable]
public struct JoinableFriendInfo
{
    public ulong SteamID;
    public string DisplayName;
    public ulong LobbyID;
    //public bool isPrivate {get; private set;}

    public JoinableFriendInfo(ulong steamID, string displayName, ulong lobbyID)
    {
        SteamID = steamID;
        DisplayName = displayName;
        LobbyID = lobbyID;
        //isPrivate = SteamMatchmaking.GetLobbyData(new CSteamID(LobbyID), "has_password").ToLower() == "true";
    }
}