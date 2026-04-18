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
    [SerializeField] private string gameplaySceneName = "NetworkTestScene";
    [SerializeField] private string lobbySceneName = "Lobby";

    private const float hostReadyTimeoutSeconds = 10f;
    private const float sessionReadyTimeoutSeconds = 5f;

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
        // todelete
        Debug.Log($"[SessionModeManager] OnEnable — Bridge.Instance is {(SteamSessionBridge.Instance == null ? "NULL" : "SET")}");
        
        if (SteamSessionBridge.Instance != null)
        {
            SteamSessionBridge.Instance.OnHostStartupStatusChanged += OnHostStartupStatusChanged;
            SteamSessionBridge.Instance.OnJoinStartupStatusChanged += OnJoinStartupStatusChanged;
        }
    }

    private void OnDisable()
    {
        if (SteamSessionBridge.Instance != null)
        {
            SteamSessionBridge.Instance.OnHostStartupStatusChanged -= OnHostStartupStatusChanged;
            SteamSessionBridge.Instance.OnJoinStartupStatusChanged -= OnJoinStartupStatusChanged;
        }
    }

    private void Start()
    {
        if (SteamSessionBridge.Instance != null)
        {
            SteamSessionBridge.Instance.OnHostStartupStatusChanged += OnHostStartupStatusChanged;
            SteamSessionBridge.Instance.OnJoinStartupStatusChanged += OnJoinStartupStatusChanged;
        }
    }
    
    private void OnDestroy()
    {
        if (SteamSessionBridge.Instance != null)
        {
            SteamSessionBridge.Instance.OnHostStartupStatusChanged -= OnHostStartupStatusChanged;
            SteamSessionBridge.Instance.OnJoinStartupStatusChanged -= OnJoinStartupStatusChanged;
        }
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
        NetworkManager netManager = NetworkManager.main;
        if (netManager != null)
        {
            if (netManager.isServer) netManager.StopServer();
            if (netManager.isClient) netManager.StopClient();
        }

        if (SteamSessionBridge.Instance != null)
        {
            SteamSessionBridge.Instance.LeaveSteamLobby();
        }

        SessionEvents.Reset();
        SetMode(SessionMode.None);

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.ForceStateChange(GameState.Menu);
        }

        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadScene("MainMenuChristina");
        }


    }

    /// <summary>
    /// Begins a solo session by entering the required state flow and loading the requested gameplay scene.
    /// </summary>
    /// <param name="sceneName"></param>
    public void StartSolo(string sceneName)
    {
        if (currentMode != SessionMode.None)
        {
            Debug.LogWarning($"[SessionModeManager] Cannot start Solo, already in {currentMode} mode.");
            return;
        }

        StartCoroutine(BeginSoloFlow());

        /*Debug.Log("[SessionModeManager] Starting Solo session...");

        SetMode(SessionMode.Solo);

        // in Solo mode we don't need a Lobby phase. But we still go through it because
        // GameStateManager requires that path.
        GameStateManager.Instance.RequestStateChange(GameState.Lobby);
        GameStateManager.Instance.RequestStateChange(GameState.Loading);

        SceneLoader.Instance.OnLoadFinished += OnSoloSceneLoaded;
        SceneLoader.Instance.LoadSceneWithAsync(sceneName);*/
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
        //GameStateManager.Instance.RequestStateChange(GameState.Loading);

        /*SceneLoader.Instance.OnLoadFinished += OnHostSceneLoaded;
        SceneLoader.Instance.LoadSceneWithAsync(gameplaySceneName);*/
        
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

    private IEnumerator BeginSoloFlow()
    {
        NetworkManager netManager = NetworkManager.main;

        if (netManager == null)
        {
            Debug.LogWarning($"[SessionModeManager] Network Manager doesn't exist in this scene.");
            ReturnToMenu();
        }
        
        // swaping to local transport
        LocalTransport localTransport = netManager.GetComponent<LocalTransport>();
        SteamTransport steamTransport = netManager.GetComponent<SteamTransport>();

        localTransport.enabled = true;
        steamTransport.enabled = false;

        netManager.StartHost();
        
        float deadline = Time.realtimeSinceStartup + hostReadyTimeoutSeconds;
        while (!netManager.isHost && Time.realtimeSinceStartup < deadline) yield return null;

        if (!netManager.isHost)
        {
            Debug.LogWarning($"[SessionModeManager] Network Manager doesn't exist in this scene.");
            ReturnToMenu();
        }

        float sessionDeadline = Time.realtimeSinceStartup + sessionReadyTimeoutSeconds;
        while ((SessionManager.Instance == null || SessionManager.Instance.CurrentSession == null) && Time.realtimeSinceStartup < deadline) yield return null;

        if (SessionManager.Instance == null || SessionManager.Instance.CurrentSession == null)
        {
            Debug.LogWarning($"[SessionModeManager] Network Manager doesn't exist in this scene.");
            ReturnToMenu();
        }
        
        // loading gameplay scene through PurrNet
        var settings = new PurrSceneSettings { isPublic = true, mode = LoadSceneMode.Single };
        var op = netManager.sceneModule.LoadSceneAsync(gameplaySceneName, settings);
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
            Debug.LogWarning($"[SessionModeManager] Join failed: {status.Message}");
            ReturnToMenu();
        }
    }
    
    /// <summary>
    /// Monitors host startup progress, returning to the menu on failure and switching to InGame once hosting is fully published.
    /// </summary>
    /// <param name="status"></param>
    private void OnHostStartupStatusChanged(HostStartupStatus status)
    {
        //todelete
        Debug.Log($"[SessionModeManager] OnHostStartupStatusChanged fired: {status.Stage}");
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

}
