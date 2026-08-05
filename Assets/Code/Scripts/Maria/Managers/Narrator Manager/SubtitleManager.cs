using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Singleton. Sequences and displays timed subtitle cues for the narrator system.
/// Receives a list of <see cref="SubtitleEntry"/> cues from <see cref="NarratorManager"/>,
/// advances through them as elapsed time passes each cue's timestamp, then fades the panel
/// out after a final offset and fires the onComplete callback to release the playback lock.
/// </summary>
public class SubtitleManager : MonoBehaviour
{
    #region Singleton :3
    public static SubtitleManager Instance { get; private set; }
    #endregion

    #region Serialized Fields
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
    #endregion

    #region Private Fields
    private Coroutine currentRoutine;
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

        if (canvasGroup != null) canvasGroup.alpha = 0f;
        if (panel != null) panel.SetActive(false);
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Begins sequencing through the given subtitle cues.
    /// If a sequence is already running it is stopped and replaced immediately.
    /// <paramref name="onComplete"/> is fired after the panel finishes fading out.
    /// </summary>
    public void ShowSubtitle(List<SubtitleEntry> subtitles, float finalSubOffset, System.Action onComplete = null)
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(SubtitleSequenceRoutine(subtitles, finalSubOffset, onComplete));
    }

    /// <summary>
    /// Immediately stops the current sequence and fades the panel out.
    /// </summary>
    public void HideSubtitle(System.Action onComplete = null)
    {
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }

        StartCoroutine(FadeOut(onComplete));
    }
    #endregion

    #region Private Methods
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

        // Sort defensively — authoring order in the Inspector is not guaranteed.
        List<SubtitleEntry> sorted = new List<SubtitleEntry>(subtitles);
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

    /// <summary>
    /// Updates the subtitle panel text. Hides the speaker label if
    /// <see cref="showSpeakerLabel"/> is false or the cue has no speaker name.
    /// </summary>
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

    /// <summary>
    /// Lerps the <see cref="canvasGroup"/> alpha from <paramref name="from"/> to <paramref name="to"/>
    /// over <paramref name="duration"/> seconds. If duration is zero or less, the target alpha is applied instantly.
    /// A final assignment after the loop guarantees the target value is reached exactly,
    /// since deltaTime accumulation can fall slightly short.
    /// </summary>
    /// <param name="from">Alpha value to start from (0 = transparent, 1 = fully visible).</param>
    /// <param name="to">Alpha value to end at.</param>
    /// <param name="duration">Length of the fade in seconds.</param>
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
    #endregion
}