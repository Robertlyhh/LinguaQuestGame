using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class InventoryEntry
{
    public Item item;
    public int quantity;

    public InventoryEntry(Item item, int quantity)
    {
        this.item = item;
        this.quantity = quantity;
    }
}

[CreateAssetMenu(menuName = "ScriptableObjects/Inventory")]
public class Inventory : ScriptableObject, ISerializationCallbackReceiver
{
    public Item currentItem;
    public List<InventoryEntry> items = new List<InventoryEntry>();
    public int coins;

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    private static void RegisterInventoryResetOnPlayMode()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            string[] guids = AssetDatabase.FindAssets("t:Inventory");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Inventory asset = AssetDatabase.LoadAssetAtPath<Inventory>(path);
                if (asset != null)
                {
                    asset.ResetInventory();
                    EditorUtility.SetDirty(asset);
                }
            }
        }
    }
#endif

    public void ResetInventory()
    {
        items.Clear();
        currentItem = null;
        coins = 0;
    }

    public void AddItem(Item item, int amount = 1)
    {
        if (item != null)
        {
            InventoryEntry entry = items.Find(e => e.item == item);
            if (entry != null)
            {
                entry.quantity += amount;
            }
            else
            {
                items.Add(new InventoryEntry(item, amount));
            }
            currentItem = item;
            Debug.Log($"Added {amount} x {item.itemName}");
        }
        else
        {
            Debug.LogWarning("Tried to add null item.");
        }
    }

    public bool HasItem(Item item)
    {
        InventoryEntry entry = items.Find(e => e.item == item);
        return entry != null && entry.quantity > 0;
    }

    public void RemoveItem(Item item, int amount = 1)
    {
        InventoryEntry entry = items.Find(e => e.item == item);
        if (entry != null)
        {
            entry.quantity -= amount;
            if (entry.quantity <= 0)
            {
                items.Remove(entry);
            }
        }
    }

    public int GetItemCount(Item item)
    {
        InventoryEntry entry = items.Find(e => e.item == item);
        return entry != null ? entry.quantity : 0;
    }

    public void OnBeforeSerialize() { }

    public void OnAfterDeserialize() { }
}
