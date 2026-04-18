using PurrNet;
using UnityEngine;

public abstract class InteractableVendor : VendorBase, IInteractable<PlayerBody>
{
    [SerializeField, Min(0)] int usageCost = 1;
    public bool CanInteract(PlayerBody interactor) => interactor.Inventory.EnoughQuantity(CurrencyItem, usageCost) && itemStash.UsedSlots > 0;
    public bool TryInteract(PlayerBody interactor)
    {
        RequestInteraction(interactor);
        return true;
    }
    protected abstract bool TryInteractBehaviour(PlayerBody interactor);
    [ServerRpc] void RequestInteraction(PlayerBody interactor)
    {
        if (!CanInteract(interactor)) return;
        bool success = TryInteractBehaviour(interactor);
        if (success) interactor.Inventory.Remove(CurrencyItem, usageCost);
    }
}
