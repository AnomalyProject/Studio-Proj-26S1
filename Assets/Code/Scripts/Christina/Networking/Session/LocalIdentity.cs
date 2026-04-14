using Steamworks;

/// <summary>
/// Resolves the host's identity (SteamID + display name).
/// Falls back to a deterministiv local identity when Steamworks is not initialized,
/// so solo session can run without a Steam client.
/// The fallback steamID is a sentinel value and not a real Steam ID.
/// Chosen to avoid overlapping with Steamworks' own "invalid/none" zero.  
/// </summary>
public static class LocalIdentity
{
    // these are public so anyone can check if this is a fallback host
    public const ulong SoloFallbackSteamID = 1UL;
    public const string SoloFallbackDisplayName = "Solo Player";

    public static (ulong steamID, string displayName) ResolveHost()
    {
        if (SteamManager.Initialized)
        {
            return (SteamUser.GetSteamID().m_SteamID, SteamFriends.GetPersonaName()); 
        }
        
        return (SoloFallbackSteamID, SoloFallbackDisplayName);
    }
}
