using PurrNet;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public abstract class VendorBase : MonoBehaviour
{
    [SerializeField] private int stashSize = 5;
    [SerializeField] private ItemData[] itemsForSale;
    [SerializeField] protected UnityEvent OnRestock;
    [SerializeField, Tooltip("What will be requested by the player to pay.")] private ItemData currencyItem;
    public ItemData CurrencyItem => currencyItem;
    protected Inventory itemStash { get; private set; }

    protected virtual void Awake()
    {
        itemStash = new Inventory(stashSize);
        GetComponent<Collider>().isTrigger = false;
        Restock();
    }

    public void Restock()
    {
        itemStash.Clear();

        for(int i = 0; i < itemStash.TotalSlots; i++ )
        {
            int randomIndex = Random.Range(0, itemsForSale.Length);
            ItemData item = itemsForSale[randomIndex];

            int amountAdded = itemStash.Add(item, item.MaxStackSize);

            if (amountAdded == 0) break;
        }
        OnRestock?.Invoke();
    }
}