using System.Collections.Generic;
using UnityEngine;

public class NarratorManager : MonoBehaviour
{
    public static NarratorManager Instance { get; private set; }

    private const string SaveKey = "NarratorFiredIDs";

    private bool isPlaying = false;
    private HashSet<string> firedOnceIDs = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadFiredFlags();
    }

    public void TriggerNarration(NarrationEntry entry)
    {
        // Null check must come before any access to entry's properties
        if (entry == null)
        {
            Debug.LogWarning("[NarratorManager] TriggerNarration called with a null entry.");
            return;
        }

        if (isPlaying)
        {
            Debug.Log($"[NarratorManager] Ignoring '{entry.TriggerID}' — line already playing.");
            return;
        }

        if (entry.FirstTimeOnly && firedOnceIDs.Contains(entry.TriggerID))
        {
            Debug.Log($"[NarratorManager] '{entry.TriggerID}' already fired (firstTimeOnly). Skipping.");
            return;
        }

        PlayEntry(entry);
    }

    public void ResetAllFlags()
    {
        firedOnceIDs.Clear();
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
        Debug.Log("[NarratorManager] All narrator flags reset.");
    }

    private void PlayEntry(NarrationEntry entry)
    {
        isPlaying = true;

        if (entry.FirstTimeOnly)
        {
            firedOnceIDs.Add(entry.TriggerID);
            SaveFiredFlags();
        }

        if (SubtitleManager.Instance != null)
        {
            // Pass OnLineFinished as the callback so isPlaying resets when subtitles finish
            SubtitleManager.Instance.ShowSubtitle(entry.Subtitles, entry.FinalSubOffset, OnLineFinished);
        }
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

    private void OnLineFinished() => isPlaying = false;

    private void SaveFiredFlags()
    {
        PlayerPrefs.SetString(SaveKey, string.Join(",", firedOnceIDs));
        PlayerPrefs.Save();
    }

    private void LoadFiredFlags()
    {
        string saved = PlayerPrefs.GetString(SaveKey, "");
        firedOnceIDs = string.IsNullOrEmpty(saved)
            ? new HashSet<string>()
            : new HashSet<string>(saved.Split(','));
    }
}