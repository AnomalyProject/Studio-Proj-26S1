using UnityEngine;
using PurrNet;
using System.Threading.Tasks;

/// <summary>
/// Speed boost consumable item.
/// Implements IInteractable<FPSController> - the interaction system calls
/// TryInteract which asks the server for permission before applying the boost.
/// The server is the sole authority on whether the item can be consumed.
/// Destroys itself immediately after a successful interaction.
/// </summary>
public class SpeedBoostItem : PlayerItem, IInteractable<PlayerBody>
{
    #region Inspector Configuration
    [Header("Boost Settings")]
    [Tooltip("Additive multiplier applied to the player's speed. Clamped by FPSController.maxSpeedBoostMultiplier.")]
    [SerializeField] private float multiplierAdditive = 0.5f;

    [Tooltip("Duration in seconds the boost lasts. Stacks additively if another boost is already active")]
    [SerializeField] private float duration = 10f;

    [SerializeField] private AudioSource consumeSound;
    #endregion

    #region IInteractable
    /// <summary>
    /// Asks the server whether this item can currently be interacted with.
    /// Returns false if the interactor is invalid or the item is already consumed.
    /// </summary>
    /// <param name="interactor"></param>
    /// <returns></returns>
    public Task<bool> CanInteract(PlayerBody interactor)
    {
        if(interactor == null || !interactor.isActiveAndEnabled) return Task.FromResult(false);
        return Task.FromResult(true);
    }

    /// <summary>
    /// Called by the interaction system. Asks the server for permission,
    /// applies the boost locally on confirmation, then destroys the item.
    /// </summary>
    /// <param name="interactor"></param>
    /// <returns></returns>
    public async Task<bool> TryInteract(PlayerBody interactor)
    {
        if(!await CanInteract(interactor)) return false;
        interactor.Movement.ApplySpeedBoost(multiplierAdditive, duration);
        if(consumeSound != null) consumeSound.Play();
        await Task.Delay((int)(consumeSound.clip.length * 1000));
        return true;
    }
    #endregion
}