using PurrNet;
using System;
using System.Collections;
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
    
    private bool inventoryEventsBound;
    private Coroutine heldItemRebuildRoutine;

    private void Awake()
    {
        playerBody = GetComponent<PlayerBody>();
        Inventory = new Inventory(inventorySize);
    }
    
    private void BindInventoryEvents()
    {
        if (inventoryEventsBound) return;

        Inventory.OnSlotsSwapped += HandleSlotsSwapped;
        Inventory.OnStackAdded += HandleStackCreation;
        Inventory.OnStackChanged += HandleStackChanged;
        Inventory.OnStackRemoved += HandleStackRemoval;

        Inventory.OnItemAdded += OnItemAdded.Invoke;
        Inventory.OnItemRemoved += OnItemRemoved.Invoke;
        OnFocusedChanged += _OnFocusedChanged.Invoke;
        OnItemUsed += _OnItemUsed.Invoke;

        inventoryEventsBound = true;
    }
    
    private void UnbindInventoryEvents()
    {
        if (!inventoryEventsBound) return;

        Inventory.OnSlotsSwapped -= HandleSlotsSwapped;
        Inventory.OnStackAdded -= HandleStackCreation;
        Inventory.OnStackChanged -= HandleStackChanged;
        Inventory.OnStackRemoved -= HandleStackRemoval;

        Inventory.OnItemAdded -= OnItemAdded.Invoke;
        Inventory.OnItemRemoved -= OnItemRemoved.Invoke;
        OnFocusedChanged -= _OnFocusedChanged.Invoke;
        OnItemUsed -= _OnItemUsed.Invoke;

        inventoryEventsBound = false;
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
    
    #region Network Lifecycle
    
    protected override void OnSpawned()
    {
        base.OnSpawned();
        if (!isOwner) return;

        BindInventoryEvents();
        RebuildHeldItemsAfterSync();
    }
    
    protected override void OnDespawned()
    {
        base.OnDespawned();

        CancelHeldItemRebuild();
        UnbindInventoryEvents();
        ClearHeldItems();
    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
        CancelHeldItemRebuild();
        UnbindInventoryEvents();
    }
    
    protected override void OnOwnerReconnected(PlayerID ownerId)
    {
        base.OnOwnerReconnected(ownerId);  
        if(!isOwner) return;

        BindInventoryEvents();
        RebuildHeldItemsAfterSync();
    }

    #endregion


    #region Inventory Event Subscribers
    private void HandleSlotsSwapped(int fromSlot, int toSlot)
    {
        if (fromSlot != focusedSlot && toSlot != focusedSlot) return;

        ShowFocusedHeldItem();
    }
    
    private void HandleStackCreation(IReadOnlyItemStack stack, int slotIndex)
    {
        CreateOrUpdateHeldItem(stack, slotIndex);
        
        if (activeInstance == null)
        {
            focusedSlot = slotIndex;
            ShowFocusedHeldItem();
        }
    }
    
    private void CreateOrUpdateHeldItem(IReadOnlyItemStack stack, int slotIndex)
    {
        if (stack == null || stack.IsEmpty()) return;

        ItemData itemData = stack.GetItemData();
        if (itemData == null || itemData.ItemPrefab == null) return;

        if (itemInstances.TryGetValue(stack.GetID(), out PlayerItem existingItem))
        {
            existingItem.BindTo(this, stack, slotIndex);
            existingItem.gameObject.SetActive(false);
            return;
        }

        PlayerItem itemObject = Instantiate(itemData.ItemPrefab, parent: itemHolder);
        itemObject.gameObject.SetActive(false);
        itemObject.BindTo(this, stack, slotIndex);

        itemInstances.Add(stack.GetID(), itemObject);
    }
    
    private void ShowFocusedHeldItem()
    {
        HideAllHeldItems();
        
        if (activeInstance != null) activeInstance.SetActive(false);

        activeInstance = null;

        if (!Inventory.TryGet(focusedSlot, out IReadOnlyItemStack stack)) return;
        if (stack == null || stack.IsEmpty()) return;

        if (!itemInstances.TryGetValue(stack.GetID(), out PlayerItem itemObject)) return;

        activeInstance = itemObject.gameObject;
        activeInstance.SetActive(true);

        OnFocusedChanged?.Invoke(stack.GetItemData());
    }
    
    private void HideAllHeldItems()
    {
        foreach (PlayerItem item in itemInstances.Values)
        {
            if (item != null) item.gameObject.SetActive(false);
        }

        for (int i = 0; i < itemHolder.childCount; i++)
        {
            PlayerItem item = itemHolder.GetChild(i).GetComponent<PlayerItem>();

            if (item != null) item.gameObject.SetActive(false);
        }
    }
    
    private void HandleStackChanged(IReadOnlyItemStack stack, int slotIndex)
    {
        if (itemInstances.TryGetValue(stack.GetID(), out PlayerItem item)) item.BindTo(this, stack, slotIndex);
    }
    
    private void HandleStackRemoval(IReadOnlyItemStack stack, int slotIndex)
    {
        if (!itemInstances.TryGetValue(stack.GetID(), out PlayerItem itemInstance)) return;

        if (activeInstance == itemInstance.gameObject)
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
        if (IsUsingItem) return;
        if (focusAtIndex >= Inventory.TotalSlots || focusAtIndex < 0) return;

        if (focusedSlot == focusAtIndex && activeInstance != null) return;

        int previous = focusedSlot;
        focusedSlot = focusAtIndex;

        ShowFocusedHeldItem();

        OnFocusedIndexChanged?.Invoke(previous, focusedSlot);
    }
    
    private void ClearHeldItems()
    {
        for (int i = itemHolder.childCount - 1; i >= 0; i--)
        {
            Transform child = itemHolder.GetChild(i);
            PlayerItem item = child.GetComponent<PlayerItem>();

            if (item == null) continue;

            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        itemInstances.Clear();
        activeInstance = null;
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

    private void RebuildHeldItemsAfterSync()
    {
        CancelHeldItemRebuild();
        heldItemRebuildRoutine = StartCoroutine(RebuildHeldItemsNextFrame());
    }
    
    private void CancelHeldItemRebuild()
    {
        if (heldItemRebuildRoutine == null) return;

        StopCoroutine(heldItemRebuildRoutine);
        heldItemRebuildRoutine = null;
    }
    
    private IEnumerator RebuildHeldItemsNextFrame()
    {
        yield return null;

        heldItemRebuildRoutine = null;
        RebuildHeldItems();
    }
    
    private void RebuildHeldItems()
    {
        ClearHeldItems();
        for (int i = 0; i < Inventory.TotalSlots; i++)
        {
            if (!Inventory.TryGet(i, out IReadOnlyItemStack stack)) continue;
            if (stack == null || stack.IsEmpty()) continue;

            CreateOrUpdateHeldItem(stack, i);
        }

        ShowFocusedHeldItem();
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