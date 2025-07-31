using UnityEngine;

public class StoreTrigger : MonoBehaviour
{
    public GameObject storeUI; // Assign your StoreCanvas here
    private bool playerInZone = false;
    public Signal contextSignal; // Signal to notify when store is opened

    void Update()
    {
        if (playerInZone && Input.GetKeyDown(KeyCode.E))
        {
            OpenStore();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = true;
            Debug.Log("Player entered store zone. Press E to open store.");
            if (contextSignal != null)
            {
                contextSignal.Raise(); // Notify that the store is open
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = false;
            CloseStore();
            Debug.Log("Player left store zone.");
            if (contextSignal != null)
            {
                contextSignal.Raise(); // Notify that the store is closed
            }
        }
    }

    void OpenStore()
    {
        if (storeUI != null)
        {
            storeUI.SetActive(true);
            //Time.timeScale = 0f; // Optional: pause game when store is open
        }
    }

    public void CloseStore()
    {
        if (storeUI != null)
        {
            storeUI.SetActive(false);
            //Time.timeScale = 1f; // Resume game
        }
    }
}
