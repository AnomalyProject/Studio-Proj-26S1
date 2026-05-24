using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine;
using PurrNet;
using PurrNet.Steam;
using Steamworks;

/// <summary>
/// Host(server)-authoritative session lifecycle manager. Handles player join/leave, ready states,
/// match settings, and game start. All validated server-side before broadcasting to clients.
///
/// Works between PurrNet's networking layer (PlayerID) and the game's identity layer (Steam ID).
/// SessionData owns the data logic and this class owns the authority decisions and network flow.
///
/// Key rules:
/// - All mutations go through ServerRpcs: clients request -> host decides
/// - Host and clients share the same join path (AddPlayerToSession) to prevent divergence bugs. One place the logic -> one place the bugs
/// - playerConnectionMap bridges PurrNet PlayerIDs to Steam IDs. SessionData only knows Steam IDs
/// - GameStateManager remains the single authority on game state (SessionData.CurrentState is not used)
/// </summary>
public class SessionManager : NetworkBehaviour, IPlayerEvents
{

    // the authoritative session container. Null means no session exists.
    // SessionManager decides WHEN to modify it. SessionData handles the HOW.
    private SessionData sessionData;
    // PurrNet ConnectionID for the Host. Used for authority checks in RPCs.
    // This is not like PlayerSessionInfo.IsHost -> that's for game logic. This one is for network authority.
    private PlayerID? hostPlayerID;
    private PlayerID? pendingHostConnection;

    private SessionPlayerRegistry registry = new SessionPlayerRegistry();
    
    private Coroutine hostRegistrationCoroutine;
    private const float hostRegistrationTimeoutSeconds = 5f;

    // this Dictionary maps PurrNet's PlayerID to Steam ulong IDs. This is nessecary because SessionData
    // only know SteamIDs, but RPCs only know PlayerIDs. Every RPC must look up Stem ID here first.
    private readonly Dictionary<PlayerID, ulong> playerConnectionMap = new();

    // Convenience check for whether this machine isn the host.
    public bool IsHost => NetworkManager.main != null && NetworkManager.main.isHost;

    // Read-only access for UI and external systems. 
    public SessionData CurrentSession => sessionData;
    
    private ClientSessionData latestClientSession;
    public ClientSessionData LatestClientSession => latestClientSession;
    
    // server-side event fired when a player is added to the session.
    // carries the PlayerID directly so spawn systems don't need reverse-lookups.
    public static event Action<PlayerID, ulong, string> OnServerPlayerAdded;
    public static event Action<PlayerID, ulong, string> OnServerPlayerRemoved;
    
    public static event Action OnServerSessionChanged;
    
    public ElevatorLobbyState CurrentElevatorState => sessionData != null ? sessionData.ElevatorState : ElevatorLobbyState.Open;


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
        sessionData = null;
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
        if (registry.Count != 0)
            return;

        if (playerID.isServer)
        {
            Debug.LogError($"[SessionManager] Refusing to register host with invalid PlayerID {playerID} from {source}.");
            return;
        }

        hostPlayerID = playerID;
        var hostIdentity = LocalIdentity.ResolveHost();

