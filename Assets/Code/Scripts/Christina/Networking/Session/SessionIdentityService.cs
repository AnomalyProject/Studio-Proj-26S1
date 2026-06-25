using UnityEngine;
using PurrNet;
using PurrNet.Steam;
using Steamworks;

/// <summary>
/// Resolves Steam/dev/solo identity for both the local host and any connected PlayerID.
/// Centralizes display-name sanitization so the rules live in one place. 
/// </summary>
public class SessionIdentityService
{
    #region Constants 
    
    private const int MaxDisplayNameLength = 32;
    private const string FallbackDisplayName = "Anomalous Player";
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Returns the SteamID and display name to use for the local host.
    /// Falls back to a deterministic display name when Steam isn't available
    /// </summary>
    /// <returns></returns>
    public LocalHostIdentity ResolveLocalHost()
    {
        var raw = LocalIdentity.ResolveHost();
        LocalHostIdentity identity = new LocalHostIdentity();
        identity.SteamID = raw.steamID;
        identity.DisplayName = Sanitize(raw.displayName);
        return identity;
    }

    public bool TryResolveJoiner(PlayerID sender, out ulong steamID, out string displayName)
    {
        steamID = 0;
        displayName = FallbackDisplayName;

        if (!PurrSteamUtils.TryGetSteamID(sender, out steamID)) return false;

        if (SteamSessionBridge.Instance == null || !SteamSessionBridge.Instance.IsLobbyMember(steamID)) return false;
        
        string rawName = SteamFriends.GetFriendPersonaName(new CSteamID(steamID));
        displayName = Sanitize(rawName);
        return true;
    }
    
    public bool TryResolveReconnectJoiner(PlayerID sender, out ulong steamID, out string displayName)
    {
        steamID = 0;
        displayName = FallbackDisplayName;

        if (!PurrSteamUtils.TryGetSteamID(sender, out steamID)) return false;
        if (steamID == 0) return false;

        string rawName = SteamFriends.GetFriendPersonaName(new CSteamID(steamID));
        displayName = Sanitize(rawName);

        return true;
    }
    
    public string Sanitize(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return FallbackDisplayName;
        }

        string trimmed = displayName.Trim();
        if (trimmed.Length > MaxDisplayNameLength)
        {
            trimmed = trimmed.Substring(0, MaxDisplayNameLength);
        }

        return trimmed;
    }
    
    #endregion
}

public struct LocalHostIdentity
{
    public ulong SteamID;
    public string DisplayName;
}