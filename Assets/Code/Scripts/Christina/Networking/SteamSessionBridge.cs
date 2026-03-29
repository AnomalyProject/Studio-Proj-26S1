using UnityEngine;
using Steamworks;
using PurrNet.Steam;
using System.Collections;

public class SteamSessionBridge : MonoBehaviour
{
    public static SteamSessionBridge Instance { get; private set; }

    private CSteamID currentLobbyId;
    private bool isInLobby = false;
    private bool isCreatingLobby = false;

    // steam callbacks (always listening)
    private Callback<LobbyEnter_t> lobbyEnteredCallback;
    private Callback<LobbyChatUpdate_t> lobbyChatUpdateCallback;
    private Callback<GameLobbyJoinRequested_t> joinRequestedCallback;

    // steam callresults (one-shot for specific API calls)
    private CallResult<LobbyCreated_t> lobbyCreatedCallResult;

    private void Awake()
    {

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // safety check
        if (!SteamManager.Initialized) return;

        lobbyEnteredCallback = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        lobbyChatUpdateCallback = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
        joinRequestedCallback = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);

        lobbyCreatedCallResult = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);

        SubscribeToSessionEvents();
    }
    
    public void CreateSteamLobby(int maxPlayers)
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogWarning("[SteamBridge] Steam is not initialized, skipping lobby creation.");
            return;
        }

        isCreatingLobby = true;
        Debug.Log($"[SteamBridge] Creating steam lobby for {maxPlayers} players.");

        SteamAPICall_t apiCall = SteamMatchmaking.CreateLobby(
            ELobbyType.k_ELobbyTypeFriendsOnly,
            maxPlayers
        );
        
        lobbyCreatedCallResult.Set(apiCall);
    }
    
    public void UpdateRichPresence(GameState state)
    {
        if (!SteamManager.Initialized) return;
        
        string mapName = "";
        if (SessionManager.Instance != null && SessionManager.Instance.CurrentSession != null)
        {
            mapName = SessionManager.Instance.CurrentSession.MapName;
        }

        string status;
        
        switch (state)
        {
            case GameState.Menu:
                status = "In Menu";
                break;
            case GameState.Lobby:
                status =  string.IsNullOrEmpty(mapName) ? "In Lobby" : $"In Lobby — {mapName}";
                break;
            case GameState.Loading:
                status = "Loading...";
                break;
            case GameState.InGame:
                status = string.IsNullOrEmpty(mapName) ? "Playing" : $"Playing — {mapName}";
                break;
            case GameState.PostGame:
                status = "Post-Game Results";
                break;
            default:
                status = "Unknown";
                break;
        }

        SteamFriends.SetRichPresence("status", status);

        // when connect has a key, Steam shows a "Join Game" button on your profile in the friends list. 
        // when someone clicks it Steam fires GameLobbyJoinRequested_t 
        if (state == GameState.Lobby && isInLobby)
        {
            SteamFriends.SetRichPresence("connect", $"+connect_lobby {currentLobbyId}");
        }
        else
        {
            SteamFriends.SetRichPresence("connect", "");
        }

        Debug.Log($"[SteamBridge] Rich Presence updated: {status}");
    }
    
    public void LeaveSteamLobby()
    {
        isCreatingLobby = false;
        
        if (isInLobby)
        {
            SteamMatchmaking.LeaveLobby(currentLobbyId);
            currentLobbyId = new CSteamID();
            isInLobby = false;
            SteamFriends.ClearRichPresence();
            Debug.Log("[SteamBridge] Left Steam lobby and cleared Rich Presence");
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromSessionEvents();
        LeaveSteamLobby();
    }

    private void OnApplicationQuit()
    {
        LeaveSteamLobby();
    }

    private void OnLobbyCreated(LobbyCreated_t result, bool ioFailure)
    {
        if (!isCreatingLobby)
        {
            Debug.LogWarning("[SteamBridge] Lobby created but player already left, cleaning up..");
            LeaveSteamLobby();
            return;
        }
        isCreatingLobby = false; 
        
        // ioFailure means the request never reached Steam's servers
        if (ioFailure)
        {
            Debug.LogError("[SteamBridge] Lobby creation failed with IO error");
            return;
        }

        // m_eResult means that the request reached Steam servers but something went wrong. 
        if (result.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError($"[SteamBridge] Lobby creation failed: {result.m_eResult}");
            return;
        }
        
        currentLobbyId = new CSteamID(result.m_ulSteamIDLobby);
        isInLobby = true;
        
        Debug.Log($"[SteamBridge] Steam lobby created! With ID: {currentLobbyId}");
        SyncMetadataToSteamLobby();

    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        // without this check, if the lobby was full or the player was banned, the code would still set
        // isInLobby = true and try to connect to the host. 
        if (callback.m_EChatRoomEnterResponse != (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
        {
            Debug.LogError($"[SteamBridge] Failed to enter lobby: {(EChatRoomEnterResponse)callback.m_EChatRoomEnterResponse}");
            return;
        }
        
        currentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        isInLobby = true;

        CSteamID lobbyOwner = SteamMatchmaking.GetLobbyOwner(currentLobbyId);
        bool isHost = lobbyOwner == SteamUser.GetSteamID();

        if (isHost)
        {
            Debug.Log("[SteamBridge] Entered lobby as HOST");
        }
        else
        {
            Debug.Log("[SteamBridge] Entered lobby as CLIENT: connecting to host...");
            ConnectToHost(lobbyOwner);
        }
    }
    
    private void ConnectToHost(CSteamID hostSteamId)
    {
        Debug.Log($"[SteamBridge] Connecting PurrNet to host: {hostSteamId}");

        var networkManager = PurrNet.NetworkManager.main;
        if (networkManager == null)
        {
            Debug.LogError("[SteamBridge] NetworkManager not found!");
            return;
        }

        var steamTransport = networkManager.transport as SteamTransport;
        if (steamTransport == null)
        {
            Debug.LogError("[SteamBridge] SteamTransport not assigned to NetworkManager!");
            return;
        }

        steamTransport.address = hostSteamId.m_SteamID.ToString();
        networkManager.StartClient();
        // we use coroutine because StartClient() is asynchronous.
        // we need to wait for the response to send RPCs
        StartCoroutine(WaitForConnectionThenJoin());
    }
    
    private IEnumerator WaitForConnectionThenJoin()
    {
        var networkManager = PurrNet.NetworkManager.main;

        // wait until PurrNet reports that the client is connected
        while (networkManager != null && !networkManager.isClient)
        {
            yield return null;
        }

        if (networkManager == null || !networkManager.isClient)
        {
            Debug.LogError("[SteamBridge] Failed to connect to host via PurrNet");
            yield break;
        }

        if (SessionManager.Instance == null)
        {
            Debug.LogError("[SteamBridge] SessionManager not available after connecting");
            yield break;
        }

        ulong steamID = SteamUser.GetSteamID().m_SteamID;
        string displayName = SteamFriends.GetPersonaName();
        SessionManager.Instance.RequestJoinSession(steamID, displayName);
        Debug.Log("[SteamBridge] PurrNet connected, session join requested");
    }

    private void OnLobbyChatUpdate(LobbyChatUpdate_t callback)
    {
        CSteamID changedUser = new CSteamID(callback.m_ulSteamIDUserChanged);
        string userName = SteamFriends.GetFriendPersonaName(changedUser);

        var changeType = (EChatMemberStateChange)callback.m_rgfChatMemberStateChange;

        if (changeType.HasFlag(EChatMemberStateChange.k_EChatMemberStateChangeEntered))
        {
            Debug.Log($"[SteamBridge] {userName} joined the Steam lobby");
        }

        if (changeType.HasFlag(EChatMemberStateChange.k_EChatMemberStateChangeLeft) ||
            changeType.HasFlag(EChatMemberStateChange.k_EChatMemberStateChangeDisconnected))
        {
            Debug.Log($"[SteamBridge] {userName} left the Steam lobby");
        }
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        Debug.Log($"[SteamBridge] Join requested for lobby: {callback.m_steamIDLobby}");
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }
    
    // note: only lobby owner can set the metadata
    private void SyncMetadataToSteamLobby()
    {
        if (!isInLobby) return;
        
        // only the lobby owner can set metadata
        if (SteamMatchmaking.GetLobbyOwner(currentLobbyId) != SteamUser.GetSteamID()) return;
        
        if (SessionManager.Instance == null) return;
        var session = SessionManager.Instance.CurrentSession;
        if (session == null) return;

        // everything inside the metadata are strings + keys have 255 character limit. Should keep them short.
        SteamMatchmaking.SetLobbyData(currentLobbyId, "session_id", session.SessionId);
        SteamMatchmaking.SetLobbyData(currentLobbyId, "map_name", session.MapName);
        SteamMatchmaking.SetLobbyData(currentLobbyId, "game_mode", session.GameMode);
        SteamMatchmaking.SetLobbyData(currentLobbyId, "host_name", SteamFriends.GetPersonaName());
        SteamMatchmaking.SetLobbyData(currentLobbyId, "player_count", session.Players.Count.ToString());
        SteamMatchmaking.SetLobbyData(currentLobbyId, "max_players", session.MaxPlayers.ToString());
        SteamMatchmaking.SetLobbyData(currentLobbyId, "game_state", GameStateManager.Instance.CurrentState.ToString());
        SteamMatchmaking.SetLobbyData(currentLobbyId, "game_version", Application.version);
        
        
        // sync custom properties with prefix to avoid key collisions, for example
        // if someone calls session.SetCustomProperty("map_name", "something"), without a prefix it would
        // overwrite the reserved map_name key. Namespacing prevents that kind of collisions.
        foreach (var kvp in session.CustomProperties)
        {
            SteamMatchmaking.SetLobbyData(currentLobbyId, $"custom_{kvp.Key}", kvp.Value);
        }

        Debug.Log("[SteamBridge] Lobby metadata successfully synced to Steam");
    }
    
    private void SubscribeToSessionEvents()
    {
        SessionEvents.OnPlayerJoined += OnPlayerJoinedSession;
        SessionEvents.OnPlayerLeft += OnPlayerLeftSession;
        SessionEvents.OnSessionDataChanged += SyncMetadataToSteamLobby;
        GameStateManager.Instance.OnStateChanged += OnGameStateChanged;
    }
    
    private void UnsubscribeFromSessionEvents()
    {
        SessionEvents.OnPlayerJoined -= OnPlayerJoinedSession;
        SessionEvents.OnPlayerLeft -= OnPlayerLeftSession;
        SessionEvents.OnSessionDataChanged -= SyncMetadataToSteamLobby;

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnStateChanged -= OnGameStateChanged;
        }
    }
    
    private void OnPlayerJoinedSession(ulong steamID, string displayName)
    {
        SyncMetadataToSteamLobby();
    }

    private void OnPlayerLeftSession(ulong steamID, string reason)
    {
        SyncMetadataToSteamLobby();
    }

    private void OnGameStateChanged(GameState previous, GameState next)
    {
        UpdateRichPresence(next);
        SyncMetadataToSteamLobby();

        if (next == GameState.Menu)
        {
            LeaveSteamLobby();
        }
        
    }
}
