using PurrNet;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public class VendorButton : MonoBehaviour, IInteractable<PlayerBody>
{
    [SerializeField, Min(0)] private int _slotIndex;
    private CompositeVendor vendorHost;
    public int SlotIndex => _slotIndex;

    void Awake()
    {
        vendorHost = GetComponentInParent<CompositeVendor>();

        if(vendorHost == null)
        {
            Debug.LogError("Vendor Button requires a CompositeVendor in its parent hierarchy.");
            return;
        }

        vendorHost.OnSlotChanged += UpdateContent;
        vendorHost.OnRestock.AddListener(UpdateContent);
        vendorHost.OnSpawnedEvent += UpdateContent;
    }

    public Task<bool> CanInteract(PlayerBody interactor)
    {
        if (vendorHost == null) return Task.FromResult(false);

        bool result = SlotIndex >= 0 && vendorHost.CanBuyerAfford(SlotIndex, interactor.Inventory);
        return Task.FromResult(result);
    }

    void UpdateContent() => UpdateContent(SlotIndex);
    void UpdateContent(int slot) // will be used for whatever visuals needed
    {
        Debug.Log("Update Content called");
        if (vendorHost == null || slot != SlotIndex) return;
        ItemData data = vendorHost.GetDataFromSlot(slot);
        Debug.Log($"Slot {slot} contains: {data?.ItemName ?? "Empty"}");
    }

    public async Task<bool> TryInteract(PlayerBody interactor)
    {
        bool success = await vendorHost.RequestTransfer_Server(SlotIndex, interactor.Inventory);
        return success;
    }
}