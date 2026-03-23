using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Hokkien/Inventory")]
public class HokkienInventory : ScriptableObject
{
    public List<HokkienItem> items = new List<HokkienItem>();

    public void AddItem(HokkienItem item)
    {
        if (item != null && !items.Contains(item))
        {
            items.Add(item);
            Debug.Log($"[Inventory] Added item: {item.displayName}");
        }
    }

    public bool HasItem(string backendItemId)
    {
        return items.Exists(i => i.backendItemId == backendItemId);
    }

    public bool HasItem(HokkienItem item)
    {
        return items.Contains(item);
    }

    public HokkienItem GetItemByBackendId(string backendItemId)
    {
        return items.Find(i => i.backendItemId == backendItemId);
    }
}
