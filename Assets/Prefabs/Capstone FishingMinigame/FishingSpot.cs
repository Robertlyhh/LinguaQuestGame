using UnityEngine;

public class FishingSpot : MonoBehaviour
{
    public bool isCorrectSpot;

    [Header("Correct Spot Result")]
    public GameObject rockToRemove;

    [Header("Wrong Spot Result")]
    public GameObject objectToSpawn;

    [HideInInspector] public bool playerInZone;
}
