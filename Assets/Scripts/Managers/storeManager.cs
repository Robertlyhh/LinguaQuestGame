using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class StoreManager : MonoBehaviour
{
    [Header("Player Inventory")]
    public Inventory playerInventory;

    [Header("Store UI Elements")]
    public GameObject storeUI;                // The StoreCanvas or Panel
    public Transform itemListParent;          // Parent for item buttons (e.g., Content in ScrollView)
    public GameObject itemButtonPrefab;       // Prefab for each item button
    public Button exitButton;                 // Exit button

    [Header("Confirmation Popup")]
    public GameObject confirmationPopup;      // The confirmation popup panel
    public TextMeshProUGUI confirmationText;  // Text: "Buy X for Y coins?"
    public Button confirmButton;
    public Button cancelButton;


    public Signal CoinSignal; // Signal to notify when store is opened
    public List<Item> items;
    private Item selectedItem; // The item currently selected for purchase

    void Start()
    {
        UpdateCoinsUI();
        PopulateStore();
        SetupUI();
    }

    void SetupUI()
    {
        storeUI.SetActive(false);           // Hide store UI initially
        confirmationPopup.SetActive(false); // Hide confirmation popup initially

        exitButton.onClick.AddListener(CloseStore);
    }

    void PopulateStore()
    {
        foreach (Item item in items)
        {
            GameObject buttonObj = Instantiate(itemButtonPrefab, itemListParent);
            Button buyButton = buttonObj.GetComponent<Button>();
            buyButton.onClick.AddListener(() => ShowConfirmation(item));

            // Set up UI visuals
            buttonObj.transform.Find("NameText").GetComponent<TextMeshProUGUI>().text = item.itemName;
            buttonObj.transform.Find("PriceText").GetComponent<TextMeshProUGUI>().text = item.price.ToString();
            buttonObj.GetComponent<Image>().sprite = item.itemSprite;
        }
    }

    void ShowConfirmation(Item item)
    {
        selectedItem = item;
        confirmationText.text = $"Buy {item.itemName} for {item.price} coins?";
        confirmationPopup.SetActive(true);

        confirmButton.onClick.RemoveAllListeners();
        cancelButton.onClick.RemoveAllListeners();

        confirmButton.onClick.AddListener(ConfirmPurchase);
        cancelButton.onClick.AddListener(() => confirmationPopup.SetActive(false));
    }

    void ConfirmPurchase()
    {
        if (playerInventory.coins >= selectedItem.price)
        {
            playerInventory.coins -= selectedItem.price;
            items.Remove(selectedItem); // Remove item from store
            playerInventory.AddItem(selectedItem);
            UpdateCoinsUI();
            UpdateStoreUI(); // Refresh store UI
            Debug.Log($"Purchased: {selectedItem.itemName}");
        }
        else
        {
            Debug.LogWarning("Not enough coins to buy " + selectedItem.itemName);
        }

        confirmationPopup.SetActive(false);
    }

    void UpdateCoinsUI()
    {
        CoinSignal.Raise(); // Notify that coins have been updated
    }

    public void OpenStore()
    {
        storeUI.SetActive(true);
        //Time.timeScale = 0f; // Pause game
    }

    public void CloseStore()
    {
        storeUI.SetActive(false);
        confirmationPopup.SetActive(false);
        //Time.timeScale = 1f; // Resume game
    }

    void Update()
    {
        if (storeUI.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseStore();
        }
    }

    void UpdateStoreUI()
    {
        // This method can be used to refresh the store UI if needed
        foreach (Transform child in itemListParent)
        {
            Destroy(child.gameObject);
        }
        PopulateStore();
    }
}
