using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static InputBridge;

public class AlmanacUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI totalCompletionText, activeCategoryText;
    [SerializeField] private AlmanacCategoryButton categoryButtonPrefab;
    [SerializeField] private AlmanacEntryUI entryPrefab;
    [SerializeField] private Transform categoryPanel, entryPanel;
    [SerializeField] AudioClip openClip, categorySelectionClip;
    [SerializeField] Scrollbar scrollbar;
    private bool hasOpenedPanel;
    readonly List<AlmanacCategoryButton> categoryButtons = new List<AlmanacCategoryButton>();

    private void Awake()
    {
        OnContextChanged += ContextChangeHandle;

        foreach (var category in Enum.GetValues(typeof(AlmanacType))) // Setup category buttons
        {
            AlmanacType type = (AlmanacType)category;
            AlmanacCategoryButton button = Instantiate(categoryButtonPrefab, categoryPanel);
            button.Setup(type, () => OpenCollection(type));
            categoryButtons.Add(button);
        }
        activeCategoryText.text = "";
        ContextChangeHandle(CurrentContext);
    }
    private void OnDestroy() => OnContextChanged -= ContextChangeHandle;

    private async void OnEnable()
    {
        totalCompletionText.text = $"Total Completion {GetCompletionPercentage(AlmanacRegistry.GetTotalCompletion())}";
        hasOpenedPanel = false;
        AudioManager.Instance.PlaySFX(openClip);

        await Awaitable.EndOfFrameAsync();
        if (categoryButtons.Count > 0) categoryButtons[0].Select();
    }

    private void OnDisable()
    {
        if (hasOpenedPanel) AlmanacRegistry.MarkAllViewed();
        ClearOpenEntries();
    }

    private void OpenCollection(AlmanacType type)
    {
        AudioManager.Instance.PlaySFX(categorySelectionClip);
        activeCategoryText.text = type.ToString();
        hasOpenedPanel = true;

        ClearOpenEntries();

        foreach (var entry in AlmanacRegistry.GetEntriesByCategory(type))
        {
            AlmanacEntryUI entryUI = Instantiate(entryPrefab, entryPanel);
            entryUI.Setup(entry);
        }

        scrollbar.value = 1;
        scrollbar.Select();
    }

    private void ClearOpenEntries()
    {
        for (int i = 0; i < entryPanel.childCount; i++) Destroy(entryPanel.GetChild(i).gameObject);
    }
    public static string GetCompletionPercentage(float completion01)
    {
        float completion = completion01 * 100;
        return $"{Mathf.RoundToInt(Mathf.Clamp(completion, 0, 100))}%";
    }
    private void ContextChangeHandle(InputContext ctx)
    {
        gameObject.SetActive(ctx == InputContext.Almanac);

        if (ctx == InputContext.Almanac) Actions.UI.Cancel.started += OnCancel;
        else Actions.UI.Cancel.started -= OnCancel;
    }

    private void OnCancel(InputAction.CallbackContext context)
    {
        if(EventSystem.current.currentSelectedGameObject == scrollbar.gameObject) categoryButtons[0].Select();
        else SetContext(InputContext.Player);
    }
}