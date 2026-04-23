using NUnit.Framework.Interfaces;
using PurrNet;
using System.Globalization;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using PurrNet.Modules;

public class ItemPickup : NetworkBehaviour, IInteractable<PlayerBody>
{
            
    [SerializeField] private UnityEvent onPickup; //add event to connect sound and other stuff for designer Inspector
    [Header("InspectorItemStack")] 
    [SerializeField] private ItemData itemData;
    [SerializeField] private int startQuantity; //  set in Inspector
    private int quantity;// runtime synced value
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
    protected override void OnSpawned()
    {        
        if (NetworkManager.main.isServer)
        {
            quantity = startQuantity;
        }
    }
    public Task<bool> CanInteract(PlayerBody player)
    {
        return Task.FromResult(quantity > 0 && itemData != null);//If there is no items doesnt let interact
    }
    public Task<bool> TryInteract(PlayerBody player)
    {
        if (isServer)
        {
            HandlePickup(player);
        }
        else
        {
            RequestPickup(player);
        }

        return Task.FromResult(true);
    }
 
    [ServerRpc]
    private void RequestPickup(PlayerBody player)
    {
        HandlePickup(player);
    }
    private void HandlePickup(PlayerBody player)
    {
       
        if (! CanInteract(player).Result) return;

        PlayerInventory playerInventory = player.GetComponent<PlayerInventory>();//Gets inventory to use Playerinventory.cs methods
        if (playerInventory == null) return ;
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
        {
            Pickup_ClientRpc(totalAdded);
        }
        
        if (quantity <= 0)
            Despawn();  //if is empty after pickup and nothing remains , then destroys the object
                        //if it is not empty just remains with less quantity

    }
    
    [ObserversRpc]
    private void Pickup_ClientRpc(int amount)
    {
        //Events are triggered when succefull pickup for all clients from server
        onPickup?.Invoke();
        OnPickup?.Invoke(itemData, amount);
    }
   
}
