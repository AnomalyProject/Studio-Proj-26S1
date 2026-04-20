using UnityEngine;
using UnityEngine.Events;


[RequireComponent(typeof(Collider))]
public class EventVolume : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private string filterTag = "";

    [Header("Events")]
    public UnityEvent OnTriggerEntered;
    public UnityEvent OnTriggerExited;

    private Collider col;

    private void Awake()
    {
    
        col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
    
        if (!string.IsNullOrEmpty(filterTag) && !other.CompareTag(filterTag))
            return;

        Debug.Log($"{other.name} entered {gameObject.name}");
        OnTriggerEntered?.Invoke();
    }

    private void OnTriggerExit(Collider other)
    {
   
        if (!string.IsNullOrEmpty(filterTag) && !other.CompareTag(filterTag))
            return;

        Debug.Log($"{other.name} exited {gameObject.name}");
        OnTriggerExited?.Invoke();
    }
}