using UnityEngine;
using Steamworks;
using PurrNet.Steam;
using System.Collections;
using PurrNet;

public class SteamSessionBridge : MonoBehaviour
{
    public static SteamSessionBridge Instance { get; private set; }

    private CSteamID currentLobbyId;
    private bool isInLobby = false;
    private bool isCreatingLobby = false;
    private bool isSteamAvailable = false;
    
    // Host
    private HostStartupStatus currentHostStartupStatus;
    private int hostStartupAttemptID = 0;
    private Coroutine hostStartupCoroutine;
    private const float clientConnectionTimeoutSeconds = 15f;
    
    public HostStartupStatus CurrentHostStartupStatus => currentHostStartupStatus;
    public event System.Action<HostStartupStatus> OnHostStartupStatusChanged;
    
    // Join
    private JoinStartupStatus currentJoinStartupStatus;
    private int joinStartupAttemptID = 0;
    private bool joinStartupInProgress = false;
    private CSteamID pendingJoinLobbyId;

    public JoinStartupStatus CurrentJoinStartupStatus => currentJoinStartupStatus;
    public event System.Action<JoinStartupStatus> OnJoinStartupStatusChanged;
    
    
    // tracks if LobbyCreated_t succeded for this boot attempt
    private bool hostLobbyCreated = false;

    // lets Steam callbacks know which host-start attempt they belong to
    private int activeHostStartupAttemptID = 0;
    
    private const float hostReadyTimeoutSeconds = 10f;
    private const float sessionReadyTimeoutSeconds = 5f;
    private const float steamPublishTimeoutSeconds = 10f;

    // steam callbacks (always listening)
    private Callback<LobbyEnter_t> lobbyEnteredCallback;
    private Callback<LobbyChatUpdate_t> lobbyChatUpdateCallback;
    private Callback<GameLobbyJoinRequested_t> joinRequestedCallback;
    
    private CSteamID pendingHostLobbyId;

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
        isSteamAvailable = true;  

