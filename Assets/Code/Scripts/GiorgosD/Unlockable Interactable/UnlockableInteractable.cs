using PurrNet;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public class UnlockableInteractable : NetworkBehaviour, IInteractable<PlayerBody>
{
    [Header("Requierment")]
    [SerializeField] private InspectorItemStack itemRequirment;

    [Header("Status")]
    [SerializeField] private bool isLocked = true;

    [Header("Event")]
    public UnityEvent OnSuccess;
    public UnityEvent OnReset;

    #region Interaction logic

    /// <summary>
    /// Makes sure the player can always try to interact.
    /// </summary>
    /// <param name="interactor"></param>
    /// <returns></returns>
    public Task<bool> CanInteract(PlayerBody interactor)
    {
        return Task.FromResult(true);
    }

    /// <summary>
    /// Executes interaction logic and consumes the item(s) if the player has them.
    /// </summary>
    /// <param name="interactor"></param>
    /// <returns></returns>
    [ServerRpc] public Task<bool> TryInteract(PlayerBody interactor)
    {
        if (!isLocked)
        {
            OnSuccess_Observers();
            return Task.FromResult(true);
        }

        if (HasItemsInInv(interactor))
        {
            var inv = interactor.Inventory;

            if(inv.TryRemoveExact(itemRequirment.Data, itemRequirment.Quantity))
            {
                isLocked = false;
                OnSuccess_Observers();
                return Task.FromResult(true);
            }
        }

        return Task.FromResult(false);
    }

    /// <summary>
    /// Checks if player can interact.
    /// </summary>
    /// <param name="interactor"></param>
    /// <returns></returns>
    private bool HasItemsInInv(PlayerBody interactor)
    {
        var inv = interactor.Inventory;

        if (inv == null)
        {
            Debug.Log("[UnlockableInteractable]: Process failed to find Player inventory");
            return false;
        }

        Debug.Log($"[UnlockableInteractable]: interaction status {inv.EnoughQuantity(itemRequirment.Data, itemRequirment.Quantity)}");
        return inv.EnoughQuantity(itemRequirment.Data, itemRequirment.Quantity);
    }
    #endregion

    #region Exposed methods
    /// <summary>
    /// Returns items needed to unlock this interactable to player inventory (idk ask mike why he wanted this).
    /// </summary>
    /// <param name="inv"></param>
    public void ReturnItems(Inventory inv)
    {
        if (!isServer)
        {
            Debug.LogWarning("[UnlockableInteractable]: Cannot return items from a client, the return request must be performed by the server.");
            return;
        }

        if (isLocked)
        {
            Debug.Log("[UnlockableInteractable]: Interactable is locked thus items cant be returned to player.");
            return;
        }

        if(inv.TryAddExact(itemRequirment.Data, itemRequirment.Quantity)) ResetToLocked();
        else Debug.Log("[UnlockableInteractable]: Player inventory is full.");
    }

    /// <summary>
    /// Resets the interacatble to locked/unInteracted.
    /// </summary>
    public void ResetToLocked()
    {
        if (!isServer)
        {
            Debug.LogWarning("Cannot reset unlockable from a client!");
            return;
        }

        isLocked = true;
        OnReset_Observers();
        Debug.Log("[UnlockableInteractable]: Locked state has been reset.");
    }
    #endregion

    #region Helper Funcs
    /// <summary>
    /// Helper class fires OnSuccess event for Opening/Using something. (VS wouldnt auto complete the OnSuccess so i made the func).
    /// </summary>
    [ObserversRpc] private void OnSuccess_Observers()
    {
        OnSuccess?.Invoke();
        Debug.Log("[UnlockableInteractable]: Open.");
    }

    [ObserversRpc] private void OnReset_Observers() => OnReset?.Invoke();
    #endregion
}