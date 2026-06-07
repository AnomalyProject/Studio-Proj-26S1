using System.Collections.Generic;
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
    public static NarratorManager Instance { get; private set; }

    private bool isPlaying = false;

    // Direct reference into CurrentSave — no local copy, so there's no risk of the
    // in-memory set drifting out of sync with the save data before a QuickSave.
    private HashSet<string> FiredIDs => RefrenceManager.CurrentSave.narratorFiredIDs;
    private HashSet<string> IgnoredIDs = new();

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

    /// <summary>
    /// Attempts to play the narrator line associated with the given entry.
    /// Silently ignored if another line is already playing, the entry is null,
    /// or the entry is firstTimeOnly and has already fired this save.
    /// </summary>
    public bool TryTriggerNarration(NarrationEntry entry)
    {
        if (entry == null)
        {
            Debug.LogWarning("[NarratorManager] TriggerNarration called with a null entry.");
            return false;
        }

        if (isPlaying)
        {
            Debug.Log($"[NarratorManager] Ignoring '{entry.TriggerID}' — line already playing.");
            return false;
        }

        if (IgnoredIDs.Contains(entry.TriggerID)) return false;

        if (entry.FirstTimeOnly && FiredIDs.Contains(entry.TriggerID))
        {
            Debug.Log($"[NarratorManager] '{entry.TriggerID}' already fired (firstTimeOnly). Skipping.");
            return false;
        }

        PlayEntry(entry);
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

    private void PlayEntry(NarrationEntry entry)
    {
        isPlaying = true;

        if (entry.FirstTimeOnly)
        {
            // Persist immediately so the flag survives a crash or force-quit mid-playback.
            FiredIDs.Add(entry.TriggerID);
            SaveSystem.QuickSave(RefrenceManager.CurrentSave);
        }

        if (SubtitleManager.Instance != null)
            SubtitleManager.Instance.ShowSubtitle(entry.Subtitles, entry.FinalSubOffset, OnLineFinished);
        else
        {
            Debug.LogWarning("[NarratorManager] SubtitleManager instance not found.");
            OnLineFinished();
        }

        if (entry.VoiceClip != null)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayUI(entry.VoiceClip);
            else
                Debug.LogWarning("[NarratorManager] AudioManager instance not found — cannot play voice clip.");
        }
    }

    // Called by SubtitleManager when the last cue finishes, releasing the playback lock.
    private void OnLineFinished() => isPlaying = false;
    public void AddIgnoredEntry(NarrationEntry entry)
    {
        if(!IgnoredIDs.Contains(entry.TriggerID)) IgnoredIDs.Add(entry.TriggerID);
    }
}