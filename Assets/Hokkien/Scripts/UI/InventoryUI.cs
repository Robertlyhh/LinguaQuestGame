using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    public Transform itemsContainer;
    public GameObject itemSlotPrefab;

    [Header("Grid Settings")]
    public Vector2 cellSize = new Vector2(150, 40);
    public Vector2 spacing = new Vector2(10, 5);
    public int constraintCount = 4;
    public TextAnchor childAlignment = TextAnchor.UpperLeft;

    private HokkienInventory inventory;
    private GridLayoutGroup gridLayout;

    private void Awake()
    {
        inventory = Resources.Load<HokkienInventory>("HokkienInventory");

        if (itemsContainer == null)
            itemsContainer = transform.Find("ItemsContainer");
    }

    private void Start()
    {
        SetupGrid();
        RefreshInventory();
    }

    private void SetupGrid()
    {
        if (itemsContainer == null) return;

        gridLayout = itemsContainer.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
            gridLayout = itemsContainer.gameObject.AddComponent<GridLayoutGroup>();

        gridLayout.cellSize = cellSize;
        gridLayout.spacing = spacing;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = constraintCount;
        gridLayout.childAlignment = childAlignment;
    }

    public void RefreshInventory()
    {
        if (inventory == null || itemsContainer == null) return;

        foreach (Transform child in itemsContainer)
            Destroy(child.gameObject);

        foreach (var item in inventory.items)
        {
            GameObject slot = Instantiate(itemSlotPrefab, itemsContainer);
            slot.name = item.displayName;
        }
    }
}