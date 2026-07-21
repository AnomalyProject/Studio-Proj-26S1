using System.Collections.Generic;
using System;
using UnityEngine;

/// <summary>
/// Singleton. Manages playback of narrator lines in response to gameplay events.
/// Enforces a one-line-at-a-time lock so narration never overlaps or interrupts gameplay.
/// Delegates subtitle display to <see cref="SubtitleManager"/> and audio to <see cref="AudioManager"/>.
/// FirstTimeOnly entries are tracked in <see cref="SaveData.narratorFiredIDs"/> and persisted
/// immediately via <see cref="SaveSystem.QuickSave"/> so flags survive crashes mid-playback.
/// </summary>
public class NarratorManager : MonoBehaviour
{
    #region Singleton :D
    public static NarratorManager Instance { get; private set; }
    #endregion

    #region Private Fields
    private bool isPlaying = false;

    // Now include the triggerOnceRequested flag with queued items
    private Queue<(NarrationEntry entry, Action callback, bool triggerOnceRequested)> queuedEntries = new Queue<(NarrationEntry, Action, bool)>();

    // Direct reference into CurrentSave — no local copy, so there's no risk of the
    // in-memory set drifting out of sync with the save data before a QuickSave.
    private HashSet<string> FiredIDs => RefrenceManager.CurrentSave.narratorFiredIDs;
    private HashSet<string> IgnoredIDs = new();
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        // No load call needed here — RefrenceManager.LoadLastSave() runs at
        // AfterAssembliesLoaded, which is guaranteed to fire before any scene Awake.
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Attempts to play the narrator line associated with the given entry.
    /// If another line is playing the manager will enqueue this entry.
    /// Returns true if the line began playing immediately; false if it was queued or rejected.
    /// </summary>
    public bool TryTriggerNarration(NarrationEntry entry, Action onFinished = null, bool triggerOnceRequested = false)
    {
        if (entry == null)
        {
            Debug.LogWarning("[NarratorManager] TriggerNarration called with a null entry.");
            return false;
        }

        // Already globally ignored
        if (IgnoredIDs.Contains(entry.TriggerID)) return false;

        if (entry.FirstTimeOnly && FiredIDs.Contains(entry.TriggerID))
        {
            Debug.Log($"[NarratorManager] '{entry.TriggerID}' already fired (firstTimeOnly). Skipping.");
            return false;
        }

        // If a line is currently playing, enqueue it (with dedupe logic for trigger-once)
        if (isPlaying)
        {
            // If a queued item with the same TriggerID already exists...
            foreach (var queued in queuedEntries)
            {
                if (queued.entry != null && queued.entry.TriggerID == entry.TriggerID)
                {
                    // If the caller requested "trigger once", skip adding a duplicate.
                    if (triggerOnceRequested)
                    {
                        Debug.Log($"[NarratorManager] Duplicate queued '{entry.TriggerID}' skipped due to trigger-once request.");
                        return false;
                    }

                    // Otherwise allow duplicates
                    break;
                }
            }

            EnqueNarrationEntry(entry, onFinished, triggerOnceRequested);
            return false;
        }

        // Not playing — start immediately.
        PlayEntry(entry, onFinished, triggerOnceRequested);

        // If the caller asked for trigger-once and we started playback immediately,
        // mark it ignored right away so other callers cannot enqueue duplicates.
        if (triggerOnceRequested) AddIgnoredEntry(entry);

        return true;
    }

    /// <summary>
    /// Clears all firstTimeOnly fired flags and immediately quick-saves.
    /// Useful for new game setup or debug resets.
    /// </summary>
    public void ResetAllFlags()
    {
        FiredIDs.Clear();
        IgnoredIDs.Clear();
        SaveSystem.QuickSave(RefrenceManager.CurrentSave);
        Debug.Log("[NarratorManager] All narrator flags reset.");
    }

    public void EnqueNarrationEntry(NarrationEntry entry, Action onLineFinished, bool triggerOnceRequested = false)
    {
        if (entry == null) return;

        // Ignore if already globally ignored
        if (IgnoredIDs.Contains(entry.TriggerID)) return;

        // Prevent duplicate queued entries for the same TriggerID if the caller requested trigger-once
        if (triggerOnceRequested)
        {
            foreach (var queued in queuedEntries)
            {
                if (queued.entry != null && queued.entry.TriggerID == entry.TriggerID)
                {
                    Debug.Log($"[NarratorManager] Skipping enqueue of duplicate '{entry.TriggerID}' due to trigger-once.");
                    return;
                }
            }
        }

        queuedEntries.Enqueue((entry, onLineFinished, triggerOnceRequested));
    }

    public void AddIgnoredEntry(NarrationEntry entry)
    {
        if (!IgnoredIDs.Contains(entry.TriggerID)) IgnoredIDs.Add(entry.TriggerID);
    }
    #endregion

    #region Private Methods
    // PlayEntry now accepts the trigger-once request so queued entries can apply it when they actually start.
    private void PlayEntry(NarrationEntry entry, Action onFinished, bool triggerOnceRequested)
    {
        isPlaying = true;

        if (entry.FirstTimeOnly)
        {
            // Persist immediately so the flag survives a crash or force-quit mid-playback.
            FiredIDs.Add(entry.TriggerID);
            SaveSystem.QuickSave(RefrenceManager.CurrentSave);
        }

        // If this queued item requested trigger-once, mark ignored now so further triggers are blocked
        if (triggerOnceRequested && !IgnoredIDs.Contains(entry.TriggerID))
        {
            IgnoredIDs.Add(entry.TriggerID);
        }

        // Wrap the subtitle completion so we both release the lock and invoke the caller callback
        Action wrappedComplete = () =>
        {
            OnLineFinished();
            onFinished?.Invoke();
        };

        if (SubtitleManager.Instance != null) SubtitleManager.Instance.ShowSubtitle(entry.Subtitles, entry.FinalSubOffset, wrappedComplete);
        else
        {
            Debug.LogWarning("[NarratorManager] SubtitleManager instance not found.");
            wrappedComplete();
        }

        if (entry.VoiceClip != null)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUI(entry.VoiceClip);
            else Debug.LogWarning("[NarratorManager] AudioManager instance not found — cannot play voice clip.");
        }
    }

    // Called by SubtitleManager when the last cue finishes, releasing the playback lock.
    private void OnLineFinished()
    {
        isPlaying = false;
        if (queuedEntries.Count > 0)
        {
            var item = queuedEntries.Dequeue();
            // Play the dequeued entry and pass along its trigger-once request and callback
            PlayEntry(item.entry, item.callback, item.triggerOnceRequested);
        }
    }
    #endregion
}