using PurrNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class CompositeVendor : VendorBase
{
    public event Action<int> OnSlotChanged;

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

    [ServerRpc] public async Task<bool> RequestTransfer_Server(int slotIndex, Inventory toInventory)
    {
        if (!itemStash.TryGet(slotIndex, out IReadOnlyItemStack stack)) return await Task.FromResult(false);
        if (!CheckPrice(slotIndex, toInventory)) return await Task.FromResult(false);
        bool success = itemStash.Transfer(slotIndex, toInventory) > 0;

        if (success)
        {
            Debug.Log("Transfer Success");
            toInventory.Remove(CurrencyItem, stack.GetItemData().VendorPrice);
            InvokeOnSlotChanged(slotIndex);
        }
        return success;
    }
    [ObserversRpc] private void InvokeOnSlotChanged(int slot) => OnSlotChanged?.Invoke(slot);
}