        lobbyEnteredCallback = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        lobbyChatUpdateCallback = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
        joinRequestedCallback = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);

        lobbyCreatedCallResult = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);

        SubscribeToSessionEvents();
        
        CheckForLaunchJoin();
    }
    
    private void CheckForLaunchJoin()
    {
        string[] args = System.Environment.GetCommandLineArgs();

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "+connect_lobby")
            {
                if (ulong.TryParse(args[i + 1], out ulong lobbyId))
                {
                    Debug.Log($"[SteamBridge] Launched with +connect_lobby {lobbyId}");

                    joinStartupAttemptID++;
                    joinStartupInProgress = true;
                    pendingJoinLobbyId = new CSteamID(lobbyId);

                    SetJoinStage(
                        JoinStartupStage.JoinRequestReceived,
                        $"Join requested via launch argument for lobby {lobbyId}");

                    SessionModeManager.Instance.StartJoining();
                    SteamMatchmaking.JoinLobby(new CSteamID(lobbyId));

                    SetJoinStage(
                        JoinStartupStage.LobbyJoinRequested,
                        $"Requested Steam lobby join for {lobbyId}.");
                }
                else
                {
                    Debug.LogWarning($"[SteamBridge] Invalid lobby ID in launch args: {args[i + 1]}");
                }
                return;
            }
        }
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
    
    public bool UpdateRichPresence(GameState state)
    {
        if (!SteamManager.Initialized) return false;
        
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

        bool statusSet = SteamFriends.SetRichPresence("status", status);
        bool connectSet;

        // when connect has a key, Steam shows a "Join Game" button on your profile in the friends list. 
        // when someone clicks it Steam fires GameLobbyJoinRequested_t 
        if (state == GameState.Lobby && isInLobby)
        {
            connectSet = SteamFriends.SetRichPresence("connect", $"+connect_lobby {currentLobbyId}");
        }
        else
        {
            connectSet = SteamFriends.SetRichPresence("connect", "");
        }

        bool success = statusSet && connectSet;

        if (success)
        {
            Debug.Log($"[SteamBridge] Rich Presence updated: {status}");
        }
        else
        {
            Debug.LogWarning("[SteamBridge] Failed to fully update Rich Presence.");
        }

        return success;
    }
    
    public void LeaveSteamLobby()
    {
        isCreatingLobby = false;
        
        if (isInLobby)
        {
            // Steam API calls are guarded separately because during shutdown SteamManager may
            // have already called SteamAPI.Shutdown() before our OnDestroy runs.
            if (isSteamAvailable)
            {
                SteamMatchmaking.LeaveLobby(currentLobbyId);
                SteamFriends.ClearRichPresence();
            }
            
            currentLobbyId = new CSteamID();
            isInLobby = false;
            
            Debug.Log("[SteamBridge] Left Steam lobby and cleared Rich Presence");
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromSessionEvents();
        LeaveSteamLobby();
        isSteamAvailable = false; 
    }

    private void OnApplicationQuit()
    {
        LeaveSteamLobby();
    }
    
    public void BeginSteamListenHost()
    {
        if (hostStartupCoroutine != null)
        {
            Debug.LogWarning("[SteamBridge:SteamHost]  Host startup is already running.");
            return;
        }
        hostStartupAttemptID++;
        activeHostStartupAttemptID = hostStartupAttemptID;
        hostLobbyCreated = false;
        pendingHostLobbyId = new CSteamID();
        
        if (!SteamManager.Initialized)
        {
            SetBootStage(HostStartupStage.Failed,
                "SteamManager is not initialized. Cannot start Steam listen-host.", 
                HostStartupStage.HostStartRequest);
            return;
        }
        
        hostStartupCoroutine = StartCoroutine(BeginSteamListenHostRoutine());
    }
    
    // the role of this function is to turn this proccess into an observable one. a state machine.
    private void SetBootStage(HostStartupStage stage, string message, HostStartupStage? failureStage = null)
    {
        currentHostStartupStatus.Stage =  stage;
        currentHostStartupStatus.Message = message;
        currentHostStartupStatus.AttemptID = hostStartupAttemptID;
        currentHostStartupStatus.FailureStage = failureStage ?? HostStartupStage.Idle;
        
        NetworkManager networkManager = NetworkManager.main;
        currentHostStartupStatus.ActiveTransport = 
            networkManager != null && 
            networkManager.transport != null ? networkManager.transport.GetType().Name : "None";

        Debug.Log(
            $"[SteamBridge:SteamHost] Attempt={currentHostStartupStatus.AttemptID} " +
            $"Stage={currentHostStartupStatus.Stage} " +
            $"FailureStage={currentHostStartupStatus.FailureStage} " +
            $"Transport={currentHostStartupStatus.ActiveTransport} " +
            $"Message={currentHostStartupStatus.Message}");
        
        // event to be able to turn the diagnostics into usable data from other classes
        // instead of only logs in the console.
        OnHostStartupStatusChanged?.Invoke(currentHostStartupStatus);
    }
    
    /// <summary>
    /// Using a coroutine because we need to wait accross frames for:
    /// StartHost() to fninsh connecting
    /// SessionManager to exist and create session data
    /// </summary>
    private IEnumerator BeginSteamListenHostRoutine()
    {
        SetBootStage(HostStartupStage.HostStartRequest, "[SteamSessionBridge] Host startup requested.");
        
        NetworkManager networkManager = NetworkManager.main;
        if (networkManager == null)
        {
            SetBootStage(HostStartupStage.Failed, "NetworkManager.main was not found.", HostStartupStage.NetworkManagerFound);
            hostStartupCoroutine = null;
            yield break;
        }
        
        SetBootStage(HostStartupStage.NetworkManagerFound, "Network Manager found!");

        var activeTransport = networkManager.transport;
        var steamTransport = activeTransport as SteamTransport;
        
        if (steamTransport == null)
        {
            string transportName = activeTransport != null ? activeTransport.GetType().Name : "None";
            
            SetBootStage(HostStartupStage.Failed, $"Active transport was {currentHostStartupStatus.ActiveTransport}, not SteamTransport.", HostStartupStage.TransportValidated);
            hostStartupCoroutine = null;
            yield break;
        }
        
        SetBootStage(HostStartupStage.TransportValidated,  $"Active transport validated: {steamTransport.GetType().Name}");
        
        networkManager.StartHost();
        SetBootStage(HostStartupStage.HostStarting, "StartHost() called. Waiting for listen-host readiness.");
        
        float hostDeadline = Time.realtimeSinceStartup + hostReadyTimeoutSeconds;
        while ((!networkManager.isHost && Time.realtimeSinceStartup < hostDeadline))
        {
            yield return null;
        }

        if (!networkManager.isHost)
        {
            SetBootStage(
                HostStartupStage.Failed,
                "Timed out waiting for NetworkManager.isHost to become true.",
                HostStartupStage.HostStarting);

            hostStartupCoroutine = null;
            yield break;
        }
        
        float sessionDeadline = Time.realtimeSinceStartup + sessionReadyTimeoutSeconds;
        
        // checking both here beacause transport readiness and gameplay session readiness are not the same thing.
        // We want network ready AND authoritative session ready
        while ((SessionManager.Instance == null || SessionManager.Instance.CurrentSession == null) &&
               Time.realtimeSinceStartup < sessionDeadline)
        {
            yield return null;
        }
        
        if (SessionManager.Instance == null || SessionManager.Instance.CurrentSession == null)
        {
            SetBootStage(
                HostStartupStage.Failed,
                "Timed out waiting for SessionManager.CurrentSession to be created.",
                HostStartupStage.HostReady);

            hostStartupCoroutine = null;
            yield break;
        }

        SetBootStage(HostStartupStage.HostReady, "Listen-host is ready and session data exists.");
        
        CreateSteamLobby(SessionManager.Instance.CurrentSession.MaxPlayers);
        SetBootStage(HostStartupStage.LobbyCreateRequested, "Steam lobby creation requested.");

        // adding timer because otherwise the coroutine can wait forever
        float steamDeadline = Time.realtimeSinceStartup + steamPublishTimeoutSeconds;
        // waiting for the Steam side to finish next steps
        while (currentHostStartupStatus.Stage != HostStartupStage.HostPublished &&
               currentHostStartupStatus.Stage != HostStartupStage.Failed &&
               Time.realtimeSinceStartup < steamDeadline)
        {
            yield return null;
        }

        if (currentHostStartupStatus.Stage != HostStartupStage.HostPublished &&
            currentHostStartupStatus.Stage != HostStartupStage.Failed)
        {
            SetBootStage(
                HostStartupStage.Failed,
                "Timed out waiting for Steam lobby creation/publish callbacks.",
                HostStartupStage.LobbyCreateRequested);

            RollbackFailedHostStartup();
        }

        hostStartupCoroutine = null;
    }
    
    private void OnLobbyCreated(LobbyCreated_t result, bool ioFailure)
    {
        if (!isCreatingLobby)
        {
            Debug.LogWarning("[SteamBridge] Lobby created callback arrived, but no host lobby creation is in progress.");
            return;
        }
        
        isCreatingLobby = false; 
        
        // ioFailure means the request never reached Steam's servers
        if (ioFailure)
        {
            SetBootStage(
                HostStartupStage.Failed,
                "Steam lobby creation failed with IO failure.",
                HostStartupStage.LobbyCreateRequested);
            RollbackFailedHostStartup();
            return;
        }

        // m_eResult means that the request reached Steam servers but something went wrong. 
        if (result.m_eResult != EResult.k_EResultOK)
        {
            SetBootStage(
                HostStartupStage.Failed,
                $"Steam lobby creation failed: {result.m_eResult}",
                HostStartupStage.LobbyCreateRequested);
            RollbackFailedHostStartup();
            return;
        }
        
        currentLobbyId = new CSteamID(result.m_ulSteamIDLobby);
        pendingHostLobbyId = currentLobbyId;
        hostLobbyCreated = true;
        isInLobby = true;
        
        SetBootStage(
            HostStartupStage.LobbyCreated,
            $"Steam lobby created successfully. Lobby ID: {currentLobbyId}");

    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        // without this check, if the lobby was full or the player was banned, the code would still set
        // isInLobby = true and try to connect to the host. 
        if (callback.m_EChatRoomEnterResponse != (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
        {
            if (joinStartupInProgress)
            {
                SetJoinStage(
                    JoinStartupStage.Failed,
                    $"Failed to enter Steam lobby: {(EChatRoomEnterResponse)callback.m_EChatRoomEnterResponse}",
                    ConnectionFailureSource.Steam);

                joinStartupInProgress = false;
            }
            bool hostIsStartingUp = hostStartupCoroutine != null &&
                                    activeHostStartupAttemptID == currentHostStartupStatus.AttemptID;

            if (hostIsStartingUp)
            {
                SetBootStage(
                    HostStartupStage.Failed,
                    $"Failed to enter Steam lobby: {(EChatRoomEnterResponse)callback.m_EChatRoomEnterResponse}",
                    HostStartupStage.LobbyEnteredAsHost);
                RollbackFailedHostStartup();
            }
            return;
        }
        
        CSteamID enteredLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        currentLobbyId = enteredLobbyId;
        isInLobby = true;

        CSteamID lobbyOwner = SteamMatchmaking.GetLobbyOwner(currentLobbyId);
        bool isHost = lobbyOwner == SteamUser.GetSteamID();

        bool hostStartupInProgress = hostStartupCoroutine != null &&
                                     activeHostStartupAttemptID == currentHostStartupStatus.AttemptID;
        
        if (hostStartupInProgress && hostLobbyCreated && enteredLobbyId != pendingHostLobbyId)
        {
            Debug.LogWarning(
                $"[SteamBridge:SteamHost] Ignoring lobby enter for unexpected lobby {enteredLobbyId}. " +
                $"Expected {pendingHostLobbyId} for attempt {activeHostStartupAttemptID}.");
            return;
        }

        if (isHost)
        {

            SetBootStage(
                HostStartupStage.LobbyEnteredAsHost,
                $"Entered Steam lobby as host. Lobby ID: {currentLobbyId}");

            bool metadataPublished = SyncMetadataToSteamLobby();
            bool richPresencePublished = UpdateRichPresence(GameState.Lobby);

            if (!metadataPublished || !richPresencePublished)
            {
                SetBootStage(
                    HostStartupStage.Failed,
                    "Steam lobby was entered, but metadata or rich presence publishing failed.",
                    HostStartupStage.HostPublished);

                RollbackFailedHostStartup();
                return;
            }

            SetBootStage(
                HostStartupStage.HostPublished,
                "Steam host lobby published successfully.");
        }
        else
        {
            Debug.Log("[SteamBridge] Entered lobby as CLIENT: connecting to host...");
            
            if (joinStartupInProgress)
            {
                pendingJoinLobbyId = enteredLobbyId;

                SetJoinStage(
                    JoinStartupStage.LobbyEntered,
                    $"Entered Steam lobby {enteredLobbyId} as client.");
            }
            
            ConnectToHost(lobbyOwner);
        }
    }
    
    private void RollbackFailedHostStartup()
    {
        if (NetworkManager.main != null)
        {
            if (NetworkManager.main.isClient)
                NetworkManager.main.StopClient();

            if (NetworkManager.main.isServer)
                NetworkManager.main.StopServer();
        }

        LeaveSteamLobby();

        hostLobbyCreated = false;
    }
    
    private void ConnectToHost(CSteamID hostSteamId)
    {
        Debug.Log($"[SteamBridge] Connecting PurrNet to host: {hostSteamId}");

        var networkManager = PurrNet.NetworkManager.main;
        if (networkManager == null)
        {
            Debug.LogError("[SteamBridge] NetworkManager not found!");
            if (joinStartupInProgress)
            {
                SetJoinStage(
                    JoinStartupStage.Failed,
                    "NetworkManager not found while starting client connection.",
                    ConnectionFailureSource.Transport);

                joinStartupInProgress = false;
            }
            return;
        }

        var steamTransport = networkManager.transport as SteamTransport;
        if (steamTransport == null)
        {
            Debug.LogError("[SteamBridge] SteamTransport not assigned to NetworkManager!");
            if (joinStartupInProgress)
            {
                SetJoinStage(
                    JoinStartupStage.Failed,
                    "SteamTransport was not assigned to the NetworkManager.",
                    ConnectionFailureSource.Transport);

                joinStartupInProgress = false;
            }
            
            return;
        }

        steamTransport.address = hostSteamId.m_SteamID.ToString();
        
        if (joinStartupInProgress)
        {
            SetJoinStage(
                JoinStartupStage.TransportConnectStarting,
                $"Starting transport connection to host {hostSteamId}.");
        }
        
        networkManager.StartClient();
        // we use coroutine because StartClient() is asynchronous.
        // we need to wait for the response to send RPCs
        StartCoroutine(WaitForConnectionThenJoin());
    }
    
    private IEnumerator WaitForConnectionThenJoin()
    {
        float deadline = Time.realtimeSinceStartup + clientConnectionTimeoutSeconds;
        var networkManager = PurrNet.NetworkManager.main;

        // wait until PurrNet reports that the client is connected
        while (networkManager != null && !networkManager.isClient && Time.realtimeSinceStartup < deadline)
        {
            yield return null;
        }

        if (networkManager == null || !networkManager.isClient)
        {
            Debug.LogError("[SteamBridge] Failed to connect to host via PurrNet");
            if (joinStartupInProgress)
            {
                SetJoinStage(
                    JoinStartupStage.Failed,
                    "Failed to connect to host through PurrNet transport.",
                    ConnectionFailureSource.Transport);

                joinStartupInProgress = false;
            }
            yield break;
        }

        while (SessionManager.Instance == null && Time.realtimeSinceStartup < deadline)
        {
            yield return null;
        }

        if (SessionManager.Instance == null)
        {

            Debug.LogError("[SteamBridge] SessionManager not available after connecting");
            if (joinStartupInProgress)
            {
                SetJoinStage(
                    JoinStartupStage.Failed,
                    "SessionManager was not available after client connection.",
                    ConnectionFailureSource.Transport);

                joinStartupInProgress = false;
            }

            yield break;
        }
        var identity = SessionManager.Instance.GetComponent<NetworkIdentity>();
        while (identity != null && !identity.isSpawned && Time.realtimeSinceStartup < deadline)
        {
            yield return null;
        }

        if (identity == null || !identity.isSpawned)
        {
            Debug.LogError("[SteamBridge] SessionManager exists but is not network-spawned");
            if (joinStartupInProgress)
            {
                SetJoinStage(
                    JoinStartupStage.Failed,
                    "SessionManager was not network-spawned in time.",
                    ConnectionFailureSource.Transport);

                joinStartupInProgress = false;
            }
            yield break;
        }

        if (joinStartupInProgress)
        {
            SetJoinStage(
                JoinStartupStage.TransportConnected,
                "Transport connected to host successfully.");
        }
        
        ulong steamID = SteamUser.GetSteamID().m_SteamID;
        string displayName = SteamFriends.GetPersonaName();
        
        if (joinStartupInProgress)
        {
            SetJoinStage(
                JoinStartupStage.SessionJoinRequested,
                "Transport connected. Requesting session approval from host.");
        }
        
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
        // Same lobby check: if player is already in the lobby they're trying to join, ignore it
        if (isInLobby && currentLobbyId ==callback.m_steamIDLobby)
        {
            Debug.LogWarning("[SteamBridge] Already in this lobby, ignoring join request.");
            return;
        }
        
        joinStartupAttemptID++;
        joinStartupInProgress = true;
        pendingJoinLobbyId = callback.m_steamIDLobby;

        SetJoinStage(
            JoinStartupStage.JoinRequestReceived,
            $"Join requested for lobby {callback.m_steamIDLobby}");
        
        // Existing lobby cleanup: if player is in a different lobby or mid-creation, clean up first
        if (isInLobby || isCreatingLobby)
        {
            Debug.Log("[SteamBridge] Leaving current lobby before joining new one.");
            LeaveSteamLobby();
        }
        SetJoinStage(
            JoinStartupStage.LeavingPreviousLobby,
            "Leaving current Steam lobby before new join attempt.");
        
        SessionModeManager.Instance.StartJoining();
        // after the checks now JoinLobby proceeds into a clean empty state
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
        
        SetJoinStage(
            JoinStartupStage.LobbyJoinRequested,
            $"Requested Steam lobby join for {callback.m_steamIDLobby}.");

    }
    
    // note: only lobby owner can set the metadata
    private bool SyncMetadataToSteamLobby()
    {
        if (!isSteamAvailable) return false; 
        if (!isInLobby) return false;
        
        // only the lobby owner can set metadata
        if (SteamMatchmaking.GetLobbyOwner(currentLobbyId) != SteamUser.GetSteamID()) return false;
        
        if (SessionManager.Instance == null) return false;
        var session = SessionManager.Instance.CurrentSession;
        if (session == null) return false;

        bool success = true;

        // everything inside the metadata are strings + keys have 255 character limit. Should keep them short.
        success &= SteamMatchmaking.SetLobbyData(currentLobbyId, "session_id", session.SessionId);
        success &= SteamMatchmaking.SetLobbyData(currentLobbyId, "map_name", session.MapName);
        success &= SteamMatchmaking.SetLobbyData(currentLobbyId, "game_mode", session.GameMode);
        success &= SteamMatchmaking.SetLobbyData(currentLobbyId, "host_name", SteamFriends.GetPersonaName());
        success &= SteamMatchmaking.SetLobbyData(currentLobbyId, "player_count", session.Players.Count.ToString());
        success &= SteamMatchmaking.SetLobbyData(currentLobbyId, "max_players", session.MaxPlayers.ToString());
        success &= SteamMatchmaking.SetLobbyData(currentLobbyId, "game_state", GameStateManager.Instance.CurrentState.ToString());
        success &= SteamMatchmaking.SetLobbyData(currentLobbyId, "game_version", Application.version);
        
        
        // sync custom properties with prefix to avoid key collisions, for example
        // if someone calls session.SetCustomProperty("map_name", "something"), without a prefix it would
        // overwrite the reserved map_name key. Namespacing prevents that kind of collisions.
        foreach (var kvp in session.CustomProperties)
        {
            success &= SteamMatchmaking.SetLobbyData(currentLobbyId, $"custom_{kvp.Key}", kvp.Value);
        }

        if (success)
        {
            Debug.Log("[SteamBridge] Lobby metadata successfully synced to Steam");
        }
        else
        {
            Debug.LogWarning("[SteamBridge] One or more Steam lobby metadata writes failed.");
        }

        return success;
    }
    
    private void SetJoinStage(JoinStartupStage stage,string message, ConnectionFailureSource failureSource = ConnectionFailureSource.None)
    {
        currentJoinStartupStatus.Stage = stage;
        currentJoinStartupStatus.Message = message;
        currentJoinStartupStatus.AttemptID = joinStartupAttemptID;
        currentJoinStartupStatus.FailureSource = failureSource;
        currentJoinStartupStatus.TargetLobbyId = pendingJoinLobbyId.IsValid() ? pendingJoinLobbyId.ToString() : "None";

        Debug.Log(
            $"[SteamBridge:Join] Attempt={currentJoinStartupStatus.AttemptID} " +
            $"Stage={currentJoinStartupStatus.Stage} " +
            $"FailureSource={currentJoinStartupStatus.FailureSource} " +
            $"TargetLobby={currentJoinStartupStatus.TargetLobbyId} " +
            $"Message={currentJoinStartupStatus.Message}");

        OnJoinStartupStatusChanged?.Invoke(currentJoinStartupStatus);
    }
    
    private void SyncMetadataToSteamLobbyFromEvent()
    {
        SyncMetadataToSteamLobby();
    }
    
    private void SubscribeToSessionEvents()
    {
        SessionEvents.OnPlayerJoined += OnPlayerJoinedSession;
        SessionEvents.OnPlayerLeft += OnPlayerLeftSession;
        SessionEvents.OnSessionDataChanged += SyncMetadataToSteamLobbyFromEvent;
        GameStateManager.Instance.OnStateChanged += OnGameStateChanged;
        SessionEvents.OnSessionError += OnSessionErrorReceived;
        SessionEvents.OnPlayerJoined += OnLocalPlayerJoinApproved;

    }
    
    private void UnsubscribeFromSessionEvents()
    {
        SessionEvents.OnPlayerJoined -= OnPlayerJoinedSession;
        SessionEvents.OnPlayerLeft -= OnPlayerLeftSession;
        SessionEvents.OnSessionDataChanged -= SyncMetadataToSteamLobbyFromEvent;
        GameStateManager.Instance.OnStateChanged -= OnGameStateChanged;
        SessionEvents.OnSessionError -= OnSessionErrorReceived;
        SessionEvents.OnPlayerJoined -= OnLocalPlayerJoinApproved;

    }

    private void OnLocalPlayerJoinApproved(ulong steamID, string displayName)
    {
        SyncMetadataToSteamLobby();

        if (!joinStartupInProgress)
            return;

        ulong localSteamId = SteamUser.GetSteamID().m_SteamID;
        if (steamID != localSteamId)
            return;

        SetJoinStage(
            JoinStartupStage.SessionJoinApproved,
            $"Session join approved for local player {displayName}.");

        joinStartupInProgress = false;
    }

    private void OnPlayerJoinedSession(ulong steamID, string displayName)
    {
        SyncMetadataToSteamLobby();
    }

    private void OnPlayerLeftSession(ulong steamID, string reason)
    {
        SyncMetadataToSteamLobby();
    }
    
    private void OnSessionErrorReceived(SessionErrorResponse error)
    {
        if (!joinStartupInProgress)
            return;

        SetJoinStage(
            JoinStartupStage.Failed,
            $"Session join failed: {error.Code} - {error.Message}",
            ConnectionFailureSource.SessionApproval);

        joinStartupInProgress = false;
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
