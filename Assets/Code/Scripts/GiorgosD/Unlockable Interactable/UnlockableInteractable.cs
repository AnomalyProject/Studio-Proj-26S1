using NUnit.Framework;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem.Utilities;

public class UnlockableInteractable : MonoBehaviour, IInteractable<PlayerBody>
{
    [Header("Requierment")]
    [SerializeField] private InspectorItemStack itemRequirment;

    [Header("Status")]
    [SerializeField] private bool isLocked = true;

    [Header("Event")]
    public UnityEvent OnSuccess;

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
    public Task<bool> TryInteract(PlayerBody interactor)
    {
        if (!isLocked)
        {
            UseSuccess();
            return Task.FromResult(true);
        }

        if (HasItemsInInv(interactor))
        {
            var inv = interactor.GetComponent<PlayerInventory>();

            if(inv.Inventory.TryRemoveExact(itemRequirment.Data, itemRequirment.Quantity))
            {
                Debug.Log($"[UnlockableInteractable]: {inv.Inventory.IsInventoryFull()}");
                isLocked = false;
                UseSuccess();
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
        var inv = interactor.GetComponent<PlayerInventory>();
        if (inv == null)
        {
            Debug.Log("[UnlockableInteractable]: Process failed to find Player inventory");

            return false;
        }

        Debug.Log($"[UnlockableInteractable]: interaction status {inv.Inventory.EnoughQuantity(itemRequirment.Data, itemRequirment.Quantity)}");
        return inv.Inventory.EnoughQuantity(itemRequirment.Data, itemRequirment.Quantity);
    }
    #endregion

    #region Exposed methods
    /// <summary>
    /// Returns items needed to unlock this interactable to player inventory (idk ask mike why he wanted this).
    /// </summary>
    /// <param name="inv"></param>
    public void ReturnItems(PlayerInventory inv)
    {
        if (isLocked)
        {
            Debug.Log("[UnlockableInteractable]: Interactable is locked thus items cant be returned to player.");
            return;
        }

        if(inv.Inventory.CanFit(itemRequirment.Data, itemRequirment.Quantity))
        {
            inv.Inventory.TryAddExact(itemRequirment.Data, itemRequirment.Quantity);
            Debug.Log($"[UnlockableInteractable]: Inventory is full: {inv.Inventory.IsInventoryFull()}");
        }
        else
        {
            Debug.Log("[UnlockableInteractable]: Player inventory is full.");
        }
    }

    /// <summary>
    /// Resets the interacatble to locked/unInteracted.
    /// </summary>
    public void ResetInteractable()
    {
        isLocked = true;

        Debug.Log("[UnlockableInteractable]: Locked state has been reset.");
    }
    #endregion

    #region Helper Funcs
    /// <summary>
    /// Helper class fires OnSuccess event for Opening/Using something. (VS wouldnt auto complete the OnSuccess so i made the func).
    /// </summary>
    private void UseSuccess()
    {
        OnSuccess?.Invoke();

        Debug.Log("[UnlockableInteractable]: Open.");
    }
    #endregion
}