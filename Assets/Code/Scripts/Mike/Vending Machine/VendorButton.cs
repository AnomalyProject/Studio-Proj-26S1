using PurrNet;
using System;
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

    public bool CanInteract(PlayerBody interactor)
    {
        if (vendorHost == null) return false;
        return SlotIndex >= 0 && vendorHost.CheckPrice(SlotIndex, interactor.Inventory);
    }

    void UpdateContent() => UpdateContent(SlotIndex);
    void UpdateContent(int slot)
    {
        Debug.Log("Update Content called");
        if (vendorHost == null || slot != SlotIndex) return;
        ItemData data = vendorHost.GetDataFromSlot(slot);
        Debug.Log($"Slot {slot} contains: {data?.ItemName ?? "Empty"}");
    }

    public bool TryInteract(PlayerBody interactor)
    {
        if (!CanInteract(interactor)) return false;
        vendorHost.RequestTransfer(SlotIndex, interactor.Inventory);
        return true;
    }
}