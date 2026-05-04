using PurrNet;
using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CycleVendor : InteractableVendor
{
    public UnityEvent<IReadOnlyItemStack> OnUpdateFocused;
    [SerializeField, Min(.1f)] float cycleRatio = 0.5f;
    [SerializeField] Image itemIcon;

    private IReadOnlyItemStack _focusedItem;
    public IReadOnlyItemStack FocusedItem
    {
        get => _focusedItem;
        private set
        {
            if (_focusedItem == value) return;

            _focusedItem = value;
            if (FocusedItem != null) OnUpdateFocused?.Invoke(value);
        }
    }
    private int focusedIndex = 0;

    protected override void OnSpawned()
    {
        base.OnSpawned();
        OnUpdateFocused.AddListener(UpdateVisuals);
    }

    protected override void OnObserverAdded(PlayerID player)
    {
        base.OnObserverAdded(player);

        if (!isServer) return;
        StartCycle(player, cycleRatio);
    }

    private void UpdateVisuals(IReadOnlyItemStack stack) => itemIcon.sprite = stack.GetItemData().ItemIcon;

    [TargetRpc] private void StartCycle(PlayerID playerID, float ratio) => InvokeRepeating(nameof(Cycle), 0f, ratio);
    protected override bool TryInteractBehaviour(PlayerBody interactor)
    {
        if (FocusedItem == null) return false;
        return itemStash.Transfer(focusedIndex, interactor.Inventory) > 0;
    }

    private void Cycle()
    {
        if(itemStash.TryGetNext(focusedIndex, out IReadOnlyItemStack newFocused, out int newIndex))
        {
            FocusedItem = newFocused;
            focusedIndex = newIndex;
            DebugFocused();
        }
    }
    public void DebugFocused()
    {
        if(FocusedItem == null)
        {
            Debug.Log("No item in focus");
            return;
        }
        Debug.Log($"Focused item: {FocusedItem.GetItemData().name} x{FocusedItem.GetQuantity()}");
    }
}