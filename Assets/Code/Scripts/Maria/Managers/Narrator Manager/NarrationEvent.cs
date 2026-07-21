using UnityEngine.Events;
using UnityEngine;

/// <summary>
/// Thin MonoBehaviour bridge that exposes narrator playback to UnityEvents and other
/// components via the Inspector. Assign a <see cref="NarrationEntry"/> and call
/// <see cref="PlayNarration"/> from a UnityEvent, trigger, or any other script.
/// </summary>
public class NarrationEvent : MonoBehaviour
{
    #region Serialized Fields
    [SerializeField] private bool triggerOnce = false;
    [SerializeField] private NarrationEntry entry;
    [SerializeField] private UnityEvent onNarrationFinished;
    #endregion

    #region Private Field
    private static readonly System.Collections.Generic.Dictionary<string,int> s_lastTriggeredFrame = new();
    #endregion

    #region Public Methods
    public void PlayNarration()
    {
        if (!NarratorManager.Instance || entry == null) return;

        if (entry != null)
        {
            if (s_lastTriggeredFrame.TryGetValue(entry.TriggerID, out int f) && f == Time.frameCount)
            {
                Debug.Log($"[NarrationEvent] Ignoring duplicate PlayNarration same-frame for {entry.TriggerID}");
                return;
            }
            s_lastTriggeredFrame[entry.TriggerID] = Time.frameCount;
        }

        // Always call through TryTriggerNarration. Manager will queue if needed and
        // invoke the provided callback when the narration actually finishes.
        NarratorManager.Instance.TryTriggerNarration(entry, OnNarrationFinsihed, triggerOnce);
    }
    #endregion

    #region Private Method
    private void OnNarrationFinsihed() => onNarrationFinished?.Invoke();
    #endregion
}