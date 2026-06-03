using UnityEngine;
using TMPro;
using System.Collections;

public class SubtitleManager : MonoBehaviour
{
    public static SubtitleManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text speakerLabel;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Timing")]
    [SerializeField,Range(1f, 10f),Tooltip("How long the subtitle stays fully visible")]
    private float displayDuration = 4f;

    [Header("Fade")]
    [SerializeField, Range(0f, 2f)]
    private float fadeInDuration = .25f;
    [SerializeField, Range(0f, 2f)]
    private float fadeOutDuration = .4f;

    [Header("Speaker Label")]
    [SerializeField] private string speakerName = "NARRATOR";
    [SerializeField] private bool showSpeakerLabel = true;

    private Coroutine currentRoutine;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (canvasGroup != null) canvasGroup.alpha = 0f;
        if(panel != null) panel.SetActive(false);
    }
    public void ShowSubtitle(string text, System.Action onComplete = null)
    {
        if(currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(DisplayRoutine(text, onComplete));
    }
    public void HideSubtitle()
    {
        if(currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }
        StartCoroutine(FadeOut(null));
    }
    private IEnumerator DisplayRoutine(string text, System.Action onComplete)
    {
        // Set content
        if(showSpeakerLabel && speakerLabel != null)
        {
            speakerLabel.text = $"[{speakerName}]";
            speakerLabel.gameObject.SetActive(true);
        }
        else if (speakerLabel != null) speakerLabel.gameObject.SetActive(false);
        if(dialogueText != null) dialogueText.text = text;

        // Show panel
        if(panel != null) panel.SetActive(true);
        // Fade in
        yield return StartCoroutine(Fade(0f, 1f, fadeInDuration));
        // Hold
        yield return new WaitForSeconds(displayDuration);
        // Fade out
        yield return StartCoroutine(FadeOut(onComplete));
    }
    private IEnumerator FadeOut(System.Action onComplete)
    {
        float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;
        yield return StartCoroutine(Fade(startAlpha, 0f, fadeOutDuration));

        if(panel != null) panel.SetActive(false);

        currentRoutine = null;
        onComplete?.Invoke();
    }
    private IEnumerator Fade(float from, float to, float duration)
    {
        if(canvasGroup == null) yield break;
        if(duration <= 0f)
        {
            canvasGroup.alpha = to;
            yield break;
        }
        float timer = 0f;
        while(timer < duration)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, timer / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}