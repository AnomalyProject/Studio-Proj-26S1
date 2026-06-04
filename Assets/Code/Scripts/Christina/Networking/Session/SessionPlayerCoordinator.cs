using PurrNet;

public class SessionPlayerCoordinator
{
    #region Dependencies

    private readonly SessionStateStore sessionStore;
    private readonly SessionPlayerRegistry registry;
    private readonly SessionIdentityService identityService;

    #endregion
    
    #region Construction

    public SessionPlayerCoordinator(
        SessionStateStore sessionStore,
        SessionPlayerRegistry registry,
        SessionIdentityService identityService)
    {
        this.sessionStore = sessionStore;
        this.registry = registry;
        this.identityService = identityService;
    }

    #endregion
    
    #region Public Methods
    
    public SessionCommandResult TryAcceptJoin(PlayerID sender, ulong steamID, string displayName)
    {
        if (!sessionStore.HasSession)
            return SessionCommandResult.Failed(SessionErrorCode.InvalidState, "There is no live Session.");

        SessionData session = sessionStore.Current;

        if (session.IsSessionFull)
            return SessionCommandResult.Failed(SessionErrorCode.SessionFull, "Session is full.");

        if (!IsJoinAllowedInCurrentGameState())
            return SessionCommandResult.Failed(SessionErrorCode.InvalidState, "Cannot join, game already in progress.");

        if (registry.IsRegistered(sender))
            return SessionCommandResult.Failed(SessionErrorCode.AlreadyInSession, "You are already in session.");

        if (registry.ContainsSteamID(steamID) || session.GetPlayer(steamID).HasValue)
            return SessionCommandResult.Failed(SessionErrorCode.AlreadyInSession, "This Steam account is already in session.");

        if (session.ElevatorState != ElevatorLobbyState.Open)
            return SessionCommandResult.Failed(SessionErrorCode.InvalidState, "The elevator is already leaving.");

        AddPlayerInternal(sender, steamID, displayName, isHost: false);
        return SessionCommandResult.Succeeded();
    }
    
    public SessionCommandResult TryLeaveSession(PlayerID sender)
    {
        if (!registry.IsRegistered(sender))
            return SessionCommandResult.Failed(SessionErrorCode.PlayerNotFound, "You are not in this session.");

        registry.TryGetSteamID(sender, out ulong steamID);
        RemovePlayerInternal(sender, steamID);
        return SessionCommandResult.Succeeded();
    }
    
    /// <summary>
    /// Called when PurrNet reports a disconnect for a player that was in session.
    /// Same data path as a voluntary leave.
    /// </summary>
    public bool TryHandleDisconnect(PlayerID playerID, out ulong steamID)
    {
        steamID = 0;
        if (!registry.IsRegistered(playerID)) return false;

        registry.TryGetSteamID(playerID, out steamID);
        RemovePlayerInternal(playerID, steamID);
        return true;
    }
    
    /// <summary>
    /// Registers the local host as the first player in the session.
    /// Assumes the caller already verified the registry is empty and the PlayerID is valid.
    /// </summary>
    public LocalHostIdentity RegisterHost(PlayerID hostPlayerID)
    {
        LocalHostIdentity host = identityService.ResolveLocalHost();
        AddPlayerInternal(hostPlayerID, host.SteamID, host.DisplayName, isHost: true);
        return host;
    }
    
    #region Internal Helpers

    private void AddPlayerInternal(PlayerID playerID, ulong steamID, string displayName, bool isHost)
    {
        PlayerSessionInfo info = new PlayerSessionInfo(steamID, displayName, isHost);
        sessionStore.Current.AddPlayer(info);
        registry.Register(playerID, steamID, isHost);
    }

    private void RemovePlayerInternal(PlayerID playerID, ulong steamID)
    {
        sessionStore.Current.RemovePlayer(steamID);
        registry.Unregister(playerID);
        sessionStore.Current.ResetReadyStates();
    }

    private bool IsJoinAllowedInCurrentGameState()
    {
        GameState current = GameStateManager.Instance.CurrentState;
        if (current == GameState.Lobby) return true;

        bool devInGameJoin =
            SessionModeManager.Instance != null &&
            SessionModeManager.Instance.CurrentMode == SessionMode.DevHost &&
            current == GameState.InGame;

        return devInGameJoin;
    }

    #endregion
    
    #endregion
}
