using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform)), ExecuteAlways]
public class ConsoleScaler : MonoBehaviour
{
    private RectTransform rectTransform;
    private RectTransform parent;
    [SerializeField] private float referenceAspectRatio = 16f / 9f;
    [SerializeField] private float maxAspectRatio = 3f;
    [SerializeField] private Vector2 offsetMin;
    [SerializeField] private Vector2 offsetMax;
    [SerializeField] private RectTransform toolbar;
    [SerializeField] private RectTransform screen;
    [SerializeField] private LayoutElement commandLine;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parent = rectTransform.parent as RectTransform;
    }

    private void LateUpdate()
    {
        // 0 at 16:9 -> 1 at maxAspectRatio
        float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((parent.rect.width / parent.rect.height - referenceAspectRatio) / (maxAspectRatio - referenceAspectRatio)));

        // Layout interpolation
        float verticalMargin = Mathf.Lerp(100f, 20f, t);
        rectTransform.offsetMin = new Vector2(offsetMin.x, verticalMargin);
        rectTransform.offsetMax = new Vector2(offsetMax.x, -verticalMargin);

        float toolbarHeight = Mathf.Lerp(50f, 30f, t);
        toolbar.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, toolbarHeight);
        screen.offsetMin = new Vector2(screen.offsetMin.x, toolbarHeight);

        // Limit width
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Min(rectTransform.rect.width, rectTransform.rect.height * maxAspectRatio));

        // Toolbar buttons sometimes are rendered wrong when changing resolutions with the console open, so we force a recalculation
        commandLine.enabled = false;
        commandLine.enabled = true;
    }
}
