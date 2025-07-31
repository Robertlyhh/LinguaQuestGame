using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collectable : MonoBehaviour
{
    public Item item;
    public Inventory playerInventory;
    public AudioClip collectSound;
    public AudioSource audioSource;
    public bool collected = false;
    void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return; // Prevent multiple collections
        Debug.Log("Collectable triggered by: " + other.gameObject.name);
        if (other.CompareTag("Player"))
        {
            Collect();
        }
    }

    void Collect()
    {
        if (playerInventory != null && item != null)
        {
            playerInventory.AddItem(item);
            collected = true;
            PlayCollectSound();
            Destroy(gameObject, 0.25f); // Destroy the collectable after a short delay
        }
    }

    void PlayCollectSound()
    {
        if (audioSource != null && collectSound != null)
        {
            audioSource.PlayOneShot(collectSound);
        }
    }
}
