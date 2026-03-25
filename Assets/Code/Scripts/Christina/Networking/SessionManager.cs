using UnityEngine;
using PurrNet;
using System.Collections.Generic;

public class SessionManager : NetworkBehaviour, IPlayerEvents
{

    // todo: Blocked from Thanos tasks
    //private SessionData sessionData;
    //private GameStateManager gameStateManager;

    // this Dictionary maps PurrNet's PlayerID to Steam ulong IDs.
    private readonly Dictionary<PlayerID, ulong> playerConnectionMap = new();

    public bool IsHost => NetworkManager.main != null && NetworkManager.main.isHost;

    private PlayerID? hostPlayerID;
    private bool sessionCreated = false;

    public static SessionManager Instance { get; private set; }

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

    public void OnPlayerConnected(PlayerID playerID, bool isReconnect, bool asServer)
    {
        if (!asServer) return;

        Debug.Log($"[SessionManager] Player connected: PlayerID {playerID} (Reconnect: {isReconnect})");

        // if the session was just created and no one is in it yet -> we have our host
        if (sessionCreated && playerConnectionMap.Count == 0)
        {
            hostPlayerID = playerID;
            AddPlayerToSession(playerID, 0, "Host Player", isHost: true);
            Debug.Log("[SessionManager] Host registered as first player.");
        }
    }

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

    private void CreateSession()
    {
        Debug.Log("[SessionManager] Creating new session...");

        // todo Generate SessionId (System.Guid.NewGuid().ToString())
        // todo  Set HostSteamId on SessionData
        // todo  Initialize SessionData with defaults

        // Host registers themselves as the first player
        // Using 0 as placeholder Steam ID until Steam integration is ready
        // todo: connect Steam ID when it's ready
        sessionCreated = true;

        Debug.Log("[SessionManager] Session created, host registered as first player.");
    }

    private void AddPlayerToSession(PlayerID playerID, ulong steamID, string displayName, bool isHost = false)
    {
        // todo: create PlayerSessionInfo and ass to SessionData.Players list

        playerConnectionMap[playerID] = steamID;

        SessionEvents.InvokePlayerJoined(steamID, displayName);
        OnPlayerJoined_Client(steamID, displayName);
    }

    private void RemovePlayerFromSession(PlayerID playerID, ulong steamID, string reason)
    {
        // todo: find and remove player from SessionData.Players list

        playerConnectionMap.Remove(playerID);

        SessionEvents.InvokePlayerLeft(steamID, reason);
        OnPlayerLeft_Client(steamID, reason);

        Debug.Log($"[SessionManager] Player removed: SteamID {steamID} (Reason: {reason})");
    }

    [ServerRpc(requireOwnership: false)]
    public void RequestJoinSession(RPCInfo info = default)
    {
        PlayerID sender = info.sender;
        Debug.Log($"[SessionManager] Join request received from PlayerID: {sender}");

        //todo: check if session is full 
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

        // if validation above passes, then add the player into the session
        string displayName = $"Player_{sender}";
        AddPlayerToSession(sender, 0, displayName);

        Debug.Log($"[SessionManager] Join approved for PlayerID: {sender}");
    }

    [ServerRpc(requireOwnership: false)]
    public void RequestLeaveSession(RPCInfo info = default)
    {
        PlayerID sender = info.sender;
        Debug.Log($"[SessionManager] PlayerID {sender} requested to leave the session.");

        if (!playerConnectionMap.ContainsKey(sender))
        {
            Debug.LogWarning($"[SessionManager] Leave rejected: PlayerID {sender} not found in session.");
            SendErrorToClient(sender, SessionErrorCode.PlayerNotFound, "You are not in this session.");
            return;
        }

        ulong steamId = playerConnectionMap[sender];
        RemovePlayerFromSession(sender, steamId, "Player left voluntarily.");

        Debug.Log($"[SessionManager] Leave approved for PlayerID: {sender}");
    }

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
        //todo: check if GameState is Lobby
        if (GameStateManager.Instance.CurrentState != GameState.Lobby)
        {
            // should reject the request
            Debug.Log($"[SessionManager] Toggle ready request rejected: PlayerID {sender} is in the wrong game state.");
            SendErrorToClient(sender, SessionErrorCode.InvalidState, "Game already in progress.");
            return;
        }


        //todo: toggle PlayerSessionInfo.IsReady

        OnSessionUpdated_Client();
        Debug.Log($"[SessionManager] Ready toggled for PlayerID: {sender}");
    }


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

        // todo: check if all the players are ready
        // todo: check if the GameState is in the right state (Lobby)
        // todo: transition game state to loading

        Debug.Log("[SessionManager] Game starting...");
    }

    [ServerRpc(requireOwnership: false)]
    public void RequestUpdateSettings(string key, string value, RPCInfo info = default)
    {
        PlayerID sender = info.sender;
        Debug.Log($"[SessionManager] Update settings request from PlayerID: {sender}");

        if (sender != PlayerID.Server)
        {
            Debug.LogWarning($"[SessionManager] Update settings rejected: PlayerID {sender} is not the host.");
            SendErrorToClient(sender, SessionErrorCode.NotHost, "Only the host can update the game settings.");
            return;
        }

        //todo: update the SessionData.Custom properties with the correct key/value pairs

        OnSessionUpdated_Client();
        Debug.Log("[SessionManager] Settings updated.");
    }


    [ObserversRpc]
    private void OnPlayerJoined_Client(ulong steamID, string displayName)
    {
        SessionEvents.InvokePlayerJoined(steamID, displayName);
        Debug.Log($"[SessionManager] [Client] Player joined: {displayName} (SteamID: {steamID})");
    }

    [ObserversRpc]
    private void OnPlayerLeft_Client(ulong steamID, string reason)
    {
        SessionEvents.InvokePlayerLeft(steamID, reason);
        Debug.Log($"[SessionManager] [Client] Player left: SteamID {steamID} (Reason: {reason})");
    }

    [ObserversRpc]
    private void OnSessionUpdated_Client()
    {
        SessionEvents.InvokeSessionDataChanged();
        //todo: will take serialized SessionData as parameters when ready

        Debug.Log($"[SessionManager] [Client] Session Data Changed.");
    }

    [TargetRpc]
    private void SendErrorToClient(PlayerID target, SessionErrorCode code, string message)
    {
        var error = new SessionErrorResponse(code, message);
        SessionEvents.InvokeSessionError(error);
        Debug.LogWarning($"[SessionManager] [Client] Error received: {code} - {message}");
    }
}
