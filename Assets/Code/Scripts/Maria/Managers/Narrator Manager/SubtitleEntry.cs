using UnityEngine;

[System.Serializable] public struct SubtitleEntry
{
    [SerializeField, Min(0)] private float timestamp;
    [SerializeField] private string speakerName;
    [SerializeField, TextArea(2, 5)] private string dialogueText;

    public float TimeStamp => timestamp;
    public string DialogueText => dialogueText;
    public string SpeakerName => speakerName;
}