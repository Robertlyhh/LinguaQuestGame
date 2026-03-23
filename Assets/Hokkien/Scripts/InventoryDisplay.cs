using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class InventoryDisplay : MonoBehaviour
{
    [Header("Grid Setup")]
    public Transform gridContainer;
    public GameObject itemSlotPrefab;

    [Header("Settings")]
    public bool loadOnStart = true;

    private HashSet<string> shownItems = new HashSet<string>();

    private void Start()
    {
        if (loadOnStart)
            LoadInventory();
    }

    public void LoadInventory()
    {
        var userId = SessionManager.Instance?.GameData?.playerId ?? GameData.DefaultPlayerId();

        APIManager.Instance.GetUserInventory(userId,
            response => DisplayInventory(response),
            error => Debug.LogWarning($"[InventoryDisplay] Failed to load: {error}")
        );
    }

    private void DisplayInventory(InventoryResponse response)
    {
        ClearGrid();

        if (response.inventory == null || response.inventory.Length == 0)
        {
            Debug.Log("[InventoryDisplay] Empty inventory");
            return;
        }

        shownItems.Clear();
        var allItems = response.inventory;

        foreach (var invItem in allItems)
        {
            if (shownItems.Contains(invItem.item_id))
                continue;

            var itemData = HokkienItemRegistry.GetItem(invItem.item_id);
            if (itemData == null)
            {
                Debug.LogWarning($"[InventoryDisplay] Item not found in registry: {invItem.item_id}");
                continue;
            }

            CreateSlot(itemData, invItem, allItems);
            shownItems.Add(invItem.item_id);
        }
    }

    private void CreateSlot(HokkienItem item, InventoryItemResponse invItem, InventoryItemResponse[] allItems)
    {
        if (itemSlotPrefab == null || gridContainer == null) return;

        var slot = Instantiate(itemSlotPrefab, gridContainer);

        var icon = slot.transform.Find("Icon")?.GetComponent<Image>();
        var nameText = slot.transform.Find("NameText")?.GetComponent<TMP_Text>();
        var countText = slot.transform.Find("CountText")?.GetComponent<TMP_Text>();

        if (icon != null)
            icon.sprite = item.icon;

        if (nameText != null)
            nameText.SetText(item.displayName);

        if (countText != null)
        {
            var count = CountItemOccurrences(invItem.item_id, allItems);
            countText.SetText($"x{count}");
            countText.gameObject.SetActive(count > 1);
        }
    }

    private int CountItemOccurrences(string itemId, InventoryItemResponse[] inventory)
    {
        if (inventory == null) return 0;
        return inventory.Count(i => i.item_id == itemId);
    }

    public void ClearGrid()
    {
        if (gridContainer == null) return;

        foreach (Transform child in gridContainer)
        {
            Destroy(child.gameObject);
        }
    }
}
