using UnityEngine;

/// <summary>
/// Nestoras Angelopoulos
/// 
/// Displays an interact prompt when the player focuses an interactable object.
/// </summary>
public class InteractPrompt : MonoBehaviour
{
    private GameObject visual;

    private void Awake()
    {
        visual = transform.GetChild(0).gameObject;
        visual.SetActive(false);

        if (PlayerBody.localPlayerBody != null) HandleLocalPlayerSpawned(PlayerBody.localPlayerBody);
        PlayerBody.OnLocalPlayerSpawned += HandleLocalPlayerSpawned;
        PlayerBody.OnLocalPlayerDespawned += HandleLocalPlayerDespawned;
    }
    private void OnDestroy()
    {
        PlayerBody.OnLocalPlayerSpawned -= HandleLocalPlayerSpawned;
        PlayerBody.OnLocalPlayerDespawned -= HandleLocalPlayerDespawned;
    }
    private void HandleLocalPlayerSpawned(PlayerBody player)
    {
        player.Interaction.interactionSystem.OnFocusedInteractable += OnFocusedInteractable;
        player.Interaction.interactionSystem.OnInteractableLostFocus += OnInteractableLostFocus;
    }
    private void HandleLocalPlayerDespawned(PlayerBody player)
    {
        player.Interaction.interactionSystem.OnFocusedInteractable -= OnFocusedInteractable;
        player.Interaction.interactionSystem.OnInteractableLostFocus -= OnInteractableLostFocus;
    }

    private void OnFocusedInteractable(IInteractable<PlayerBody> interactable) => visual.SetActive(true);
    private void OnInteractableLostFocus(IInteractable<PlayerBody> interactable) => visual.SetActive(false);
}
