using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SubtitleManager : MonoBehaviour
{
    public static SubtitleManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text speakerLabel;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Fade Settings")]
    [SerializeField, Range(0f, 2f)] private float fadeInDuration = 0.2f;
    [SerializeField, Range(0f, 2f)] private float fadeOutDuration = 0.35f;

    [Header("Speaker Label")]
    [SerializeField] private bool showSpeakerLabel = true;

    private Coroutine currentRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (canvasGroup != null) canvasGroup.alpha = 0f;
        if (panel != null) panel.SetActive(false);
    }

    public void ShowSubtitle(List<SubtitleEntry> subtitles, float finalSubOffset, System.Action onComplete = null)
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(SubtitleSequenceRoutine(subtitles, finalSubOffset, onComplete));
    }

    public void HideSubtitle(System.Action onComplete = null)
    {
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }

        StartCoroutine(FadeOut(onComplete));
    }

    private IEnumerator SubtitleSequenceRoutine(List<SubtitleEntry> subtitles, float finalSubOffset, System.Action onComplete)
    {
        if (subtitles == null || subtitles.Count == 0)
        {
            onComplete?.Invoke();
            yield break;
        }

        // Show panel
        if (panel != null) panel.SetActive(true);

        float elapsed = 0f;
        int index = 0;
        bool panelVisible = false;

        // Sort by timestamp defensively — authoring order may vary
        var sorted = new List<SubtitleEntry>(subtitles);
        sorted.Sort((a, b) => a.TimeStamp.CompareTo(b.TimeStamp));

        while (index < sorted.Count)
        {
            SubtitleEntry cue = sorted[index];

            // Wait until this cue's timestamp is reached
            if (elapsed < cue.TimeStamp)
            {
                elapsed += Time.deltaTime;
                yield return null;
                continue;
            }

            // Set text content
            SetContent(cue.SpeakerName, cue.DialogueText);

            // Fade in on first cue only
            if (!panelVisible)
            {
                yield return StartCoroutine(Fade(0f, 1f, fadeInDuration));
                panelVisible = true;
            }

            index++;
            yield return null;
            elapsed += Time.deltaTime;
        }

        // All cues shown — wait for finalSubOffset then fade out
        float offsetTimer = 0f;
        while (offsetTimer < finalSubOffset)
        {
            offsetTimer += Time.deltaTime;
            yield return null;
        }

        HideSubtitle(onComplete);
    }

    private void SetContent(string speaker, string dialogue)
    {
        if (dialogueText != null)
            dialogueText.text = dialogue;

        if (speakerLabel != null)
        {
            bool hasSpeaker = showSpeakerLabel && !string.IsNullOrEmpty(speaker);
            speakerLabel.gameObject.SetActive(hasSpeaker);
            if (hasSpeaker) speakerLabel.text = $"[{speaker.ToUpper()}]";
        }
    }

    private IEnumerator FadeOut(System.Action onComplete)
    {
        float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;
        yield return StartCoroutine(Fade(startAlpha, 0f, fadeOutDuration));

        if (panel != null) panel.SetActive(false);
        onComplete?.Invoke();
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (canvasGroup == null) yield break;

        if (duration <= 0f)
        {
            canvasGroup.alpha = to;
            yield break;
        }

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, timer / duration);
            yield return null;
        }

        canvasGroup.alpha = to;
    }
}