using Steamworks;

public static class SteamIdentity
{
    public static bool TryGetLocalSteamID(out ulong steamID)
    {
        steamID = 0;

        if (!SteamManager.Initialized) return false;
        
        steamID = SteamUser.GetSteamID().m_SteamID;
        return steamID != 0;
    }
}
