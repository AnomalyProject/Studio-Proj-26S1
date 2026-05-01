using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine;

/// <summary>
/// Nestoras Angelopoulos
/// 
/// Displays an interact prompt when the player focuses an interactable object. The prompt's icon changes based on the player's current input device.
/// </summary>
public class InteractPrompt : MonoBehaviour
{
    [SerializeField] private Image icon;

    [SerializeField] private PlayerInput playerInput;

    private void Awake()
    {
        icon = GetComponentInChildren<Image>(true);
        icon.gameObject.SetActive(false);

        PlayerBody.OnLocalPlayerSpawned += HandleLocalPlayerSpawned;
        PlayerBody.OnLocalPlayerDespawned += HandleLocalPlayerDespawned;

        InputIconService.OnDeviceChanged += HandleInputDeviceChanged;
    }

    private void OnDestroy()
    {
        PlayerBody.OnLocalPlayerSpawned -= HandleLocalPlayerSpawned;
        PlayerBody.OnLocalPlayerDespawned -= HandleLocalPlayerDespawned;

        InputIconService.OnDeviceChanged -= HandleInputDeviceChanged;
    }

    private void HandleInputDeviceChanged(InputIconService.InputDeviceType deviceType)
    {
        if (playerInput != null) icon.sprite = InputIconService.GetIcon(playerInput.actions["Interact"]);
    }

    private void HandleLocalPlayerSpawned(PlayerBody player)
    {
        playerInput = player.GetComponent<PlayerInput>();
        icon.sprite = InputIconService.GetIcon(playerInput.actions["Interact"]);

        player.Interaction.interactionSystem.OnFocusedInteractable += OnFocusedInteractable;
        player.Interaction.interactionSystem.OnInteractableLostFocus += OnInteractableLostFocus;
    }

    private void HandleLocalPlayerDespawned(PlayerBody player)
    {
        player.Interaction.interactionSystem.OnFocusedInteractable -= OnFocusedInteractable;
        player.Interaction.interactionSystem.OnInteractableLostFocus -= OnInteractableLostFocus;
    }

    private void OnFocusedInteractable(IInteractable<PlayerBody> interactable) => icon.gameObject.SetActive(true);
    private void OnInteractableLostFocus(IInteractable<PlayerBody> interactable) => icon.gameObject.SetActive(false);
}
