
/// <summary>
/// Owns the authoritative SessionData for the current network session.
/// Creates it on session start, exposes it for read access and resets it on teardown
/// </summary>
public class SessionStateStore
{
    #region Fields

    private SessionData currentSession;

    #endregion
    
    #region Properties

    public SessionData Current
    {
        get { return currentSession; }
    }

    public bool HasSession
    {
        get { return currentSession != null; }
    }

    #endregion
    
    #region Public Methods

    /// <summary>
    /// Creates a fresh session with default settings.
    /// Returns  the new SessionData so the caller can wire it up to other systems.
    /// </summary>
    /// <returns></returns>
    public SessionData CreateSession(ulong hostSteamID)
    {
        SessionData data = new SessionData();
        data.HostSteamID = hostSteamID;
        data.MapName = "Default";
        data.GameMode = "Default";
        data.MaxPlayers = 4;
        
        currentSession = data;
        return currentSession;
    }

    public void Clear()
    {
        currentSession = null;
    }
    
    #endregion
}
