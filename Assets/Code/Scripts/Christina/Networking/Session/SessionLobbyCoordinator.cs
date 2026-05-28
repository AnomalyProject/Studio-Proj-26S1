using PurrNet;

public class SessionLobbyCoordinator
{
    private readonly SessionStateStore sessionStore;
    private readonly SessionPlayerRegistry registry;
    
    public SessionLobbyCoordinator(SessionStateStore sessionStore, SessionPlayerRegistry registry)
    {
        this.sessionStore = sessionStore;
        this.registry = registry;
    }
    
    public bool CanStartElevatorSequence()
    {
        if (!sessionStore.HasSession) return false;
        if (GameStateManager.Instance.CurrentState != GameState.Lobby) return false;
        if (sessionStore.Current.ElevatorState != ElevatorLobbyState.Open) return false;

        return sessionStore.Current.AllPlayersReadyInElevator;
    }
    
    public SessionCommandResult TrySetElevatorState(ElevatorLobbyState state)
    {
        if (!sessionStore.HasSession)
            return SessionCommandResult.Failed(SessionErrorCode.InvalidState, "No active session.");

        sessionStore.Current.ElevatorState = state;
        return SessionCommandResult.Succeeded();
    }
    
    public SessionCommandResult TrySetPlayerInElevator(PlayerID playerID, bool isInside)
    {
        if (!sessionStore.HasSession)
            return SessionCommandResult.Failed(SessionErrorCode.InvalidState, "No active session.");

        if (!registry.TryGetSteamID(playerID, out ulong steamID))
            return SessionCommandResult.Failed(SessionErrorCode.PlayerNotFound, "Player is not in this session.");

        if (sessionStore.Current.ElevatorState == ElevatorLobbyState.DoorsClosed)
            return SessionCommandResult.Failed(SessionErrorCode.InvalidState, "The elevator doors are already closed.");

        int playerIndex = sessionStore.Current.Players.FindIndex(player => player.SteamID == steamID);

        if (playerIndex == -1)
            return SessionCommandResult.Failed(SessionErrorCode.PlayerNotFound, "Player data not found in session.");

        PlayerSessionInfo playerInfo = sessionStore.Current.Players[playerIndex];
        playerInfo.IsInElevator = isInside;
        playerInfo.IsReady = isInside;
        sessionStore.Current.Players[playerIndex] = playerInfo;

        return SessionCommandResult.Succeeded();
    }
    
    public SessionCommandResult TrySetPlayerReady(PlayerID sender)
    {
        if (!sessionStore.HasSession) return SessionCommandResult.Failed(SessionErrorCode.InvalidState, "No active session.");
        
        if (!registry.TryGetSteamID(sender, out ulong steamID))
            return SessionCommandResult.Failed(SessionErrorCode.PlayerNotFound, "You are not in this session.");

        if (GameStateManager.Instance.CurrentState != GameState.Lobby)
            return SessionCommandResult.Failed(SessionErrorCode.InvalidState, "Game already in progress.");

        int playerIndex = sessionStore.Current.Players.FindIndex(player => player.SteamID == steamID);

        if (playerIndex == -1)
            return SessionCommandResult.Failed(SessionErrorCode.PlayerNotFound, "Player data not found in session.");

        PlayerSessionInfo playerInfo = sessionStore.Current.Players[playerIndex];

        if (sessionStore.Current.ElevatorState != ElevatorLobbyState.Open)
            return SessionCommandResult.Failed(SessionErrorCode.InvalidState, "The elevator is already leaving.");

        if (!playerInfo.IsInElevator)
            return SessionCommandResult.Failed(SessionErrorCode.InvalidState, "Enter the elevator before readying up.");

        if (playerInfo.IsReady)
            return SessionCommandResult.Succeeded();

        playerInfo.IsReady = true;
        sessionStore.Current.Players[playerIndex] = playerInfo;

        return SessionCommandResult.Succeeded();
    }
}
