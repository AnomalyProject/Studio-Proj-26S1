using PurrNet;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class AfkHandler : NetworkBehaviour, IAfk
{
    public event Action OnAfkDetected;
    public event Action OnAfkCancelled;

    public bool IsAfk { get; private set; }

    [Tooltip("Seconds of no input before the player is considered AFK.")]
    [SerializeField] private float afkThresholdSeconds = 120f; //tweek here

    private float idleTimer;
    private bool isTracking;

    private void Start()
    {
        if(isHost)
        {
            Debug.Log("[AfkHandler] AFK tracking is disabled for the host.");
            return;
        }

        ReconnectUIController reconnectUI = FindFirstObjectByType<ReconnectUIController>(FindObjectsInactive.Include);
        if (reconnectUI != null)
        {
            reconnectUI.InjectAfkDependencies(this);
        }
        else
        {
            Debug.LogWarning("[AfkHandler] ReconnectUIController not found - AFK UI won't react.");
        }

        SessionReconnectService reconnectService = FindFirstObjectByType<SessionReconnectService>(FindObjectsInactive.Include);
        if (reconnectService != null)
        {
            reconnectService.OnConnectionLost += HandleConnectionLost;
            reconnectService.OnReconnected += HandleReconnected;
            reconnectService.OnReconnectFailed += _ => HandleReconnected();
        }
        else
        {
            Debug.LogWarning("[AfkHandler] SessionReconnectService not found — AFK will not pause during real disconnects.");
        }

        SubscribeToAllActions();
        StartTracking();
    }

    private void OnDestroy()
    {
        UnsubscribeFromAllActions();

        SessionReconnectService reconnectService = FindFirstObjectByType<SessionReconnectService>(FindObjectsInactive.Include);
        if (reconnectService != null)
        {
            reconnectService.OnConnectionLost -= HandleConnectionLost;
            reconnectService.OnReconnected -= HandleReconnected;
            reconnectService.OnReconnectFailed -= _ => HandleReconnected();
        }
    }

    private void Update()
    {
        if (!isTracking) return;

        idleTimer += Time.unscaledDeltaTime;

        if (idleTimer >= afkThresholdSeconds)
        {
            TriggerAfk();
        }
    }
    private void SubscribeToAllActions()
    {
        if (InputBridge.Actions == null) return;

        foreach (InputActionMap map in InputBridge.Actions.asset.actionMaps)
        {
            foreach (InputAction action in map.actions)
            {
                action.performed += OnAnyActionPerformed;
            }
        }
    }

    private void UnsubscribeFromAllActions()
    {
        if (InputBridge.Actions == null) return;

        foreach (InputActionMap map in InputBridge.Actions.asset.actionMaps)
        {
            foreach (InputAction action in map.actions)
            {
                action.performed -= OnAnyActionPerformed;
            }
        }
    }

    private void OnAnyActionPerformed(InputAction.CallbackContext ctx)
    {
        if (IsAfk)
        {
            CancelAfk();
            return;
        }

        ResetIdleTimer();
    }
    private void StartTracking()
    {
        isTracking = true;
        ResetIdleTimer();
        Debug.Log("[AfkHandler] AFK tracking started.");
    }

    private void StopTracking()
    {
        isTracking = false;
        Debug.Log("[AfkHandler] AFK tracking paused (real disconnect in progress).");
    }

    private void ResetIdleTimer()
    {
        idleTimer = 0f;
    }

    private void TriggerAfk()
    {
        if (IsAfk) return;

        IsAfk = true;
        isTracking = false;
        ResetIdleTimer();

        Debug.Log("[AfkHandler] Player is AFK - triggering reconnect state.");
        OnAfkDetected?.Invoke();
    }

    private void CancelAfk()
    {
        if (!IsAfk) return;

        IsAfk = false;
        ResetIdleTimer();
        StartTracking();

        Debug.Log("[AfkHandler] AFK cancelled by player input.");
        OnAfkCancelled?.Invoke();
    }
    private void HandleConnectionLost()
    {
        IsAfk = false;
        StopTracking();
    }

    private void HandleReconnected()
    {
        StartTracking();
    }
}

