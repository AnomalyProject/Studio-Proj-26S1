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
    private Image icon;
    private PlayerInput playerInput;

    private void Awake()
    {
        icon = GetComponent<Image>();
        if (PlayerBody.localPlayerBody != null) HookToLocalPlayer(PlayerBody.localPlayerBody);
        else PlayerBody.OnLocalPlayerSpawned += HookToLocalPlayer;
        InputIconService.OnDeviceChanged += HandleInputDeviceChanged;
    }

    private void OnDestroy()
    {
        PlayerBody.OnLocalPlayerSpawned -= HookToLocalPlayer;
        InputIconService.OnDeviceChanged -= HandleInputDeviceChanged;
    }

    private void HookToLocalPlayer(PlayerBody player)
    {
        playerInput = player.GetComponent<PlayerInput>();
        icon.sprite = InputIconService.GetIcon(playerInput.actions[action]);
    }

    private void HandleInputDeviceChanged(InputIconService.InputDeviceType deviceType)
    {
        if (playerInput == null || icon == null) return;
        icon.sprite = InputIconService.GetIcon(playerInput.actions[action]);
    }
}
