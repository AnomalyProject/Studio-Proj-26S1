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
        if (playerInventory != null)
        {
            UnbindInventoryUIEvents();
            playerInventory.OnFocusedIndexChanged -= SwitchSlot;
        }
        
        PlayerBody.OnLocalPlayerSpawned -= ConstructInventory;
        PlayerBody.OnLocalPlayerDespawned -= HandleLocalPlayerDespawned;
    }
    
    private void BindInventoryUIEvents()
    {
        if (playerInventory == null) return;

        playerInventory.Inventory.OnStackAdded += HandleStackAdded;
        playerInventory.Inventory.OnStackRemoved += HandleStackRemoved;
        playerInventory.Inventory.OnStackChanged += HandleStackChanged;
    }

    private void UnbindInventoryUIEvents()
    {
        if (playerInventory == null) return;

        playerInventory.Inventory.OnStackAdded -= HandleStackAdded;
        playerInventory.Inventory.OnStackRemoved -= HandleStackRemoved;
        playerInventory.Inventory.OnStackChanged -= HandleStackChanged;
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

            slots.Add(i, new InventorySlotUI
            {
                background = background,
                icon = icon,
                count = count,
                usePrompt = usePrompt
            });
        }

        BindInventoryUIEvents();
        RepaintAllSlots();

        SwitchSlot(playerInventory.focusedSlot, playerInventory.focusedSlot);
    }

    private void HandleLocalPlayerDespawned(PlayerBody player)
    {
        if (playerInventory != null)
        {
            UnbindInventoryUIEvents();
            
            playerInventory.OnFocusedIndexChanged -= SwitchSlot;
            playerInventory = null;
        }

        if (slots.Count == 0) return;
        foreach (KeyValuePair<int, InventorySlotUI> slot in slots) Destroy(slot.Value.background.transform.parent.gameObject);
        slots.Clear();
    }
    private void SwitchSlot(int previous, int current)
    {
        if (playerInventory == null) return;
        
        // fix for preventing UI error during rebuild timing
        if (!slots.ContainsKey(previous) || !slots.ContainsKey(current)) return;

        slots[previous].background.transform.localScale = Vector3.one;
        slots[previous].usePrompt.SetActive(false);
        slots[current].background.transform.localScale = Vector3.one * focusedSlotScale;

        if (playerInventory.CanUseFocused()) slots[current].usePrompt.SetActive(true);
    }
    
    private void HandleStackAdded(IReadOnlyItemStack item, int index)
    {
        PaintSlot(index, item);
    }

    private void HandleStackRemoved(IReadOnlyItemStack item, int index)
    {
        ClearSlot(index);
    }

    private void HandleStackChanged(IReadOnlyItemStack item, int index)
    {
        PaintSlot(index, item);
    }
    
    // this is the main fix for reconnect
    private void RepaintAllSlots()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (playerInventory.Inventory.TryGet(i, out IReadOnlyItemStack stack))
                PaintSlot(i, stack);
            else
                ClearSlot(i);
        }
    }
    
    private void PaintSlot(int index, IReadOnlyItemStack item)
    {
        if (item == null || item.IsEmpty())
        {
            ClearSlot(index);
            return;
        }

        slots[index].icon.sprite = item.GetItemData().ItemIcon;
        slots[index].icon.enabled = true;

        int count = item.GetQuantity();
        slots[index].count.enabled = count > 1;
        slots[index].count.text = count > 1 ? count.ToString() : string.Empty;

        bool showUsePrompt = index == playerInventory.focusedSlot && playerInventory.CanUseFocused();
        slots[index].usePrompt.SetActive(showUsePrompt);
    }
    
    private void ClearSlot(int index)
    {
        slots[index].icon.sprite = null;
        slots[index].icon.enabled = false;
        slots[index].count.enabled = false;
        slots[index].count.text = string.Empty;
        slots[index].usePrompt.SetActive(false);
    }
    
}
