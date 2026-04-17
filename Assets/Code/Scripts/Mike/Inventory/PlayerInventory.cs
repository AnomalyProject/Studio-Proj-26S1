using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(PlayerBody))]
public class PlayerInventory : MonoBehaviour
{
    public event Action<ItemData> OnFocusedChanged, OnItemUsed;

    [SerializeField, Min(1)] int inventorySize = 5;
    [SerializeField] Transform itemHolder;

    public Inventory Inventory { get; private set; }

    int focusedIndex = 0;
    GameObject activeInstance;
    Dictionary<IReadOnlyItemStack, GameObject> itemInstances = new();
    PlayerBody playerBody;

    void Awake()
    {
        playerBody = GetComponent<PlayerBody>();
        Inventory = new Inventory(inventorySize);

        Inventory.OnSlotsMoved += HandleSlotsMoved;
        Inventory.OnStackAdded += HandleStackCreation;
        Inventory.OnStackRemoved += HandleStackRemoval;
    }

    #region Inventory Event Subscribers
    void HandleSlotsMoved()
    {
        if(Inventory.TryGet(focusedIndex, out var stack))
        {
            GameObject itemInstance = itemInstances[stack];

            if (itemInstance != activeInstance)
            {
                if (activeInstance != null) activeInstance.SetActive(false);
                activeInstance = itemInstance;
                if (activeInstance != null) activeInstance.SetActive(true);
            }
        }
    }
    void HandleStackCreation(IReadOnlyItemStack stack, int slotIndex)
    {
        if (stack.GetItemData().ItemPrefab == null) return;

        GameObject itemObject = Instantiate(stack.GetItemData().ItemPrefab, parent: itemHolder);
        itemInstances.Add(stack, itemObject);

        if (activeInstance == null) ChangeFocused(slotIndex);
        else itemObject.SetActive(false);
    }
    void HandleStackRemoval(IReadOnlyItemStack stack, int slotIndex)
    {
        if (!itemInstances.TryGetValue(stack, out GameObject itemInstance)) return;

        if (activeInstance == itemInstance)
        {
            activeInstance = null;

            if (Inventory.UsedSlots > 0) NextItem();
        }

        Destroy(itemInstance);
        itemInstances.Remove(stack);
    }
    #endregion

    #region Inventory Control
    public void NextItem()
    {
        if (Inventory.TryGetNext(focusedIndex, out var stack, out int nextIndex))
        {
            ChangeFocused(nextIndex);
            return;
        }
    }
    public void PreviousItem()
    {
        if (Inventory.TryGetPrevious(focusedIndex, out IReadOnlyItemStack stack, out int nextIndex))
        {
            ChangeFocused(nextIndex);
            return;
        }
    }
    public void ChangeFocused(int focusAtIndex)
    {
        bool differentIndex = focusedIndex != focusAtIndex;

        if (!differentIndex && activeInstance != null) return;
        if (focusAtIndex >= Inventory.TotalSlots || focusAtIndex < 0) return;

        IReadOnlyItemStack stack;
        GameObject itemObject = null;

        if (differentIndex && Inventory.TryGet(focusedIndex, out stack) && itemInstances.TryGetValue(stack, out itemObject))
        {
            itemObject?.SetActive(false);
        }

        focusedIndex = focusAtIndex;

        if (Inventory.TryGet(focusedIndex, out stack) && itemInstances.TryGetValue(stack, out itemObject))
        {
            itemObject?.SetActive(true);
        }

        activeInstance = itemObject;

        if (stack != null) OnFocusedChanged?.Invoke(stack.GetItemData());
    }
    public bool TryUseFocused()
    {
        if(!Inventory.TryGet(focusedIndex, out IReadOnlyItemStack stack)) return false; // Check if item exists in the inventory
        if(!itemInstances.TryGetValue(stack, out GameObject itemInstance)) return false; // Try get item's world instance

        if (!InteractionUtils.TryGetInteractable<PlayerBody>(itemInstance, out IInteractable<PlayerBody> interactable)) return false; // Check if its interactable, could get refactored in the future
        
        bool success = interactable.TryInteract(playerBody); // Try interact with instance.

        if(success)
        {
            if(stack.GetItemData().IsConsumable)
            Inventory.TryRemoveOne(focusedIndex); // Deplete if consumable.

            OnItemUsed?.Invoke(stack.GetItemData());
        }

        return success;
    }

    #region Input Actions
    public void UseFocused(InputAction.CallbackContext ctx)
    {
        if (ctx.started) TryUseFocused();
    }
    public void NextItem(InputAction.CallbackContext ctx)
    {
        if (ctx.started) NextItem();
    }
    public void PreviousItem(InputAction.CallbackContext ctx)
    {
        if (ctx.started) PreviousItem();
    }
    #endregion

    #endregion

    #region Helpers
    public IReadOnlyItemStack GetFocusedItem()
    {
        Inventory.TryGet(focusedIndex, out var stack);
        return stack;
    }
    #endregion
}