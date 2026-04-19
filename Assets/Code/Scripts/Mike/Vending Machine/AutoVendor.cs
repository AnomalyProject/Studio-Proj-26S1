using UnityEngine;

public class AutoVendor : InteractableVendor
{
    protected override bool TryInteractBehaviour(PlayerBody interactor) => TryTransferFirstAvailable(interactor);
    private bool TryTransferFirstAvailable(PlayerBody interactor)
    {
        if (itemStash.TryGetNext(0, out var item, out int index))
        {
            int succesfullyTransfered = itemStash.Transfer(index, interactor.Inventory);
            Debug.Log($"Transfer index: {index} | Remaining Used Slots: {itemStash.UsedSlots} | Total Slots: {itemStash.TotalSlots}");
            if (succesfullyTransfered > 0) return true;
        }
        return false;
    }
}