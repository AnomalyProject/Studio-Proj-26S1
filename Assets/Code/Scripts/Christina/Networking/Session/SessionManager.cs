using System.Collections;
using System;
using UnityEngine;
using PurrNet;

/// <summary>
/// SessionData owns raw session data, SessionPlayerRegistry owns PlayerID-to-SteamID mapping,
/// SessionPlayerCoordinator owns player membership mutations, and this class owns RPC flow,
/// authority entry points, broadcasts, and client synchronization.
///
/// Key rules:
/// - All mutations go through ServerRpcs: clients request -> host decides
/// - Host and clients share the same join path (AddPlayerToSession) to prevent divergence bugs. One place the logic -> one place the bugs
/// - playerConnectionMap bridges PurrNet PlayerIDs to Steam IDs. SessionData only knows Steam IDs
/// - GameStateManager remains the single authority on game state (SessionData.CurrentState is not used)
/// </summary>
public class SessionManager : NetworkBehaviour, IPlayerEvents
{
    private SessionPlayerRegistry registry = new SessionPlayerRegistry();
    private SessionStateStore sessionStore = new SessionStateStore();
    private SessionIdentityService identityService = new SessionIdentityService();
    private SessionPlayerCoordinator playerCoordinator;
    private SessionLobbyCoordinator lobbyCoordinator;
    
    private Coroutine hostRegistrationCoroutine;
    private const float hostRegistrationTimeoutSeconds = 5f;

    // Convenience check for whether this machine isn the host.
    public bool IsHost => NetworkManager.main != null && NetworkManager.main.isHost;

    // Read-only access for UI and external systems. 
    public SessionData CurrentSession => sessionStore.Current;
    
    private ClientSessionData latestClientSession;
    public ClientSessionData LatestClientSession => latestClientSession;
    
    // server-side event fired when a player is added to the session.
    // carries the PlayerID directly so spawn systems don't need reverse-lookups.
    public static event Action<PlayerID, ulong, string> OnServerPlayerAdded;
    public static event Action<PlayerID, ulong, string> OnServerPlayerRemoved;
    
    public static event Action OnServerSessionChanged;
    
    public ElevatorLobbyState CurrentElevatorState => sessionStore.HasSession ? sessionStore.Current.ElevatorState : ElevatorLobbyState.Open;


    // Singleton instance. Accessible globally so RPCs can be called from UI.
    public static SessionManager Instance { get; private set; }
    

    /// <summary>
    /// Singleton setup. If a duplicate SessionManager exists, destroy it.
    /// </summary>
    private void Awake()
    {
        // a simple check to see if SessionManager exist or is it "me" and if yes, destroy it
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        playerCoordinator = new SessionPlayerCoordinator(sessionStore, registry, identityService);
        lobbyCoordinator = new SessionLobbyCoordinator(sessionStore, registry);
    }

    /// <summary>
    /// Called by PurrNet when this NetworkBehaviour is spawned on the network.
    /// The server creates the session immediately and then clients just log their connection.
    /// DontDestroyOnLoad ensures the session survives scene transitions (e.g., lobby to gameplay).
    /// </summary>
    protected override void OnSpawned(bool asServer)
    {
        if (asServer)
        {
            Debug.Log("[SessionManager] Server started, I am the host.");
            CreateSession();
            
            if (registry.PendingHostConnection.HasValue && registry.Count == 0)
            {
                RegisterHostPlayer(registry.PendingHostConnection.Value, "pending OnPlayerConnected");
                registry.PendingHostConnection = null;
            }
            else if (registry.Count == 0)
            {
                hostRegistrationCoroutine = StartCoroutine(WaitForLocalHostThenRegister());
            }
        }
        else
        {
            Debug.Log("[SessionManager] Client connected to host.");
        }
    }

