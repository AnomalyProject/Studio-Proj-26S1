using System;
using TMPro;
using UnityEngine;
using static InputBridge;

public class AlmanacUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI totalCompletionText, activeCategoryText;
    [SerializeField] private AlmanacCategoryButton categoryButtonPrefab;
    [SerializeField] private AlmanacEntryUI entryPrefab;
    [SerializeField] private Transform categoryPanel, entryPanel;
    [SerializeField] AudioClip openClip, categorySelectionClip;
    private bool hasOpenedPanel;

    private void Awake()
    {
        OnContextChanged += ContextChangeHandle;

        foreach (var category in Enum.GetValues(typeof(AlmanacType))) // Setup category buttons
        {
            AlmanacType type = (AlmanacType)category;
            AlmanacCategoryButton button = Instantiate(categoryButtonPrefab, categoryPanel);
            button.Setup(type, () => OpenCollection(type));
        }
        activeCategoryText.text = "";
        ContextChangeHandle(CurrentContext);
    }
    private void OnDestroy() => OnContextChanged -= ContextChangeHandle;

    private void OnEnable()
    {
        totalCompletionText.text = $"Total Completion {GetCompletionPercentage(AlmanacRegistry.GetTotalCompletion())}";
        hasOpenedPanel = false;
        AudioManager.Instance.PlaySFX(openClip);
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
    }

    private void ClearOpenEntries()
    {
        for (int i = 0; i < entryPanel.childCount; i++) Destroy(entryPanel.GetChild(i).gameObject);
    }
    public static string GetCompletionPercentage(float completion01)
    {
        float completion = completion01 * 100;
        return $"{(int)Mathf.Clamp(completion, 0, 100)}%";
    }
    private void ContextChangeHandle(InputContext ctx) => gameObject.SetActive(ctx == InputContext.Almanac);
}