        AddPlayerToSession(playerID, hostIdentity.steamID, hostIdentity.displayName, isHost: true);
        Debug.Log($"[SessionManager] Host registered as first player in {source}. PlayerID={playerID}");
    }
    
    private void SendSessionUpdate()
    {
        OnSessionUpdated_Client( SessionSnapshotFactory.Build(sessionData));
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
            if (playerConnectionMap.Count != 0)
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
    /// This keeps host and client on the same AddPlayerToSession code path.
    /// </summary>
    public void OnPlayerConnected(PlayerID playerID, bool isReconnect, bool asServer)
    {
        if (!asServer) return;

        Debug.Log($"[SessionManager] Player connected: PlayerID {playerID} (Reconnect: {isReconnect})");

        if (registry.Count != 0) return;
        
        if (sessionData == null)
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
    /// Only runs server-side. Uses the same RemovePlayerFromSession path as voluntary leaves,
    /// ensuring consistent cleanup (SessionData removal, ready state reset, client broadcast)
    /// regardless of whether the player left intentionally or lost connection.
    /// </summary>
    public void OnPlayerDisconnected(PlayerID playerID, bool asServer)
    {
        if (!asServer) return;

        Debug.Log($"[SessionManager] Player Disconnected: PlayerID {playerID}");

        // checking in case a client connected but never succesfully joined the session
        if (!registry.IsRegistered(playerID))
        {
            Debug.LogWarning($"[SessionManager] Disconnected PlayerID {playerID} was not in the session.");
            return;
        }

        registry.TryGetSteamID(playerID, out ulong steamID);
        RemovePlayerFromSession(playerID, steamID, "Disconnected");
    }

    /// <summary>
    /// Initializes a new SessionData with default settings and transitions to Lobby state.
    /// SessionData is created BEFORE the state transition to ensure it exists if anything
    /// during the transition tries to read it. The host is NOT added here. That happens
    /// in OnPlayerConnected via the shared AddPlayerToSession path.
    /// </summary>
    private void CreateSession()
    {
        Debug.Log("[SessionManager] Creating new session...");

        var hostIdentity = LocalIdentity.ResolveHost();
        
        sessionData = new SessionData
        {
            HostSteamID =  hostIdentity.steamID,
            MapName = "Default",
            GameMode = "Default",
            MaxPlayers = 2
        };

        GameStateManager.Instance.OnStateChanged += HandleStateChanged;

        Debug.Log("[SessionManager] Session created.");
    }

    /// <summary>
    /// Shared entry point for adding ANY player (host or client) to the session.
    /// Creates a PlayerSessionInfo, registers it in SessionData, maps the PurrNet PlayerID
    /// to the Steam ID, and broadcasts the join to all clients. Using one path for both
    /// host and clients prevents divergence bugs. Any fix here applies to everyone.
    /// Non-host players also receive the current game state to sync them up on join.
    /// </summary>
    private void AddPlayerToSession(PlayerID playerID, ulong steamID, string displayName, bool isHost = false)
    {

        var playerInfo = new PlayerSessionInfo(steamID, displayName, isHost);
        sessionData.AddPlayer(playerInfo);

        registry.Register(playerID, steamID, isHost);

        OnPlayerJoined_Client(steamID, displayName);
        SendSessionUpdate();
        OnServerPlayerAdded?.Invoke(playerID, steamID, displayName);
        
        
        if (!isHost)
        {
            // snapshot for the joining player. This guarantees they receive the full state even if the 
            // ObserverRpc timing has issues.
            SendSessionSnapshot(playerID,  SessionSnapshotFactory.Build(sessionData));
            
            // adding CurrentState here and not hard coded GameState.Lobby in case we need support for midgame re-connection later
            SendStateChangeToClient(playerID, GameStateManager.Instance.CurrentState);
        }

    }

    /// <summary>
    /// Shared exit point for removing ANY player from the session, whether they left
    /// voluntarily or disconnected. Removes from both SessionData and playerConnectionMap,
    /// broadcasts the leave to all clients, then resets all ready states because the
    /// group composition changed and remaining players should re-confirm readiness.
    /// </summary>
    private void RemovePlayerFromSession(PlayerID playerID, ulong steamID, string reason)
    {
        sessionData.RemovePlayer(steamID);

        registry.Unregister(playerID);
        
        sessionData.ResetReadyStates();

        OnPlayerLeft_Client(steamID, reason);
        SendSessionUpdate();

        OnServerPlayerRemoved?.Invoke(playerID, steamID, reason);
        Debug.Log($"[SessionManager] Player removed: SteamID {steamID} (Reason: {reason})");
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
        if (sessionData == null) return false;
        if (GameStateManager.Instance.CurrentState != GameState.Lobby) return false;
        if (sessionData.ElevatorState != ElevatorLobbyState.Open) return false;

        return sessionData.AllPlayersReadyInElevator;
    }
    
    public void SetElevatorState(ElevatorLobbyState state)
    {
        if (!isServer || sessionData == null) return;

        sessionData.ElevatorState = state;
        SendSessionUpdate();
    }
    
    public void SetPlayerInElevator(PlayerID playerID, bool isInside)
    {
        if (!isServer || sessionData == null) return;
        if (!registry.IsRegistered(playerID)) return;
        if (sessionData.ElevatorState == ElevatorLobbyState.DoorsClosed) return;

        registry.TryGetSteamID(playerID, out ulong steamID);
        int playerIndex = sessionData.Players.FindIndex(player => player.SteamID == steamID);

        if (playerIndex == -1) return;

        var playerInfo = sessionData.Players[playerIndex];
        
        playerInfo.IsInElevator = isInside;
        playerInfo.IsReady = isInside;

        sessionData.Players[playerIndex] = playerInfo;
        SendSessionUpdate();
    }

    /// <summary>
    /// Client-to-server RPC: requests to join the session. Validates in order:
    /// 1. Session not full (SessionData.IsSessionFull)
    /// 2. Game is still in Lobby state
    /// 3. Player isn't already in session (playerConnectionMap check)
    /// Order matters -> cheapest checks first to reject early. Uses a temporary Steam ID
    /// derived from PlayerID hash until Steamworks integration is ready.
    /// </summary>
    [ServerRpc(requireOwnership: false)]
    public void RequestJoinSession(RPCInfo info = default)
    {
        PlayerID sender = info.sender;

        if (!PurrSteamUtils.TryGetSteamID(sender, out ulong steamID))
        {
            SendErrorToClient(sender, SessionErrorCode.InvalidState, "Could not verify Steam identity.");
            return;
        }

        if (!SteamSessionBridge.Instance.IsLobbyMember(steamID))
        {
            SendErrorToClient(sender, SessionErrorCode.InvalidState, "You are not in this Steam lobby.");
            return;
        }

        string displayName = SteamFriends.GetFriendPersonaName(new CSteamID(steamID));
        TryAcceptJoin(sender, steamID, SanitizeDisplayName(displayName));
    }

    [ServerRpc(requireOwnership: false)]
    public void RequestJoinDevSession(ulong fakeSteamID, string displayName, RPCInfo info = default)
    {
        PlayerID sender = info.sender;

        if (SessionModeManager.Instance == null ||
            SessionModeManager.Instance.CurrentMode != SessionMode.DevHost)
        {
            SendErrorToClient(sender, SessionErrorCode.InvalidState, "Dev joins are only allowed in Dev Host mode.");
            return;
        }

        if (fakeSteamID == 0)
        {
            SendErrorToClient(sender, SessionErrorCode.InvalidState, "Dev SteamID was invalid.");
            return;
        }

        TryAcceptJoin(sender, fakeSteamID, SanitizeDisplayName(displayName));
    }
    
    private void TryAcceptJoin(PlayerID sender, ulong steamID, string displayName)
    {
        if (sessionData == null)
            return;

        if (sessionData.IsSessionFull)
        {
            SendErrorToClient(sender, SessionErrorCode.SessionFull, "Session is full.");
            return;
        }

        if (GameStateManager.Instance.CurrentState != GameState.Lobby)
        {
            bool devInGameJoin =
                SessionModeManager.Instance != null &&
                SessionModeManager.Instance.CurrentMode == SessionMode.DevHost &&
                GameStateManager.Instance.CurrentState == GameState.InGame;

            if (!devInGameJoin)
            {
                SendErrorToClient(sender, SessionErrorCode.InvalidState, "Cannot join, game already in progress.");
                return;
            }
        }

        if (registry.IsRegistered(sender))
        {
            SendErrorToClient(sender, SessionErrorCode.AlreadyInSession, "You are already in session.");
            return;
        }

        if (registry.ContainsSteamID(steamID) || sessionData.GetPlayer(steamID).HasValue)
        {
            SendErrorToClient(sender, SessionErrorCode.AlreadyInSession, "This Steam account is already in session.");
            return;
        }

        if (sessionData.ElevatorState != ElevatorLobbyState.Open)
        {
            SendErrorToClient(sender, SessionErrorCode.InvalidState, "The elevator is already leaving.");
            return;
        }

        AddPlayerToSession(sender, steamID, displayName);
    }


    /// <summary>
    /// Client-to-server RPC: requests to voluntarily leave the session.
    /// Validates the player is actually in the session, then delegates to
    /// RemovePlayerFromSession. Same path used by disconnection cleanup.
    /// </summary>
    [ServerRpc(requireOwnership: false)]
    public void RequestLeaveSession(RPCInfo info = default)
    {
        PlayerID sender = info.sender;
        Debug.Log($"[SessionManager] PlayerID {sender} requested to leave the session.");

        if (!registry.IsRegistered(sender))
        {
            Debug.LogWarning($"[SessionManager] Request leave rejected: PlayerID {sender} not found in session.");
            SendErrorToClient(sender, SessionErrorCode.PlayerNotFound, "You are not in this session.");
            return;
        }

        registry.TryGetSteamID(sender, out ulong steamID);
        RemovePlayerFromSession(sender, steamID, "Player left voluntarily.");

        Debug.Log($"[SessionManager] Leave approved for PlayerID: {sender}");
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

        if (!registry.IsRegistered(sender))
        {
            Debug.Log($"[SessionManager] Rejected: PlayerID {sender} not found in session.");
            SendErrorToClient(sender, SessionErrorCode.PlayerNotFound, "You are not in this session.");
            return;
        }

        if (GameStateManager.Instance.CurrentState != GameState.Lobby)
        {
            // should reject the request
            Debug.Log($"[SessionManager] Toggle ready request rejected: PlayerID {sender} is in the wrong game state.");
            SendErrorToClient(sender, SessionErrorCode.InvalidState, "Game already in progress.");
            return;
        }


        registry.TryGetSteamID(sender, out ulong steamID);
        int playerIndex = sessionData.Players.FindIndex(player => player.SteamID == steamID);

        if (playerIndex == -1)
        {
            SendErrorToClient(sender, SessionErrorCode.PlayerNotFound, "Player data not found in session.");
            return;
        }

        var playerInfo = sessionData.Players[playerIndex];
        
        // elevator check
        if (sessionData.ElevatorState != ElevatorLobbyState.Open)
        {
            SendErrorToClient(sender, SessionErrorCode.InvalidState, "The elevator is already leaving.");
            return;
        }

        if (!playerInfo.IsInElevator)
        {
            SendErrorToClient(sender, SessionErrorCode.InvalidState, "Enter the elevator before readying up.");
            return;
        }
        
        if (playerInfo.IsReady)
        {
            Debug.Log($"[SessionManager] Ready request ignored: PlayerID {sender} is already ready.");
            return;
        }
        
        playerInfo.IsReady = true;
        sessionData.Players[playerIndex] = playerInfo;

        SendSessionUpdate();
        Debug.Log($"[SessionManager] Ready set for PlayerIDD: {sender}");
    }
    
    public bool TryStartMatchFromServer()
    {
        if (!isServer || sessionData == null) return false;
        if (GameStateManager.Instance.CurrentState != GameState.Lobby) return false;
        if (sessionData.ElevatorState != ElevatorLobbyState.DoorsClosed) return false;
        if (!sessionData.AllPlayersReadyInElevator) return false;

        GameStateManager.Instance.RequestStateChange(GameState.Loading);

        if (SessionModeManager.Instance == null)
        {
            Debug.LogError("[SessionManager] SessionModeManager missing. Cannot load gameplay scene.");
            return false;
        }

        SessionModeManager.Instance.LoadGameplayScene();
        Debug.Log("[SessionManager] Elevator locked. Game starting...");
        return true;
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

        if (!registry.IsHost(sender))
        {
            SendErrorToClient(sender, SessionErrorCode.NotHost, "Only the host can return to lobby.");
            return;
        }

        if (GameStateManager.Instance.CurrentState != GameState.InGame &&
            GameStateManager.Instance.CurrentState != GameState.PostGame)
        {
            SendErrorToClient(sender, SessionErrorCode.InvalidState, "Can only return to lobby from gameplay.");
            return;
        }

        // Reset ready/elevator session data here before loading lobby.
        sessionData.ResetReadyStates();
        sessionData.ElevatorState = ElevatorLobbyState.Open;
        SendSessionUpdate();

        SessionModeManager.Instance.LoadLobbyScene();
    }


    /// <summary>
    /// Client-to-server RPC: host requests to start the match. Three validations:
    /// 1. Sender is the host (authority check via hostPlayerID)
    /// 2. Game is in Lobby state (can't  twice)
    /// 3. All players are ready (SessionData.AllPlayersReady)
    /// Only after all three pass does the state transition to Loading.
    /// </summary>
    [ServerRpc(requireOwnership: false)]
    public void RequestStartMatch(RPCInfo info = default)
    {
        PlayerID sender = info.sender;
        Debug.Log($"[SessionManager] Start match request from PlayerID: {sender}");

        // ONLY the host can start the game session
        if (!registry.IsHost(sender))
        {
            Debug.LogWarning($"[SessionManager] Start rejected: PlayerID {sender} is not the host.");
            SendErrorToClient(sender, SessionErrorCode.NotHost, "Only the host can start the game.");
            return;
        }

        if (GameStateManager.Instance.CurrentState != GameState.Lobby)
        {
            Debug.LogWarning($"[SessionManager] Start rejected: PlayerID {sender} is in the wrong game state.");
            SendErrorToClient(sender, SessionErrorCode.InvalidState, "Game already in progress.");
            return;
        }

        if (!sessionData.AllPlayersReady)
        {
            Debug.LogWarning($"[SessionManager] Start rejected: Not all players are ready.");
            SendErrorToClient(sender, SessionErrorCode.PlayersNotReady, "Not all players are ready.");
            return;
        }

        TryStartMatchFromServer();
        
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

        if (!registry.IsHost(sender))
        {
            Debug.LogWarning($"[SessionManager] Update settings rejected: PlayerID {sender} is not the host.");
            SendErrorToClient(sender, SessionErrorCode.NotHost, "Only the host can update the game settings.");
            return;
        }

        bool shouldResetReadyStates = false;
        
        switch (key)
        {
            case "MapName":
                sessionData.MapName = value;
                shouldResetReadyStates = true;
                break;
            case "GameMode":
                sessionData.GameMode = value;
                shouldResetReadyStates = true;
                break;
            case "MaxPlayers":
                if (!int.TryParse(value, out int maxPlayers))
                {
                    SendErrorToClient(sender, SessionErrorCode.Unknown, "Max players value was invalid.");
                    return;
                }
                if (maxPlayers < 2 || maxPlayers > 4)
                {
                    SendErrorToClient(sender, SessionErrorCode.Unknown, "Max players must be between 2 and 4.");
                    return;
                }

                if (maxPlayers < sessionData.Players.Count)
                {
                    SendErrorToClient(sender, SessionErrorCode.Unknown, "Max players cannot be lower than the current player count.");
                    return;
                }
                sessionData.MaxPlayers = maxPlayers;
                shouldResetReadyStates = true;
                break;
            case "LobbyVisibility":
                if (value != "Friends Only" && value != "Public")
                {
                    SendErrorToClient(sender, SessionErrorCode.Unknown, "Lobby visibility value was invalid.");
                    return;
                }

                sessionData.SetCustomProperty(key, value);
                break;
            default:
                sessionData.SetCustomProperty(key, value);
                break;
        }

        if(shouldResetReadyStates) sessionData.ResetReadyStates();

        SendSessionUpdate();
        Debug.Log("[SessionManager] Settings updated.");
    }
    
    /// <summary>
    /// Client-to-server RPC: this exists in case a client ever gets into a state where their local data feels wrong,
    /// they can tell via RequestSessionSnapshot() to get a fresh copy.
    /// </summary>
    /// <param name="info"></param>
    [ServerRpc(requireOwnership: false)]
    public void RequestSessionSnapshot(RPCInfo info = default)
    {
        PlayerID sender = info.sender;

        if (!registry.IsRegistered(sender))
        {
            SendErrorToClient(sender, SessionErrorCode.PlayerNotFound, "You are not in this session.");
            return;
        }

        SendSessionSnapshot(sender, SessionSnapshotFactory.Build(sessionData));
        Debug.Log($"[SessionManager] Session snapshot sent to PlayerID: {sender}");
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
        // host startup. Late-joining clients receive the current state vie SendStateChangeToClient in AddPlayerToSession.
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
    
    [TargetRpc]
    private void SendSessionSnapshot(PlayerID target, ClientSessionData clientData)
    {
        latestClientSession = clientData;
        SessionEvents.InvokeSessionDataChanged();
        Debug.Log("[SessionManager] [Client] Received initial session snapshot.");
    }
    
    /// <summary>
    /// HELPERS
    /// </summary>
    /// <param name="displayName"></param>
    /// <returns></returns>
    private static string SanitizeDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return "Player";

        displayName = displayName.Trim();

        if (displayName.Length > 32)
            displayName = displayName.Substring(0, 32);

        return displayName;
    }
}
