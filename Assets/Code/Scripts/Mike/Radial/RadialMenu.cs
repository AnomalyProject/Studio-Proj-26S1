using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class RadialMenu : MonoBehaviour
{
    [Serializable] private struct RadialAction
    {
        public string label;
        public Sprite icon;
        public UnityEvent OnExecute;
    }

    [Header("Chat Options")]
    [SerializeField] private RadialAction[] options;

    [Header("Menu UI")]
    [SerializeField] private RadialElement elementPrefab;
    [SerializeField] private float orbitRadius = 120f;
    [SerializeField] private Color normalColor = Color.gray2;
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private float deadZone = 0.3f;

    private List<RadialElement> segments = new();
    private int currentSelection = -1;
    private Dictionary<RadialAction, float> cooldowns = new();

    private const float COOLDOWN = 3f;

    void Awake()
    {
        BuildSegments();
        gameObject.SetActive(false);
        InputBridge.OnContextChanged += OnContextChanged;
    }

    private void OnDestroy() => InputBridge.OnContextChanged -= OnContextChanged;
    private void OnContextChanged(InputBridge.InputContext context)
    {
        if (context == InputBridge.InputContext.Radial) OpenMenu();
        else if (gameObject.activeInHierarchy) ConfirmAndClose();
    }

    void Update()
    {
        UpdateInput(out Vector2 inputValue);

        if (inputValue.magnitude < deadZone)
        {
            SetHighlight(-1);
            return;
        }

        float angle = Mathf.Atan2(inputValue.y, inputValue.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;

        float step = 360f / options.Length;
        int index = Mathf.RoundToInt((angle - 90f) / step);
        index = ((index % options.Length) + options.Length) % options.Length;

        SetHighlight(index);
    }

    private void UpdateInput(out Vector2 inputValue)
    {
        Vector2 raw = InputBridge.Actions.Chat.Radial.ReadValue<Vector2>();

        if (raw.magnitude > deadZone) inputValue = raw;
        else if (Mouse.current != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector2 dir = mousePos - center;
            inputValue = dir.magnitude > 40f ? dir.normalized : Vector2.zero;
        }
        else inputValue = Vector2.zero;
    }

    [ContextMenu("Open Menu")] private void OpenMenu()
    {
        gameObject.SetActive(true);
        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Mouse.current.WarpCursorPosition(center);
    }

    [ContextMenu("Close Menu")] private void ConfirmAndClose()
    {
        gameObject.SetActive(false);

        if (currentSelection < 0 || currentSelection >= options.Length) return;

        RadialAction picked = options[currentSelection];

        // Cooldown check
        if (cooldowns.TryGetValue(picked, out float last))
        {
            if (Time.time - last < COOLDOWN) return;
        }        

        cooldowns[picked] = Time.time;
        currentSelection = -1;

        picked.OnExecute?.Invoke();
    }

    void BuildSegments()
    {
        Transform parent = gameObject.transform;
        float step = 360f / options.Length;

        foreach(var segment in segments) Destroy(segment.gameObject);

        segments.Clear();

        for (int i = 0; i < options.Length; i++)
        {
            float angle = (90f + i * step) * Mathf.Deg2Rad;
            var element = Instantiate(elementPrefab, parent);
            var rt = element.GetComponent<RectTransform>();

            rt.anchoredPosition = new Vector2(Mathf.Cos(angle) * orbitRadius, Mathf.Sin(angle) * orbitRadius);

            element.SetData(options[i].icon, options[i].label);
            element.SetBGColor(normalColor);
            segments.Add(element);
        }
    }

    void SetHighlight(int index)
    {
        currentSelection = index;

        for (int i = 0; i < segments.Count; i++)
        {
            Color segmentColor = i == index? highlightColor : normalColor;
            segments[i].SetBGColor(segmentColor);
        }
    }
}