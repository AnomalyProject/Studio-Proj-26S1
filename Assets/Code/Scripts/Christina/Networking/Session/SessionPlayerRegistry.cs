using System.Collections.Generic;
using PurrNet;

/// <summary>
/// Tracks PlayerID to SteamID mappings for the active session.
/// Knows which connected PlayerID is the Host. Pure data, no RPCs in this class.
/// </summary>
public class SessionPlayerRegistry
{
    #region Fields
    
    private readonly Dictionary<PlayerID, ulong> playerConnectionMap = new Dictionary<PlayerID, ulong>();
    private PlayerID? hostPlayerID;
    private PlayerID? pendingHostConnection;
    
    #endregion
    
    #region Properties

    public int Count
    {
        get { return playerConnectionMap.Count;  }
    }

    public PlayerID? HostPlayerID
    {
        get { return hostPlayerID; }
    }

    public PlayerID? PendingHostConnection
    {
        get { return pendingHostConnection; }
        set { pendingHostConnection = value; }
    }
    
    #endregion
    
    #region Public Methods

    public void Register(PlayerID playerID, ulong steamID, bool isHost)
    {
        playerConnectionMap[playerID] = steamID;
        if (isHost) hostPlayerID = playerID;
    }

    public void Unregister(PlayerID playerID)
    {
        playerConnectionMap.Remove(playerID);
    }

    public bool IsRegistered(PlayerID playerID)
    {
        return playerConnectionMap.ContainsKey(playerID);
    }

    public bool ContainsSteamID(ulong steamID)
    {
        return playerConnectionMap.ContainsValue(steamID);
    }

    public bool TryGetSteamID(PlayerID playerID, out ulong steamID)
    {
        return playerConnectionMap.TryGetValue(playerID, out steamID);
    }

    public PlayerID? FindPlayerIDForSteam(ulong steamID)
    {
        foreach (KeyValuePair<PlayerID, ulong> entry in playerConnectionMap)
        {
            if(entry.Value == steamID) return entry.Key;
        }

        return null;
    }

    public bool IsHost(PlayerID playerID)
    {
        return hostPlayerID.HasValue && hostPlayerID.Value == playerID;
    }

    public IEnumerable<PlayerID> AllPlayerIDs()
    {
        return playerConnectionMap.Keys;
    }

    public void Clear()
    {
        playerConnectionMap.Clear();
        hostPlayerID = null;
        pendingHostConnection = null;
    }
    
    #endregion
}
