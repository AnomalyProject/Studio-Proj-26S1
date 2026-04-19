using System;
using UnityEngine;

public class InventoryTestUnit : MonoBehaviour
{
    [SerializeField] int inventorySize = 10;
    [SerializeField] ItemData testItem;
    [SerializeField] PlayerInventory playerInventory;

    Inventory inventory, inventory2;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        inventory = new Inventory(inventorySize);
        inventory2 = new Inventory(inventorySize);

        ItemStack inputStack = new ItemStack(testItem, 1);
        int totalAdded = inventory.Add(inputStack, false);
        totalAdded = inventory.Add(inputStack, false);
        Debug.Log("total added: " + totalAdded);
        Debug.Log("Input Stack Quantity: " + inputStack.Quantity);
        Debug.Log("Got item: " + inventory.TryGet(testItem, out var stack, out int slotIndex) + $" {stack.GetItemData().name} in index {slotIndex}");
        inventory.SwapSlots(1, inventory.TotalSlots - 1);
        //inventory.Remove(testItem, 5);
        inventory.SwapSlots(0, inventory.TotalSlots - 1);
        Debug.Log(inventory.Contains(null));
        RunDebug();

        playerInventory.Inventory.TryAddOne(testItem);
        playerInventory.Inventory.TryAddOne(testItem);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.V)) _ = playerInventory.TryUseFocused();
        if (Input.GetKeyDown(KeyCode.RightArrow)) playerInventory.NextItem();
        if (Input.GetKeyDown(KeyCode.LeftArrow)) playerInventory.PreviousItem();
        if (Input.GetKeyDown(KeyCode.DownArrow)) playerInventory.Inventory.TryRemoveOne(testItem);
    }

    void RunDebug()
    {
        string invItemOutput = "";
        string inv2ItemOutput = "";

        foreach (var itemStack in inventory.GetEnumeration())
        {
            invItemOutput += itemStack == null? "Empty Slot\n" : $"Item: {itemStack.GetItemData().name}, Quantity: {itemStack.GetQuantity()}\n";
        }

        foreach (var itemStack in inventory2.GetEnumeration())
        {
            inv2ItemOutput += itemStack == null ? "Empty Slot\n" : $"Item: {itemStack.GetItemData().name}, Quantity: {itemStack.GetQuantity()}\n";
        }

        Debug.Log("Inventory 1:\n" + invItemOutput);
        Debug.Log("Inventory 2:\n" + inv2ItemOutput);
        Debug.Log("1. Used Slots: " + inventory.UsedSlots);
        Debug.Log("1. Empty Slots: " + inventory.EmptySlots);
        Debug.Log("1. Total Slots: " + inventory.TotalSlots);
    }
}
