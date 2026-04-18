using PurrNet;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public abstract class VendorBase : NetworkBehaviour
{
    [SerializeField] private int stashSize = 5;
    [SerializeField] private bool randomizeStashContent = true;
    [SerializeField, Tooltip("What will be requested by the player to pay.")] private ItemData currencyItem;
    [SerializeField] private ItemData[] itemsForSale;
    public UnityEvent OnRestock;
    public event System.Action OnSpawnedEvent;

    public ItemData CurrencyItem => currencyItem;
    protected Inventory itemStash { get; private set; }
    public int StashSize => stashSize;

    protected virtual void Awake()
    {
        itemStash = new Inventory(stashSize);
        GetComponent<Collider>().isTrigger = false;
    }

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);

        if (asServer) Restock();
        else OnSpawnedEvent?.Invoke();
    }

    public virtual void Restock()
    {
        if (!isServer || itemsForSale.Length == 0) return;

        itemStash.Clear();

        for(int i = 0; i < itemStash.TotalSlots; i++ )
        {
            int itemIndex = randomizeStashContent? Random.Range(0, itemsForSale.Length) : i % itemsForSale.Length;
            ItemData item = itemsForSale[itemIndex];

            int amountAdded = itemStash.Add(item, item.MaxStackSize);

            if (amountAdded == 0)
            {
                Debug.Log($"Stopped Restock at index {i}");
                break;
            }
        }
        Debug.Log($"Restocked: New used slots: {itemStash.UsedSlots}");
        InvokeOnRestock();
    }
    [ObserversRpc] private void InvokeOnRestock() => OnRestock?.Invoke();
}