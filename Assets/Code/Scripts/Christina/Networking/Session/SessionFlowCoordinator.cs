using UnityEngine;

/// <summary>
/// Orchestrates server-side session flow-> starting the match -> returning to the lobby.
/// Validates conditions, mutates session state, then asks
/// GameStateManager and SessionModeManager to perform the actual transitions.
/// Authority concerns (isServer) stay on SessionManager.
/// </summary>
public class SessionFlowCoordinator
{
    #region Dependencies

    private readonly SessionStateStore sessionStore;

    #endregion
    
    #region Construction

    public SessionFlowCoordinator(SessionStateStore sessionStore)
    {
        this.sessionStore = sessionStore;
    }
    #endregion
    
    #region Public Methods
    /// <summary>
    /// Validates that the elevator sealed and every player inside is ready, then
    /// transitions to Loading and asks SessionModeManager to load the gameplay scene.
    /// Does NOT check AllPlayersReady. That's a button concern.
    /// </summary>
    public SessionCommandResult TryStartMatch()
    {
        if (!sessionStore.HasSession)
            return SessionCommandResult.Failed(SessionErrorCode.InvalidState, "No active session.");

        if (GameStateManager.Instance.CurrentState != GameState.Lobby)
            return SessionCommandResult.Failed(SessionErrorCode.InvalidState, "Game already in progress.");

        SessionData session = sessionStore.Current;

        if (session.ElevatorState != ElevatorLobbyState.DoorsClosed)
            return SessionCommandResult.Failed(SessionErrorCode.InvalidState, "The elevator is not ready.");

        if (!session.AllPlayersReadyInElevator)
            return SessionCommandResult.Failed(SessionErrorCode.PlayersNotReady, "Not all players are ready.");

        if (SessionModeManager.Instance == null)
            return SessionCommandResult.Failed(SessionErrorCode.InvalidState, "SessionModeManager missing. Cannot load gameplay scene.");

        GameStateManager.Instance.RequestStateChange(GameState.Loading);
        SessionModeManager.Instance.LoadGameplayScene();
        Debug.Log("[SessionFlowCoordinator] Elevator locked. Game starting.");
        return SessionCommandResult.Succeeded();
    }
    
    /// <summary>
    /// Validates the game is in a gameplay state, then resets lobby data and asks
    /// SessionModeManager to load the lobby scene. The SessionModeManager null check
    /// runs BEFORE any mutation, so a missing scene manager cannot leave clients in
    /// a half-state.
    /// </summary>
    public SessionCommandResult TryReturnToLobby()
    {
        if (!sessionStore.HasSession)
            return SessionCommandResult.Failed(SessionErrorCode.InvalidState, "No active session.");

        GameState current = GameStateManager.Instance.CurrentState;
        if (current != GameState.InGame && current != GameState.PostGame)
            return SessionCommandResult.Failed(SessionErrorCode.InvalidState, "Can only return to lobby from gameplay.");

        if (SessionModeManager.Instance == null)
            return SessionCommandResult.Failed(SessionErrorCode.InvalidState, "SessionModeManager missing. Cannot return to lobby.");

        SessionData session = sessionStore.Current;
        session.ResetReadyStates();
        session.ElevatorState = ElevatorLobbyState.Open;

        SessionModeManager.Instance.LoadLobbyScene();
        return SessionCommandResult.Succeeded();
    }
    
    #endregion
}
