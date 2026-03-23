using UnityEngine;

[CreateAssetMenu(menuName = "Hokkien/Item")]
public class HokkienItem : ScriptableObject
{
    public string backendItemId;
    public string displayName;
    public string description;
    public Sprite icon;
}
