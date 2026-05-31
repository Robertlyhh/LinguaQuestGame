using UnityEngine;

public class enablechest : MonoBehaviour
{
    public GameObject Chest;
    private bool hasActivated = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !hasActivated)
        {
            Chest.SetActive(true);
            hasActivated = true;
        }
    }
}
