using UnityEngine;

[CreateAssetMenu(fileName = "ProvinceQuestion", menuName = "GeoGame/ProvinceQuestion")]
public class ProvinceQuestion : ScriptableObject
{
    public string questionText;
    public Sprite questionImage; // optional photo clue
    public string correctProvince; // must match province GameObject name
    public string funFact; // shown after answer
}