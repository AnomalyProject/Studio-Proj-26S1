using PurrNet;
using System.Threading.Tasks;
using UnityEngine;

public abstract class InteractableVendor : VendorBase, IInteractable<PlayerBody>
{
    [SerializeField, Min(0)] int usageCost = 1;
    public Task<bool> CanInteract(PlayerBody interactor) => 
        Task.FromResult(interactor.Inventory.EnoughQuantity(CurrencyItem, usageCost) && itemStash.UsedSlots > 0);
    [ServerRpc] public async Task<bool> TryInteract(PlayerBody interactor)
    {
        bool canInteract = await CanInteract(interactor);
        if (!canInteract) return false;

        bool success = TryInteractBehaviour(interactor);
        if (success) interactor.Inventory.Remove(CurrencyItem, usageCost);
        return success;
    }
    protected abstract bool TryInteractBehaviour(PlayerBody interactor);
}