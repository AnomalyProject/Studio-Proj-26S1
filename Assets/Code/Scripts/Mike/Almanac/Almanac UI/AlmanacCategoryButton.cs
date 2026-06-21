using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class AlmanacCategoryButton : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI percentage, collectionName;
    [SerializeField] private Image newIcon;
    [SerializeField] private Button button;
    private AlmanacType assignedType;

    private void OnEnable() => UpdateNewIcon();

    public void Setup(AlmanacType type, Action callback)
    {
        percentage.text = AlmanacUI.GetCompletionPercentage(AlmanacRegistry.GetCategoryCompletion(type));
        collectionName.text = type.ToString();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(new UnityAction(callback));
        assignedType = type;
        UpdateNewIcon();
    }
    private void UpdateNewIcon() => newIcon.gameObject.SetActive(AlmanacRegistry.CategoryHasNewEntries(assignedType));
}