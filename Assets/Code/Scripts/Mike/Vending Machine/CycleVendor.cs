using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class CycleVendor : InteractableVendor
{
    [SerializeField, Min(.1f)] float cycleRatio = 0.5f;
    IReadOnlyItemStack _focusedItem;
    public IReadOnlyItemStack FocusedItem
    {
        get => _focusedItem;
        private set
        {
            _focusedItem = value;
            if (FocusedItem != null) OnUpdateFocused?.Invoke(FocusedItem);
        }
    }
    public UnityEvent<IReadOnlyItemStack> OnUpdateFocused;
    int focusedIndex = 0;

    void Start() => InvokeRepeating(nameof(Cycle), 0f, cycleRatio);
    protected override bool TryInteractBehaviour(PlayerBody interactor)
    {
        if (FocusedItem == null) return false;
        return itemStash.TryTransferExact(FocusedItem.GetItemData(), FocusedItem.GetQuantity(), interactor.Inventory);
    }

    void Cycle()
    {
        if(itemStash.UsedSlots == 0)
        {
            FocusedItem = null;
            return;
        }

        int startingIndex = focusedIndex;
        focusedIndex = (focusedIndex + 1) % itemStash.TotalSlots;
        IReadOnlyItemStack newFocused;

        while (!itemStash.TryGet(focusedIndex, out newFocused))
        {
            focusedIndex = (focusedIndex + 1) % itemStash.TotalSlots;
            if (focusedIndex == startingIndex) break;
        }

        FocusedItem = newFocused;
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