using PurrNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class Inventory : NetworkModule
{
    #region Events
    /// <summary>
    /// Provides the item data and quantity amount.
    /// </summary>
    public event Action<ItemData, int> OnItemAdded, OnItemRemoved;
    /// <summary>
    /// Provides the actual stack in read-only form and the slot index it's positioned at.
    /// </summary>
    public event Action<IReadOnlyItemStack, int> OnStackAdded, OnStackRemoved, OnStackChanged;
    public event Action OnInventoryFull;
    public event Action<int, int> OnSlotsSwapped;
    [ObserversRpc] private void InvokeInventoryFull() => OnInventoryFull?.Invoke();
    [ObserversRpc] private void InvokeSlotsSwapped(int indexA, int indexB) => OnSlotsSwapped?.Invoke(indexA, indexB);
    [ObserversRpc] private void InvokeItemAdded(ItemData itemData, int amount) => OnItemAdded?.Invoke(itemData, amount);
    [ObserversRpc] private void InvokeItemRemoved(ItemData itemData, int amount) => OnItemRemoved?.Invoke(itemData, amount);
    [ObserversRpc] private void InvokeStackAdded(IReadOnlyItemStack stack, int slotIndex) => OnStackAdded?.Invoke(stack, slotIndex);
    [ObserversRpc] private void InvokeStackRemoved(IReadOnlyItemStack stack, int slotIndex) => OnStackRemoved?.Invoke(stack, slotIndex);
    [ObserversRpc] private void InvokeStackChanged(IReadOnlyItemStack stack, int slotIndex) => OnStackChanged?.Invoke(stack, slotIndex);

    #endregion

    #region Fields and Properties

    SyncArray<ItemStack> slots;
    public int EmptySlots => slots.Count(slot => slot == null);
    public int UsedSlots => slots.Count(slot => slot != null);
    public int TotalSlots => slots.Length;

    #endregion

    #region Constructor
    public Inventory(int size, params ItemStack[] startingItems)
    {
        size = Math.Max(size, 1);
        slots = new(size, ownerAuth: false);
        OnStackChanged += (_, index) => slots.SetDirty(index);

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
        if (stack == null || stack.Quantity <= 0) return 0;
        int totalAdded = Add(stack.ItemData, stack.Quantity);
        if (modifyInputStack) stack.RemoveFromStack(totalAdded);
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

            if(added > 0) InvokeStackChanged(slots[index], index);
            totalAmountAdded += added;
            quantity -= added;
        }

        while (quantity > 0 && TryFindEmptySlot(out int emptySlotIndex))
        {
            int possibleToAdd = Mathf.Min(quantity, itemData.MaxStackSize);
            slots[emptySlotIndex] = new ItemStack(itemData, possibleToAdd);
            InvokeStackAdded(slots[emptySlotIndex], emptySlotIndex);
            totalAmountAdded += possibleToAdd;
            quantity -= possibleToAdd;
        }

        if (totalAmountAdded > 0) InvokeItemAdded(itemData, totalAmountAdded);

        if (IsInventoryFull()) InvokeInventoryFull();

        return totalAmountAdded;
    }
    /// <summary>
    /// Adds one of the given item to the collection.
    /// </summary>
    /// <param name="itemData">The item to add to the collection. Cannot be null.</param>
    /// <returns>true if successful.</returns>
    public bool TryAddOne(ItemData itemData) => Add(itemData, 1) > 0;

    /// <summary>
    /// Attempts to add the specified quantity of the given item to the inventory only if the entire quantity can be
    /// added without exceeding stack or slot limits.
    /// </summary>
    /// <remarks>This method does not add any items if the full quantity cannot be accommodated. No partial
    /// addition occurs. The method checks for available space in existing stacks and empty slots as needed.</remarks>
    /// <param name="itemData">The item to add to the inventory. Cannot be null.</param>
    /// <param name="quantity">The exact number of items to add. Must be greater than zero.</param>
    /// <returns>true if the entire quantity was added to the inventory. Otherwise, false.</returns>
    public bool TryAddExact(ItemData itemData, int quantity)
    {
        if (!CanFit(itemData, quantity)) return false;

        Add(itemData, quantity);
        return true;
    }
    #endregion

    #region Get Methods
    public bool TryGet(int slotIndex, out IReadOnlyItemStack itemStack)
    {
        itemStack = null;

        if (slotIndex < 0 || slotIndex >= slots.Length) return false;

        itemStack = slots[slotIndex];
        return itemStack != null;

    }
    public bool TryGet(ItemData itemData, out IReadOnlyItemStack itemStack, out int slotIndex)
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
    /// Attempts to retrieve the next available item stack cycling after the specified index.
    /// </summary>
    /// <param name="fromIndex">The zero-based index from which to start the search for the next item stack.</param>
    /// <param name="stack">When this method returns, contains the next available item stack, if found; otherwise, null.</param>
    /// <param name="itemIndex">When this method returns, contains the index of the next available item stack, if found; otherwise, -1.</param>
    /// <returns><see langword="true"/> if a next item stack is found; otherwise, <see langword="false"/>.</returns>
    public bool TryGetNext(int fromIndex, out IReadOnlyItemStack stack, out int itemIndex)
    {
        return TryGetInDirection(fromIndex, direction: 1, out stack, out itemIndex);
    }
    /// <summary>
    /// Attempts to retrieve the next available item stack, cycling the contents backwards.
    /// </summary>
    /// <param name="fromIndex">The zero-based index from which to search backward for the previous item stack.</param>
    /// <param name="stack">When this method returns, contains the previous item stack if found; otherwise, <see langword="null"/>.</param>
    /// <param name="itemIndex">When this method returns, contains the index of the previous item stack if found; otherwise, -1.</param>
    /// <returns><see langword="true"/> if a previous item stack is found; otherwise, <see langword="false"/>.</returns>
    public bool TryGetPrevious(int fromIndex, out IReadOnlyItemStack stack, out int itemIndex)
    {
        return TryGetInDirection(fromIndex, direction: -1, out stack, out itemIndex);
    }

    /// <summary>
    /// Returns an enumerable readonly collection of all item stacks contained in the slots, null slots included.
    /// </summary>
    /// <returns>An of all <see cref="ItemStack"/>'s in the inventory that can be used to iterate through the slots.</returns>
    public IEnumerable<IReadOnlyItemStack> GetEnumeration()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            yield return slots[i]?.Clone();
        }
    }
    /// <summary>
    /// Returns a readonly enumeration for all non-empty item slots.
    /// </summary>
    /// <returns>An enumerable collection of <see cref="ItemStack"/> objects representing the non-null slots. The collection will
    /// be empty if all slots are null.</returns>
    public IEnumerable<IReadOnlyItemStack> GetNonEmptyEnumeration()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
                yield return slots[i].Clone();
        }
    }
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

        amount = Mathf.Min(amount, slots[fromIndex].Quantity);

        int amountTransfered = toInventory.Add(slots[fromIndex].ItemData, amount);
        if (amountTransfered == 0) return 0;

        Remove(fromIndex, amountTransfered);

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

            if (successfullyTransfered == 0) break; // break if transaction was unsuccessful (other inventory full)

            Remove(index, successfullyTransfered, notify: false);

            totalTransfered += successfullyTransfered;
            amount -= successfullyTransfered;

            if (slots[index].Quantity <= 0) ClearSlot(index);
            if (amount <= 0) break; // break if nothing left to transfer
        }

        if (totalTransfered > 0) InvokeItemRemoved(itemData, totalTransfered);
        return totalTransfered;
    }

    /// <summary>
    /// Attempts to transfer the specified quantity of an item to another inventory, only if the exact amount can be
    /// moved.
    /// </summary>
    /// <remarks>The transfer will only occur if the source inventory contains at least the specified amount
    /// and the destination inventory can accept the entire amount. No partial transfers are performed.</remarks>
    /// <param name="itemData">The item to transfer. Cannot be null.</param>
    /// <param name="amount">The exact quantity of the item to transfer. Must be greater than zero.</param>
    /// <param name="toInventory">The destination inventory to receive the item. Cannot be null.</param>
    /// <returns>true if the exact quantity of the item was successfully transferred to the target inventory. Otherwise,
    /// false.</returns>
    public bool TryTransferExact(ItemData itemData, int amount, Inventory toInventory)
    {
        if (toInventory == null) return false;
        if (amount <= 0 || itemData == null) return false;
        if (!EnoughQuantity(itemData, amount)) return false;
        if (!toInventory.TryAddExact(itemData, amount)) return false;

        Remove(itemData, amount);
        return true;
    }

    /// <summary>
    /// Attempts to transfer the entire item stack from the specified slot to the target inventory, requiring an exact
    /// fit.
    /// </summary>
    /// <remarks>The transfer only succeeds if the target inventory can accept the full quantity of the item
    /// stack without modification. If the transfer is successful, the source slot is cleared.</remarks>
    /// <param name="fromSlot">The zero-based index of the slot containing the item stack to transfer.</param>
    /// <param name="toInventory">The inventory to which the item stack will be transferred. Cannot be null.</param>
    /// <returns>true if the entire item stack was successfully transferred to the target inventory; otherwise, false.</returns>
    public bool TryTransferExact(int fromSlot, Inventory toInventory)
    {
        if (toInventory == null) return false;
        if(!TryGet(fromSlot, out IReadOnlyItemStack stack)) return false;
        if(!toInventory.TryAddExact(stack.GetItemData(), stack.GetQuantity())) return false;
        ClearSlot(fromSlot);
        return true;
    }

    #endregion

    #region Remove Methods
    /// <summary>
    /// Removes the specified quantity of items matching the given item data from the collection.
    /// </summary>
    /// <remarks>If the requested quantity exceeds the available amount, all matching items are removed and
    /// the actual number removed is returned.</remarks>
    /// <param name="itemData">The item data identifying the type of item to remove. Cannot be null.</param>
    /// <param name="quantity">The number of items to remove. Must be greater than zero.</param>
    /// <returns>The total number of items actually removed. Returns 0 if no items were removed.</returns>
    public int Remove(ItemData itemData, int quantity)
    {
        if (quantity <= 0 || itemData == null) return 0;

        List<int> sameItemSlots = FindSlotsWithItem(itemData);
        int totalAmountRemoved = 0;

        for (int i = 0; i < sameItemSlots.Count && quantity > 0; i++)
        {
            int index = sameItemSlots[i];

            int removed = Remove(index, quantity, notify: false);
            totalAmountRemoved += removed;
            quantity -= removed;
        }

        if (totalAmountRemoved > 0) InvokeItemRemoved(itemData, totalAmountRemoved);

        return totalAmountRemoved;
    }
    int Remove(int index, int quantity, bool notify)
    {
        var stack = slots[index];
        if (stack == null || quantity <= 0) return 0;

        int removed = stack.RemoveFromStack(quantity);
        if(removed > 0) InvokeStackChanged(stack, index);

        if (notify) InvokeItemRemoved(stack.GetItemData(), removed);
        if (stack.IsEmpty()) ClearSlot(index);

        return removed;
    }

    /// <summary>
    /// Remove the requested amount from the specific slot.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="quantity"></param>
    /// <returns></returns>
    public int Remove(int index, int quantity) => Remove(index, quantity, true);

    /// <summary>
    /// Removes the specified quantity of items from the collection based on the provided item stack.
    /// </summary>
    /// <param name="stack">The item stack specifying the item type and quantity to remove. The stack's quantity determines how many items
    /// to remove.</param>
    /// <returns>The number of items that were actually removed from the collection. This value may be less than the requested
    /// quantity if insufficient items are available.</returns>
    public int Remove(ItemStack stack) => stack != null ? Remove(stack.ItemData, stack.Quantity) : 0;

    /// <summary>
    /// Removes one of the specified item from the collection.
    /// </summary>
    /// <param name="itemData">The item to remove from the collection. Cannot be null.</param>
    /// <returns>true if the item was successfully removed. Otherwise, false.</returns>
    public bool TryRemoveOne(ItemData itemData) => Remove(itemData, 1) > 0;
    public bool TryRemoveOne(int index) => Remove(index, 1) > 0;

    /// <summary>
    /// Attempts to remove the specified quantity of the given item from the collection if the exact quantity is
    /// available.
    /// </summary>
    /// <remarks>No items are removed if the collection does not contain at least the specified quantity of
    /// the given item.</remarks>
    /// <param name="itemData">The item to remove from the collection. Cannot be null.</param>
    /// <param name="quantity">The exact quantity of the item to remove. Must be greater than zero.</param>
    /// <returns>true if the exact quantity of the item was present and removed. Otherwise, false.</returns>
    public bool TryRemoveExact(ItemData itemData, int quantity)
    {
        if (itemData == null || quantity <= 0) return false;

        if (EnoughQuantity(itemData, quantity))
        {
            Remove(itemData, quantity);
            return true;
        }
        return false;
    }

    #endregion

    #region Clear Methods
    /// <summary>
    /// Clears the contents of the specified slot, setting it to null if the index is valid.
    /// </summary>
    /// <param name="slotIndex">The index of the slot to clear. Must be greater than or equal to 0 and less than the total number of
    /// slots.</param>
    public void ClearSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Length) return;

        ItemStack stackToRemove = slots[slotIndex];
        slots[slotIndex] = null;

        if (stackToRemove != null)
        {
            if (!stackToRemove.IsEmpty())
                InvokeItemRemoved(stackToRemove.GetItemData(), stackToRemove.GetQuantity());

            InvokeStackRemoved(stackToRemove, slotIndex);
        }
    }

    /// <summary>
    /// Removes all items from the collection, resetting it to an empty state.
    /// </summary>
    /// <remarks>Use this method to clear all item slots at once. After calling this method, the collection
    /// will contain no items, and its size remains unchanged.</remarks>
    public void Clear()
    {
        for (int i = 0; i < slots.Length; i++) ClearSlot(i);
    }
    #endregion

    #endregion

    #region Helper Methods
    private bool TryGetInDirection(int fromIndex, int direction, out IReadOnlyItemStack stack, out int itemIndex)
    {
        itemIndex = fromIndex;
        stack = null;

        bool invalidIndex = itemIndex < 0 || itemIndex >= TotalSlots;

        if (invalidIndex)
        {
            itemIndex = -1;
            return false;
        }

        for (int i = 0; i < TotalSlots; i++)
        {
            itemIndex = (itemIndex + direction + TotalSlots) % TotalSlots;

            if (TryGet(itemIndex, out stack)) return true;
        }

        itemIndex = -1;
        return false;
    }
    private bool TryFindEmptySlot(out int index)
    {
        index = -1;

        for (int i = 0; i < slots.Length; i++)
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
        if (itemData == null) return indices;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && slots[i].ItemData == itemData)
            {
                indices.Add(i);
            }
        }
        return indices;
    }
    public bool IsInventoryFull() => !slots.Any(slot => slot == null || slot.GetRemainingCapacity() > 0);

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
        if (quantity < 0 || itemData == null) return false;
        if (quantity == 0) return true;

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
    /// their contents are swapped. Cancels if contents of <paramref name="fromSlot"/> are null.</remarks>
    /// <param name="fromSlot">The zero-based index of the source slot to move or combine items from. Must be within the valid range of slot
    /// indices.</param>
    /// <param name="toSlot">The zero-based index of the destination slot to move or combine items to. Must be within the valid range of slot
    /// indices.</param>
    public void MoveSlot(int fromSlot, int toSlot)
    {
        if (fromSlot == toSlot) return;
        if (TryCombine(fromSlot, toInventory: this, toSlot, out bool operationValid)) return;
        if (!operationValid) return;

        ItemStack temp = slots[fromSlot];
        slots[fromSlot] = slots[toSlot];
        slots[toSlot] = temp;

        InvokeSlotsSwapped(fromSlot, toSlot);
    }
    /// <summary>
    /// <inheritdoc cref = "MoveSlot(int, int)"/>
    /// </summary>
    /// <remarks>
    /// <inheritdoc cref = "MoveSlot(int, int)"/>
    /// </remarks>
    public void MoveSlot(int fromSlot, Inventory toInventory, int toSlot)
    {
        if (toInventory == null) return;

        if (toInventory == this)
        {
            MoveSlot(fromSlot, toSlot);
            return;
        }

        if (TryCombine(fromSlot, toInventory: toInventory, toSlot, out bool operationValid)) return;
        if (!operationValid) return;

        ItemStack otherStack = toInventory.Detach(toSlot);
        ItemStack thisStack = Detach(fromSlot);

        toInventory.slots[toSlot] = thisStack;
        slots[fromSlot] = otherStack;

        toInventory.InvokeItemAdded(thisStack.GetItemData(), thisStack.GetQuantity());
        toInventory.InvokeStackAdded(thisStack, toSlot);

        if (otherStack != null)
        {
            InvokeItemAdded(otherStack.GetItemData(), otherStack.GetQuantity());
            InvokeStackAdded(otherStack, fromSlot);
        }
    }
    private bool TryCombine(int fromSlot, Inventory toInventory, int toSlot, out bool operationValid)
    {
        operationValid = false;
        if (fromSlot < 0 || fromSlot >= slots.Length) return false;
        if (toSlot < 0 || toSlot >= toInventory.slots.Length) return false;

        ItemStack thisStack = slots[fromSlot];
        ItemStack otherStack = toInventory.slots[toSlot];

        if (thisStack == null) return false;
        operationValid = true;

        bool sameItem = otherStack != null && thisStack.ItemData == otherStack.ItemData;

        if (sameItem)
        {
            int added = otherStack.AddToStack(thisStack.Quantity);

            if (added > 0)
            {
                thisStack.RemoveFromStack(added);

                if (thisStack.IsEmpty()) ClearSlot(fromSlot);
                else InvokeStackChanged(thisStack, fromSlot);

                toInventory.InvokeStackChanged(otherStack, toSlot);
                return true;
            }
        }

        return false;
    }
    private ItemStack Detach(int fromSlot)
    {
        var stack = slots[fromSlot];
        ClearSlot(fromSlot);
        return stack;
    }

    /// <summary>
    /// Determines whether the collection contains at least one slot with the specified item and a quantity greater than
    /// zero.
    /// </summary>
    /// <param name="data">The item to locate in the collection. Cannot be null.</param>
    /// <returns>true if a slot containing the specified item with a quantity greater than zero is found; otherwise, false.</returns>
    public bool Contains(ItemData data) => slots.Any(s => s != null && s.ItemData == data && s.Quantity > 0);

    /// <summary>
    /// Determines whether the specified quantity of the given item can fit into the inventory, considering current
    /// stack sizes and available slots.
    /// </summary>
    /// <remarks>This method accounts for partially filled stacks of the same item and available empty slots.
    /// It does not modify the inventory.</remarks>
    /// <param name="data">The item to check for available space in the inventory. Cannot be null.</param>
    /// <param name="quantity">The number of items to check for available space. Must be greater than zero.</param>
    /// <returns>true if the entire quantity of the item can fit into the inventory; otherwise, false.</returns>
    public bool CanFit(ItemData data, int quantity)
    {
        if (quantity < 0 || data == null) return false;

        int totalRemaining = quantity;
        List<int> sameItemSlots = FindSlotsWithItem(data);
        for (int i = 0; i < sameItemSlots.Count && totalRemaining > 0; i++)
        {
            int index = sameItemSlots[i];
            if (slots[index].IsFull()) continue;
            int used = Mathf.Min(totalRemaining, slots[index].GetRemainingCapacity());
            totalRemaining -= used;
        }
        if (totalRemaining <= 0) return true;
        int stacksNeeded = Mathf.CeilToInt((float)totalRemaining / data.MaxStackSize);
        return stacksNeeded <= EmptySlots;
    }
    #endregion
}