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
public class SpeedBoostItem : NetworkBehaviour, IInteractable<FPSController>
{
    #region Inspector Configuration
    [Header("Boost Settings")]
    [Tooltip("Additive multiplier applied to the player's speed. Clamped by FPSController.maxSpeedBoostMultiplier.")]
    [SerializeField] private float multiplierAdditive = 0.5f;

    [Tooltip("Duration in seconds the boost lasts. Stacks additively if another boost is already active")]
    [SerializeField] private float duration = 10f;
    #endregion

    #region IInteractable
    /// <summary>
    /// Asks the server whether this item can currently be interacted with.
    /// Returns false if the interactor is invalid or the item is already consumed.
    /// </summary>
    /// <param name="interactor"></param>
    /// <returns></returns>
    public async Task<bool> CanInteract(FPSController interactor)
    {
        if(interactor == null || !interactor.isActiveAndEnabled) return false;

        return await RequestCanInteract();
    }

    /// <summary>
    /// Called by the interaction system. Asks the server for permission,
    /// applies the boost locally on confirmation, then destroys the item.
    /// </summary>
    /// <param name="interactor"></param>
    /// <returns></returns>
    public async Task<bool> TryInteract(FPSController interactor)
    {
        if(!await CanInteract(interactor)) return false;

        bool granted = await RequestInteract();

        if(!granted) return false;

        interactor.ApplySpeedBoost(multiplierAdditive, duration);
        return true;
    }
    #endregion

    #region Server RPCs

    /// <summary>
    /// Asks the server if the item is still valid to interact with.
    /// The server is the authority - it checks whether the item still exists
    /// and hasn't already been consumed by another player.
    /// </summary>
    /// <returns></returns>
    [ServerRpc(requireOwnership: false, asyncTimeoutInSec: 5)]
    private async Task<bool> RequestCanInteract()

    {
       // Iteem still exists on the server and is active.
       return this != null && gameObject.activeInHierarchy;
    }

    /// <summary>
    /// Asks the server to confirm and execute the consumption of this item.
    /// The server destroys the object so all clients see it despawn simultaneously.
    /// </summary>
    /// <returns></returns>
    [ServerRpc(requireOwnership: false, asyncTimeoutInSec: 5)]
    private async Task<bool> RequestInteract()
    {
        if(this == null || !gameObject.activeInHierarchy) return false;

        // Server destroys the item - this will trigger network despawn and remove the item from all clients.
        Destroy(gameObject);
        return true;
    }
    #endregion
}