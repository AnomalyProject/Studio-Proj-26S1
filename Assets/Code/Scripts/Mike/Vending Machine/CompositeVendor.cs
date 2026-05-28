using PurrNet;
using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class CompositeVendor : VendorBase
{
    public event Action<int> OnSlotChanged;
    [SerializeField] private TextMeshPro stockText;
    [SerializeField] private VendorButton[] vendorButtons;

    protected override void Awake()
    {
        base.Awake();
        itemStash.OnStackRemoved += (_, _) => UpdateStockText();
        OnRestock.AddListener(UpdateStockText);
        for (int i = 0; i < vendorButtons.Length; i++) vendorButtons[i].SlotIndex = i;
    }
    protected override void OnSpawned()
    {
        base.OnSpawned();
        UpdateStockText();
    }
    private void UpdateStockText()
    {
        int usedSlots = itemStash.UsedSlots;
        stockText.text = usedSlots > 0? $"Stock x{usedSlots}" : "Out of Stock";
    }

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

    public IReadOnlyItemStack GetStackFromSlot(int slotIndex)
    {
        if (!itemStash.TryGet(slotIndex, out var stack)) return null;
        return stack;
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

            if(vendorButtons.Length < itemStash.TotalSlots)
            {
                if (itemStash.TryGetNext(vendorButtons.Length -1, out _, out int nextSlot) && nextSlot >= vendorButtons.Length)
                {
                    itemStash.MoveSlot(nextSlot, slotIndex);
                }
            }

            InvokeTransferSuccess(slotIndex);
        }
        return Task.FromResult(success);
    }
    [ObserversRpc] private void InvokeTransferSuccess(int transferedSlot)
    {
        OnSlotChanged?.Invoke(transferedSlot);
        OnTransferSuccess?.Invoke();
    }
}