using UnityEngine;
using UnityEngine.Events;


[RequireComponent(typeof(Collider))]
public class EventVolume : MonoBehaviour
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

    private void OnTriggerEnter(Collider other)
    {
     
        if (((1 << other.gameObject.layer) & detectionLayers) != 0)
        {
            Debug.Log($"{other.name} entered {gameObject.name}");
            TriggerEntered?.Invoke();
        }
    }

    private void OnTriggerExit(Collider other)
    {
      
        if (((1 << other.gameObject.layer) & detectionLayers) != 0)
        {
            Debug.Log($"{other.name} exited {gameObject.name}");
            TriggerExited?.Invoke();
        }
    }
}