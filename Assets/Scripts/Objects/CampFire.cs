using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CampFire : Interactable
{

    public SpriteRenderer campfireSprite; // Reference to the campfire sprite renderer
    public Animator campfireAnimator; // Reference to the campfire animator
    public Transform respawnPoint; // The point where the player will respawn
    public FloatValue playerHealth; // Reference to the player's health
    public Signal playerHealthSignal; // Signal to notify when the player's health is reset
    public BoolValue activated; // Reference to a boolean value indicating if the campfire is activated

    void Awake()
    {
        if (campfireSprite == null)
        {
            campfireSprite = GetComponent<SpriteRenderer>();
        }
        if (campfireAnimator == null)
        {
            campfireAnimator = GetComponent<Animator>();
        }
        if (activated.runtimeValue)
        {
            ActivateCampfire(); // If the campfire is already activated, set it up
        }
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E) && !activated.runtimeValue)
        {
            ActivateCampfire();
        }
    }

    private void ActivateCampfire()
    {
        Debug.Log("Campfire activated!");
        if (audioSource != null && interactSound != null)
        {
            audioSource.PlayOneShot(interactSound); // Play interaction sound
        }
        campfireAnimator.SetBool("activated", true); // Set the animator parameter to indicate the campfire is lit
        PlayerRespawnManager.Instance.SetRespawnPoint(respawnPoint.position);
        playerHealth.runtimeValue = Math.Max(playerHealth.runtimeValue, playerHealth.initialValue); // Reset player health
        playerHealthSignal.Raise(); // Notify that the player's health has been reset
        activated.runtimeValue = true; // Set the campfire as activated


    }
}
