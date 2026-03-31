using UnityEngine;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
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

    public PlayerSessionInfo(ulong steamID, string name, bool isHost = false, int teamID = -1)
    {
        SteamID = steamID;
        DisplayName = name;
        IsHost = isHost;
        TeamID = teamID;
        IsReady = false;
        JoinedAt = DateTime.UtcNow; // TODO: check if we need to convert to long
    }

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
            Debug.Log($"[SessionDataManager] Player {newPlayer.SteamID} already exists. Skipping to next player.");
            return;
        }
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

    public string GetCustomProperty(string key)
    {
        return CustomProperties.TryGetValue(key, out string value) ? value : null;
    }
}
