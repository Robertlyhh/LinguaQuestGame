using UnityEngine;
using TMPro;

public class TutorialNPC : MonoBehaviour
{
    public GameObject interactionPrompt; // Assign TutorialPanel / "Press E" UI here
    public GameObject arrowContainer;     // Assign ArrowContainer here
    
    [Header("Dialogue UI Connections")]
    public GameObject dialoguePanel;      // Assign DialoguePanel here
    public TMP_Text dialogueText;         // Assign DialogueText here
    
    [TextArea(3, 10)]
    public string helloMessage = "Hello! Welcome to the night market.";

    private bool isPlayerNearby = false;
    private bool hasInteracted = false; // Tracks if the conversation started

    void Start()
    {
        if (interactionPrompt != null) interactionPrompt.SetActive(true); 
        
        if (arrowContainer != null) arrowContainer.SetActive(true); 
        if (dialoguePanel != null) dialoguePanel.SetActive(false);  
    }

    void Update()
    {
        // Only allow interaction if nearby AND they haven't talked to the NPC yet
        if (isPlayerNearby && !hasInteracted && Input.GetKeyDown(KeyCode.E))
        {
            Interact();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !hasInteracted)
        {
            isPlayerNearby = true;
            if (interactionPrompt != null) interactionPrompt.SetActive(true);
            if (arrowContainer != null) arrowContainer.SetActive(false); 
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            
            // FIX: Only hide the "Press E" prompt if they walk away WITHOUT interacting
            if (!hasInteracted)
            {
                if (interactionPrompt != null) interactionPrompt.SetActive(false);
                if (arrowContainer != null) arrowContainer.SetActive(true); 
            }
        }
    }

    void Interact()
    {
        Debug.Log("Interacting with NPC! Dialogue locked open.");
        hasInteracted = true; // Locks the dialogue state
        
        // 1. Turn off the tutorial hint prompt ("Press E to Interact") permanently
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
        
        // 2. Open the Dialogue box and assign the message text
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (dialogueText != null) dialogueText.text = helloMessage;

        FinishTutorialStep();
    }

    void FinishTutorialStep()
    {
        if (arrowContainer != null)
        {
            Destroy(arrowContainer); 
        }
    }
}