    /// <summary>
    /// Cleanup when the NetworkBehaviour is despawned. Nulls sessionData so that
    /// sessionData != null checks correctly report "no session exists" if respawned.
    /// Unsubscribes from GameStateManager to prevent stale event handlers.
    /// </summary>
    protected override void OnDespawned()
    {
        sessionStore.Clear();
        hostRegistrationCoroutine = null;
        latestClientSession = default;
        registry.Clear();

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged -= HandleStateChanged;
        }
    }
    
    private void RegisterHostPlayer(PlayerID playerID, string source)
    {
        if (registry.Count != 0) return;
        
        if (playerID.isServer)
        {
            Debug.LogError($"[SessionManager] Refusing to register host with invalid PlayerID {playerID} from {source}.");
            return;
        }

        LocalHostIdentity host = playerCoordinator.RegisterHost(playerID);
        BroadcastPlayerJoined(playerID, host.SteamID, host.DisplayName, isHost: true);
        Debug.Log($"[SessionManager] Host registered as first player in {source}. PlayerID={playerID}");
    }
    
    private void SendSessionUpdate()
    {
        OnSessionUpdated_Client( SessionSnapshotFactory.Build(CurrentSession));
        OnServerSessionChanged?.Invoke();
    }

    
    /// <summary>
    /// Waits briefly for the local networking player to become available, then
    /// registers it as the host if no host-player mappins has already been created.
    /// </summary>
    /// <returns></returns>
    private IEnumerator WaitForLocalHostThenRegister()
    {
        float deadline = Time.realtimeSinceStartup + hostRegistrationTimeoutSeconds;

        while (Time.realtimeSinceStartup < deadline)
        {
            if (registry.Count != 0)
            {
                hostRegistrationCoroutine = null;
                yield break;
            }

            if (NetworkManager.main != null && NetworkManager.main.isLocalPlayerReady)
            {
                PlayerID localId = NetworkManager.main.localPlayer;

                if (!localId.isServer)
                {
                    RegisterHostPlayer(localId, "WaitForLocalHostThenRegister");
                    hostRegistrationCoroutine = null;
                    yield break;
                }
            }

            yield return null;
        }

        Debug.LogError("[SessionManager] Timed out waiting for a valid local host PlayerID.");
        hostRegistrationCoroutine = null;
    }

    /// <summary>
    /// PurrNet IPlayerEvents callback. Fires automatically when any player connects.
    /// Only runs server-side (asServer check). Only auto-adds the FIRST player (the host)
    /// when the session exists but is empty. All other players must explicitly call RequestJoinSession.
    /// Host registration is delegated to SessionPlayerCoordinator, then SessionManager broadcasts the result.
    /// </summary>
    public void OnPlayerConnected(PlayerID playerID, bool isReconnect, bool asServer)
    {
        if (!asServer) return;

        Debug.Log($"[SessionManager] Player connected: PlayerID {playerID} (Reconnect: {isReconnect})");

        if (registry.Count != 0) return;
        
        if (!sessionStore.HasSession)
        {
            registry.PendingHostConnection = playerID;
            Debug.Log("[SessionManager] Storing pending host connection until session exists.");
            return;
        }

        RegisterHostPlayer(playerID, "OnPlayerConnected");
        
        if (hostRegistrationCoroutine != null)
        {
            StopCoroutine(hostRegistrationCoroutine);
            hostRegistrationCoroutine = null;
        }
        
    }

    /// <summary>
    /// PurrNet IPlayerEvents callback. Fires automatically when any player disconnects.
    /// Only runs server-side. SessionPlayerCoordinator removes the player from session data,
    /// then SessionManager broadcasts the leave event and refreshed session state.
    /// </summary>
    public void OnPlayerDisconnected(PlayerID playerID, bool asServer)
    {
        if (!asServer) return;
        Debug.Log($"[SessionManager] Player Disconnected: PlayerID {playerID}");

        if (!playerCoordinator.TryHandleDisconnect(playerID, out ulong steamID))
        {
            Debug.LogWarning($"[SessionManager] Disconnected PlayerID {playerID} was not in the session.");
            return;
        }

        BroadcastPlayerLeft(playerID, steamID, "Disconnected");
    }

    /// <summary>
    /// Initializes a new SessionData with default settings and transitions to Lobby state.
    /// SessionData is created BEFORE the state transition to ensure it exists if anything
    /// during the transition tries to read it. The host is NOT added here. That happens
    /// in OnPlayerConnected via SessionPlayerCoordinator.RegisterHost.
    /// </summary>
    private void CreateSession()
    {
        Debug.Log("[SessionManager] Creating new session...");
        
        LocalHostIdentity hostIdentity = identityService.ResolveLocalHost();

        sessionStore.CreateSession(hostIdentity.SteamID);

        GameStateManager.Instance.OnStateChanged += HandleStateChanged;

        Debug.Log("[SessionManager] Session created.");
    }
    
    /// <summary>
    /// Given a Steam ID, returns the PurrNet PlayerID.
    /// </summary>
    public PlayerID? GetPlayerIDForSteam(ulong steamID)
    {
        return registry.FindPlayerIDForSteam(steamID);
    }
    
    /// <summary>
    /// ELEVATOR
    /// </summary>
    /// <returns></returns>
    public bool CanStartElevatorSequence()
    {
        return lobbyCoordinator.CanStartElevatorSequence();
    }
    
    public void SetElevatorState(ElevatorLobbyState state)
    {
        if (!isServer || !sessionStore.HasSession) return;

        SessionCommandResult result = lobbyCoordinator.TrySetElevatorState(state);
        if (!result.Success) return;

        SendSessionUpdate();
    }
    
    public void SetPlayerInElevator(PlayerID playerID, bool isInside)
    {
        if (!isServer) return;

        SessionCommandResult result = lobbyCoordinator.TrySetPlayerInElevator(playerID, isInside);
        if (!result.Success) return;

        SendSessionUpdate();
    }

    /// <summary>
    /// Client-to-server RPC: requests to join the active session.
    /// The identity service resolves and validates the sender's Steam identity,
    /// including Steam lobby membership and display-name sanitization.
    /// SessionPlayerCoordinator then validates session rules: session exists, not full,
    /// join is allowed in the current game state, player is not already registered,
    /// and the elevator has not started leaving.
    /// </summary>
    [ServerRpc(requireOwnership: false)]
    public void RequestJoinSession(RPCInfo info = default)
    {
        PlayerID sender = info.sender;

        if (!identityService.TryResolveJoiner(sender, out ulong steamID, out string displayName))
        {
            SendCommandErrorIfFailed(
                sender,
                SessionCommandResult.Failed(
                    SessionErrorCode.InvalidState,
                    "Could not verify Steam lobby identity."
                )
            );
            return;
        }
        
        SessionCommandResult result = TryAcceptJoin(sender, steamID, displayName);
        
        if (SendCommandErrorIfFailed(sender, result)) return;
    }

    /// <summary>
    /// Client-to-server RPC: requests to join using a supplied dev identity.
    /// Only allowed while SessionModeManager is in DevHost mode.
    /// The fake SteamID is validated locally, the display name is sanitized by
    /// the identity service, then SessionPlayerCoordinator applies the normal join rules.
    /// </summary>
    [ServerRpc(requireOwnership: false)]
    public void RequestJoinDevSession(ulong fakeSteamID, string displayName, RPCInfo info = default)
    {
        PlayerID sender = info.sender;

        if (SessionModeManager.Instance == null ||
            SessionModeManager.Instance.CurrentMode != SessionMode.DevHost)
        {
            SendCommandErrorIfFailed(sender, SessionCommandResult.Failed(SessionErrorCode.InvalidState,"Dev joins are only allowed in Dev Host mode."));
            return;
        }

        if (fakeSteamID == 0)
        {
            SendCommandErrorIfFailed(sender, SessionCommandResult.Failed(SessionErrorCode.InvalidState,"Dev SteamID was invalid."));
            return;
        }

        SessionCommandResult result = TryAcceptJoin(sender, fakeSteamID, identityService.Sanitize(displayName));

        if (SendCommandErrorIfFailed(sender, result)) return;
    }
    
    private SessionCommandResult TryAcceptJoin(PlayerID sender, ulong steamID, string displayName)
    {
        SessionCommandResult result = playerCoordinator.TryAcceptJoin(sender, steamID, displayName);
        if (!result.Success) return result;

        // success path, so do the broadcasts
        BroadcastPlayerJoined(sender, steamID, displayName, isHost: false);
        return result;
    }
    
    private void BroadcastPlayerJoined(PlayerID playerID, ulong steamID, string displayName, bool isHost)
    {
        OnPlayerJoined_Client(steamID, displayName);
        SendSessionUpdate();
        OnServerPlayerAdded?.Invoke(playerID, steamID, displayName);

        if (!isHost)
        {
            SendSessionSnapshot(playerID, SessionSnapshotFactory.Build(sessionStore.Current));
            SendStateChangeToClient(playerID, GameStateManager.Instance.CurrentState);
        }
    }
    
    private void BroadcastPlayerLeft(PlayerID playerID, ulong steamID, string reason)
    {
        OnPlayerLeft_Client(steamID, reason);
        SendSessionUpdate();
        OnServerPlayerRemoved?.Invoke(playerID, steamID, reason);
    }

    /// <summary>
    /// Client-to-server RPC: requests to voluntarily leave the session.
    /// SessionPlayerCoordinator validates and removes the player from session data,
    /// then SessionManager broadcasts the leave event and refreshed session state.
    /// </summary>
    [ServerRpc(requireOwnership: false)]
    public void RequestLeaveSession(RPCInfo info = default)
    {
        PlayerID sender = info.sender;
        Debug.Log($"[SessionManager] PlayerID {sender} requested to leave the session.");

        SessionCommandResult result = TryLeaveSession(sender);

        if (SendCommandErrorIfFailed(sender, result)) return;

        Debug.Log($"[SessionManager] Leave approved for PlayerID: {sender}");
    }
    
    private SessionCommandResult TryLeaveSession(PlayerID sender)
    {
        if (!registry.TryGetSteamID(sender, out ulong steamID))
        {
            Debug.LogWarning($"[SessionManager] Request leave rejected: PlayerID {sender} not found in session.");
            return SessionCommandResult.Failed(SessionErrorCode.PlayerNotFound,"You are not in this session.");
        }

        SessionCommandResult result = playerCoordinator.TryLeaveSession(sender);
        if (!result.Success) return result;

        BroadcastPlayerLeft(sender, steamID, "Player left voluntarily.");
        return result;
    }

    /// <summary>
    /// Client-to-server RPC: toggles the sender's ready state. Validates the player
    /// is in session and the game is in Lobby state. Uses FindIndex + copy-modify-replace
    /// because PlayerSessionInfo is a struct. Pulling it from the list gives a copy,
    /// so modifications must be written back to the list at the same index.
    /// Broadcasts a session update to all clients after toggling.
    /// </summary>
    [ServerRpc(requireOwnership: false)]
    public void RequestToggleReady(RPCInfo info = default)
    {
        PlayerID sender = info.sender;
        Debug.Log($"[SessionManager] Toggle ready request from PlayerID: {sender}");

        SessionCommandResult result = TrySetPlayerReady(sender);

        if (SendCommandErrorIfFailed(sender, result)) return;
    }
    
    private SessionCommandResult TrySetPlayerReady(PlayerID sender)
    {
        SessionCommandResult result = lobbyCoordinator.TrySetPlayerReady(sender);
        if (!result.Success) return result;

        SendSessionUpdate();
        Debug.Log($"[SessionManager] Ready set for PlayerID: {sender}");

        return result;
    }

    /// <summary>
    /// Client-to-server request for returning from gameplay to the lobby.
    /// Validates that the requester is allowed to trigger the transition, resets lobby data
    /// such as player ready states and elevator state, then asks SessionModeManager to load the lobby
    /// scene for the whole session.
    /// </summary>
    [ServerRpc(requireOwnership: false)]
    public void RequestReturnToLobby(RPCInfo info = default)
    {
        PlayerID sender = info.sender;

        SessionCommandResult result = TryReturnToLobby(sender);

        if (SendCommandErrorIfFailed(sender, result)) return;
    }
    
    private SessionCommandResult TryReturnToLobby(PlayerID sender)
    {
        if (!registry.IsHost(sender))
            return SessionCommandResult.Failed(SessionErrorCode.NotHost, "Only the host can return to lobby.");

        if (GameStateManager.Instance.CurrentState != GameState.InGame &&
            GameStateManager.Instance.CurrentState != GameState.PostGame)
            return SessionCommandResult.Failed(SessionErrorCode.InvalidState, "Can only return to lobby from gameplay.");

        if (!sessionStore.HasSession)
            return SessionCommandResult.Failed(SessionErrorCode.InvalidState, "No active session.");

        CurrentSession.ResetReadyStates();
        CurrentSession.ElevatorState = ElevatorLobbyState.Open;
        SendSessionUpdate();

        if (SessionModeManager.Instance == null)
            return SessionCommandResult.Failed(SessionErrorCode.InvalidState,"SessionModeManager missing. Cannot return to lobby.");
        SessionModeManager.Instance.LoadLobbyScene();

        return SessionCommandResult.Succeeded();
    }


    /// <summary>
    /// Client-to-server RPC: host requests to start the match. Three validations:
    /// 1. Sender is the host (authority check via SessionPlayerRegistry)
    /// 2. Game is in Lobby state (can't  twice)
    /// 3. All players are ready (SessionData.AllPlayersReady)
    /// Only after all three pass does the state transition to Loading.
    /// </summary>
    [ServerRpc(requireOwnership: false)]
    public void RequestStartMatch(RPCInfo info = default)
    {
        PlayerID sender = info.sender;
        Debug.Log($"[SessionManager] Start match request from PlayerID: {sender}");

        SessionCommandResult result = TryStartMatchCommand(sender);

        if (SendCommandErrorIfFailed(sender, result)) return;
    }
    
    private SessionCommandResult TryStartMatchCommand(PlayerID sender)
    {
        if (!registry.IsHost(sender))
            return SessionCommandResult.Failed(SessionErrorCode.NotHost, "Only the host can start the game.");

        if (GameStateManager.Instance.CurrentState != GameState.Lobby)
            return SessionCommandResult.Failed(SessionErrorCode.InvalidState, "Game already in progress.");

        if (!sessionStore.HasSession)
            return SessionCommandResult.Failed(SessionErrorCode.InvalidState, "No active session.");

        if (!CurrentSession.AllPlayersReady)
            return SessionCommandResult.Failed(SessionErrorCode.PlayersNotReady, "Not all players are ready.");

        return TryStartMatchInternal();
    }
    
    public bool TryStartMatchFromServer()
    {
        SessionCommandResult result = TryStartMatchInternal();

        if (!result.Success) Debug.LogWarning($"[SessionManager] Start match failed: {result.ErrorCode} - {result.Message}");

        return result.Success;
    }
    
    private SessionCommandResult TryStartMatchInternal()
    {
        if (!isServer || !sessionStore.HasSession)
            return SessionCommandResult.Failed(SessionErrorCode.InvalidState, "No active session.");

        if (GameStateManager.Instance.CurrentState != GameState.Lobby)
            return SessionCommandResult.Failed(SessionErrorCode.InvalidState, "Game already in progress.");

        if (CurrentSession.ElevatorState != ElevatorLobbyState.DoorsClosed)
            return SessionCommandResult.Failed(SessionErrorCode.InvalidState, "The elevator is not ready.");

        if (!CurrentSession.AllPlayersReadyInElevator)
            return SessionCommandResult.Failed(SessionErrorCode.PlayersNotReady, "Not all players are ready.");

        if (SessionModeManager.Instance == null)
            return SessionCommandResult.Failed(SessionErrorCode.InvalidState, "SessionModeManager missing. Cannot load gameplay scene.");

        GameStateManager.Instance.RequestStateChange(GameState.Loading);
        SessionModeManager.Instance.LoadGameplayScene();

        Debug.Log("[SessionManager] Elevator locked. Game starting...");
        return SessionCommandResult.Succeeded();
    }

    /// <summary>
    /// Client-to-server RPC: host requests to change session settings. Host-only.
    /// First-class fields (MapName, GameMode, MaxPlayers) are set directly on SessionData.
    /// Everything else goes through SetCustomProperty for game-specific settings.
    /// Resets all ready states after any change. Players agreed to the previous settings,
    /// so they must re-confirm after a change. Broadcasts update to all clients.
    /// </summary>
    [ServerRpc(requireOwnership: false)]
    public void RequestUpdateSettings(string key, string value, RPCInfo info = default)
    {
        PlayerID sender = info.sender;
        Debug.Log($"[SessionManager] Update settings request from PlayerID: {sender}");

        SessionCommandResult result = TryUpdateSettings(sender, key, value);

        if (SendCommandErrorIfFailed(sender, result)) return;
    }
    
    private SessionCommandResult TryUpdateSettings(PlayerID sender, string key, string value)
    {
        if (!registry.IsHost(sender)) return SessionCommandResult.Failed(SessionErrorCode.NotHost, "Only the host can update the game settings.");

        if (!sessionStore.HasSession) return SessionCommandResult.Failed(SessionErrorCode.InvalidState, "No active session.");

        bool shouldResetReadyStates = false;

        switch (key)
        {
            case "MapName":
                CurrentSession.MapName = value;
                shouldResetReadyStates = true;
                break;

            case "GameMode":
                CurrentSession.GameMode = value;
                shouldResetReadyStates = true;
                break;

            case "MaxPlayers":
                if (!int.TryParse(value, out int maxPlayers))
                    return SessionCommandResult.Failed(SessionErrorCode.Unknown, "Max players value was invalid.");

                if (maxPlayers < 2 || maxPlayers > 4)
                    return SessionCommandResult.Failed(SessionErrorCode.Unknown, "Max players must be between 2 and 4.");

                if (maxPlayers < CurrentSession.Players.Count)
                    return SessionCommandResult.Failed(SessionErrorCode.Unknown, "Max players cannot be lower than the current player count.");

                CurrentSession.MaxPlayers = maxPlayers;
                shouldResetReadyStates = true;
                break;

            case "LobbyVisibility":
                if (value != "Friends Only" && value != "Public")
                    return SessionCommandResult.Failed(SessionErrorCode.Unknown, "Lobby visibility value was invalid.");

                CurrentSession.SetCustomProperty(key, value);
                break;

            default:
                CurrentSession.SetCustomProperty(key, value);
                break;
        }

        if (shouldResetReadyStates)
            CurrentSession.ResetReadyStates();

        SendSessionUpdate();
        Debug.Log("[SessionManager] Settings updated.");

        return SessionCommandResult.Succeeded();
    }

    /// <summary>
    /// Server-to-all-clients broadcast: notifies every client that a player joined.
    /// This is the ONLY path that fires the PlayerJoined event. The server doesnt
    /// invoke SessionEvents directly, avoiding double-invocation on the host
    /// (since the host is also a client and receives ObserversRpcs).
    /// </summary>
    [ObserversRpc]
    private void OnPlayerJoined_Client(ulong steamID, string displayName)
    {
        SessionEvents.InvokePlayerJoined(steamID, displayName);
        Debug.Log($"[SessionManager] [Client] Player joined: {displayName} (SteamID: {steamID})");
    }

    /// <summary>
    /// Server-to-all-clients broadcast: notifies every client that a player left.
    /// Same single-path pattern as OnPlayerJoined_Client. Events fire only through
    /// this RPC to prevent the host from receiving them twice.
    /// </summary>
    [ObserversRpc]
    private void OnPlayerLeft_Client(ulong steamID, string reason)
    {
        SessionEvents.InvokePlayerLeft(steamID, reason);
        Debug.Log($"[SessionManager] [Client] Player left: SteamID {steamID} (Reason: {reason})");
    }

    /// <summary>
    /// Server-to-all-clients broadcast: notifies clients that session data changed
    /// (ready state toggled, settings updated). Clients
    /// only know something changed, not what. Will carry serialized SessionData once
    /// serialization issues (DateTime, Dictionary) with PurrNet are resolved. Needs research.
    /// </summary>
    [ObserversRpc]
    private void OnSessionUpdated_Client(ClientSessionData clientData)
    {
        latestClientSession = clientData;
        SessionEvents.InvokeSessionDataChanged();
        Debug.Log($"[SessionManager] [Client] Session Data Changed.");
    }

    /// <summary>
    /// Callback for GameStateManager.OnStateChanged. When the game state changes on the server,
    /// this forwards the new state to every connected player individually via TargetRpc.
    /// Note: the first transition (Menu -> Lobby) fires before any players are in the map,
    /// so it effectively does nothing. This is a known edge case for future review.
    /// </summary>
    private void HandleStateChanged(GameState currentState, GameState nextState)
    {
        // note: the menu->lobby transition fires here before any clients are connected, so the loop body excecutes zero times.
        // This is expected. The host doesn't need to send itseld a state change and no clients exist yet during initial
        // host startup. Late-joining clients receive the current state via SendStateChangeToClient after a successful join.
        foreach (PlayerID id in registry.AllPlayerIDs())
        {
            SendStateChangeToClient(id, nextState);
        }
    }


    /// <summary>
    /// Server-to-one-client RPC: tells a specific client to transition to a new game state.
    /// Guards against redundant transitions. If the client is already in the target state,
    /// it does nothing. Used both for state sync during gameplay and for late-joining clients
    /// who need to catch up to the current state on join.
    /// </summary>
    [TargetRpc]
    private void SendStateChangeToClient(PlayerID target, GameState stateToTransition)
    {
        if (GameStateManager.Instance.CurrentState == stateToTransition) return;
        GameStateManager.Instance.RequestStateChange(stateToTransition);
    }

    /// <summary>
    /// Server-to-one-client RPC: sends a structured error to a specific client.
    /// Wraps the error code and message in a SessionErrorResponse, then fires it
    /// through SessionEvents so UI can display it without knowing about networking.
    /// TargetRpc ensures only the player who caused the error receives it.
    /// </summary>
    [TargetRpc]
    private void SendErrorToClient(PlayerID target, SessionErrorCode code, string message)
    {
        var error = new SessionErrorResponse(code, message);
        SessionEvents.InvokeSessionError(error);
        Debug.LogWarning($"[SessionManager] [Client] Error received: {code} - {message}");
    }
    
    private bool SendCommandErrorIfFailed(PlayerID target, SessionCommandResult result)
    {
        if (result.Success) return false;

        SendErrorToClient(target, result.ErrorCode, result.Message);
        return true;
    }
    
    [TargetRpc]
    private void SendSessionSnapshot(PlayerID target, ClientSessionData clientData)
    {
        latestClientSession = clientData;
        SessionEvents.InvokeSessionDataChanged();
        Debug.Log("[SessionManager] [Client] Received initial session snapshot.");
    }
    
}
