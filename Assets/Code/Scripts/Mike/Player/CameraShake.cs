using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [SerializeField] private Transform target;

    [Header("Shake Config")]
    [SerializeField, Min(0f)] private float amplitude = 0.2f;
    [SerializeField, Min(0f)] private float frequency = 25f;

    [Header("Fading")]
    [SerializeField, Min(0f)] private float fadeInDuration = 0.25f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.5f;
    [SerializeField] private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    public bool IsShaking => targetBlend > 0f || currentBlend > 0f;

    private Vector3 initialLocalPosition;
    private float noiseTime;

    private float currentBlend;
    private float targetBlend;

    private void Awake()
    {
        if (!target) target = transform;

        initialLocalPosition = target.localPosition;
        enabled = false;
    }

    [ContextMenu("Start Shake")]
    public void StartShake()
    {
        targetBlend = 1f;
        enabled = true;
    }

    [ContextMenu("Stop Shake")]
    public void StopShake()
    {
        targetBlend = 0f;
        enabled = true;
    }

    public void StartShake(float duration)
    {
        StartShake();
        Invoke(nameof(StopShake), duration);
    }

    public void StopImmediately()
    {
        currentBlend = 0f;
        targetBlend = 0f;
        target.localPosition = initialLocalPosition;
        enabled = false;
    }

    private void LateUpdate() => UpdateBlend();
    private void UpdateBlend()
    {
        bool fadingIn = targetBlend > currentBlend;

        float fadeDuration = fadingIn? fadeInDuration : fadeOutDuration;
        AnimationCurve fadeCurve = fadingIn? fadeInCurve : fadeOutCurve;

        if (fadeDuration > 0f) currentBlend = Mathf.MoveTowards(currentBlend, targetBlend, Time.deltaTime / fadeDuration);
        else currentBlend = targetBlend;

        if (currentBlend <= 0f && targetBlend <= 0f)
        {
            StopImmediately();
            return;
        }

        float strength = fadeCurve.Evaluate(fadingIn ? currentBlend : 1f - currentBlend);
        noiseTime += Time.deltaTime * frequency;

        Vector2 noise = new(Mathf.PerlinNoise(noiseTime, 0f), Mathf.PerlinNoise(0f, noiseTime));
        Vector3 offset = (Vector3)(noise * (amplitude * strength));
        target.localPosition = initialLocalPosition + offset;
    }
}