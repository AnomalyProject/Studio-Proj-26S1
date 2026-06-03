using UnityEngine;

[System.Serializable]
public class NarratorEntry
{
    [Tooltip("Must match the string passed to NarratorManager.TriggerNarrator()")]
    public string triggerID;

    [TextArea(2, 5)]
    public string dialogueText;

    [Tooltip("Optional. Played via AudioManager.PlayUI()")]
    public AudioClip voiceClip;

    [Tooltip("If true, this entry will only fire once per save profile.")]
    public bool firstTimeOnly;
}
