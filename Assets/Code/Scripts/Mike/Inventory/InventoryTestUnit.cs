using UnityEngine;

public class InventoryTestUnit : MonoBehaviour
{
    [SerializeField] int inventorySize = 10;
    [SerializeField] ItemData testItem;

    Inventory inventory, inventory2;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        inventory = new Inventory(inventorySize);
        inventory2 = new Inventory(inventorySize);

        ItemStack inputStack = new ItemStack(testItem, 16);
        int totalAdded = inventory.Add(inputStack);
        Debug.Log("total added: " + totalAdded);
        Debug.Log("Input Stack Quantity: " + inputStack.Quantity);
        Debug.Log("Got item: " + inventory.TryGet(testItem, out ItemStack stack, out int slotIndex) + $" {stack.ItemData.name} in index {slotIndex}");
        inventory.SwapSlots(1, inventory.TotalSlots - 1);
        inventory.Remove(testItem, 5);
        inventory.SwapSlots(0, inventory.TotalSlots - 1);
        inventory.Transfer(testItem, 50, inventory2);
        inventory.ClearSlot(0);
        RunDebug();
    }

    void RunDebug()
    {
        string invItemOutput = "";
        string inv2ItemOutput = "";

        foreach (var itemStack in inventory.GetEnumeration())
        {
            invItemOutput += itemStack == null? "Empty Slot\n" : $"Item: {itemStack.ItemData.name}, Quantity: {itemStack.Quantity}\n";
        }

        foreach (var itemStack in inventory2.GetEnumeration())
        {
            inv2ItemOutput += itemStack == null ? "Empty Slot\n" : $"Item: {itemStack.ItemData.name}, Quantity: {itemStack.Quantity}\n";
        }

        Debug.Log("Inventory 1:\n" + invItemOutput);
        Debug.Log("Inventory 2:\n" + inv2ItemOutput);
        Debug.Log("1. Used Slots: " + inventory.UsedSlots);
        Debug.Log("1. Empty Slots: " + inventory.EmptySlots);
        Debug.Log("1. Total Slots: " + inventory.TotalSlots);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
