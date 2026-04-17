using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CompositeVendor : VendorBase
{
    [SerializeField] private VendorButton[] itemButtons;
    private Queue<IReadOnlyItemStack> unusedItems = new();

    protected override void Awake()
    {
        OnRestock.AddListener(OnRestockHandle);
        base.Awake();
    }
    public bool TryPerformTransfer(VendorButton button, Inventory toInventory)
    {
        if(button.HeldItem == null || button.HeldItem.GetQuantity() <= 0) return false;

        bool success = itemStash.TryTransferExact(button.HeldItem.GetItemData(), button.HeldItem.GetQuantity(), toInventory);

        if (success)
        {
            toInventory.Remove(CurrencyItem, button.HeldItem.GetItemData().VendorPrice);
            SetNewButtonContent(button);
        }
        return success;
    }

    public bool CheckPrice(VendorButton button, Inventory buyerInventory)
    {
        if (button.HeldItem == null) return false;
        int price = button.HeldItem.GetItemData().VendorPrice;
        return buyerInventory.EnoughQuantity(CurrencyItem, price);
    }

    private void OnRestockHandle()
    {
        unusedItems.Clear();

        foreach(var item in itemStash.GetNonEmptyEnumeration()) unusedItems.Enqueue(item);
        foreach(var button in itemButtons) SetNewButtonContent(button);
    }

    private void SetNewButtonContent(VendorButton button)
    {
        if(unusedItems.TryDequeue(out var stack))
        {
            button.SetItemAndVendor(itemStack: stack, vendor: this);
            return;
        }

        button.SetItemAndVendor(itemStack: null, vendor: this);
    }
}