using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VendorButton : MonoBehaviour, IInteractable<PlayerBody>
{
    [SerializeField] private Canvas buttonCanvas;
    [SerializeField] private Image itemIcon;
    [SerializeField] private Sprite lockedSlotSprite;
    [SerializeField] private TextMeshProUGUI itemNameText, itemPriceText, itemAmountText, itemDescription;
    private CompositeVendor vendorHost;
    public int SlotIndex;

    private Vector2 itemNameTextRectSize;

    void Awake()
    {
        vendorHost = GetComponentInParent<CompositeVendor>();

        if(vendorHost == null)
        {
            Debug.LogError("Vendor Button requires a CompositeVendor in its parent hierarchy.");
            return;
        }

        vendorHost.OnSlotChanged += UpdateContent;
        vendorHost.OnRestock.AddListener(UpdateContent);
        vendorHost.OnSpawnedEvent += UpdateContent;
        vendorHost.QueueOnSpawned(UpdateContent);

        itemNameTextRectSize = itemNameText.rectTransform.sizeDelta;
    }

    public Task<bool> CanInteract(PlayerBody interactor)
    {
        if (vendorHost == null) return Task.FromResult(false);

        bool result = SlotIndex >= 0 && vendorHost.CanBuyerAfford(SlotIndex, interactor.Inventory);
        return Task.FromResult(result);
    }

    void UpdateContent() => UpdateContent(SlotIndex);
    void UpdateContent(int slot) // will be used for whatever visuals needed
    {
        Debug.Log("Update Content called");
        if (vendorHost == null || slot != SlotIndex) return;

        IReadOnlyItemStack stack = vendorHost.GetStackFromSlot(slot);

        if (stack != null)
        {
            ItemData data = stack.GetItemData();
            itemIcon.sprite = data.ItemIcon;
            itemNameText.text = data.ItemName;
            itemNameText.rectTransform.sizeDelta = itemNameTextRectSize;
            itemDescription.text = data.ItemDescription;
            itemPriceText.text = $"x{vendorHost.GetStackPrice(slot).ToString()}";
            itemAmountText.text = $"Stock: x{stack.GetQuantity()}";
        }
        else
        {
            itemNameText.rectTransform.sizeDelta = new Vector2(itemNameTextRectSize.x, itemNameTextRectSize.y * 2);
            itemIcon.sprite = lockedSlotSprite;
            itemNameText.text = "Locked";
            itemPriceText.text = "";
            itemAmountText.text = "";
            itemDescription.text = "Nothing in stock.";
        }

        itemPriceText.gameObject.SetActive(stack != null);
    }

    public async Task<bool> TryInteract(PlayerBody interactor)
    {
        bool success = await vendorHost.RequestTransfer_Server(SlotIndex, interactor.Inventory);
        return success;
    }
}