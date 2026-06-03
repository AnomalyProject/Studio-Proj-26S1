using UnityEngine;

[RequireComponent(typeof(EventVolume))]
public class NarratorVolumeBridge : MonoBehaviour
{
    [Tooltip("TriggerID to fire when something enters this volume. Leave empty to ignore.")]
    [SerializeField] private string enterTriggerID;

    [Tooltip("TriggerID to fire when something exits this volume. Leave empty to ignore.")]
    [SerializeField] private string exitTriggerID;

    private void Start()
    {
        EventVolume volume = GetComponent<EventVolume>();
        
        if(!string.IsNullOrEmpty(enterTriggerID))
        {
            string id = enterTriggerID; // Capture for closure
            volume.TriggerEntered.AddListener(() => NarratorManager.Instance.TriggerNarrator(id));
        }
        if(!string.IsNullOrEmpty(exitTriggerID))
        {
            string id = exitTriggerID;
            volume.TriggerExited.AddListener(() => NarratorManager.Instance.TriggerNarrator(id));
        }
    }
}