using System;
using System.Collections.Generic;
using UnityEngine;

public class ItemStack : IReadOnlyItemStack
{
    #region Fields and Properties
    public ItemData ItemData { get; }
    public int Quantity  { get; private set; }
    public string Id { get; } = Guid.NewGuid().ToString();
    private Dictionary<string, object> metadata;

    #endregion

    #region Constructors
    public ItemStack(ItemData itemData, int quantity, Dictionary<string, object> metadata = null)
    {
        this.ItemData = itemData ?? throw new ArgumentNullException(nameof(itemData));
        this.Quantity = Math.Clamp(quantity, 0, itemData.MaxStackSize);
        this.metadata = metadata;
    }
    public ItemStack(ItemData itemData, Dictionary<string, object> metadata = null)
    {
        this.ItemData = itemData ?? throw new ArgumentNullException(nameof(itemData));
        Quantity = 1;
        this.metadata = metadata;
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

        int spaceLeft = ItemData.MaxStackSize - Quantity;
        int added = Mathf.Min(amount, spaceLeft);

        Quantity += added;

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

        int removed = Mathf.Min(amount, Quantity);

        Quantity -= removed;

        return removed;
    }

    /// <summary>
    /// Removes a single item from the stack, if available.
    /// </summary>
    /// <returns>true if one item was successfully removed; otherwise, false.</returns>
    public bool TryRemoveOne() => RemoveFromStack(1) == 1;
    public bool IsFull() => Quantity >= ItemData.MaxStackSize;
    public bool CanFit(int amount) => Quantity + amount <= ItemData.MaxStackSize;
    public int GetQuantity() => Quantity;
    public ItemData GetItemData() => ItemData;
    public bool IsEmpty() => Quantity <= 0;
    public int GetRemainingCapacity() => ItemData.MaxStackSize - Quantity;
    public int GetMaxCapacity() => ItemData.MaxStackSize;
    public string GetID() => Id;
    public ItemStack Clone()
    {
        var clone = new ItemStack(ItemData, Quantity);
        if (metadata != null) clone.metadata = new Dictionary<string, object>(metadata);
        return clone;
    }
    public ItemStack CloneWithQuantity(int quantity)
    {
        var clone = new ItemStack(ItemData, quantity);
        if (metadata != null) clone.metadata = new Dictionary<string, object>(metadata);
        return clone;
    }

    #region Metadata
    public bool TryGetMeta<T>(string key, out T value)
    {
        value = default;
        if (metadata == null || !metadata.TryGetValue(key, out object raw) || raw is not T typed) return false;
        value = typed;
        return true;
    }
    public T GetMeta<T>(string key, T fallback = default) => TryGetMeta(key, out T value)? value : fallback;
    public void SetMeta(string key, object value)
    {
        metadata ??= new Dictionary<string, object>();
        metadata[key] = value;
    }
    public bool HasMetadata() => metadata != null && metadata.Count > 0;
    public IReadOnlyDictionary<string, object> GetMetadata() => metadata;
    #endregion

    #endregion
}
[Serializable] public class InspectorItemStack
{
    [SerializeField] ItemData itemData;
    [SerializeField, Min(1)] int quantity;
    private ItemStack _cachedStack;

    public ItemData Data => itemData;
    public int Quantity => quantity;

    /// <summary>
    /// Gets or creates an <see cref="ItemStack"/> instance based on the data stored in this <see cref="InspectorItemStack"/> and internally caches it.
    /// </summary>
    /// <returns>The created or cached <see cref="ItemStack"/> instacne.</returns>
    public ItemStack GetItemStack() => _cachedStack??= new ItemStack(itemData, quantity);
    /// <summary>
    /// Creates a new <see cref="ItemStack"/> instance based on the data stored in this <see cref="InspectorItemStack"/> and updates the internal cache with it, if cache parameter is true.
    /// </summary>
    /// <returns>The new cached <see cref="ItemStack"/> instacne.</returns>
    public ItemStack CreateItemStack(bool cache = true)
    {
        ItemStack newStack = new ItemStack(itemData, quantity);
        if (cache) _cachedStack = newStack;
        return newStack;
    }

    public void SetStack(ItemStack stack)
    {
        itemData = stack.ItemData;
        quantity = stack.Quantity;
        _cachedStack = stack;
    }

    /// <summary>
    /// Validates the current quantity value, ensuring it is within the allowed range for the associated item.
    /// </summary>
    /// <remarks>If the associated item data is null, the quantity is reset to zero. Otherwise, the quantity
    /// is clamped between 1 and the item's maximum stack size. This method should be called after modifying the
    /// quantity or item data to maintain valid state.</remarks>
    public void Validate()
    {
        if (itemData == null)
        {
            quantity = 0;
            return;
        }

        quantity = Mathf.Clamp(quantity, 1, itemData.MaxStackSize);
    }
}

public interface IReadOnlyItemStack
{
    string GetID();
    int GetQuantity();
    ItemData GetItemData();
    bool IsFull();
    bool IsEmpty();
    /// <summary>
    /// Determines whether the specified amount can be added without exceeding the maximum stack size.
    /// </summary>
    /// <param name="amount">The number of items to check for available space in the stack. Must be zero or greater.</param>
    /// <returns>true if the specified amount can be added. Otherwise false.</returns>
    bool CanFit(int amount);
    int GetRemainingCapacity();
    int GetMaxCapacity();
    bool TryGetMeta<T>(string key, out T value);
    T GetMeta<T>(string key, T fallback = default);
    ItemStack Clone();
    /// <summary>
    /// Clones the stack with a new quantity but also passes metadata.
    /// </summary>
    /// <param name="quantity"></param>
    /// <returns></returns>
    ItemStack CloneWithQuantity(int quantity);
}