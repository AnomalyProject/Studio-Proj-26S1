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
    private InputAction inputAction;
    private Image icon;

    private void Awake()
    {
        icon = GetComponent<Image>();
        inputAction = InputBridge.Actions.FindAction(action);
        icon.sprite = InputIconService.GetIcon(inputAction);

        InputIconService.OnDeviceChanged += HandleInputDeviceChanged;
    }
    private void OnDestroy() => InputIconService.OnDeviceChanged -= HandleInputDeviceChanged;
    private void HandleInputDeviceChanged(InputIconService.InputDeviceType deviceType) => icon.sprite = InputIconService.GetIcon(inputAction);
}
