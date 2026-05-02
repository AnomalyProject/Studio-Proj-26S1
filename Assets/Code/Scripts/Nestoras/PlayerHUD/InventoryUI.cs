using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    private struct InventorySlotUI
    {
        public Image background;
        public Image icon;
        public TextMeshProUGUI count;
    }
    private Dictionary<int, InventorySlotUI> slots;

    [SerializeField] private GameObject inventorySlotPrefab;
    private PlayerInventory playerInventory;
    private PlayerInput playerInput;
    private Transform UI;

    [SerializeField] float focusedSlotScale = 1.2f;

    private void Awake()
    {
        UI = transform.GetChild(0);

        if (PlayerBody.localPlayerBody != null) ConstructInventory(PlayerBody.localPlayerBody);
        PlayerBody.OnLocalPlayerSpawned += ConstructInventory;
        PlayerBody.OnLocalPlayerDespawned += HandleLocalPlayerDespawned;
    }
    private void OnDestroy()
    {
        PlayerBody.OnLocalPlayerSpawned -= ConstructInventory;
        PlayerBody.OnLocalPlayerDespawned -= HandleLocalPlayerDespawned;

        if (playerInput != null) playerInput.actions["Scroll Inventory"].performed -= SwitchSlot;
    }
    private void ConstructInventory(PlayerBody player)
    {
        playerInput = player.GetComponent<PlayerInput>();
        playerInventory = player.GetComponent<PlayerInventory>();

        slots = new Dictionary<int, InventorySlotUI>(player.Inventory.TotalSlots);
        for (int i = 0; i < player.Inventory.TotalSlots; i++)
        {
            GameObject slot = Instantiate(inventorySlotPrefab, UI);
            Image background = slot.GetComponent<Image>();
            Image icon = slot.transform.GetChild(0).GetComponent<Image>();
            TextMeshProUGUI count = slot.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            slots.Add(i, new InventorySlotUI { background = background, icon = icon, count = count });
        }

        playerInput.actions["Scroll Inventory"].performed += SwitchSlot;
        SwitchSlot(0);
        
        player.Inventory.OnStackAdded += (item, index) =>
        {
            slots[index].icon.sprite = item.GetItemData().ItemIcon;
            slots[index].icon.enabled = true;
            int count = item.GetQuantity();
            if (count > 1)
            {
                slots[index].count.enabled = true;
                slots[index].count.text = count.ToString();
            }
        };
        player.Inventory.OnStackRemoved += (item, index) =>
        {
            slots[index].icon.sprite = null;
            slots[index].icon.enabled = false;
            slots[index].count.enabled = false;
        };
        player.Inventory.OnStackChanged += (item, index) =>
        {
            int count = item.GetQuantity();
            if (count > 1)
            {
                slots[index].count.enabled = true;
                slots[index].count.text = count.ToString();
            }
            else slots[index].count.enabled = false;
        };
    }

    private void HandleLocalPlayerDespawned(PlayerBody player)
    {
        playerInput.actions["Scroll Inventory"].performed -= SwitchSlot;
        playerInput = null;
        playerInventory = null;

        foreach (KeyValuePair<int, InventorySlotUI> slot in slots) Destroy(slot.Value.background.gameObject);
        slots.Clear();
    }

    private void SwitchSlot(InputAction.CallbackContext context) => SwitchSlot((context.ReadValue<float>() > 0 ? playerInventory.focusedSlot + 1 : playerInventory.focusedSlot - 1 + slots.Count) % slots.Count);
    private void SwitchSlot(int newIndex)
    {
        if (playerInventory == null) return;

        slots[playerInventory.focusedSlot].background.transform.localScale = Vector3.one;
        slots[newIndex].background.transform.localScale = Vector3.one * focusedSlotScale;

        playerInventory.ChangeFocused(newIndex);
    }
}
