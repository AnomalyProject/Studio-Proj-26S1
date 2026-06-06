using System;
using PurrNet;
using UnityEngine;
using System.Collections;
using PurrNet.Steam;
using PurrNet.Transports;
using PurrNet.Modules;
using UnityEngine.SceneManagement;

public class SessionModeManager : MonoBehaviour
{
    public static SessionModeManager Instance { get; private set; }
    private SessionMode currentMode = SessionMode.None;
    public SessionMode CurrentMode => currentMode;

    private bool isLocallyInitiatedTeardown = false;
    public bool IsLocallyInitiatedTeardown => isLocallyInitiatedTeardown;
    public event Action OnLocalTeardownStarted;

    [SerializeField] private string gameplaySceneName = "MainGameplayScene";
    [SerializeField] private string lobbySceneName = "Lobby";

    private const float hostReadyTimeoutSeconds = 10f;
    private const float sessionReadyTimeoutSeconds = 5f;
    private const float devClientSpawnTimeoutSeconds = 15f;
    // because devtool is too fast for the system we've build and causes issues.
    private const float devClientStartupDelaySeconds = 1f; 
    
    public string LastJoinFailureMessage { get; private set; }

    public string PendingLobbyPassword { get; set; }
    private Coroutine hostLeftRoutine;

