using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Inventory 
{
    #region Events
    public event Action<ItemData, int> OnItemAdded, OnItemRemoved;
    public event Action OnInventoryFull;
    #endregion

    #region Fields and Properties

    ItemStack[] slots;
    public int EmptySlots => slots.Count(slot => slot == null);
    public int TotalSlots => slots.Length;

    #endregion

    public Inventory(int size, params ItemStack[] startingItems)
    {
        slots = new ItemStack[size];

        for(int i = 0; i < startingItems.Length && !IsInventoryFull(); i++)
        {
            Add(startingItems[i]);
        }
    }

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
    public int Add(ItemStack stack)
    {
        if (IsInventoryFull() || stack.Quantity == 0) return 0;

        int totalAmountAdded = 0;

        List<int> sameItemSlots = FindSlotsWithItem(stack.ItemData);

        for (int i = 0; i < sameItemSlots.Count && stack.Quantity > 0; i++)
        {
            int index = sameItemSlots[i];
            if (slots[index].IsFull()) continue;

            int added = slots[index].AddToStack(stack.Quantity);
            totalAmountAdded += added;
            stack.RemoveFromStack(added);
        }

        if (stack.Quantity > 0 && TryFindEmptySlot(out int emptySlotIndex))
        {
            slots[emptySlotIndex] = new ItemStack(stack.ItemData, stack.Quantity);
            totalAmountAdded += stack.Quantity;
            stack.RemoveFromStack(stack.Quantity);
        }

        if (totalAmountAdded > 0) 
            OnItemAdded?.Invoke(stack.ItemData, totalAmountAdded);

        if (IsInventoryFull()) 
            OnInventoryFull?.Invoke();

        return totalAmountAdded;
    }
    /// <summary>
    /// Adds the specified quantity of the given item to the collection.
    /// </summary>
    /// <param name="itemData">The item to add to the collection. Cannot be null.</param>
    /// <param name="quantity">The number of items to add. Must be greater than zero.</param>
    /// <returns>The total quantity successfully received after the addition.</returns>
    public int Add(ItemData itemData, int quantity) => Add(new ItemStack(itemData, quantity));
    public bool TryAddOne(ItemData itemData) => Add(new ItemStack(itemData, 1)) > 0;
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
    public bool TryGet(ItemData itemData, out ItemStack itemStack)
    {
        itemStack = null;
        List<int> sameItemSlots = FindSlotsWithItem(itemData);
        if (sameItemSlots.Count == 0) return false;
        itemStack = slots[sameItemSlots[0]];
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
    public void Transfer(int fromIndex, int amount, Inventory toInventory)
    {
        if (fromIndex < 0 || fromIndex >= slots.Length) return;
        if (amount <= 0) return;
        if (toInventory == null) return;
        if (slots[fromIndex] == null) return;

        ItemStack stackToTransfer = slots[fromIndex];
        amount = Mathf.Min(amount, slots[fromIndex].Quantity);

        int amountTransfered = toInventory.Add(stackToTransfer.ItemData, amount);
        if(amountTransfered == 0) return;

        stackToTransfer.RemoveFromStack(amountTransfered);
        OnItemRemoved?.Invoke(stackToTransfer.ItemData, amountTransfered);
        if (stackToTransfer.Quantity <= 0) slots[fromIndex] = null;
    }
    public void Transfer(int fromIndex, Inventory toInventory) => Transfer(fromIndex, slots[fromIndex]?.Quantity ?? 0, toInventory);
    public void Transfer(ItemData itemData, int amount, Inventory toInventory)
    {
        if (toInventory == null) return;
        if (amount <= 0 || itemData == null) return;
        if (!EnoughQuantity(itemData, amount)) return;

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

        if(totalTransfered > 0)
        OnItemRemoved?.Invoke(itemData, totalTransfered);
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
    List<int> FindSlotsWithItem(ItemData itemData)
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
    public void SwapSlots(int index1, int index2)
    {
        if (index1 < 0 || index1 >= slots.Length || index2 < 0 || index2 >= slots.Length) return;

        ItemStack temp = slots[index1];
        slots[index1] = slots[index2];
        slots[index2] = temp;
    }
    public bool Contains(ItemData data) => slots.Any(s => s != null && s.ItemData == data && s.Quantity > 0);
    #endregion
}
