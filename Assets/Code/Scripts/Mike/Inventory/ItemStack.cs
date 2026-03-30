using System;
using UnityEngine;

public class ItemStack
{
    #region Fields and Properties

    // Fields
    ItemData itemData;
    int quantity;

    //Properties
    public ItemData ItemData => itemData;
    public int Quantity => quantity;
    public bool IsEmpty => quantity <= 0;
    public int RemainingCapacity => itemData.MaxStackSize - quantity;

    #endregion

    #region Constructors
    public ItemStack(ItemData itemData, int quantity)
    {
        this.itemData = itemData ?? throw new ArgumentNullException(nameof(itemData));
        this.quantity = Math.Clamp(quantity, 0, itemData.MaxStackSize);
    }
    public ItemStack(ItemData itemData)
    {
        this.itemData = itemData;
        quantity = 1;
    }
    #endregion

    #region Exposed Methods

    /// <summary>
    /// Attempts to add the specified number of items to the stack, up to the maximum stack size.
    /// </summary>
    /// <remarks>If the requested amount exceeds the available space in the stack, only the maximum possible
    /// number of items will be added.</remarks>
    /// <param name="amount">The number of items to add to the stack. Must be greater than 0.</param>
    /// <returns>The actual number of items added to the stack. Returns 0 if the amount is less than or equal to 0 or if the
    /// stack is already full.</returns>
    public int AddToStack(int amount)
    {
        if (amount <= 0) return 0;

        int spaceLeft = itemData.MaxStackSize - quantity;
        int added = Mathf.Min(amount, spaceLeft);

        quantity += added;

        return added;
    }

    /// <summary>
    /// Attempts to add 1 to the stack.
    /// </summary>
    /// <returns>true if 1 was successfully added to the stack; otherwise, false.</returns>
    public bool TryAddOne() => AddToStack(1) == 1;

    /// <summary>
    /// Removes the specified amount from the stack and updates the quantity accordingly.
    /// </summary>
    /// <remarks>If the requested amount exceeds the current quantity, the stack is emptied and the method
    /// returns the number of items that successfully got removed.</remarks>
    /// <param name="amount">The number of items to remove from the stack. Must be greater than zero.</param>
    /// <returns>The number of items successfully removed.</returns>
    public int RemoveFromStack(int amount)
    {
        if (amount <= 0) return 0;

        int removed = Mathf.Min(amount, quantity);

        quantity -= removed;

        return removed;
    }

    /// <summary>
    /// Removes a single item from the stack, if available.
    /// </summary>
    /// <returns>true if one item was successfully removed; otherwise, false.</returns>
    public bool TryRemoveOne() => RemoveFromStack(1) == 1;
    public bool IsFull() => quantity >= itemData.MaxStackSize;

    /// <summary>
    /// Determines whether the specified amount can be added without exceeding the maximum stack size.
    /// </summary>
    /// <param name="amount">The number of items to check for available space in the stack. Must be zero or greater.</param>
    /// <returns>true if the specified amount can be added. Otherwise false.</returns>
    public bool CanFit(int amount) => quantity + amount <= itemData.MaxStackSize;
    #endregion
}
