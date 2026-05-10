using System.Collections;
using UnityEngine.UI;
using UnityEngine;

/// <summary>
/// Nestoras Angelopoulos
/// 
/// Displays an interact prompt when the player focuses an interactable object.
/// </summary>
public class InteractPrompt : MonoBehaviour
{
    private const float FADE_SPEED = 5f;
    private Transform crosshair;
    private Image prompt;

    private void Awake()
    {
        crosshair = transform.GetChild(0);
        prompt = transform.GetComponentInChildren<InputIcon>(true).GetComponent<Image>();

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

    private void OnFocusedInteractable(IInteractable<PlayerBody> interactable)
    {
        StopAllCoroutines();
        StartCoroutine(FadeOutline(interactable, true));
    }
    private void OnInteractableLostFocus(IInteractable<PlayerBody> interactable) => StartCoroutine(FadeOutline(interactable, false));
    private IEnumerator FadeOutline(IInteractable<PlayerBody> interactable, bool show)
    {
        Color promptColor = prompt.color;
        while (promptColor.a < 1f && show || promptColor.a > 0f && !show)
        {
            promptColor.a += (show ? 1 : -1) * FADE_SPEED * Time.unscaledDeltaTime;
            prompt.color = promptColor;
            crosshair.localScale = Vector3.one * Mathf.Lerp(0, 1, promptColor.a);
            yield return null;
        }
        promptColor.a = show ? 1 : 0;
        prompt.color = promptColor;
        crosshair.localScale = Vector3.one * promptColor.a;
    }
}
