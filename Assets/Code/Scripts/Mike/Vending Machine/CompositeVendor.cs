using PurrNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class CompositeVendor : VendorBase
{
    public event Action<int> OnSlotChanged;

    public bool CanBuyerAfford(int itemIndex, Inventory buyerInventory)
    {
        if(!itemStash.TryGet(itemIndex, out IReadOnlyItemStack stack)) return false;
       return CanBuyerAfford(stack, buyerInventory, out _);
    }

    private bool CanBuyerAfford(IReadOnlyItemStack stack, Inventory buyerInventory, out int stackPrice)
    {
        stackPrice = GetStackPrice(stack);
        bool success = buyerInventory.EnoughQuantity(CurrencyItem, stackPrice);
        return success;
    }

    public int GetStackPrice(int itemIndex)
    {
        if (!itemStash.TryGet(itemIndex, out IReadOnlyItemStack stack)) return 0;
        return GetStackPrice(stack);
    }
    private int GetStackPrice(IReadOnlyItemStack stack) => stack.GetItemData().PricePerUnit * stack.GetQuantity();

    public ItemData GetDataFromSlot(int slotIndex)
    {
        if (!itemStash.TryGet(slotIndex, out var stack)) return null;
        return stack.GetItemData();
    }

    [ServerRpc] public Task<bool> RequestTransfer_Server(int slotIndex, Inventory toInventory)
    {
        if (!itemStash.TryGet(slotIndex, out IReadOnlyItemStack stack)) return Task.FromResult(false);
        if (!CanBuyerAfford(stack, toInventory, out int stackPrice)) return Task.FromResult(false);
        bool success = itemStash.TryTransferExact(slotIndex, toInventory);

        if (success)
        {
            Debug.Log("Transfer Success");
            toInventory.Remove(CurrencyItem, stackPrice);
            InvokeOnSlotChanged(slotIndex);
        }
        return Task.FromResult(success);
    }
    [ObserversRpc] private void InvokeOnSlotChanged(int slot) => OnSlotChanged?.Invoke(slot);
}