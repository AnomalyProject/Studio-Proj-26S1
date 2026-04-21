using NUnit.Framework.Interfaces;
using PurrNet;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public class ItemPickup : MonoBehaviour, IInteractable<PlayerBody>
{
            
    [SerializeField] private UnityEvent onPickup; //add event to connect sound and other stuff for designer Inspector
    [Header("InspectorItemStack")] 
    [SerializeField] private ItemData itemData;
    [SerializeField] private int quantity;
    public event System.Action<ItemData, int> OnPickup; //add event to use in other scripts
    void Awake()
    {
        //If nothing is being assigned first in inspector would lead to an error so doesnt let object break
        if (itemData == null ) 
        {
            Debug.LogError("InspectorItemStack is not assigned!", this);
            return;
        }
    }
    public Task<bool> CanInteract(PlayerBody player)
    {
        return Task.FromResult(quantity > 0 && itemData != null);//If there is no items doesnt let interact
    }
     public async Task<bool> TryInteract(PlayerBody player)
    {
        if (!await CanInteract(player)) return false;
        PlayerInventory playerInventory = player.GetComponent<PlayerInventory>();//Gets inventory to use Playerinventory.cs methods
        if (playerInventory == null) return false;
        //Stacking and fill slots , Stack updates automatically
        int totalAdded = 0;

        while (quantity > 0)
        {
            int added = playerInventory.Inventory.Add(itemData, quantity);

            if (added == 0)
            {
                Debug.Log("Inventory is full");//if inventory is full there is no added so interact is unsuccesfull
                break;
            }

            quantity -= added;
            totalAdded += added;
        }
     
        
        if (totalAdded > 0)
        {    //Events are triggered when succefull pickup
            onPickup?.Invoke();
            OnPickup?.Invoke(itemData, totalAdded);
        }
        
        if (quantity <= 0)
            Destroy(gameObject); ; //if is empty after pickup and nothing remains , then destroys the object
        //if it is not empty just remains with less quantity
        return true;
    }
}