    public event Action<SessionMode, SessionMode> OnModeChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        
        TrySubscribeToSteamBridge();
    }
    
    private void OnDisable()
    {
        if (SteamSessionBridge.Instance)
        {
            SteamSessionBridge.Instance.OnHostStartupStatusChanged -= OnHostStartupStatusChanged;
            SteamSessionBridge.Instance.OnJoinStartupStatusChanged -= OnJoinStartupStatusChanged;
        }
    }

    private void Start()
    {
        TrySubscribeToSteamBridge();
        
        if (NetworkManager.main)
        {
            NetworkManager.main.onClientConnectionState += OnClientConnectionStateChanged;
        }
    }
    
    private void OnDestroy()
    {
        if (SteamSessionBridge.Instance)
        {
            SteamSessionBridge.Instance.OnHostStartupStatusChanged -= OnHostStartupStatusChanged;
            SteamSessionBridge.Instance.OnJoinStartupStatusChanged -= OnJoinStartupStatusChanged;
        }
        
        if (NetworkManager.main)
        {
            NetworkManager.main.onClientConnectionState -= OnClientConnectionStateChanged;
        }
    }
    
    private void TrySubscribeToSteamBridge()
    {
        if (SteamSessionBridge.Instance == null) return;

        SteamSessionBridge.Instance.OnHostStartupStatusChanged -= OnHostStartupStatusChanged;
        SteamSessionBridge.Instance.OnJoinStartupStatusChanged -= OnJoinStartupStatusChanged;

        SteamSessionBridge.Instance.OnHostStartupStatusChanged += OnHostStartupStatusChanged;
        SteamSessionBridge.Instance.OnJoinStartupStatusChanged += OnJoinStartupStatusChanged;
    }


    /// <summary>
    /// Updates the active session mode and notifies listeners when the mode actually changes
    /// </summary>
    /// <param name="mode"></param>
    public void SetMode(SessionMode mode)
    {
        if (mode == currentMode) return;

        Debug.Log($"[SessionModeManager] Mode: {currentMode} changed to {mode}.");

        SessionMode previousMode = currentMode;
        currentMode = mode;

        OnModeChanged?.Invoke(previousMode, currentMode);
    }

    /// <summary>
    /// Shuts down any active network session, leaves the Steam lobbyu, resets session state, and
    /// returns the game to the main menu flow. 
    /// </summary>
    public void ReturnToMenu()
    {
        if (isLocallyInitiatedTeardown) return;
        
        isLocallyInitiatedTeardown = true;
        OnLocalTeardownStarted?.Invoke();

        StartCoroutine(ReturnToMenuRoutine());
    }

    private IEnumerator ReturnToMenuRoutine()
    {
        try
        {
            NetworkManager netManager = NetworkManager.main;
            if (netManager)
            {
                if (netManager.isServer) netManager.StopServer();
                if (netManager.isClient) netManager.StopClient();
                
                float shutdownDeadline = Time.realtimeSinceStartup + 5f;
                
                while (netManager &&
                       (netManager.clientState != ConnectionState.Disconnected ||
                        netManager.serverState != ConnectionState.Disconnected ||
                        !netManager.isOffline) &&
                       Time.realtimeSinceStartup < shutdownDeadline)
                {
                    yield return null;
                }
                
                if (netManager != null &&
                    (netManager.clientState != ConnectionState.Disconnected ||
                     netManager.serverState != ConnectionState.Disconnected ||
                     !netManager.isOffline))
                {
                    Debug.LogWarning("[SessionModeManager] Network teardown timed out. Continuing to menu anyway.");
                }
            }

            if (SteamSessionBridge.Instance)
            {
                SteamSessionBridge.Instance.LeaveSteamLobby();
            }
            
            if (GameStateManager.Instance)
            {
                GameStateManager.Instance.ForceStateChange(GameState.Menu);
            }

            Scene menuScene = default;
            float deadline = Time.realtimeSinceStartup + 3f;
            while (Time.realtimeSinceStartup < deadline)
            {
                menuScene = SceneManager.GetSceneByName("MainMenu");
                if (menuScene.IsValid() && menuScene.isLoaded) break;
                yield return null;
            }


            if (menuScene.IsValid() && menuScene.isLoaded)
            {
                SceneManager.SetActiveScene(menuScene);
            }
            else
            {
                Debug.LogWarning("[SessionModeManager] PurrNet didn't restore MainMenu.. Manually loading.");
                SceneLoader.Instance.LoadScene("MainMenu");
            }

            PendingLobbyPassword = string.Empty;
            SetMode(SessionMode.None);
        }
        finally
        {
            isLocallyInitiatedTeardown = false;
        }
    }

    /// <summary>
    /// Server-side session flow for returning all connected players from gameplay back to the lobby.
    /// Keeps the active network session and Steam lobby alive.
    /// </summary>
    public void LoadLobbyScene()
    {
        if (NetworkManager.main == null || !NetworkManager.main.isServer) return;

        if (GameStateManager.Instance.CurrentState == GameState.InGame)
        {
            GameStateManager.Instance.RequestStateChange(GameState.PostGame);
        }

        GameStateManager.Instance.RequestStateChange(GameState.Lobby);

        var settings = new PurrSceneSettings
        {
            isPublic = true,
            mode = LoadSceneMode.Single
        };

        var op = NetworkManager.main.sceneModule.LoadSceneAsync(lobbySceneName, settings);
        SceneLoader.Instance.PerformAsyncOperation(op);
    }


    /// <summary>
    /// Begins a solo session by entering the required state flow and loading the requested gameplay scene.
    /// </summary>
    /// <param name="sceneName"></param>
    public void StartSolo()
    {
        if (currentMode != SessionMode.None)
        {
            Debug.LogWarning($"[SessionModeManager] Cannot start Solo, already in {currentMode} mode.");
            return;
        }

        StartCoroutine(BeginSoloLobbyFlow());
    }
    
    private IEnumerator BeginSoloLobbyFlow()
    {
        NetworkManager netManager = NetworkManager.main;

        if (netManager == null)
        {
            Debug.LogError("[SessionModeManager] NetworkManager.main not found. Cannot start solo lobby.");
            ReturnToMenu();
            yield break;
        }

        LocalTransport localTransport = netManager.GetComponent<LocalTransport>();
        SteamTransport steamTransport = netManager.GetComponent<SteamTransport>();

        if (localTransport == null)
        {
            Debug.LogError("[SessionModeManager] LocalTransport component missing on NetworkManager.");
            ReturnToMenu();
            yield break;
        }

        SetMode(SessionMode.Solo);
        GameStateManager.Instance.RequestStateChange(GameState.Lobby);

        localTransport.enabled = true;
        if (steamTransport != null) steamTransport.enabled = false;

        netManager.transport = localTransport;
        netManager.StartHost();

        float hostDeadline = Time.realtimeSinceStartup + hostReadyTimeoutSeconds;
        while (!netManager.isHost && Time.realtimeSinceStartup < hostDeadline)
            yield return null;

        if (!netManager.isHost)
        {
            Debug.LogError("[SessionModeManager] Timed out waiting for solo host.");
            ReturnToMenu();
            yield break;
        }

        float sessionDeadline = Time.realtimeSinceStartup + sessionReadyTimeoutSeconds;
        while ((SessionManager.Instance == null || SessionManager.Instance.CurrentSession == null) &&
               Time.realtimeSinceStartup < sessionDeadline)
        {
            yield return null;
        }

        if (SessionManager.Instance == null || SessionManager.Instance.CurrentSession == null)
        {
            Debug.LogError("[SessionModeManager] Timed out waiting for solo session.");
            ReturnToMenu();
            yield break;
        }

        var settings = new PurrSceneSettings
        {
            isPublic = true,
            mode = LoadSceneMode.Single
        };

        var op = netManager.sceneModule.LoadSceneAsync(lobbySceneName, settings);
        SceneLoader.Instance.PerformAsyncOperation(op);

        while (op != null && !op.isDone)
            yield return null;

        Debug.Log("[SessionModeManager] Solo lobby loaded.");
    }

    public void StartSoloInScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("[SessionModeManager] Cannot start solo. Scene name was empty.");
            ReturnToMenu();
            return;
        }
        
        if (currentMode != SessionMode.None)
        {
            Debug.LogWarning($"[SessionModeManager] Cannot start Solo, already in {currentMode} mode.");
            return;
        }
        
        SetMode(SessionMode.Solo);
        GameStateManager.Instance.RequestStateChange(GameState.Lobby);
        GameStateManager.Instance.RequestStateChange(GameState.Loading);
        StartCoroutine(BeginSoloFlowForScene(sceneName));
    }
    
    public void StartDevHost(string sceneName, DevBootstrapRequest request)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("[SessionModeManager] Cannot start dev host. Scene name was empty.");
            ReturnToMenu();
            return;
        }

        if (currentMode != SessionMode.None)
        {
            Debug.LogWarning($"[SessionModeManager] Cannot start dev host, already in {currentMode} mode.");
            return;
        }

        SetMode(SessionMode.DevHost);
        GameStateManager.Instance.RequestStateChange(GameState.Lobby);
        GameStateManager.Instance.RequestStateChange(GameState.Loading);
        StartCoroutine(BeginDevHostFlow(sceneName, request));
    }
    
      private IEnumerator BeginDevHostFlow(string sceneName, DevBootstrapRequest request)
  {
      NetworkManager netManager = NetworkManager.main;
      if (netManager == null)
      {
          Debug.LogError("[SessionModeManager] NetworkManager.main not found. Cannot start dev host.");
          ReturnToMenu();
          yield break;
      }

      UDPTransport udpTransport = netManager.GetComponent<UDPTransport>();
      if (udpTransport == null)
      {
          Debug.LogError("[SessionModeManager] UDPTransport missing on NetworkManager. Add it to the Network Manager prefab.");
          ReturnToMenu();
          yield break;
      }

      // Dev host should uses UDP, disable the others so nothing else is live.
      LocalTransport localTransport = netManager.GetComponent<LocalTransport>();
      SteamTransport steamTransport = netManager.GetComponent<SteamTransport>();
      if (localTransport != null) localTransport.enabled = false;
      if (steamTransport != null) steamTransport.enabled = false;

      udpTransport.address        = request.address;          
      udpTransport.serverPort     = (ushort)request.port;
      udpTransport.maxConnections = request.maxPlayers;
      udpTransport.enabled        = true;
      netManager.transport        = udpTransport;

      Debug.Log($"[SessionModeManager] Dev host on UDP port {udpTransport.serverPort}. Starting host...");
      netManager.StartHost();

      float hostDeadline = Time.realtimeSinceStartup + hostReadyTimeoutSeconds;
      while (!netManager.isHost && Time.realtimeSinceStartup < hostDeadline) yield return null;

      if (!netManager.isHost)
      {
          Debug.LogError("[SessionModeManager] Timed out waiting for dev listen-host to become ready.");
          ReturnToMenu();
          yield break;
      }

      float sessionDeadline = Time.realtimeSinceStartup + sessionReadyTimeoutSeconds;
      while ((SessionManager.Instance == null || SessionManager.Instance.CurrentSession == null)
             && Time.realtimeSinceStartup < sessionDeadline) yield return null;

      if (SessionManager.Instance == null || SessionManager.Instance.CurrentSession == null)
      {
          Debug.LogError("[SessionModeManager] Timed out waiting for SessionManager to create the dev session.");
          ReturnToMenu();
          yield break;
      }

      // loading the dev target scene through PurrNet so it replicates to clients that join later.
      var settings = new PurrSceneSettings { isPublic = true, mode = LoadSceneMode.Single };
      var op = netManager.sceneModule.LoadSceneAsync(sceneName, settings);
      SceneLoader.Instance.PerformAsyncOperation(op);
      while (op != null && !op.isDone) yield return null;

      GameStateManager.Instance.RequestStateChange(GameState.InGame);
      Debug.Log("[SessionModeManager] Dev host ready. -> InGame");
  }
    

    /// <summary>
    /// Starts the co-op host flow by entering lobby/loading states and loading the gameplay scene before
    /// host startuo begins.
    /// </summary>
    public void StartHosting()
    {
        if (currentMode != SessionMode.None)
        {
            Debug.LogWarning($"[SessionModeManager] Cannot start hosting, already in {currentMode} mode.");
            return;
        }

        Debug.Log("[SessionModeManager] Starting Co-Op Host...");

        SetMode(SessionMode.CoOpHost);

        GameStateManager.Instance.RequestStateChange(GameState.Lobby);
        
        SteamSessionBridge.Instance.BeginSteamListenHost();
    }

    /// <summary>
    /// Runs after the host scene finishes loading to start the Steam listen-host setup.
    /// </summary>
    private void OnHostSceneLoaded()
    {
        SceneLoader.Instance.OnLoadFinished -= OnHostSceneLoaded;

        SteamSessionBridge.Instance.BeginSteamListenHost();
    }

    /// <summary>
    /// Runs after the solo scene finishes loading to kick off the local listen-host startup coroutine.
    /// </summary>
    private void OnSoloSceneLoaded()
    {
        SceneLoader.Instance.OnLoadFinished -= OnSoloSceneLoaded;
        StartCoroutine(BeginLocalListenHost());
    }
    
    /// <summary>
    /// server only method. Loads the gameplay scene through PurrNet's scene module which replicates
    /// the load to every connected client automatically.
    /// </summary>
    public void LoadGameplayScene()
    {
        if (NetworkManager.main == null || !NetworkManager.main.isServer) return;
        
        var settings = new PurrSceneSettings {
            isPublic = true, // note: public true means that every connecte player gets pulled into the scene
            mode = LoadSceneMode.Single
        };

        var op = NetworkManager.main.sceneModule.LoadSceneAsync(gameplaySceneName, settings);
        SceneLoader.Instance.PerformAsyncOperation(op); 
        
        StartCoroutine(WaitForGameplayLoadThenInGame(op));
    }

    private IEnumerator WaitForGameplayLoadThenInGame(AsyncOperation op)
    {
        while (op != null && !op.isDone) yield return null;

        if (GameStateManager.Instance.CurrentState == GameState.Loading)
        {
            GameStateManager.Instance.RequestStateChange(GameState.InGame);
            Debug.Log("[SessionModeManager] Gameplay scene loaded. Transitioned to InGame.");
        }
        else
        {
            Debug.LogWarning($"[SessionModeManager] Gameplay scene loaded but state was {GameStateManager.Instance.CurrentState}, not Loading. InGame transition skipped.");
        }
            
    }

    private IEnumerator BeginSoloFlowForScene(string sceneName)
    {
        NetworkManager netManager = NetworkManager.main;

        if (netManager == null)
        {
            Debug.LogWarning($"[SessionModeManager] Network Manager doesn't exist in this scene.");
            ReturnToMenu();
            yield break;
        }
        
        // swaping to local transport
        LocalTransport localTransport = netManager.GetComponent<LocalTransport>();
        SteamTransport steamTransport = netManager.GetComponent<SteamTransport>();
        
        if (localTransport == null)
        {
            Debug.LogError("[SessionModeManager] LocalTransport component missing on NetworkManager.");
            ReturnToMenu();
            yield break;
        }

        localTransport.enabled = true;
        steamTransport.enabled = false;
        netManager.transport = localTransport;

        netManager.StartHost();
        
        float deadline = Time.realtimeSinceStartup + hostReadyTimeoutSeconds;
        while (!netManager.isHost && Time.realtimeSinceStartup < deadline) yield return null;

        if (!netManager.isHost)
        {
            Debug.LogWarning($"[SessionModeManager] Network Manager doesn't exist in this scene.");
            ReturnToMenu();
            yield break;
        }
        
        float sessionDeadline = Time.realtimeSinceStartup + sessionReadyTimeoutSeconds;
        while ((SessionManager.Instance == null || SessionManager.Instance.CurrentSession == null) && Time.realtimeSinceStartup < sessionDeadline) yield return null;

        if (SessionManager.Instance == null || SessionManager.Instance.CurrentSession == null)
        {
            Debug.LogWarning($"[SessionModeManager] Network Manager doesn't exist in this scene.");
            ReturnToMenu();
            yield break;
        }
        
        // loading gameplay scene through PurrNet
        var settings = new PurrSceneSettings { isPublic = true, mode = LoadSceneMode.Single };
        var op = netManager.sceneModule.LoadSceneAsync(sceneName, settings);
        SceneLoader.Instance.PerformAsyncOperation(op);

        // waiting for the load to actually finish
        while (op != null && !op.isDone) yield return null;

        GameStateManager.Instance.RequestStateChange(GameState.InGame);
    }
    

    /// <summary>
    /// Boots a solo session as a Purrnet listen-host over LocalTransport.
    /// Flips the NetworkManager's active transport from Steam to Local, starts the host, and waits for
    /// both listen-host readiness and session creation before transitioning to InGame. Returns
    /// to the main menu if any step times out.
    /// </summary>
    /// <returns></returns>
    private IEnumerator BeginLocalListenHost()
    {
        // wait so the new scene's Start method can run.
        // Otherwhise network manager is null. 
        float readyDeadline = Time.realtimeSinceStartup + 2f;
        while (NetworkManager.main == null && Time.realtimeSinceStartup < readyDeadline)
            yield return null;
        
        NetworkManager netManager = NetworkManager.main;

        if (netManager == null)
        {
            Debug.LogError("[SessionModeManager] NetworkManager.main not found after solo scene load.");
            ReturnToMenu();
            yield break;
        }
        
        LocalTransport localTransport = netManager.GetComponent<LocalTransport>();
        if (localTransport == null)
        {
            Debug.LogError("[SessionModeManager] LocalTransport component missing on NetworkManager GameObject.");
            ReturnToMenu();
            yield break;
        }
        
        SteamTransport steamTransport = netManager.GetComponent<SteamTransport>();

        localTransport.enabled = true;
        if(steamTransport != null) steamTransport.enabled = false;
        netManager.transport = localTransport;
        
        Debug.Log($"[SessionModeManager] Solo transport set to {netManager.transport.GetType().Name}. Starting host....");
        netManager.StartHost();
        
        float hostDeadline = Time.realtimeSinceStartup + hostReadyTimeoutSeconds;
        while (!netManager.isHost && Time.realtimeSinceStartup < hostDeadline)
            yield return null;

        if (!netManager.isHost)
        {
            Debug.LogError("[SessionModeManager] Timed out waiting for solo listen-host to become ready.");
            ReturnToMenu();
            yield break;
        }

        float sessionDeadline = Time.realtimeSinceStartup + sessionReadyTimeoutSeconds;
        while ((SessionManager.Instance == null || SessionManager.Instance.CurrentSession == null)
               && Time.realtimeSinceStartup < sessionDeadline)
            yield return null;

        if (SessionManager.Instance == null || SessionManager.Instance.CurrentSession == null)
        {
            Debug.LogError("[SessionModeManager] Timed out waiting for SessionManager to create the solo session.");
            ReturnToMenu();
            yield break;
        }

        GameStateManager.Instance.RequestStateChange(GameState.InGame);
        Debug.Log("[SessionModeManager] Solo host ready. -> Transitioned to InGame");
    }


    /// <summary>
    /// Starts the co-op client join flow by entering lobby/loading states and loading the gameplay scene before joining begins.
    /// </summary>
    public void StartJoining()
    {
        if (currentMode != SessionMode.None)
        {
            Debug.LogWarning($"[SessionModeManager] Cannot join, already in {currentMode} mode.");
            return;
        }

        Debug.Log("[SessionModeManager] Starting Co-Op Client join...");

        SetMode(SessionMode.CoOpClient);
        GameStateManager.Instance.RequestStateChange(GameState.Lobby);

        SteamSessionBridge.Instance.BeginPendingSteamJoin();
    }
    
    /// <summary>
    /// Dev tool flow
    /// </summary>
    /// <param name="request"></param>
    public void StartDevClient(DevBootstrapRequest request)
    {
        if (currentMode != SessionMode.None)
        {
            Debug.LogWarning($"[SessionModeManager] Cannot start dev client, already in {currentMode} mode.");
            return;
        }

        SetMode(SessionMode.DevClient);
        GameStateManager.Instance.RequestStateChange(GameState.Lobby);
        StartCoroutine(BeginDevClientFlow(request));
    }
    
     private IEnumerator BeginDevClientFlow(DevBootstrapRequest request)
      {
          NetworkManager netManager = NetworkManager.main;
          if (netManager == null)
          {
              Debug.LogError("[SessionModeManager] NetworkManager.main not found. Cannot start dev client.");
              ReturnToMenu();
              yield break;
          }

          UDPTransport udpTransport = netManager.GetComponent<UDPTransport>();
          if (udpTransport == null)
          {
              Debug.LogError("[SessionModeManager] UDPTransport missing on NetworkManager.");
              ReturnToMenu();
              yield break;
          }

          LocalTransport localTransport = netManager.GetComponent<LocalTransport>();
          SteamTransport steamTransport = netManager.GetComponent<SteamTransport>();
          if (localTransport != null) localTransport.enabled = false;
          if (steamTransport != null) steamTransport.enabled = false;

          udpTransport.address    = request.address;
          udpTransport.serverPort = (ushort)request.port;
          udpTransport.enabled    = true;
          netManager.transport    = udpTransport;

          // letting client's bootstrap scene + PurrNet hierarchy finish registering before we connect.
          // connecting mid-initialization makes the host's earliest spawn packets arrive before we can build them
          // and then PurrNets drops them permanently and client cannot connect.
          yield return new WaitForSeconds(devClientStartupDelaySeconds);
          
          Debug.Log($"[SessionModeManager] Dev client connecting to {request.address}:{request.port}...");
          netManager.StartClient();

          // waiting for the transport to connect
          float connectDeadline = Time.realtimeSinceStartup + hostReadyTimeoutSeconds;
          while (!netManager.isClient && Time.realtimeSinceStartup < connectDeadline) yield return null;

          if (!netManager.isClient)
          {
              Debug.LogError("[SessionModeManager] Dev client failed to connect to host.");
              ReturnToMenu();
              yield break;
          }

          // waiting for SessionManager to exist and be network spawned
          float instanceDeadline = Time.realtimeSinceStartup + sessionReadyTimeoutSeconds;
          while (SessionManager.Instance == null && Time.realtimeSinceStartup < instanceDeadline) yield return null;

          NetworkIdentity identity = SessionManager.Instance != null
              ? SessionManager.Instance.GetComponent<NetworkIdentity>()
              : null;
          
          float spawnDeadline = Time.realtimeSinceStartup + devClientSpawnTimeoutSeconds;
          while (identity != null && !identity.isSpawned && Time.realtimeSinceStartup < spawnDeadline) yield return null;

          if (identity == null || !identity.isSpawned)
          {
              Debug.LogError("[SessionModeManager] Dev client: SessionManager not network-spawned in time.");
              ReturnToMenu();
              yield break;
          }

          // requesting to join with our deterministic dev identity
          Debug.Log($"[SessionModeManager] Dev client connected. Joining as '{request.displayName}' (id {request.fakeSteamId}).");
          SessionManager.Instance.RequestJoinDevSession(request.fakeSteamId, request.displayName);
      }
    
    /// <summary>
    ///  Runs after the join scene finishes loading to begin the pending Steam join process.
    /// </summary>
    private void OnJoinSceneLoaded()
    {
        SceneLoader.Instance.OnLoadFinished -= OnJoinSceneLoaded;
        SteamSessionBridge.Instance.BeginPendingSteamJoin();
    }
    
    /// <summary>
    /// Monitors join startup progress and returns to the menu if the join process fails.
    /// </summary>
    /// <param name="status"></param>
    private void OnJoinStartupStatusChanged(JoinStartupStatus status)
    {
        if (status.Stage == JoinStartupStage.Failed)
        {
            //===============================================
            if(status.Message.Contains("password"))
            {
                LastJoinFailureMessage = "";
                return;
            }
            //===============================================
            LastJoinFailureMessage =  $"Could not join lobby: {status.Message}";
            Debug.LogWarning($"[SessionModeManager] Join failed: {status.Message}");
            ReturnToMenu();
        }
    }

    /// <summary>
    /// A method clearing the message after showing it, otherwise the same old error will re appear
    /// every time the player opens the menu
    /// </summary>
    public void ClearLastJoinFailureMessage()
    {
        LastJoinFailureMessage = "";
    }
    
    /// <summary>
    /// Monitors host startup progress, returning to the menu on failure and switching to InGame once hosting is fully published.
    /// </summary>
    /// <param name="status"></param>
    private void OnHostStartupStatusChanged(HostStartupStatus status)
    {
        if (status.Stage == HostStartupStage.Failed)
        {
            Debug.LogWarning($"[SessionModeManager] Host startup failed: {status.Message}");
            ReturnToMenu();
            return;
        }
        
        if (status.Stage == HostStartupStage.HostPublished)
        {
            var settings = new PurrSceneSettings{ isPublic = true, mode = LoadSceneMode.Single };
            var op = NetworkManager.main.sceneModule.LoadSceneAsync(lobbySceneName, settings);
            SceneLoader.Instance.PerformAsyncOperation(op);
        }
        
    }

    private void OnClientConnectionStateChanged(ConnectionState state)
    {
        if (state != ConnectionState.Disconnected) return;
        if (currentMode != SessionMode.CoOpClient) return;
        if (isLocallyInitiatedTeardown) return;

        Debug.LogWarning("[SessionModeManager] Client disconnected. Waiting for reconnect service to handle timeout.");
       // SessionEvents.InvokeHostMigrationStarted("Host left the lobby.");
    }
    
    private IEnumerator HandleHostLeftRoutine()
    {
        SessionEvents.InvokeHostMigrationStarted("Host left the lobby.");
        yield return new WaitForSecondsRealtime(2f);
        hostLeftRoutine = null;
        ReturnToMenu();
    }

}
