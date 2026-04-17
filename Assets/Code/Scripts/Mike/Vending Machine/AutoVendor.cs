using UnityEngine;

public class AutoVendor : InteractableVendor
{
    protected override bool TryInteractBehaviour(PlayerBody interactor) => TryTransferFirstAvailable(interactor);
    private bool TryTransferFirstAvailable(PlayerBody interactor)
    {
        for (int i = 0; i < itemStash.TotalSlots; i++)
        {
            if (itemStash.TryGet(i, out var item))
            {
                int succesfullyTransfered = itemStash.Transfer(i, interactor.Inventory);
                if (succesfullyTransfered > 0) return true;
            }
        }
        return false;
    }
}