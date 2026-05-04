using PurrNet;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public abstract class InteractableVendor : VendorBase, IInteractable<PlayerBody>
{
    [SerializeField, Min(0)] int usageCost = 1;
    [SerializeField] protected TextMeshPro priceText;
    public Task<bool> CanInteract(PlayerBody interactor) => 
        Task.FromResult(interactor.Inventory.EnoughQuantity(CurrencyItem, usageCost) && itemStash.UsedSlots > 0);

    protected override void OnSpawned()
    {
        base.OnSpawned();
        priceText.text = usageCost == 0? "For Free!": $"Only {usageCost} Duckies!";
    }
    [ServerRpc] public async Task<bool> TryInteract(PlayerBody interactor)
    {
        bool canInteract = await CanInteract(interactor);
        if (!canInteract) return false;

        bool success = TryInteractBehaviour(interactor);

        if (success)
        {
            interactor.Inventory.Remove(CurrencyItem, usageCost);
            InvokeTransferSuccess();
        }
        return success;
    }

    [ObserversRpc] void InvokeTransferSuccess() => OnTransferSuccess?.Invoke();
    protected abstract bool TryInteractBehaviour(PlayerBody interactor);
}