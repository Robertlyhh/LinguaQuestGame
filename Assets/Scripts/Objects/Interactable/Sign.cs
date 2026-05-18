using TMPro;
using UnityEngine;

public class Sign : Interactable
{
    public GameObject dialogBox;
    public TextMeshProUGUI dialogText;
    public string[] dialogs;
    public bool dialogActive;
    public int currentDialogIndex = 0;
    public PetBubble petBubble;

    public override void Start()
    {
        if (dialogBox == null)
            dialogBox = GameObject.FindGameObjectWithTag("DialogBox");
        if (dialogText == null)
            dialogText = dialogBox.GetComponentInChildren<TextMeshProUGUI>();

        dialogActive = false;
        currentDialogIndex = 0;
        base.Start();
    }

    public virtual void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            if (audioSource != null && interactSound != null)
                audioSource.PlayOneShot(interactSound);

            if (!dialogBox.activeSelf)
            {
                // Pause fox and flag interaction
                if (petBubble != null)
                {
                    petBubble.playerHasInteracted = true;
                    petBubble.isPaused = true;
                }
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
                    // Dialog finished naturally — unpause fox
                    dialogBox.SetActive(false);
                    dialogActive = false;
                    currentDialogIndex = 0;
                    if (petBubble != null) petBubble.isPaused = false;
                    base.Interact();
                }
            }
        }
        else if (dialogBox.activeSelf && playerInRange && Input.GetKeyDown(KeyCode.Escape))
        {
            dialogBox.SetActive(false);
            dialogActive = false;
            currentDialogIndex = 0;
            if (petBubble != null) petBubble.isPaused = false; // unpause fox
        }
        else if (dialogBox.activeSelf && playerInRange && Input.GetKeyDown(KeyCode.Space))
        {
            dialogBox.SetActive(false);
            dialogActive = false;
            currentDialogIndex = 0;
            if (petBubble != null) petBubble.isPaused = false; // unpause fox
        }
        else if (dialogBox.activeSelf && playerInRange && Input.GetKeyDown(KeyCode.Return))
        {
            dialogBox.SetActive(false);
            dialogActive = false;
            currentDialogIndex = 0;
            if (petBubble != null) petBubble.isPaused = false; // unpause fox
        }
    }

    public override void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !other.isTrigger)
        {
            playerInRange = true;
            dialogActive = true;
            currentDialogIndex = 0;
            context.Raise();
        }
    }

    public override void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !other.isTrigger)
        {
            playerInRange = false;
            dialogActive = false;
            dialogBox.SetActive(false);
            currentDialogIndex = 0;
            if (petBubble != null) petBubble.isPaused = false; // unpause fox on exit
            context.Raise();
        }
    }
}