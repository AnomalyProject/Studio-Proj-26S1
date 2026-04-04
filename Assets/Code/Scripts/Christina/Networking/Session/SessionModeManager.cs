using System;
using PurrNet;
using UnityEngine;

public class SessionModeManager : MonoBehaviour
{
    public static SessionModeManager Instance {get; private set;}
    private SessionMode currentMode = SessionMode.None;
    public SessionMode CurrentMode => currentMode;
     
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
    
    public void SetMode(SessionMode mode)
    {
        if (mode == currentMode) return;
        
        Debug.Log($"[SessionModeManager] Mode: {currentMode} changed to {mode}.");

        SessionMode previousMode = currentMode;
        currentMode = mode;

        OnModeChanged?.Invoke(previousMode, currentMode);
    }

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
            SceneLoader.Instance.LoadScene("MainMenu");
        }
            
        
    }
    
    public void StartSolo(string sceneName)
    {
        if (currentMode != SessionMode.None)
        {
            Debug.LogWarning($"[SessionModeManager] Cannot start Solo, already in {currentMode} mode.");
            return;
        }

        Debug.Log("[SessionModeManager] Starting Solo session...");
        
        SetMode(SessionMode.Solo);

        // in Solo mode we don't need a Lobby phase. But we still go through it because
        // GameStateManager requires that path. 
        GameStateManager.Instance.RequestStateChange(GameState.Lobby);
        GameStateManager.Instance.RequestStateChange(GameState.Loading);
        
        SceneLoader.Instance.OnLoadFinished += OnSceneLoaded;
        SceneLoader.Instance.LoadSceneWithAsync(sceneName);
    }
    
    public void StartHosting()
    {
        if (currentMode != SessionMode.None)
        {
            Debug.LogWarning($"[SessionModeManager] Cannot start hosting, already in {currentMode} mode.");
            return;
        }

        Debug.Log("[SessionModeManager] Starting Co-Op Host...");

        SetMode(SessionMode.CoOpHost);

        SteamSessionBridge.Instance.BeginSteamListenHost();
    }

    private void OnSceneLoaded()
    {
        // unsubscribing immediately because the next time any scene loads through SceneLoader, 
        // OnSceneLoaded fires again and tries to transition to InGame from Menu. 
        SceneLoader.Instance.OnLoadFinished -= OnSceneLoaded;
        
        GameStateManager.Instance.RequestStateChange(GameState.InGame);
        Debug.Log("[SessionModeManager] Scene loaded. Transitioned to InGame");
    }
    
    public void StartJoining()
    {
        if (currentMode != SessionMode.None)
        {
            Debug.LogWarning($"[SessionModeManager] Cannot join, already in {currentMode} mode.");
            return;
        }

        Debug.Log("[SessionModeManager] Starting Co-Op Client join...");

        SetMode(SessionMode.CoOpClient);
    }
    
    private void OnJoinStartupStatusChanged(JoinStartupStatus status)
    {
        if (status.Stage == JoinStartupStage.Failed)
        {
            Debug.LogWarning($"[SessionModeManager] Join failed: {status.Message}");
            ReturnToMenu();
        }
    }
    
    private void OnHostStartupStatusChanged(HostStartupStatus status)
    {
        if (status.Stage == HostStartupStage.Failed)
        {
            Debug.LogWarning($"[SessionModeManager] Host startup failed: {status.Message}");
            ReturnToMenu();
        }
    }

}
