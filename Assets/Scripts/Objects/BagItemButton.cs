using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BagItemButton : MonoBehaviour
{
    public Image itemIcon;
    public TextMeshProUGUI QuantityText;
    private Item item;
    private BagManager bagManager;
    private int quantity = 1; // Default quantity

    public void Setup(Item newItem, BagManager manager, int quantity = 1)
    {
        item = newItem;
        bagManager = manager;
        itemIcon.sprite = item.itemSprite;
        this.quantity = quantity;

        if (QuantityText != null)
        {
            QuantityText.text = this.quantity.ToString();
        }
        Debug.Log("Setting up BagItemButton for item: " + item.itemName + " with quantity: " + this.quantity);
        // Hook up button click event
        GetComponent<Button>().onClick.RemoveAllListeners();
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    public void OnClick()
    {
        bagManager.ShowItemDetails(item);
    }
}
