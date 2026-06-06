using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class RadialElement : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private Image icon, bg;
    [SerializeField] Color focusedColor, normalColor;
    [SerializeField] private UnityEvent OnFocused, OnUnfocused;
    private bool isFocused = true;

    private void Awake() => SetFocus(false);
    public void SetData(Sprite icon, string label)
    {
        this.icon.sprite = icon;
        labelText.text = label;
    }
    public void SetFocus(bool focused)
    {
        if (isFocused == focused) return;
        
        isFocused = focused;
        bg.color = focused? focusedColor : normalColor;

        if (focused) OnFocused?.Invoke();
        else OnUnfocused?.Invoke();
    }
}