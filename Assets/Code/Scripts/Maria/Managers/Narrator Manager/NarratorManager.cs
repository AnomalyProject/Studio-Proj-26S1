using System.Collections.Generic;
using UnityEngine;

public class NarratorManager : MonoBehaviour
{
    public static NarratorManager Instance { get; private set; }

    [SerializeField] private NarratorDatabase database;

    // Key used to persist fired-once IDs across sessions
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
    public void TriggerNarrator(string triggerID)
    {
        if (isPlaying)
        {
            Debug.Log($"[NarratorManager] Ignoring '{triggerID}' - line already playing.");
            return;
        }
        NarratorEntry entry = database.GetEntry(triggerID);
        if (entry == null)
        {
            Debug.LogWarning($"[NarratorManager] No entry found for triggerID: '{triggerID}'");
            return;
        }
        if (entry.firstTimeOnly && firedOnceIDs.Contains(triggerID))
        {
            Debug.Log($"[NarratorManager] '{triggerID}' already fired (firstTimeOnly). Skipping.");
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
    private void PlayEntry(NarratorEntry entry)
    {
        isPlaying = true;
        if (entry.firstTimeOnly)
        {
            firedOnceIDs.Add(entry.triggerID);
            SaveFiredFlags();
        }
        if(SubtitleManager.Instance != null)
        {
            SubtitleManager.Instance.ShowSubtitle(entry.dialogueText, OnLineFinished);
        }
        else
        {
            Debug.LogWarning("[NarratorManager] SubtitleManager instance not found.");
            OnLineFinished();
        }
        if(entry.voiceClip != null)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUI(entry.voiceClip);
            else Debug.LogWarning("[NarratorManager] AudioManager instance not found — cannot play voice clip.");
        }
    }
    private void OnLineFinished() => isPlaying = false;
    private void SaveFiredFlags()
    {
        PlayerPrefs.SetString(SaveKey, string.Join(",",firedOnceIDs));
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