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
    
    public bool TryMarkDisconnected(PlayerID playerID, float reconnectDeadline, out ulong steamID)
    {
        // required because steamID must always get a value
        steamID = 0;
        
        if (!sessionStore.HasSession) return false;

        if (!registry.IsRegistered(playerID)) return false;

        if (!registry.TryGetSteamID(playerID, out steamID)) return false;

        if (!sessionStore.Current.SetPlayerConnected(steamID, false, reconnectDeadline)) return false;

        sessionStore.Current.ResetReadyStates();

        return true;
    }
    
    public SessionCommandResult TryReconnect(PlayerID playerID, ulong steamID)
    {
        if (!sessionStore.HasSession)
        {
            return SessionCommandResult.Failed(SessionErrorCode.InvalidState, "There is no live Session.");
        }

        if (!sessionStore.Current.IsPlayerWaitingToReconnect(steamID))
        {
            return SessionCommandResult.Failed(SessionErrorCode.PlayerNotFound, "Player is not waiting to reconnect.");
        }

        PlayerID? waitingPlayerID = registry.FindPlayerIDForSteam(steamID);

        if (!waitingPlayerID.HasValue)
        {
            return SessionCommandResult.Failed(SessionErrorCode.PlayerNotFound, "Player was not found in session.");
        }
        
        if (waitingPlayerID.Value != playerID)
        {
            return SessionCommandResult.Failed(SessionErrorCode.InvalidState, "Reconnect PlayerID did not match the disconnected player.");
        }
        
        PlayerSessionInfo? playerInfo = sessionStore.Current.GetPlayer(steamID);
        
        if (!playerInfo.HasValue)
        {
            return SessionCommandResult.Failed(SessionErrorCode.PlayerNotFound, "Player was not found in session.");
        }

        sessionStore.Current.SetPlayerConnected(steamID, true, 0f);
        registry.Register(playerID, steamID, playerInfo.Value.IsHost);

        return SessionCommandResult.Succeeded();
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
    
    public SessionCommandResult TryRemoveDisconnectedPlayer(ulong steamID, PlayerID oldPlayerID)
    {
        if (!sessionStore.HasSession)
        {
            return SessionCommandResult.Failed(SessionErrorCode.InvalidState, "There is no live Session.");
        }

        if (!sessionStore.Current.IsPlayerWaitingToReconnect(steamID))
        {
            return SessionCommandResult.Failed(SessionErrorCode.InvalidState, "Player is not waiting to reconnect.");
        }

        sessionStore.Current.RemovePlayer(steamID);
        registry.Unregister(oldPlayerID);
        sessionStore.Current.ResetReadyStates();

        return SessionCommandResult.Succeeded();
    }

    #endregion
    
    #endregion
}
