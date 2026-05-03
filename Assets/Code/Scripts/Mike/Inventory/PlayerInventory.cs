using PurrNet;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;


[RequireComponent(typeof(PlayerBody))]
public class PlayerInventory : NetworkBehaviour
{
    public event Action<ItemData> OnFocusedChanged, OnItemUsed;

    [SerializeField, Min(1)] private int inventorySize = 5;
    [SerializeField] private Transform itemHolder;
    [SerializeField] private UnityEvent<ItemData> _OnFocusedChanged, _OnItemUsed;
    [SerializeField] private UnityEvent<ItemData, int> OnItemAdded, OnItemRemoved;

    public Inventory Inventory { get; private set; }

    public int focusedSlot { get; private set; } = 0;
    private GameObject activeInstance;
    private Dictionary<string, GameObject> itemInstances = new();
    private PlayerBody playerBody;
    private Task<bool> currentUseTask;

    private void Awake()
    {
        playerBody = GetComponent<PlayerBody>();
        Inventory = new Inventory(inventorySize);
    }

    protected override void OnSpawned()
    {
        if (!isOwner) return;
        Inventory.OnSlotsSwapped += HandleSlotsSwapped;
        Inventory.OnStackAdded += HandleStackCreation;
        Inventory.OnStackRemoved += HandleStackRemoval;

        Inventory.OnItemAdded += OnItemAdded.Invoke;
        Inventory.OnItemRemoved += OnItemRemoved.Invoke;
        OnFocusedChanged += _OnFocusedChanged.Invoke;
        OnItemUsed += _OnItemUsed.Invoke;
    }

    public void DebugInventory()
    {
        foreach(var item in Inventory.GetEnumeration())
        {
            if (item == null) Debug.Log("Empty Slot");
            else Debug.Log($"Item: {item.GetItemData().name}, Quantity: {item.GetQuantity()}");
        }

        foreach(var item in itemInstances)
        {
            if (item.Value == null) Debug.Log("Empty Slot");
            else Debug.Log($"Instances: Item Key: {item.Key}, Item Object: {item.Value.name}");
        }
    }

    #region Inventory Event Subscribers
    private void HandleSlotsSwapped(int fromSlot, int toSlot)
    {
        if (fromSlot != focusedSlot) return;

        if (Inventory.TryGet(focusedSlot, out var stack))
        {
            GameObject itemInstance = itemInstances[stack.GetID()];

            if (itemInstance != activeInstance)
            {
                if (activeInstance != null) activeInstance.SetActive(false);
                activeInstance = itemInstance;
                if (activeInstance != null) activeInstance.SetActive(true);
            }
        }
    }
    private void HandleStackCreation(IReadOnlyItemStack stack, int slotIndex)
    {
        if (stack.GetItemData().ItemPrefab == null) return;

        GameObject itemObject = Instantiate(stack.GetItemData().ItemPrefab, parent: itemHolder);
        itemInstances.Add(stack.GetID(), itemObject);

        if (activeInstance == null) ChangeFocused(slotIndex);
        else itemObject.SetActive(false);
    }
    private void HandleStackRemoval(IReadOnlyItemStack stack, int slotIndex)
    {
        if (!itemInstances.TryGetValue(stack.GetID(), out GameObject itemInstance)) return;

        if (activeInstance == itemInstance)
        {
            activeInstance = null;

            if (Inventory.UsedSlots > 0) NextItem();
        }

        Destroy(itemInstance);
        itemInstances.Remove(stack.GetID());
    }
    #endregion

    #region Inventory Control
    public void NextItem()
    {
        DebugInventory();
        if (Inventory.TryGetNext(focusedSlot, out var stack, out int nextIndex))
        {
            ChangeFocused(nextIndex);
            return;
        }
    }
    public void PreviousItem()
    {
        if (Inventory.TryGetPrevious(focusedSlot, out IReadOnlyItemStack stack, out int nextIndex))
        {
            ChangeFocused(nextIndex);
            return;
        }
    }
    public void ChangeFocused(int focusAtIndex)
    {
        bool differentIndex = focusedSlot != focusAtIndex;

        if (!differentIndex && activeInstance != null) return;
        if (focusAtIndex >= Inventory.TotalSlots || focusAtIndex < 0) return;

        IReadOnlyItemStack stack;
        GameObject itemObject = null;

        if (differentIndex && Inventory.TryGet(focusedSlot, out stack) && itemInstances.TryGetValue(stack.GetID(), out itemObject))
        {
            itemObject?.SetActive(false);
        }

        focusedSlot = focusAtIndex;

        if (Inventory.TryGet(focusedSlot, out stack) && itemInstances.TryGetValue(stack.GetID(), out itemObject))
        {
            itemObject?.SetActive(true);
        }

        activeInstance = itemObject;

        if (stack != null) OnFocusedChanged?.Invoke(stack.GetItemData());
    }
    public async Task<bool> TryUseFocused()
    {
        if (!Inventory.TryGet(focusedSlot, out IReadOnlyItemStack stack)) return false; // Check if item exists in the inventory
        if(!itemInstances.TryGetValue(stack.GetID(), out GameObject itemInstance)) return false; // Try get item's world instance

        if (!InteractionUtils.TryGetInteractable<PlayerBody>(itemInstance, out IInteractable<PlayerBody> interactable)) return false; // Check if its interactable, could get refactored in the future   
        bool success = await interactable.TryInteract(playerBody); // Try interact with instance.

        if(success)
        {
            await RegisterUsage_ServerRpc(focusedSlot);
            OnItemUsed?.Invoke(stack.GetItemData());
        }
        return success;
    }
    public bool CanUseFocused()
    {
        if (!Inventory.TryGet(focusedSlot, out IReadOnlyItemStack stack)) return false; // Check if item exists in the inventory
        if (!itemInstances.TryGetValue(stack.GetID(), out GameObject itemInstance)) return false; // Try get item's world instance
        if (!InteractionUtils.TryGetInteractable<PlayerBody>(itemInstance, out IInteractable<PlayerBody> interactable)) return false; // Check if its interactable, could get refactored in the future   
        return interactable.CanInteract(playerBody).Result; // Check if you can interact with instance.
    }

    [ServerRpc] async Task RegisterUsage_ServerRpc(int slotIndex)
    {
        if (!Inventory.TryGet(slotIndex, out IReadOnlyItemStack stack)) await Task.CompletedTask;
        if (stack.GetItemData().IsConsumable) Inventory.TryRemoveOne(slotIndex);
    }

    #region Input Actions
    public void UseFocused(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            if(currentUseTask != null && !currentUseTask.IsCompleted) return;
            currentUseTask = TryUseFocused();
        }
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
        Inventory.TryGet(focusedSlot, out var stack);
        return stack;
    }
    #endregion
}