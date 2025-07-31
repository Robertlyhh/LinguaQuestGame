using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SpecialDoor : Interactable
{
    public GameObject dialogBox;
    public TextMeshProUGUI dialogText;
    public string[] dialogs;
    public bool dialogActive;
    public int currentDialogIndex = 0;
    public List<Item> requiredItems;
    public Inventory playerInventory;
    public bool isUnlocked = false;
    public BoolValue isUnlockedValue;
    public int coinsAbsorbed = 0;


    public void Awake()
    {
        this.gameObject.SetActive(!isUnlockedValue.runtimeValue); // Initially hide the door
    }

    public virtual void Update()
    {
        detectCoinsAndAbsorb();
        if (dialogActive && !isUnlocked && Input.GetKeyDown(KeyCode.E))
        {
            // Check if player has all required items
            bool hasAllItems = true;
            foreach (var item in requiredItems)
            {
                if (!playerInventory.HasItem(item))
                {
                    hasAllItems = false;
                    break;
                }
            }

            if (hasAllItems)
            {
                isUnlocked = true;
                dialogBox.SetActive(false);
                dialogActive = false;
                currentDialogIndex = 0;
                // Optionally, add code to open the door here
                this.gameObject.SetActive(false); // Example action for unlocking
                isUnlockedValue.runtimeValue = true; // Update the BoolValue
                Debug.Log("Door unlocked!");
            }
            else
            {
                if (audioSource != null && interactSound != null)
                {
                    audioSource.PlayOneShot(interactSound);
                }

                if (!dialogBox.activeSelf)
                {
                    dialogBox.SetActive(true);
                    currentDialogIndex = 0;
                    dialogText.text = dialogs.Length > 0 ? dialogs[currentDialogIndex] : "";
                }
                else
                {
                    currentDialogIndex++;
                    if (currentDialogIndex < dialogs.Length)
                    {
                        dialogText.text = dialogs[currentDialogIndex];
                    }
                    else
                    {
                        dialogBox.SetActive(false);
                        dialogActive = false;
                        currentDialogIndex = 0;
                    }
                }
            }
        }
        else if (dialogActive && Input.GetKeyDown(KeyCode.Escape))
        {
            dialogBox.SetActive(false);
            dialogActive = false;
            currentDialogIndex = 0;
        }
        else if (dialogActive && Input.GetKeyDown(KeyCode.Space))
        {
            dialogBox.SetActive(false);
            dialogActive = false;
            currentDialogIndex = 0;
        }
        else if (dialogActive && Input.GetKeyDown(KeyCode.Return))
        {
            dialogBox.SetActive(false);
            dialogActive = false;
            currentDialogIndex = 0;
        }
    }


    public override void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !other.isTrigger)
        {
            dialogActive = true;
            currentDialogIndex = 0;
            context.Raise();
        }
    }

    public override void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !other.isTrigger)
        {
            dialogActive = false;
            dialogBox.SetActive(false);
            context.Raise();
            currentDialogIndex = 0;
        }
    }

    public void detectCoinsAndAbsorb()
    {
        // Detect coins in the vicinity and absorb them
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 5f);
        foreach (var collider in colliders)
        {
            if (collider.CompareTag("Coin"))
            {
                // Absorb the coin
                Destroy(collider.gameObject);
                coinsAbsorbed++;
                if (coinsAbsorbed >= 2)
                {
                    isUnlocked = true;
                    isUnlockedValue.runtimeValue = true; // Update the BoolValue
                    this.gameObject.SetActive(false); // Hide the door
                    Debug.Log("Door unlocked by absorbing coins!");
                }
                Debug.Log("Absorbed a coin!");
            }
        }
    }

}

