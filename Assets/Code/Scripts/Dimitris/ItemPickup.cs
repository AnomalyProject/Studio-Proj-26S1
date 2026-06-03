using PurrNet;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(NetworkRigidbody))]
public class ItemPickup : NetworkBehaviour, IInteractable<PlayerBody>
{
            
    [SerializeField] private UnityEvent onPickup; //add event to connect sound and other stuff for designer Inspector
    [SerializeField] InspectorItemStack itemStack;

    NetworkRigidbody _rb;
    public NetworkRigidbody Rigidbody => _rb;

    private void Awake()
    {
        _rb = GetComponent<NetworkRigidbody>();
    }

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);

        //If nothing is being assigned first in inspector would lead to an error so doesnt let object break
        if (asServer && itemStack.Data == null)
        {
            Debug.LogError("InspectorItemStack is not assigned!", this);
            return;
        }
    }

    private void OnValidate() => itemStack.Validate();

    public Task<bool> CanInteract(PlayerBody player)
    {
        return Task.FromResult(true); //If there is no items doesnt let interact
    }
    [ServerRpc] public Task<bool> TryInteract(PlayerBody player)
    {
        return HandlePickup_Server(player);
    }
    public void SetStack(ItemStack stack) => itemStack.SetStack(stack);

    private Task<bool> HandlePickup_Server(PlayerBody player)
    {
        if (!isServer) return Task.FromResult(false);
        ItemStack stack = itemStack.GetItemStack();
        Inventory playerInventory = player.Inventory;//Gets inventory to use Playerinventory.cs methods

        if (playerInventory == null || itemStack.Data == null) return Task.FromResult(false);
        int added = playerInventory.Add(stack, modifyInputStack: true);

        if (added > 0)
        {
            InvokeOnPickup_Observers();
            if (stack.Quantity <= 0) StartCoroutine(DespawnNextFrame());
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    IEnumerator DespawnNextFrame()
    {
        if (isServer)
        {
            yield return null;
            Despawn();
        }
    }

    [ObserversRpc] void InvokeOnPickup_Observers() => onPickup?.Invoke();
}