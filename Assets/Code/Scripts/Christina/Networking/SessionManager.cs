using UnityEngine;
using PurrNet;
using System.Collections.Generic;
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

    // this Dictionary maps PurrNet's PlayerID to Steam ulong IDs. This is nessecary because SessionData
    // only know SteamIDs, but RPCs only know PlayerIDs. Every RPC must look up Stem ID here first.
    private readonly Dictionary<PlayerID, ulong> playerConnectionMap = new();

    // Convenience check for whether this machine isn the host.
    public bool IsHost => NetworkManager.main != null && NetworkManager.main.isHost;

    // Read-only access for UI and external systems. 
    public SessionData CurrentSession => sessionData;

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
    }

    /// <summary>
    /// Called by PurrNet when this NetworkBehaviour is spawned on the network.
    /// The server creates the session immediately and then clients just log their connection.
    /// DontDestroyOnLoad ensures the session survives scene transitions (e.g., lobby to gameplay).
    /// </summary>
    protected override void OnSpawned(bool asServer)
    {
        DontDestroyOnLoad(gameObject);

        if (asServer)
        {
            Debug.Log("[SessionManager] Server started, I am the host.");
            CreateSession();
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

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged -= HandleStateChanged;
        }
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

        // check: Does an actual state exists? And is it empty / no one in it yet? Then we have our host 
        if (sessionData != null && playerConnectionMap.Count == 0)
        {
            hostPlayerID = playerID;
            // todo: replace with real steamID
            ulong hostSteamID = SteamUser.GetSteamID().m_SteamID;
            string hostName = SteamFriends.GetPersonaName();
            AddPlayerToSession(playerID, hostSteamID, hostName, isHost: true);
            Debug.Log("[SessionManager] Host registered as first player.");
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
        if (!playerConnectionMap.ContainsKey(playerID))
        {
            Debug.LogWarning($"[SessionManager] Disconnected PlayerID {playerID} was not in the session.");
            return;
        }

        ulong steamID = playerConnectionMap[playerID];
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


        sessionData = new SessionData
        {
            HostSteamID =  SteamUser.GetSteamID().m_SteamID,
            MapName = "Default",
            GameMode = "Default",
            MaxPlayers = 4
        };

        GameStateManager.Instance.OnStateChanged += HandleStateChanged;
        GameStateManager.Instance.RequestStateChange(GameState.Lobby);

        Debug.Log("[SessionManager] Session created, host registered as first player.");
        
        // the null check ensures that CreateSession will run even if steam is not available
        // or the build doesn't run on steam
        if (SteamSessionBridge.Instance != null)
        {
            SteamSessionBridge.Instance.CreateSteamLobby(sessionData.MaxPlayers);
        }
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

        playerConnectionMap[playerID] = steamID;

        OnPlayerJoined_Client(steamID, displayName);

        if (!isHost)
        {
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

        playerConnectionMap.Remove(playerID);

        OnPlayerLeft_Client(steamID, reason);

        sessionData.ResetReadyStates();
        Debug.Log($"[SessionManager] Player removed: SteamID {steamID} (Reason: {reason})");
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
    public void RequestJoinSession(ulong steamID, string displayName, RPCInfo info = default)
    {
        PlayerID sender = info.sender;

        if (sessionData.IsSessionFull)
        {
            Debug.Log($"[SessionManager] Join rejected: Session is full!");
            SendErrorToClient(sender, SessionErrorCode.SessionFull, "Session is full..");
            return;
        }

        Debug.Log($"[SessionManager] Join request received from PlayerID: {sender}");

        if (GameStateManager.Instance.CurrentState != GameState.Lobby)
        {
            // should reject the request
            Debug.Log($"[SessionManager] Join rejected: PlayerID {sender} is in the wrong game state.");
            SendErrorToClient(sender, SessionErrorCode.InvalidState, "Cannot join, game already in progress.");
            return;
        }

        // check if player is already in session
        if (playerConnectionMap.ContainsKey(sender))
        {
            Debug.Log($"[SessionManager] Join rejected: PlayerID {sender} is already in session.");
            SendErrorToClient(sender, SessionErrorCode.AlreadyInSession, "You are already in session.");
            return;
        }
        
        AddPlayerToSession(sender, steamID, displayName);

        Debug.Log($"[SessionManager] Join approved for PlayerID: {sender}");
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

        if (!playerConnectionMap.ContainsKey(sender))
        {
            Debug.LogWarning($"[SessionManager] Request leave rejected: PlayerID {sender} not found in session.");
            SendErrorToClient(sender, SessionErrorCode.PlayerNotFound, "You are not in this session.");
            return;
        }

        ulong steamId = playerConnectionMap[sender];
        RemovePlayerFromSession(sender, steamId, "Player left voluntarily.");

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

        if (!playerConnectionMap.ContainsKey(sender))
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


        ulong steamId = playerConnectionMap[sender];
        int playerIndex = sessionData.Players.FindIndex(player => player.SteamID == steamId);

        if (playerIndex == -1)
        {
            SendErrorToClient(sender, SessionErrorCode.PlayerNotFound, "Player data not found in session.");
            return;
        }

        var playerInfo = sessionData.Players[playerIndex];
        playerInfo.IsReady = !playerInfo.IsReady;
        sessionData.Players[playerIndex] = playerInfo;

        OnSessionUpdated_Client();
        Debug.Log($"[SessionManager] Ready toggled for PlayerID: {sender}");
    }

    /// <summary>
    /// Client-to-server RPC: host requests to start the match. Three validations:
    /// 1. Sender is the host (authority check via hostPlayerID)
    /// 2. Game is in Lobby state (can't start twice)
    /// 3. All players are ready (SessionData.AllPlayersReady)
    /// Only after all three pass does the state transition to Loading.
    /// </summary>
    [ServerRpc(requireOwnership: false)]
    public void RequestStartMatch(RPCInfo info = default)
    {
        PlayerID sender = info.sender;
        Debug.Log($"[SessionManager] Start match request from PlayerID: {sender}");

        // ONLY the host can start the game session
        if (!hostPlayerID.HasValue || sender != hostPlayerID.Value)
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

        GameStateManager.Instance.RequestStateChange(GameState.Loading);


        Debug.Log("[SessionManager] Game starting...");
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

        if (!hostPlayerID.HasValue || sender != hostPlayerID.Value)
        {
            Debug.LogWarning($"[SessionManager] Update settings rejected: PlayerID {sender} is not the host.");
            SendErrorToClient(sender, SessionErrorCode.NotHost, "Only the host can update the game settings.");
            return;
        }

        switch (key)
        {
            case "MapName":
                sessionData.MapName = value;
                break;
            case "GameMode":
                sessionData.GameMode = value;
                break;
            case "MaxPlayers":
                if (int.TryParse(value, out int maxPlayers))
                    sessionData.MaxPlayers = maxPlayers;
                break;
            default:
                sessionData.SetCustomProperty(key, value);
                break;
        }

        sessionData.ResetReadyStates();

        OnSessionUpdated_Client();
        Debug.Log("[SessionManager] Settings updated.");
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
    /// (ready state toggled, settings updated). Currently sends no payload. Clients
    /// only know something changed, not what. Will carry serialized SessionData once
    /// serialization issues (DateTime, Dictionary) with PurrNet are resolved. Needs research.
    /// </summary>
    [ObserversRpc]
    private void OnSessionUpdated_Client()
    {
        SessionEvents.InvokeSessionDataChanged();
        //todo: pass sessionData to clients. watch for serialization issues with DateTime and Dictionary fields and PurrNet.
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
        // todo: the first transition here does nothing, which is fine for now but should take a closer look later 
        // on how to fix this
        foreach (KeyValuePair<PlayerID, ulong> playerID in playerConnectionMap)
        {
            SendStateChangeToClient(playerID.Key, nextState);
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
}
