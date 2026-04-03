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
    
    public void SetMode(SessionMode mode)
    {
        // don't fire an event if mode is already the same
        if (mode == currentMode) return;
        // log the transition
        Debug.Log($"Mode: {currentMode} changed to {mode}.");
        // store previous mode update current
        SessionMode previousMode = currentMode;
        currentMode = mode;
        // fire OnModeChanged event
        OnModeChanged?.Invoke(previousMode, currentMode);
    }

    public void ReturnToMenu()
    {
        if (currentMode == SessionMode.None)
        {
            Debug.LogWarning("[SessionModeManager] Already in None mode, skipping shutdown.");
            return;
        }

        NetworkManager netManager = NetworkManager.main;
        if (netManager != null)
        {
            if (netManager.isServer)
                netManager.StopServer();

            if (netManager.isClient)
                netManager.StopClient();
        }
        
        if (SteamSessionBridge.Instance != null)
            SteamSessionBridge.Instance.LeaveSteamLobby();
        
        SetMode(SessionMode.None);
        
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.ForceStateChange(GameState.Menu);
        
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadScene("MainMenu");

        Debug.Log("[SessionModeManager] Shutdown complete. Back at menu.");
        
    }
    
}
