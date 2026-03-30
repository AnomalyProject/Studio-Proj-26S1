using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Inventory 
{
    #region Events
    /// <summary>
    /// Provides the item data and quantity amount.
    /// </summary>
    public event Action<ItemData, int> OnItemAdded, OnItemRemoved;
    public event Action OnInventoryFull, OnSlotsMoved, OnInventoryChanged;
    #endregion

    #region Fields and Properties

    ItemStack[] slots;
    public int EmptySlots => slots.Count(slot => slot == null);
    public int UsedSlots => slots.Count(slot => slot != null);
    public int TotalSlots => slots.Length;

    #endregion

    #region Constructor
    public Inventory(int size, params ItemStack[] startingItems)
    {
        slots = new ItemStack[size];

        OnItemAdded += (itemData, amount) => OnInventoryChanged?.Invoke();
        OnItemRemoved += (itemData, amount) => OnInventoryChanged?.Invoke();
        OnSlotsMoved += () => OnInventoryChanged?.Invoke();

        for (int i = 0; i < startingItems.Length && !IsInventoryFull(); i++)
        {
            Add(startingItems[i]);
        }
    }
    #endregion

    #region Exposed Methods

    #region Add Methods

    /// <summary>
    /// Attempts to add the specified item stack to the inventory, combining with existing stacks of the same item where
    /// possible and placing any remaining items in an empty slot.
    /// </summary>
    /// <remarks>If the inventory is full or the provided stack has a quantity of zero, no items are added.
    /// Items are first merged with existing stacks of the same item type before occupying empty slots. 
    /// The method will modify the input stack and reflect the number of items successfully added.</remarks>
    /// <returns>The total number of items that were successfully added to the inventory.</returns>
    /// <param name="stack">The item stack to add to the inventory. The stack's quantity must be greater than zero.</param>
    public int Add(ItemStack stack, bool modifyInputStack = true)
    {
        int totalAdded = Add(stack.ItemData, stack.Quantity);
        if(modifyInputStack) stack.RemoveFromStack(totalAdded);
        return totalAdded;
    }
    /// <summary>
    /// Attempts to add the specified quantity of the given item to the inventory.
    /// </summary>
    /// <remarks>If the inventory does not have enough space, only a portion of the requested quantity may be
    /// added.</remarks>
    /// <param name="itemData">The item to add to the inventory. Cannot be null.</param>
    /// <param name="quantity">The number of items to add. Must be greater than zero.</param>
    /// <returns>The total number of items actually added to the inventory. Returns 0 if the inventory is full, the quantity is
    /// zero, or the item is null.</returns>
    public int Add(ItemData itemData, int quantity)
    {
        if (IsInventoryFull() || quantity == 0 || itemData == null) return 0;

        int totalAmountAdded = 0;

        List<int> sameItemSlots = FindSlotsWithItem(itemData);

        for (int i = 0; i < sameItemSlots.Count && quantity > 0; i++)
        {
            int index = sameItemSlots[i];
            if (slots[index].IsFull()) continue;

            int added = slots[index].AddToStack(quantity);
            totalAmountAdded += added;
            quantity -= added;
        }

        while (quantity > 0 && TryFindEmptySlot(out int emptySlotIndex))
        {
            int possibleToAdd = Mathf.Min(quantity, itemData.MaxStackSize);
            slots[emptySlotIndex] = new ItemStack(itemData, possibleToAdd);
            totalAmountAdded += possibleToAdd;
            quantity -= possibleToAdd;
        }

        if (totalAmountAdded > 0) OnItemAdded?.Invoke(itemData, totalAmountAdded);

        if (IsInventoryFull())
            OnInventoryFull?.Invoke();

        return totalAmountAdded;
    }
    /// <summary>
    /// Adds one of the given item to the collection.
    /// </summary>
    /// <param name="itemData">The item to add to the collection. Cannot be null.</param>
    /// <returns>true if successful.</returns>
    public bool TryAddOne(ItemData itemData) => Add(itemData, 1) > 0;
    #endregion

    #region Get Methods
    public bool TryGet(int slotIndex, out ItemStack itemStack)
    {
        itemStack = null;

        if (slotIndex < 0 || slotIndex >= slots.Length) return false;

        itemStack = slots[slotIndex];

        if(itemStack != null) return true;
        else return false;
    }
    public bool TryGet(ItemData itemData, out ItemStack itemStack, out int slotIndex)
    {
        slotIndex = -1;
        itemStack = null;
        List<int> sameItemSlots = FindSlotsWithItem(itemData);
        if (sameItemSlots.Count == 0) return false;
        slotIndex = sameItemSlots[0];
        itemStack = slots[slotIndex];
        return true;
    }

    /// <summary>
    /// Returns an enumerable collection of all item stacks contained in the slots, null slots included.
    /// </summary>
    /// <returns>An of all <see cref="ItemStack"/>'s in the inventory that can be used to iterate through the slots.</returns>
    public IEnumerable<ItemStack> GetEnumeration() => slots;
    /// <summary>
    /// Returns an enumeration of all non-empty item slots.
    /// </summary>
    /// <returns>An enumerable collection of <see cref="ItemStack"/> objects representing the non-null slots. The collection will
    /// be empty if all slots are null.</returns>
    public IEnumerable<ItemStack> GetNonEmptyEnumeration() => slots.Where(slot => slot != null);
    #endregion

    #region Transfer Methods

    /// <summary>
    /// Transfers a specified quantity of items from the current inventory slot to another inventory.
    /// </summary>
    /// <remarks>The method will transfer up to the requested amount, limited by the quantity available in
    /// the source slot and the capacity of the target inventory. After a successful transfer, the source slot may be
    /// cleared if all items are moved.</remarks>
    /// <param name="fromIndex">The zero-based index of the slot in the current inventory from which to transfer items. Must be within the valid
    /// range of slot indices.</param>
    /// <param name="amount">The number of items to transfer. Must be greater than zero and not exceed the quantity available in the
    /// specified slot.</param>
    /// <param name="toInventory">The target inventory to which the items will be transferred. Cannot be null.</param>
    /// <returns>The actual number of items transferred to the target inventory. Returns 0 if the transfer could not be
    /// completed.</returns>
    public int Transfer(int fromIndex, int amount, Inventory toInventory)
    {
        if (fromIndex < 0 || fromIndex >= slots.Length) return 0;
        if (amount <= 0) return 0;
        if (toInventory == null) return 0;
        if (slots[fromIndex] == null) return 0;

        ItemStack stackToTransfer = slots[fromIndex];
        amount = Mathf.Min(amount, slots[fromIndex].Quantity);

        int amountTransfered = toInventory.Add(stackToTransfer.ItemData, amount);
        if(amountTransfered == 0) return 0;

        stackToTransfer.RemoveFromStack(amountTransfered);
        OnItemRemoved?.Invoke(stackToTransfer.ItemData, amountTransfered);

        if (stackToTransfer.Quantity <= 0) slots[fromIndex] = null;

        return amountTransfered;
    }
    /// <summary>
    /// Transfers all items from the specified slot to the given inventory.
    /// </summary>
    /// <param name="fromIndex">The zero-based index of the slot to transfer items from. Must be within the valid range of slot indices.</param>
    /// <param name="toInventory">The inventory to which the items will be transferred. Cannot be null.</param>
    /// <returns>The number of items successfully transferred to the target inventory.</returns>
    public int Transfer(int fromIndex, Inventory toInventory)
    {
        if (fromIndex < 0 || fromIndex >= slots.Length) return 0;
        return Transfer(fromIndex, slots[fromIndex]?.Quantity ?? 0, toInventory);
    }
    /// <summary>
    /// Transfers a specified amount of the given item to another inventory.
    /// </summary>
    /// <remarks>If the destination inventory does not have enough space, fewer items may be transferred than
    /// requested.</remarks>
    /// <param name="itemData">The item to transfer. Cannot be null.</param>
    /// <param name="amount">The number of items to transfer. Must be greater than zero.</param>
    /// <param name="toInventory">The destination inventory to receive the items. Cannot be null.</param>
    /// <returns>The number of items successfully transferred. Returns 0 if the transfer could not be completed.</returns>
    public int Transfer(ItemData itemData, int amount, Inventory toInventory)
    {
        if (toInventory == null) return 0;
        if (amount <= 0 || itemData == null) return 0;

        List<int> sameItemSlots = FindSlotsWithItem(itemData);
        int totalTransfered = 0;

        for (int i = 0; i < sameItemSlots.Count; i++)
        {
            int index = sameItemSlots[i];
            if (slots[index] == null) continue;

            int amountToTransfer = Mathf.Min(amount, slots[index].Quantity);
            int successfullyTransfered = toInventory.Add(slots[index].ItemData, amountToTransfer);

            if(successfullyTransfered == 0) break; // break if transaction was unsuccessful (other inventory full)

            slots[index].RemoveFromStack(successfullyTransfered);
            totalTransfered += successfullyTransfered;
            amount -= successfullyTransfered;

            if (slots[index].Quantity <= 0) slots[index] = null;
            if (amount <= 0) break; // break if nothing left to transfer
        }

        if(totalTransfered > 0) OnItemRemoved?.Invoke(itemData, totalTransfered);
        return totalTransfered;
    }
    #endregion

    #region Remove Methods
    public void Remove(ItemData itemData, int quantity)
    {
        if(quantity <= 0) return;

        List<int> sameItemSlots = FindSlotsWithItem(itemData);
        int totalAmountRemoved = 0;

        for (int i = 0; i < sameItemSlots.Count; i++)
        {
            int index = sameItemSlots[i];

            if (slots[index].IsEmpty)
            {
                slots[index] = null;
                continue;
            }

            int amountRemoved = slots[index].RemoveFromStack(quantity);
            totalAmountRemoved += amountRemoved;
            quantity -= amountRemoved;

            if (slots[index].IsEmpty) slots[index] = null;

            if (quantity <= 0)
            {
                OnItemRemoved?.Invoke(itemData, totalAmountRemoved);
                return;
            }
        }

        if(totalAmountRemoved > 0)
        OnItemRemoved?.Invoke(itemData, totalAmountRemoved);
    }
    public void RemoveOne(ItemData itemData) => Remove(itemData, 1);
    public void Remove(ItemStack stack) => Remove(stack.ItemData, stack.Quantity);

    #endregion

    #region Clear Methods
    public void ClearSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Length) return;
        slots[slotIndex] = null;
    }

    /// <summary>
    /// Removes all items from the collection, resetting it to an empty state.
    /// </summary>
    /// <remarks>Use this method to clear all item slots at once. After calling this method, the collection
    /// will contain no items, and its size remains unchanged.</remarks>
    public void Clear() => slots = new ItemStack[slots.Length];
    #endregion

    #endregion

    #region Helper Methods
    bool TryFindEmptySlot(out int index)
    {
        index = -1;

        for(int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                index = i;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Finds the indices of all slots that contain the specified item.
    /// </summary>
    /// <param name="itemData">The item to search for within the slots. Cannot be null.</param>
    /// <returns>A list of indices representing the slots that contain the specified item. The list is empty if the
    /// item is not found in any slot.</returns>
    public List<int> FindSlotsWithItem(ItemData itemData)
    {
        List<int> indices = new List<int>();

        for(int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && slots[i].ItemData == itemData)
            {
                indices.Add(i);
            }
        }
        return indices;
    }
    public bool IsInventoryFull() => slots.All(slot => slot != null && slot.Quantity >= slot.ItemData.MaxStackSize);

    /// <summary>
    /// Determines whether the total quantity of the specified item in the inventory is greater than or equal to the
    /// required amount.
    /// </summary>
    /// <param name="itemData">The item to check for available quantity in the inventory. Cannot be null.</param>
    /// <param name="quantity">The minimum quantity required. Must be greater than or equal to zero.</param>
    /// <returns>true if the total quantity of the specified item is greater than or equal to the required amount. Otherwise,
    /// false.</returns>
    public bool EnoughQuantity(ItemData itemData, int quantity)
    {
        List<int> sameItemSlots = FindSlotsWithItem(itemData);
        int totalQuantity = 0;
        for (int i = 0; i < sameItemSlots.Count; i++)
        {
            int index = sameItemSlots[i];
            totalQuantity += slots[index].Quantity;
            if (totalQuantity >= quantity) return true;
        }
        return false;
    }

    /// <summary>
    /// Swaps the contents of two inventory slots, combining item stacks if they contain the same item type.
    /// </summary>
    /// <remarks>If both slots contain the same item type, their quantities are combined in the destination
    /// slot up to its stack limit, and the source slot is reduced accordingly. If the slots contain different items,
    /// their contents are swapped.</remarks>
    /// <param name="fromSlot">The zero-based index of the source slot to move or combine items from. Must be within the valid range of slot
    /// indices.</param>
    /// <param name="toSlot">The zero-based index of the destination slot to move or combine items to. Must be within the valid range of slot
    /// indices.</param>
    public void SwapSlots(int fromSlot, int toSlot)
    {
        if (fromSlot < 0 || fromSlot >= slots.Length || toSlot < 0 || toSlot >= slots.Length) return;

        bool slotsNotNull = (slots[fromSlot] != null && slots[toSlot] != null);
        bool sameItemType = slotsNotNull && (slots[fromSlot].ItemData == slots[toSlot].ItemData);

        if (sameItemType)
        {
            int totalAdded = slots[toSlot].AddToStack(slots[fromSlot].Quantity);
            slots[fromSlot].RemoveFromStack(totalAdded);
            if(slots[fromSlot].Quantity <= 0) slots[fromSlot] = null;
            OnSlotsMoved?.Invoke();
            return;
        }

        ItemStack temp = slots[fromSlot];
        slots[fromSlot] = slots[toSlot];
        slots[toSlot] = temp;
        OnSlotsMoved?.Invoke();
    }
    public bool Contains(ItemData data) => slots.Any(s => s != null && s.ItemData == data && s.Quantity > 0);
    #endregion
}
