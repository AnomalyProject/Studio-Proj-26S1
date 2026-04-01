using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Scriptable Objects/ItemData")]
public class ItemData : ScriptableObject
{
    [SerializeField] string _itemName = "Item";
    [SerializeField] string _itemDescription = "This is an epic description.";
    [SerializeField] Sprite _itemIcon;
    [SerializeField] bool _isConsumable;
    [SerializeField, Min(1)] int _maxStackSize;
    [SerializeField] GameObject _itemPrefab;

    public string ItemName => _itemName;
    public string ItemDescription => _itemDescription;
    public Sprite ItemIcon => _itemIcon;
    public bool IsConsumable => _isConsumable;
    public int MaxStackSize => _maxStackSize;
    public GameObject ItemPrefab => _itemPrefab;
}
