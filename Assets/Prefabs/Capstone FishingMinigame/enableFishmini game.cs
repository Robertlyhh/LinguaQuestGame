using UnityEngine;

public class EnableFishMiniGame : MonoBehaviour
{
    public GameObject miniGame;
    public FishingMiniGame fishingMiniGame;

    private FishingSpot fishingSpot;

    private void Awake()
    {
        fishingSpot = GetComponent<FishingSpot>();

        if (fishingSpot == null)
        {
            Debug.LogError("No FishingSpot found on " + gameObject.name);
        }

        if (fishingMiniGame == null)
        {
            Debug.LogError("FishingMiniGame reference is missing on " + gameObject.name);
        }

        if (miniGame == null)
        {
            Debug.LogError("miniGame reference is missing on " + gameObject.name);
        }
    }

    void Update()
    {
        if (fishingSpot == null || fishingMiniGame == null || miniGame == null)
            return;

        if (fishingSpot.playerInZone && Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("Player entered fishing zone and pressed E. Enabling fishing mini-game.");
            fishingMiniGame.SetCurrentSpot(fishingSpot);
            miniGame.SetActive(true);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        //Debug.Log("OnTriggerEnter2D called on " + gameObject.name + " with collider: " + other.name);
        if (fishingSpot == null) return;

        if (other.CompareTag("Player"))
        {
            Debug.Log("Player entered zone: " + gameObject.name);
            fishingSpot.playerInZone = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (fishingSpot == null) return;

        if (other.CompareTag("Player"))
        {
            fishingSpot.playerInZone = false;
        }
    }
}