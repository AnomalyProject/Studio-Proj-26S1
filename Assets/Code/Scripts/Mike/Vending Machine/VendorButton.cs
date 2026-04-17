using UnityEngine;
using UnityEngine.Events;

public class VendorButton : MonoBehaviour, IInteractable<PlayerBody>
{
    [SerializeField] UnityEvent<IReadOnlyItemStack> OnUpdateHeldItem;
    private CompositeVendor vendorHost;
    IReadOnlyItemStack heldItem;
    public IReadOnlyItemStack HeldItem => heldItem;

    public bool CanInteract(PlayerBody interactor)
    {
        if (heldItem == null || vendorHost == null) return false;
        return vendorHost.CheckPrice(this, interactor.Inventory);
    }

    public bool TryInteract(PlayerBody interactor)
    {
        if (!CanInteract(interactor)) return false;
        return vendorHost.TryPerformTransfer(this, interactor.Inventory);
    }

    public void SetItemAndVendor(IReadOnlyItemStack itemStack, CompositeVendor vendor)
    {
        heldItem = itemStack;
        vendorHost = vendor;
        OnUpdateHeldItem?.Invoke(heldItem);
    }
}