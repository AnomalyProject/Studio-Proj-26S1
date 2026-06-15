using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject data container for a single narrator line.
/// Holds the trigger ID, optional voice clip, a list of timed subtitle cues,
/// and whether the line should only fire once per save profile.
/// Created via Assets > Create > Narrator > DialogueEntry.
/// </summary>
[CreateAssetMenu(menuName = "Narrator/DialogueEntry", fileName = "NarrationEntry")]
public class NarrationEntry : ScriptableObject
{
    [Tooltip("Must match the string passed to NarratorManager.TriggerNarrator()")]
    [SerializeField]private string triggerID;

    [Tooltip("If true, this entry will only fire once per save profile.")]
    [SerializeField] private bool firstTimeOnly;

    [Tooltip("Optional. Played via AudioManager.PlayUI()")]
    [SerializeField] private AudioClip voiceClip;
    public AudioClip Clip => voiceClip;

    [SerializeField] private List<SubtitleEntry> subtitles = new();

    [SerializeField, Min(2f)] private float finalSubOffset;

    public string TriggerID => triggerID;
    public bool FirstTimeOnly => firstTimeOnly;
    public List<SubtitleEntry> Subtitles => subtitles;
    public AudioClip VoiceClip => voiceClip;
    public float FinalSubOffset => finalSubOffset;
}