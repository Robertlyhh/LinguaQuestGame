using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class TutorialNPC : MonoBehaviour
{
    [Header("UI")]
    public GameObject dialoguePanel;
    public TMP_Text dialogueText;
    public TMP_Text nameText;
    public string npcName = "Market Guide";

    [Header("Settings")]
    public float typingSpeed = 0.04f;
    public KeyCode interactKey = KeyCode.E;

    [Header("Dialogue")]
    public List<string> tutorialLines = new List<string>()
    {
        "Welcome to the market, little frog!",
        "There are wonderful stalls here, each with something to teach you.",
        "Walk up to a stall and press E to interact with the vendor.",
        "They will share their knowledge with you — so listen closely!",
        "Go ahead, explore the market. Your adventure begins now!"
    };

    private bool playerInRange = false;
    private bool isActive = false;
    private bool isTyping = false;
    private int currentIndex = 0;
    private Coroutine typingCoroutine;

    void Start()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
        if (nameText != null)
            nameText.text = npcName;
    }

    void Update()
    {
        if (!playerInRange) return;

        if (Input.GetKeyDown(interactKey) ||
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.Space))
        {
            if (!isActive)
            {
                StartDialogue();
            }
            else if (isTyping)
            {
                // Skip to full line instantly
                SkipTyping();
            }
            else
            {
                NextLine();
            }
        }
    }

    private void StartDialogue()
    {
        isActive = true;
        currentIndex = 0;
        dialoguePanel.SetActive(true);
        ShowLine(tutorialLines[currentIndex]);
    }

    private void ShowLine(string line)
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeLine(line));
    }

    private IEnumerator TypeLine(string line)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }

    private void SkipTyping()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        dialogueText.text = tutorialLines[currentIndex];
        isTyping = false;
    }

    private void NextLine()
    {
        currentIndex++;

        if (currentIndex < tutorialLines.Count)
        {
            ShowLine(tutorialLines[currentIndex]);
        }
        else
        {
            EndDialogue();
        }
    }

    private void EndDialogue()
    {
        isActive = false;
        currentIndex = 0;
        dialoguePanel.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            EndDialogue();
        }
    }
}