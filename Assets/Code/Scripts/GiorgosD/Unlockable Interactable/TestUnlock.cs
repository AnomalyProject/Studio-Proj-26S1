using UnityEngine;

public class TestUnlock : MonoBehaviour
{
    [SerializeField] private PlayerInventory inv;
    [SerializeField] private UnlockableInteractable interactable;
    [SerializeField] private ItemData test;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            inv = Object.FindFirstObjectByType<PlayerInventory>();

            inv.Inventory.TryAddOne(test);
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            interactable.ResetToLocked();
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            interactable.ReturnItems(inv.Inventory);
        }
    }
}
