using UnityEngine.InputSystem;
using UnityEngine;

/// <summary>
/// Nestoras Angelopoulos
/// 
/// Same as <see cref="InputIcon"/>but for sprites.
/// </summary>
public class ControlsScreen : MonoBehaviour
{
    [SerializeField] private GameObject keyboard;
    [SerializeField] private GameObject controller;

    private void Awake() => InputIconService.OnDeviceChanged += HandleInputDeviceChanged;
    private void OnDestroy() => InputIconService.OnDeviceChanged -= HandleInputDeviceChanged;
    private void HandleInputDeviceChanged(InputIconService.InputDeviceType deviceType)
    {
        switch (deviceType)
        {
            case InputIconService.InputDeviceType.KeyboardAndMouse:
                controller.SetActive(false);
                keyboard.SetActive(true);
                break;
            default:
                keyboard.SetActive(false);
                controller.SetActive(true);
                break;
        }
    }
}
