using UnityEngine;

/// <summary>
/// A single timed subtitle cue within a <see cref="NarrationEntry"/>.
/// Timestamp is relative to the start of the narration line — the SubtitleManager
/// displays this cue once elapsed playback time reaches it.
/// </summary>
[System.Serializable] public struct SubtitleEntry
{
    [SerializeField, Min(0)] private float timestamp;
    [SerializeField] private string speakerName;
    [SerializeField, TextArea(2, 5)] private string dialogueText;

    public float TimeStamp => timestamp;
    public string DialogueText => dialogueText;
    public string SpeakerName => speakerName;
}