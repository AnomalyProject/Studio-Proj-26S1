using UnityEngine;
using UnityEngine.Events;
using PurrNet;

[RequireComponent(typeof(Collider))]
public class EventVolume : NetworkBehaviour

{
    [Header("Detection Settings")]
    [Tooltip("Only objects on these layers will fire the events.")]
    [SerializeField] private LayerMask detectionLayers;

    [Header("Events")]
    public UnityEvent TriggerEntered;
    public UnityEvent TriggerExited;

    private Collider col;

    private void Awake()
    {

        col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    [ObserversRpc] private void InvokeTrigger_ObserversRPC(bool enter)
    {
        if (enter) TriggerEntered?.Invoke();
        else TriggerExited?.Invoke();
    }
    private void OnTriggerEnter(Collider other)
    {

        if (!isServer) return;

        if (((1 << other.gameObject.layer) & detectionLayers) != 0)
        {
            Debug.Log($"{other.name} entered {gameObject.name}");
            InvokeTrigger_ObserversRPC(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!isServer) return;

        if (((1 << other.gameObject.layer) & detectionLayers) != 0)
        {
            Debug.Log($"{other.name} exited {gameObject.name}");
            InvokeTrigger_ObserversRPC(false);
        }
    }
}