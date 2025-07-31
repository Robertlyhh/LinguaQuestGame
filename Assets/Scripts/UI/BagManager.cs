using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BagManager : MonoBehaviour
{
    public GameObject bagUIPanel;
    public GameObject itemDetailsPanel;
    public Transform itemGridParent;
    public GameObject itemButtonPrefab;
    public GameObject MeatPrefab;
    public GameObject CarrotPrefab;
    public GameObject coinPrefab; // Assign in inspector

    public Image detailImage;
    public TMP_Text detailName;
    public TMP_Text detailDescription;
    public Button useButton;
    public Button sellButton;
    public Button closeButton;
    public Button closeDetailsButton;
    public Signal CoinSignal;

    public Inventory playerInventory;
    private InventoryEntry selectedItem;

    void Start()
    {
        bagUIPanel.SetActive(false);
        itemDetailsPanel.SetActive(false);
        RefreshCoins();

        closeButton.onClick.AddListener(CloseBag);
        closeDetailsButton.onClick.AddListener(OnCloseDetailsButton);
        useButton.onClick.AddListener(OnUseButton);
        sellButton.onClick.AddListener(OnSellButton);

    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            ToggleBag();
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseBag();
        }
    }

    public void ToggleBag()
    {
        bool isActive = bagUIPanel.activeSelf;
        bagUIPanel.SetActive(!isActive);
        if (!isActive) PopulateBag();
    }

    public void CloseBag()
    {
        bagUIPanel.SetActive(false);
        itemDetailsPanel.SetActive(false);
    }

    void PopulateBag()
    {
        foreach (Transform child in itemGridParent) Destroy(child.gameObject);

        foreach (InventoryEntry entry in playerInventory.items)
        {
            GameObject buttonObj = Instantiate(itemButtonPrefab, itemGridParent);
            BagItemButton btn = buttonObj.GetComponent<BagItemButton>();
            btn.Setup(entry.item, this, entry.quantity);
        }
    }

    public void ShowItemDetails(Item item)
    {
        selectedItem = playerInventory.items.Find(e => e.item == item);
        if (selectedItem == null) return;

        detailImage.sprite = item.itemSprite;
        detailName.text = item.itemName;
        detailDescription.text = item.description;
        itemDetailsPanel.SetActive(true);
    }

    public void OnCloseDetailsButton()
    {
        itemDetailsPanel.SetActive(false);
    }

    public void OnUseButton()
    {
        Debug.Log("Using " + selectedItem.item.itemName);

        switch (selectedItem.item.itemID)
        {
            case 0:
                //PlayerStats.Instance.Heal(20);
                Debug.Log("Can not use this item.");
                break;
            case 1:
                // Unlock door logic here
                Debug.Log("Using key to unlock a door.");
                break;
            case 4:
                Vector2 playerposition = GameObject.FindGameObjectWithTag("Player").transform.position;
                Instantiate(MeatPrefab, playerposition + new Vector2(1, 0), Quaternion.identity);
                Debug.Log("Using meat item.");
                break;
            case 5:
                Vector2 playerPos = GameObject.FindGameObjectWithTag("Player").transform.position;
                Instantiate(CarrotPrefab, playerPos + new Vector2(1, 0), Quaternion.identity);
                Debug.Log("Using carrot item.");
                break;
            default:
                Debug.Log("No use effect for this item.");
                break;
        }

        playerInventory.RemoveItem(selectedItem.item);
        itemDetailsPanel.SetActive(false);
        PopulateBag();
    }

    public void OnSellButton()
    {
        if (selectedItem == null || selectedItem.item.price <= 0)
        {
            Debug.Log("Cannot sell this item.");
            return;
        }
        Debug.Log("Selling " + selectedItem.item.itemName);
        playerInventory.coins += selectedItem.item.price;
        playerInventory.RemoveItem(selectedItem.item);
        RefreshCoins();
        itemDetailsPanel.SetActive(false);
        PopulateBag();
    }

    void RefreshCoins()
    {
        CoinSignal.Raise();
    }

    public void ThrowCoins()
    {
        if (playerInventory.coins <= 0) return;
        playerInventory.coins -= 1;
        RefreshCoins();

        Vector2 playerPosition = GameObject.FindGameObjectWithTag("Player").transform.position;
        Instantiate(coinPrefab, playerPosition + new Vector2(1, 0), Quaternion.identity);
        Debug.Log("Threw a coin!");
        CloseBag();
    }
}
