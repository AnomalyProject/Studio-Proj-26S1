using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

/// <summary>
/// Nestoras Angelopoulos
/// 
/// Draws a hotbar with the correct number of slots and handles scrolling through them and updating their icons and counts when the inventory changes.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    private struct InventorySlotUI
    {
        public Image background;
        public Image icon;
        public TextMeshProUGUI count;
        public GameObject usePrompt;
    }
    private Dictionary<int, InventorySlotUI> slots;

    [SerializeField] private GameObject inventorySlotPrefab;
    private PlayerInventory playerInventory;
    private Transform UI;

    [SerializeField] private float focusedSlotScale = 1.2f;

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
    }
    private void ConstructInventory(PlayerBody player)
    {
        if (playerInventory != null) return;

        playerInventory = player.GetComponent<PlayerInventory>();
        playerInventory.OnFocusedIndexChanged += SwitchSlot;

        slots = new Dictionary<int, InventorySlotUI>(player.Inventory.TotalSlots);
        for (int i = 0; i < player.Inventory.TotalSlots; i++)
        {
            GameObject slot = Instantiate(inventorySlotPrefab, UI);
            Image background = slot.transform.GetChild(0).GetComponent<Image>();
            Image icon = background.transform.GetChild(0).GetComponent<Image>();
            TextMeshProUGUI count = background.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            GameObject usePrompt = background.transform.GetChild(2).gameObject;
            slots.Add(i, new InventorySlotUI { background = background, icon = icon, count = count, usePrompt = usePrompt });
        }
        playerInventory.ChangeFocused(0);
        
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
            if (index == playerInventory.focusedSlot && playerInventory.CanUseFocused()) slots[index].usePrompt.SetActive(true);
        };
        player.Inventory.OnStackRemoved += (item, index) =>
        {
            slots[index].icon.sprite = null;
            slots[index].icon.enabled = false;
            slots[index].count.enabled = false;
            slots[index].usePrompt.SetActive(false);
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
        playerInventory.OnFocusedIndexChanged -= SwitchSlot;
        playerInventory = null;
        foreach (KeyValuePair<int, InventorySlotUI> slot in slots) Destroy(slot.Value.background.transform.parent.gameObject);
        slots.Clear();
    }
    private void SwitchSlot(int previous, int current)
    {
        if (playerInventory == null) return;

        slots[previous].background.transform.localScale = Vector3.one;
        slots[previous].usePrompt.SetActive(false);
        slots[current].background.transform.localScale = Vector3.one * focusedSlotScale;

        if (playerInventory.CanUseFocused()) slots[current].usePrompt.SetActive(true);
    }
}
