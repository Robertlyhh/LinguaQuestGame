using UnityEngine;

public class AddToPartyTest : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PartyManager.Instance.AddToParty(gameObject);
            Debug.Log($"{gameObject.name} added to the party!");
            // gameObject.SetActive(false);
        }
    }
}
