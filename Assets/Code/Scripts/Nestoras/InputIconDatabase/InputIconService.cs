using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Switch;
using UnityEngine.InputSystem;
using UnityEngine;
using System;

/// <summary>
/// Nestoras Angelopoulos
/// 
/// Observes latest input and broadcasts device changes to subscribed scripts.
/// Also provides a method for retrieving the correct icon for a given input action based on the current device.
/// </summary>
public static class InputIconService
{
    [Serializable] public enum InputDeviceType
    {
        KeyboardAndMouse,
        DualShock,
        Switch,
        Gamepad
    }

    public static event Action<InputDeviceType> OnDeviceChanged;
    private static InputDevice currentDevice;

    private static InputIconDatabase iconDatabase;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    private static void Initialize()
    {
        iconDatabase = Resources.Load<InputIconDatabase>("InputIcons/InputIconDatabase");
        InputSystem.onActionChange += OnActionChange;
    }

    private static void OnActionChange(object obj, InputActionChange change)
    {
        if (change == InputActionChange.ActionStarted)
        {
            InputAction action = obj as InputAction;
            if (action == null) return;

            InputDevice device = action.activeControl?.device;
            if (device == null) return;

            switch (device)
            {
                case Keyboard:
                case Mouse:
                    iconDatabase.BuildLookup("Keyboard&Mouse");
                    if (currentDevice != device) OnDeviceChanged?.Invoke(InputDeviceType.KeyboardAndMouse);
                    break;
                case DualShockGamepad:
                    iconDatabase.BuildLookup("DualShock");
                    if (currentDevice != device) OnDeviceChanged?.Invoke(InputDeviceType.DualShock);
                    break;
                case SwitchProControllerHID:
                    iconDatabase.BuildLookup("Switch");
                    if (currentDevice != device) OnDeviceChanged?.Invoke(InputDeviceType.Switch);
                    break;
                default:
                    iconDatabase.BuildLookup("Gamepad");
                    if (currentDevice != device) OnDeviceChanged?.Invoke(InputDeviceType.Gamepad);
                    break;
            }
            currentDevice = device;
        }
    }

    public static Sprite GetIcon(InputAction action)
    {
        foreach (InputBinding binding in action.bindings)
        {
            if (binding.isComposite) continue;
            Sprite sprite = iconDatabase.GetIcon(binding.effectivePath);
            if (sprite != null) return sprite;
        }
        return null;
    }
}
