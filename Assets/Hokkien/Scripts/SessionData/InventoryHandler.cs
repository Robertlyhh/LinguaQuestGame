using UnityEngine;
using System.Linq;

public class InventoryHandler : MonoBehaviour, ISessionData
{
    public HokkienInventory inventory;

    public void LoadData(GameData data)
    {
        if (inventory == null) return;
        
        inventory.items.Clear();
        
        if (data.acquiredItemIds == null) return;

        foreach (var itemId in data.acquiredItemIds)
        {
            var item = HokkienItemRegistry.GetItem(itemId);
            if (item != null)
            {
                inventory.AddItem(item);
            }
        }
    }

    public void SaveData(ref GameData data)
    {
        if (data.acquiredItemIds == null)
            data.acquiredItemIds = new System.Collections.Generic.List<string>();
        else
            data.acquiredItemIds.Clear();

        if (inventory != null)
        {
            data.acquiredItemIds.AddRange(inventory.items.Select(i => i.backendItemId));
        }
    }
}
