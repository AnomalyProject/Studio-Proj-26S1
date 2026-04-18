using PurrNet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CompositeVendor : VendorBase
{
    public event Action<int> OnSlotChanged;
    bool TryPerformTransfer(int slotIndex, Inventory toInventory)
    {
        if (!itemStash.TryGet(slotIndex, out IReadOnlyItemStack stack)) return false;
        if (!CheckPrice(slotIndex, toInventory)) return false;

        bool success = itemStash.Transfer(slotIndex, toInventory) > 0;

        if (success)
        {
            Debug.Log("Transfer Success");
            toInventory.Remove(CurrencyItem, stack.GetItemData().VendorPrice);
            InvokeOnSlotChanged(slotIndex);
        }
        return success;
    }

    public bool CheckPrice(int itemIndex, Inventory buyerInventory)
    {
        if(!itemStash.TryGet(itemIndex, out IReadOnlyItemStack stack)) return false;
        int price = stack.GetItemData().VendorPrice;
        bool success = buyerInventory.EnoughQuantity(CurrencyItem, price);
        return success;
    }

    public ItemData GetDataFromSlot(int slotIndex)
    {
        if (!itemStash.TryGet(slotIndex, out var stack)) return null;
        return stack.GetItemData();
    }

    [ServerRpc] public void RequestTransfer(int slotIndex, Inventory inventory)
    {
        TryPerformTransfer(slotIndex, inventory);
    }
    [ObserversRpc] private void InvokeOnSlotChanged(int slot) => OnSlotChanged?.Invoke(slot);
}