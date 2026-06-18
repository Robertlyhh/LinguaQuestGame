using UnityEngine;
using TMPro;

public class TutorialNPC : MonoBehaviour
{
    public GameObject interactionPrompt; // "Press E" UI
    public GameObject arrowContainer;     // Tracking Arrow
    
    [Header("Dialogue UI")]
    public GameObject dialoguePanel;
    public TMP_Text dialogueText;
    
    [Header("Dialogue Lines")]
    [TextArea(2, 5)]
    public string[] dialogueLines; // Array to hold your 2-3 sentences
    private int currentLine = 0;   // Keeps track of which sentence we are on

    private bool isPlayerNearby = false;
    private bool isTalking = false; 

    void Start()
    {
        if (interactionPrompt != null) interactionPrompt.SetActive(true); 
        if (arrowContainer != null) arrowContainer.SetActive(true); 
        if (dialoguePanel != null) dialoguePanel.SetActive(false);  
    }

    void Update()
    {
        if (isPlayerNearby && !isTalking && Input.GetKeyDown(KeyCode.E))
        {
            StartDialogue();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isTalking)
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
            if (!isTalking)
            {
                if (interactionPrompt != null) interactionPrompt.SetActive(false);
                if (arrowContainer != null) arrowContainer.SetActive(true); 
            }
        }
    }

    void StartDialogue()
    {
        if (dialogueLines.Length == 0) return; // Safety check in case you forgot to type anything!

        isTalking = true; 
        currentLine = 0; // Always start at the first sentence
        
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
        if (arrowContainer != null) Destroy(arrowContainer); // Get rid of the arrow
        
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        dialogueText.text = dialogueLines[currentLine];
    }

    // ==========================================
    // UI BUTTON METHODS
    // ==========================================

    public void ContinueDialogue()
    {
        Debug.Log("Continue button");
        currentLine++; // Move to the next sentence in the list

        // Check if we still have lines left to show
        if (currentLine < dialogueLines.Length)
        {
            dialogueText.text = dialogueLines[currentLine];
        }
        else
        {
            // If we ran out of lines, just close the dialogue automatically
            CloseDialogue(); 
        }
    }

    public void CloseDialogue()
    {
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        isTalking = false; // Resets so the player can press E to read it again if they want
    }
}