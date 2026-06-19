using System.Collections;
using UnityEngine.UI;
using UnityEngine;

public class BlackFadeManager : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    public static void ResetInstanceReference() => Instance = null;

    public static BlackFadeManager Instance;
    static private Image overlay;

    [SerializeField] private float transitionTime = 0.5f;
    public float TransitionTime => transitionTime;
    private float targetAlpha = 1f;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        overlay = GetComponentInChildren<Image>();
        DontDestroyOnLoad(gameObject);
    }

    public void FadeIn()
    {
        if (Mathf.Approximately(targetAlpha, 1f)) return;
        StopAllCoroutines();
        SetAlpha(0f);
        StartCoroutine(Fade(1f));
    }

    private void OnLevelWasLoaded(int level) => FadeOut();
    public void FadeOut()
    {
        if (Mathf.Approximately(targetAlpha, 0f)) return;
        StopAllCoroutines();
        SetAlpha(1f);
        StartCoroutine(Fade(0f));
    }

    private IEnumerator Fade(float targetAlpha)
    {
        this.targetAlpha = targetAlpha;
        Color color = overlay.color;
        float startAlpha = overlay.color.a;

        while (!Mathf.Approximately(overlay.color.a, targetAlpha))
        {
            color.a = Mathf.MoveTowards(overlay.color.a, targetAlpha, Time.unscaledDeltaTime / transitionTime);
            overlay.color = color;
            yield return null;
        }
        color.a = targetAlpha;
        overlay.color = color;
    }
    public void SetAlpha(float alpha)
    {
        StopAllCoroutines();
        Color color = overlay.color;
        color.a = alpha;
        overlay.color = color;
    }
}
