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
    public event Action<int, int> OnFocusedIndexChanged;

    [Header("Inventory Setup")]
    [SerializeField, Min(1)] private int inventorySize = 5;
    [SerializeField] private Transform itemHolder;

    [Header("Unity Events")]
    [SerializeField] private UnityEvent<ItemData> _OnFocusedChanged, _OnItemUsed;
    [SerializeField] private UnityEvent<ItemData, int> OnItemAdded, OnItemRemoved;

    [Header("Item Discard Options")]
    [SerializeField] private float throwItemForce = 5f;
    [SerializeField, Range(5, 30)] private float destroyDropAfterSeconds = 15f;

    public Inventory Inventory { get; private set; }

    public int focusedSlot { get; private set; } = 0;
    private GameObject activeInstance;
    private Dictionary<string, PlayerItem> itemInstances = new();
    private PlayerBody playerBody;
    private Task<bool> currentUseTask;
    public bool IsUsingItem => currentUseTask != null && !currentUseTask.IsCompleted;

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
        Inventory.OnStackChanged += HandleStackChanged;
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
            PlayerItem itemInstance = itemInstances[stack.GetID()];

            if (itemInstance != activeInstance)
            {
                if (activeInstance != null) activeInstance.SetActive(false);
                activeInstance = itemInstance.gameObject;
                if (activeInstance != null) activeInstance.SetActive(true);
            }
        }
    }
    private void HandleStackCreation(IReadOnlyItemStack stack, int slotIndex)
    {
        if (stack.GetItemData().ItemPrefab == null) return;

        PlayerItem itemObject = Instantiate(stack.GetItemData().ItemPrefab, parent: itemHolder);
        itemInstances.Add(stack.GetID(), itemObject);
        itemObject.BindTo(this, stack, slotIndex);

        if (activeInstance == null) ChangeFocused(slotIndex);
        else itemObject.gameObject.SetActive(false);
    }
    private void HandleStackChanged(IReadOnlyItemStack stack, int slotIndex)
    {
        if (itemInstances.TryGetValue(stack.GetID(), out PlayerItem item)) item.BindTo(this, stack, slotIndex);
    }
    private void HandleStackRemoval(IReadOnlyItemStack stack, int slotIndex)
    {
        if (!itemInstances.TryGetValue(stack.GetID(), out PlayerItem itemInstance)) return;

        if (activeInstance == itemInstance)
        {
            activeInstance = null;

            if (Inventory.UsedSlots > 0) NextItem();
        }

        Destroy(itemInstance.gameObject);
        itemInstances.Remove(stack.GetID());
    }
    #endregion

    #region Inventory Control
    private void NextItem()
    {
        DebugInventory();
        if (Inventory.TryGetNext(focusedSlot, out var stack, out int nextIndex))
        {
            ChangeFocused(nextIndex);
            return;
        }
    }
    private void PreviousItem()
    {
        if (Inventory.TryGetPrevious(focusedSlot, out IReadOnlyItemStack stack, out int nextIndex))
        {
            ChangeFocused(nextIndex);
            return;
        }
    }
    public void ChangeFocused(int focusAtIndex)
    {
        if(IsUsingItem) return;

        bool differentIndex = focusedSlot != focusAtIndex;

        if (!differentIndex && activeInstance != null) return;
        if (focusAtIndex >= Inventory.TotalSlots || focusAtIndex < 0) return;

        IReadOnlyItemStack stack;
        PlayerItem itemObject = null;

        if (differentIndex && Inventory.TryGet(focusedSlot, out stack) && itemInstances.TryGetValue(stack.GetID(), out itemObject))
        {
            itemObject.gameObject?.SetActive(false);
        }

        int previous = focusedSlot;
        focusedSlot = focusAtIndex;

        if (Inventory.TryGet(focusedSlot, out stack) && itemInstances.TryGetValue(stack.GetID(), out itemObject))
        {
            itemObject.gameObject?.SetActive(true);
        }

        activeInstance = itemObject?.gameObject;
        OnFocusedIndexChanged?.Invoke(previous, focusedSlot);

        if (stack != null) OnFocusedChanged?.Invoke(stack.GetItemData());
    }
    private async Task<bool> TryUseFocused()
    {
        if (!Inventory.TryGet(focusedSlot, out IReadOnlyItemStack stack)) return false; // Check if item exists in the inventory
        if(!itemInstances.TryGetValue(stack.GetID(), out PlayerItem itemInstance)) return false; // Try get item's world instance

        if (!InteractionUtils.TryGetInteractable<PlayerBody>(itemInstance.gameObject, out IInteractable<PlayerBody> interactable)) return false; // Check if its interactable, could get refactored in the future   
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
        if (!itemInstances.TryGetValue(stack.GetID(), out PlayerItem itemInstance)) return false; // Try get item's world instance
        if (!InteractionUtils.TryGetInteractable<PlayerBody>(itemInstance.gameObject, out IInteractable<PlayerBody> interactable)) return false; // Check if its interactable, could get refactored in the future   
        return interactable.CanInteract(playerBody).Result; // Check if you can interact with instance.
    }
    [ServerRpc] private void DropItem_ServerRpc(int slotIndex)
    {
        if (!Inventory.TryGet(slotIndex, out IReadOnlyItemStack stack)) return;
        if (TutorialManager.Instance != null) return; // Don't allow items to be dropped during tutorial to avoid them despawning / getting lost

        int quantityRemoved = Inventory.Remove(slotIndex, 1);
        if (stack.GetItemData().PickupPrefab == null || quantityRemoved == 0) return;

        Vector3 throwDirection = itemHolder.transform.forward + Vector3.up;

        ItemPickup droppedItem = Instantiate(
            stack.GetItemData().PickupPrefab, 
            playerBody.transform.position + throwDirection, 
            Quaternion.identity);

        droppedItem.SetStack(stack.CloneWithQuantity(quantityRemoved));
        ApplyThrowForce_Server(droppedItem.Rigidbody, throwDirection);

        if (!stack.GetItemData().IsKeyItem)
        Destroy(droppedItem.gameObject, destroyDropAfterSeconds);
    }
    private void DropConsumed_Server(ItemData data)
    {
        if (!isServer || !data.ConsumedPrefab) return;

        Vector3 throwDirection = itemHolder.transform.forward + itemHolder.right * 0.5f + Vector3.up;

        NetworkRigidbody consumedDrop = Instantiate(data.ConsumedPrefab, 
            playerBody.transform.position + throwDirection, 
            Quaternion.identity);

        ApplyThrowForce_Server(consumedDrop, throwDirection);
        Destroy(consumedDrop, 5);
    }

    private void ApplyThrowForce_Server(NetworkRigidbody rb, Vector3 throwDirection)
    {
        if (!isServer) return;

        rb.isKinematic = false;
        rb.AddForce(throwDirection.normalized * throwItemForce, ForceMode.Impulse);
        rb.AddTorque(UnityEngine.Random.insideUnitSphere, ForceMode.Force);
    }

    [ServerRpc] private async Task RegisterUsage_ServerRpc(int slotIndex)
    {
        if (!Inventory.TryGet(slotIndex, out IReadOnlyItemStack stack)) await Task.CompletedTask;

        if (stack.GetItemData().IsConsumable && Inventory.TryRemoveOne(slotIndex))
        {
            DropConsumed_Server(stack.GetItemData());
        }
    }

    #region Input Actions
    public void UseFocused(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            if(IsUsingItem) return;
            currentUseTask = TryUseFocused();
        }
    }
    public void DropFocused(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            if (IsUsingItem) return;
            DropItem_ServerRpc(focusedSlot);
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
    public void ScrollSlot(InputAction.CallbackContext ctx)
    {
        if(ctx.performed) ChangeFocused((ctx.ReadValue<float>() > 0 ? focusedSlot + 1 : focusedSlot - 1 + Inventory.TotalSlots) % Inventory.TotalSlots);
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