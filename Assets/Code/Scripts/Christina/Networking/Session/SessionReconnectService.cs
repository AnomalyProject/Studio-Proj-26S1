using UnityEngine;
using PurrNet;
using PurrNet.Transports;
using System;
using UnityEngine.SceneManagement;
using System.Collections;

public class SessionReconnectService : MonoBehaviour, IReconnect
{
    public event Action OnConnectionLost;
    public event Action OnHostMigrating;
    public event Action OnReconnected;

    private bool wasConnected;
    private bool isWaitingToReconnect;
    
    [SerializeField] private float reconnectAttemptDelay = 2f;
    private NetworkManager subscribedNetworkManager;
    private Coroutine reconnectRoutine;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ReconnectUIController reconnectUI = FindFirstObjectByType<ReconnectUIController>(FindObjectsInactive.Include);

        if (reconnectUI != null)
        {
            reconnectUI.InjectDependencies(this);
        }
        else
        {
            Debug.LogWarning("[SessionReconnectService] ReconnectUIController was not found.");
        }

        TrySubscribeToNetworkManager();

        SessionEvents.OnHostMigrationStarted += HandleHostMigrationStarted;
        SessionEvents.OnReconnectApproved += HandleReconnectApproved;
        
        if (SessionModeManager.Instance != null)
        {
            SessionModeManager.Instance.OnLocalTeardownStarted += ResetReconnectState;
        }
    }
    
    private void Update()
    {
        TrySubscribeToNetworkManager();
    }
    
    private void OnDestroy()
    {
        if (subscribedNetworkManager  != null)
        {
            subscribedNetworkManager.onClientConnectionState -= HandleClientConnectionState;
        }

        SessionEvents.OnHostMigrationStarted -= HandleHostMigrationStarted;
        SessionEvents.OnReconnectApproved -= HandleReconnectApproved;
        
        if (SessionModeManager.Instance != null)
        {
            SessionModeManager.Instance.OnLocalTeardownStarted -= ResetReconnectState;
        }
    }
    
    private bool IsLocalTeardownInProgress()
    {
        return SessionModeManager.Instance != null &&
               SessionModeManager.Instance.IsLocallyInitiatedTeardown;
    }
    
    private void HandleClientConnectionState(ConnectionState state)
    {
        Debug.Log($"[SessionReconnectService] Client state changed: {state}");
        if (SceneManager.GetActiveScene().name == "MainMenu") return;

        if (state == ConnectionState.Connected)
        {
            wasConnected = true;
            return;
        }

        if (state != ConnectionState.Disconnecting && state != ConnectionState.Disconnected) return;
        
        if (IsLocalTeardownInProgress())
        {
            ResetReconnectState();
            return;
        }
        
        if (SessionModeManager.Instance == null) return;
        
        if (SessionModeManager.Instance.CurrentMode != SessionMode.CoOpClient && SessionModeManager.Instance.CurrentMode != SessionMode.DevClient) return;
        
        if (!wasConnected)
        {
            Debug.Log("[SessionReconnectService] Ignoring disconnect before first successful connection.");
            return;
        }
        
        if (isWaitingToReconnect) return;
        
        isWaitingToReconnect = true;
        OnConnectionLost?.Invoke();

        if (reconnectRoutine == null) reconnectRoutine = StartCoroutine(ReconnectRoutine());
        
    }
    
    private void HandleReconnectApproved()
    {
        if (!isWaitingToReconnect) return;

        isWaitingToReconnect = false;
        wasConnected = true;
        StopReconnectRoutine();

        OnReconnected?.Invoke();
    }
    
    public float ReconnectTimeoutSeconds
    {
        get
        {
            if (SessionManager.Instance != null) return SessionManager.Instance.ReconnectTimeoutSeconds;

            return 30f;
        }
    }
    
    
    private void ResetReconnectState()
    {
        isWaitingToReconnect = false;
        wasConnected = false;
        StopReconnectRoutine();
    }
    
    private void HandleHostMigrationStarted(string message)
    {
        if (SceneManager.GetActiveScene().name == "MainMenu") return;

        isWaitingToReconnect = false;
        StopReconnectRoutine();
        
        OnHostMigrating?.Invoke();
    }

    public void CancelAndReturnToMenu()
    {
        isWaitingToReconnect = false;
        wasConnected = false;
        
        StopReconnectRoutine();
        
        if (SessionModeManager.Instance != null) SessionModeManager.Instance.ReturnToMenu();
    }
    
    private void TrySubscribeToNetworkManager()
    {
        if (subscribedNetworkManager != null) return;
        if (NetworkManager.main == null) return;

        subscribedNetworkManager = NetworkManager.main;
        subscribedNetworkManager.onClientConnectionState += HandleClientConnectionState;
        wasConnected = subscribedNetworkManager.clientState == ConnectionState.Connected;
    }
    
    private IEnumerator ReconnectRoutine()
    {
        while (isWaitingToReconnect)
        {
            yield return new WaitForSecondsRealtime(reconnectAttemptDelay);

            if (NetworkManager.main == null) continue;

            if (NetworkManager.main.clientState != ConnectionState.Disconnected) continue;

            Debug.Log("[SessionReconnectService] Trying to reconnect...");
            
            NetworkManager.main.StartClient();
        }

        reconnectRoutine = null;
    }

    private void StopReconnectRoutine()
    {
        if (reconnectRoutine != null)
        {
            StopCoroutine(reconnectRoutine);
            reconnectRoutine = null;
        }
    }
}
