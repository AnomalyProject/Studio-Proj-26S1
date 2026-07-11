using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine;

/// <summary>
/// Nestoras Angelopoulos
/// 
/// A UI script that converts an input action name to the appropriate icon based on the player's current input device.
/// </summary>
[RequireComponent(typeof(Image))]
public class InputIcon : MonoBehaviour
{
    [SerializeField] private string action;
    [SerializeField] private string overrideIconName;
    private InputAction inputAction;
    private Image icon;
    [SerializeField] private bool controlOpacity = true;

    private void Start()
    {
        icon = GetComponent<Image>();
        ReloadIcon();

        InputIconService.OnDeviceChanged += HandleInputDeviceChanged;
    }

    [ContextMenu("Reload Icon")]
    private void ReloadIcon()
    {
        inputAction = InputBridge.Actions.FindAction(action);
        if (inputAction != null) icon.sprite = InputIconService.GetIcon(inputAction);
    }
    private void OnDestroy() => InputIconService.OnDeviceChanged -= HandleInputDeviceChanged;
    private void HandleInputDeviceChanged(InputIconService.InputDeviceType deviceType)
    {
        if (string.IsNullOrEmpty(overrideIconName)) icon.sprite = InputIconService.GetIcon(inputAction);
        else icon.sprite = InputIconService.GetIcon(overrideIconName);

        if (!controlOpacity) return;
        if (icon.sprite == null) icon.color = Color.clear;
        else icon.color = Color.white;
    }
}
