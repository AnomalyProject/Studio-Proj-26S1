using UnityEngine.Events;
using UnityEngine;

/// <summary>
/// Thin MonoBehaviour bridge that exposes narrator playback to UnityEvents and other
/// components via the Inspector. Assign a <see cref="NarrationEntry"/> and call
/// <see cref="PlayNarration"/> from a UnityEvent, trigger, or any other script.
/// </summary>
public class NarrationEvent : MonoBehaviour
{
    [SerializeField] private bool triggerOnce = false;
    [SerializeField] private NarrationEntry entry;
    [SerializeField] private UnityEvent onNarrationFinished;

    public void PlayNarration()
    {
        if (!NarratorManager.Instance || entry == null) return;

        if (NarratorManager.Instance.TryTriggerNarration(entry) && triggerOnce)
        {
            NarratorManager.Instance.AddIgnoredEntry(entry);
            Invoke(nameof(OnNarrationFinsihed), entry.Clip.length);
        }
    }

    private void OnNarrationFinsihed() => onNarrationFinished?.Invoke();
}