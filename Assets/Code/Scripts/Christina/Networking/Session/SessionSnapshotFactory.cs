using System.Collections.Generic;

/// <summary>
/// Creates the client-safe session snapshot sent from the host/server to connected clients.
/// </summary>
/// <remarks>
/// This class only copies data from the authoritative <see cref="SessionData"/> into
/// <see cref="ClientSessionData"/>. It does not validate, mutate, store, or send anything.
/// Use this when SessionManager needs to broadcast the current lobby/session state after
/// a join, leave, ready change, lobby setting change, or elevator state update.
///
/// If new fields are added to <see cref="ClientSessionData"/> or <see cref="ClientPlayerInfo"/>,
/// update this factory so clients receive them.
/// </remarks>
public static class SessionSnapshotFactory
{
    #region public

    /// <summary>
    /// Copies the public serializable fields the client needs out of the autoritative session.
    /// Safe to call any time on the server. 
    /// </summary>
    /// <param name="sessionData"></param>
    /// <returns></returns>
    public static ClientSessionData Build(SessionData sessionData)
    {
        if(sessionData == null) return new ClientSessionData();
        
        List<ClientPlayerInfo> players = new List<ClientPlayerInfo>();

        for (int i = 0; i < sessionData.Players.Count; i++)
        {
            PlayerSessionInfo p = sessionData.Players[i];
            ClientPlayerInfo info = new ClientPlayerInfo();
            info.SteamID = p.SteamID;
            info.DisplayName = p.DisplayName;
            info.IsReady = p.IsReady;
            info.IsHost = p.IsHost;
            info.IsInElevator = p.IsInElevator;
            info.IsConnected = p.IsConnected;
            info.IsWaitingToReconnect = !p.IsConnected;
            players.Add(info);
        }
        
        List<string> keys = new List<string>();
        List<string> values = new List<string>();

        foreach (KeyValuePair<string, string> kvp in sessionData.CustomProperties)
        {
            keys.Add(kvp.Key);
            values.Add(kvp.Value);
        }
        
        ClientSessionData snapshot = new ClientSessionData();
        snapshot.HostSteamID = sessionData.HostSteamID;
        snapshot.MapName = sessionData.MapName;
        snapshot.SelectedLevelId = sessionData.SelectedLevelId;
        snapshot.SelectedLevelId = sessionData.SelectedLevelId;
        snapshot.GameMode = sessionData.GameMode;
        snapshot.MaxPlayers = sessionData.MaxPlayers;
        snapshot.PlayerCount = sessionData.Players.Count;
        snapshot.Players = players;
        snapshot.CustomPropertyKeys = keys;
        snapshot.CustomPropertyValues = values;
        snapshot.ElevatorState = sessionData.ElevatorState;
        return snapshot;
    }
    
    #endregion
}
