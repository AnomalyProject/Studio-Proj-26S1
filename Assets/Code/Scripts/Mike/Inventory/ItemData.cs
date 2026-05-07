using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Scriptable Objects/ItemData")]
public class ItemData : ScriptableObject
{
    [SerializeField] private string _itemName = "Item";
    [SerializeField] private string _itemDescription = "This is an epic description.";
    [SerializeField] private Sprite _itemIcon;
    [SerializeField] private bool _isConsumable;
    [SerializeField, Min(1)] private int _maxStackSize;
    [SerializeField, Min(0)] private int _vendorPrice = 1;
    [SerializeField] private PlayerItem _itemPrefab;

    public string ItemName => _itemName;
    public string ItemDescription => _itemDescription;
    public Sprite ItemIcon => _itemIcon;
    public bool IsConsumable => _isConsumable;
    public int MaxStackSize => _maxStackSize;
    public int PricePerUnit => _vendorPrice;
    public PlayerItem ItemPrefab => _itemPrefab;
}
