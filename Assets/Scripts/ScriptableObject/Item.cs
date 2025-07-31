using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class Item : ScriptableObject
{
    public string itemName;
    public string description;
    public Sprite itemSprite;
    public int itemID;
    public int price;

    // You can add more properties or methods as needed
}

