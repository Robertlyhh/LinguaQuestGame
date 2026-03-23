using UnityEngine;
using System.Collections.Generic;

public class HokkienItemRegistry : MonoBehaviour
{
    public static HokkienItemRegistry Instance { get; private set; }

    [Header("Items (assign in Inspector)")]
    public List<HokkienItem> items = new List<HokkienItem>();
    
    private Dictionary<string, HokkienItem> itemLookup;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildLookup();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void BuildLookup()
    {
        itemLookup = new Dictionary<string, HokkienItem>();
        
        foreach (var item in items)
        {
            if (item != null && !string.IsNullOrEmpty(item.backendItemId))
            {
                itemLookup[item.backendItemId] = item;
                UnityEngine.Debug.Log($"[ItemRegistry] Registered: {item.backendItemId} -> {item.displayName}");
            }
        }
        
        UnityEngine.Debug.Log($"[ItemRegistry] Total registered: {itemLookup.Count}");
    }

    public static HokkienItem GetItem(string backendItemId)
    {
        if (Instance == null)
        {
            UnityEngine.Debug.LogWarning("[ItemRegistry] No instance found!");
            return null;
        }

        if (string.IsNullOrEmpty(backendItemId))
            return null;

        Instance.itemLookup.TryGetValue(backendItemId, out var item);
        return item;
    }

    public static List<HokkienItem> GetAllItems()
    {
        if (Instance == null)
            return new List<HokkienItem>();

        return Instance.items;
    }
}
