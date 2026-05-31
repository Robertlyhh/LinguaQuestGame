using UnityEngine;

public class PrairieVisitNPC : MonoBehaviour
{
    public int npcID;
    private bool playerInRange = false;
    private bool visited = false;

    void Update()
    {
        if (playerInRange && !visited && Input.GetKeyDown(KeyCode.E))
        {
            visited = true;
            PrairieNPCTracker.Instance.MarkVisited(npcID);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerInRange = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerInRange = false;
    }
}