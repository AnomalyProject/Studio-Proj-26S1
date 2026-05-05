using PurrNet;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CycleVendor : InteractableVendor
{
    public UnityEvent<IReadOnlyItemStack> OnUpdateFocused;
    [SerializeField, Min(.1f)] float cycleRatio = 0.5f;
    [SerializeField] Image itemIcon;
    private SyncVar<int> focusedIndex = new(0, ownerAuth: false);
    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);

        focusedIndex.onChanged += UpdateFocused;
        if (asServer) InvokeRepeating(nameof(Cycle), 0, cycleRatio);
        UpdateFocused(focusedIndex.value);
    }
    protected override bool TryInteractBehaviour(PlayerBody interactor)
    {
        return itemStash.Transfer(focusedIndex.value, interactor.Inventory) > 0;
    }

    private void Cycle()
    {
        if (!isServer) return;

        itemStash.TryGetNext(focusedIndex, out _, out int newIndex);
        focusedIndex.value = newIndex;
    }

    private void UpdateFocused(int newIndex)
    {
        if(itemStash.TryGet(newIndex, out var stack)) OnUpdateFocused?.Invoke(stack);
        UpdateVisuals(stack);
    }
    private void UpdateVisuals(IReadOnlyItemStack stack)
    {
        itemIcon.enabled = priceText.enabled = stack != null;
        itemIcon.sprite = stack?.GetItemData().ItemIcon;
    }
}