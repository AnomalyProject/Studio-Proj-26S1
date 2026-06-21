using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public struct PlayerSessionInfo
{
    public ulong SteamID;
    public string DisplayName;
    public int TeamID;
    public bool IsReady;
    public bool IsHost;
    public DateTime JoinedAt;
    public bool IsInElevator;
    public int ColorIndex;
    public bool IsConnected;
    public float ReconnectDeadline;


    public PlayerSessionInfo(ulong steamID, string name, bool isHost = false, int teamID = -1, bool isInElevator = false)
    {
        SteamID = steamID;
        DisplayName = name;
        IsHost = isHost;
        TeamID = teamID;
        IsReady = false;
        JoinedAt = DateTime.UtcNow;
        IsInElevator = isInElevator;
        ColorIndex = 0;
        IsConnected = true;
        ReconnectDeadline = 0f;
    }

    public Color GetPlayerColor() => PlayerColour.GetColor(ColorIndex);

}

[Serializable]
public struct ClientPlayerInfo
{
    public ulong SteamID;
    public string DisplayName;
    public bool IsReady;
    public bool IsHost;
    public bool IsInElevator;
    public bool IsConnected;
    public bool IsWaitingToReconnect;
    public int ColorIndex;
}

[Serializable]
public struct ClientSessionData
{
    public ulong HostSteamID;
    public string MapName;
    public string GameMode;
    public int MaxPlayers;
    public int PlayerCount;
    public List<ClientPlayerInfo> Players;
    public ElevatorLobbyState ElevatorState;
    
    // level selection
    public string SelectedLevelId;

    //workaround for serialization issues with Dictionaries
    public List<string> CustomPropertyKeys;
    public List<string> CustomPropertyValues;
}

[Serializable]
public class SessionData
{
    public string SessionId { get; private set; }
    public ulong HostSteamID { get; set; }
    public List<PlayerSessionInfo> Players { get; set; } = new List<PlayerSessionInfo>();
    public string MapName { get; set; }
    public string GameMode { get; set; }
    public int MaxPlayers { get; set; }
    public DateTime CreatedAt { get; private set; }
    public GameState CurrentState { get; set; }
    public Dictionary<string, string> CustomProperties { get; set; } = new Dictionary<string, string>();

    // elevator
    public ElevatorLobbyState ElevatorState;
    public bool AllPlayersReadyInElevator => Players.Count > 0 && Players.All(pp => pp.IsInElevator && pp.IsReady);
    
    // level selection
    public string SelectedLevelId { get; set; }
    public string SelectedLevelSceneName { get; set; }


    public SessionData()
    {
        SessionId = Guid.NewGuid().ToString();
        CreatedAt = DateTime.UtcNow;
        CurrentState = GameState.Lobby;
    }

    public void AddPlayer(PlayerSessionInfo newPlayer)
    {
        if (Players.Any(pp => pp.SteamID == newPlayer.SteamID))
        {
            Debug.LogWarning($"[SessionDataManager] Player {newPlayer.SteamID} already exists.");
            return;
        }

        List<int> usedColors = Players.Select(p => p.ColorIndex).ToList();
        int assignedColor = 0;

        for (int i = 0; i < PlayerColour.Colors.Length; i++)
        {
            if (!usedColors.Contains(i))
            {
                assignedColor = i;
                break;
            }
        }

        newPlayer.ColorIndex = assignedColor;
        Players.Add(newPlayer);
    }

    public void RemovePlayer(ulong steamID)
    {
        int removedCount = Players.RemoveAll(pp => pp.SteamID == steamID);
        if (removedCount == 0)
        {
            Debug.LogWarning($"[SessionDataManager] Attempted to remove player {steamID}, but they were not in this session.");
        }
    }

    public PlayerSessionInfo? GetPlayer(ulong steamID)
    {
        var player = Players.FirstOrDefault(pp => pp.SteamID == steamID);
        return player.SteamID == 0 ? (PlayerSessionInfo?)null : player;
    }

    public bool IsSessionFull => Players.Count == MaxPlayers;

    public bool AllPlayersReady => Players.Count > 0 && Players.All(pp => pp.IsReady);

    public void ResetReadyStates()
    {
        for (int i = 0; i < Players.Count; i++)
        {
            var pp = Players[i];
            pp.IsReady = false;
            Players[i] = pp;
        }
    }

    public void SetCustomProperty(string key, string value) => CustomProperties[key] = value;

    public string GetCustomProperty(string key) => CustomProperties.ContainsKey(key) ? CustomProperties[key] : "";

    public int FindPlayerIndex(ulong steamID) => Players.FindIndex(pp => pp.SteamID == steamID);

    public bool SetPlayerConnected(ulong steamID, bool connected, float reconnectDeadline)
    {
        int index = FindPlayerIndex(steamID);

        if (index < 0) return false;

        PlayerSessionInfo player = Players[index];
        player.IsConnected = connected;
        player.ReconnectDeadline = reconnectDeadline;
        Players[index] = player;

        return true;
    }

    public bool IsPlayerWaitingToReconnect(ulong steamID)
    {
        int index = FindPlayerIndex(steamID);

        if (index < 0)
        {
            return false;
        }

        return !Players[index].IsConnected;
    }
}
