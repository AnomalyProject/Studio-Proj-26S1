using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField, Min(1)] int inventorySize = 5;
    [SerializeField] Transform itemHolder;

    public Inventory inventory { get; private set; }
    Dictionary<ItemData, GameObject> itemInstances = new();

    int focusedIndex = 0;
    GameObject activeInstance;

    void Awake()
    {
        inventory = new Inventory(inventorySize);

        inventory.OnItemAdded += HandleItemAddition;
        inventory.OnItemRemoved += HandleItemRemoval;
        inventory.OnSlotsMoved += HandleSlotsMoved;
    }

    #region Inventory Event Subscribers
    void HandleSlotsMoved()
    {
        if(inventory.TryGet(focusedIndex, out var stack) && itemInstances.TryGetValue(stack.GetItemData(), out GameObject actualInstance))
        {
            if(actualInstance != activeInstance)
            {
                activeInstance.SetActive(false);
                activeInstance = actualInstance;
                activeInstance.SetActive(true);
            }
        }
    }
    void HandleItemRemoval(ItemData data, int amount)
    {
        if (!inventory.Contains(data) && itemInstances.TryGetValue(data, out GameObject instance))
        {
            Destroy(instance);
            itemInstances.Remove(data);
        }
    }
    void HandleItemAddition(ItemData data, int amount)
    {
        if (data.ItemPrefab == null) return;

        if (!itemInstances.ContainsKey(data))
        {
            var itemInstance = Instantiate(data.ItemPrefab, itemHolder);
            itemInstances.Add(data, itemInstance);
            itemInstance.SetActive(false);
        }
    }
    #endregion

    #region Inventory Control
    public void NextItem()
    {
        int current = focusedIndex + 1;

        while(current != focusedIndex)
        {
            if(current >= inventory.TotalSlots) current = 0;

            if(inventory.TryGet(current, out IReadOnlyItemStack stack))
            {
                ChangeFocused(current);
                break;
            }

            current++;
        }
    }
    public void PreviousItem()
    {
        int current = focusedIndex - 1;

        while (current != focusedIndex)
        {
            if (current < 0) current = inventory.TotalSlots - 1;

            if (inventory.TryGet(current, out IReadOnlyItemStack stack))
            {
                ChangeFocused(current);
                break;
            }

            current--;
        }
    }
    public void ChangeFocused(int focusAtIndex)
    {
        if (focusedIndex == focusAtIndex) return;
        if (focusAtIndex >= inventory.TotalSlots || focusAtIndex < 0) return;

        IReadOnlyItemStack stack;

        if(inventory.TryGet(focusedIndex, out stack) && itemInstances.TryGetValue(stack.GetItemData(), out activeInstance))
        {
            activeInstance.SetActive(false);
        }

        focusedIndex = focusAtIndex;

        if (inventory.TryGet(focusedIndex, out stack) && itemInstances.TryGetValue(stack.GetItemData(), out activeInstance))
        {
            activeInstance.SetActive(true);
        }
    }
    public bool TryUseFocused()
    {
        if (!inventory.TryGet(focusedIndex, out IReadOnlyItemStack stack)) return false; // Check if item exists in the inventory
        if (!itemInstances.TryGetValue(stack.GetItemData(), out GameObject instance)) return false; // Try get item's world instance
        if (!InteractionSystem<MonoBehaviour>.TryGetInteractable(instance, out var interactable)) return false; // Check if its interactable
        
        bool success = interactable.TryInteract(this); // Try interact with instance.
        if(success && stack.GetItemData().IsConsumable) inventory.TryRemoveOne(stack.GetItemData()); // Deplete if consumable.

        return success;
    }
    #endregion
}