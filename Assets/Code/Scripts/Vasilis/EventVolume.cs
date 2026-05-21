using UnityEngine;
using UnityEngine.Events;
using PurrNet;

[RequireComponent(typeof(Collider))]
public class EventVolume : NetworkBehaviour

{
    [SerializeField, Tooltip("Attached events happen only once.")] private bool doOnce;
    [Header("Detection Settings")]
    [Tooltip("Only objects on these layers will fire the events.")]
    [SerializeField] private LayerMask detectionLayers;

    [Header("Events")]
    public UnityEvent TriggerEntered;
    public UnityEvent TriggerExited;

    private Collider col;
    private bool didEnter = false, didExit = false;

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
        if (doOnce && didEnter) return;

        if (((1 << other.gameObject.layer) & detectionLayers) != 0)
        {
            Debug.Log($"{other.name} entered {gameObject.name}");
            InvokeTrigger_ObserversRPC(true);
            didEnter = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!isServer) return;
        if(doOnce && didExit) return;

        if (((1 << other.gameObject.layer) & detectionLayers) != 0)
        {
            Debug.Log($"{other.name} exited {gameObject.name}");
            InvokeTrigger_ObserversRPC(false);
            didExit = true;
        }
    }
}