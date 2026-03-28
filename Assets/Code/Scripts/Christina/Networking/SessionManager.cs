using UnityEngine;
using PurrNet;
using System.Collections.Generic;

public class SessionManager : NetworkBehaviour, IPlayerEvents
{

    private SessionData sessionData;
    private PlayerID? hostPlayerID;

    // this Dictionary maps PurrNet's PlayerID to Steam ulong IDs.
    private readonly Dictionary<PlayerID, ulong> playerConnectionMap = new();

    public bool IsHost => NetworkManager.main != null && NetworkManager.main.isHost;

    // for UI to be able to read the sessionData
    public SessionData CurrentSession => sessionData;

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

    protected override void OnDespawned()
    {
        sessionData = null;

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged -= HandleStateChanged;
        }

    }

    public void OnPlayerConnected(PlayerID playerID, bool isReconnect, bool asServer)
    {
        if (!asServer) return;

        Debug.Log($"[SessionManager] Player connected: PlayerID {playerID} (Reconnect: {isReconnect})");

        // check: Does an actual state exists? And is it empty / no one in it yet? Then we have our host 
        if (sessionData != null && playerConnectionMap.Count == 0)
        {
            hostPlayerID = playerID;
            // todo: replace with real steamID
            ulong tempSteamID = (ulong)playerID.GetHashCode();
            AddPlayerToSession(playerID, tempSteamID, "Host Player", isHost: true);
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


        sessionData = new SessionData
        {
            HostSteamID = 0, // todo: this is a placeholder. to be replaced once Steamworks is wired in
            MapName = "Default",
            GameMode = "Default",
            MaxPlayers = 4
        };

        GameStateManager.Instance.OnStateChanged += HandleStateChanged;
        GameStateManager.Instance.RequestStateChange(GameState.Lobby);

        Debug.Log("[SessionManager] Session created, host registered as first player.");
    }

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

    private void RemovePlayerFromSession(PlayerID playerID, ulong steamID, string reason)
    {
        sessionData.RemovePlayer(steamID);

        playerConnectionMap.Remove(playerID);

        OnPlayerLeft_Client(steamID, reason);

        sessionData.ResetReadyStates();
        Debug.Log($"[SessionManager] Player removed: SteamID {steamID} (Reason: {reason})");
    }

    [ServerRpc(requireOwnership: false)]
    public void RequestJoinSession(RPCInfo info = default)
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

        // todo: replace with real steamID
        ulong tempSteamId = (ulong)sender.GetHashCode();
        // if validation above passes, then add the player into the session
        string displayName = $"Player_{sender}";
        AddPlayerToSession(sender, tempSteamId, displayName);

        Debug.Log($"[SessionManager] Join approved for PlayerID: {sender}");
    }

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
        //todo: pass sessionData to clients. watch for serialization issues with DateTime and Dictionary fields and PurrNet.
        Debug.Log($"[SessionManager] [Client] Session Data Changed.");
    }

    private void HandleStateChanged(GameState currentState, GameState nextState)
    {
        // todo: the first transition here does nothing, which is fine for now but should take a closer look later 
        // on how to fix this
        foreach (KeyValuePair<PlayerID, ulong> playerID in playerConnectionMap)
        {
            SendStateChangeToClient(playerID.Key, nextState);
        }
    }

    [TargetRpc]
    private void SendStateChangeToClient(PlayerID target, GameState stateToTransition)
    {
        if (GameStateManager.Instance.CurrentState == stateToTransition) return;

        GameStateManager.Instance.RequestStateChange(stateToTransition);
    }

    [TargetRpc]
    private void SendErrorToClient(PlayerID target, SessionErrorCode code, string message)
    {
        var error = new SessionErrorResponse(code, message);
        SessionEvents.InvokeSessionError(error);
        Debug.LogWarning($"[SessionManager] [Client] Error received: {code} - {message}");
    }
}
