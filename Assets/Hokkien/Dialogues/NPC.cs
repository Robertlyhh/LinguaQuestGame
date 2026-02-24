using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using DG.Tweening; // 1. Add the DOTween namespace

public class NPC : MonoBehaviour, IInteractable
{
    public NPCDialogue dialogueData;
    public GameObject dialoguePanel;
    public TMP_Text dialogueText, nameText;
    public Image portraitImage;

    [Header("Fade Settings")]
    public float fadeDuration = 0.5f;
    private CanvasGroup panelCanvasGroup; // Used for fading the whole panel

    private int dialogueIndex;
    private bool isTyping, isDialogueActive;

    private void Awake()
    {
        // Get or Add CanvasGroup to the panel
        panelCanvasGroup = dialoguePanel.GetComponent<CanvasGroup>();
        if (panelCanvasGroup == null)
            panelCanvasGroup = dialoguePanel.AddComponent<CanvasGroup>();

        // Ensure it starts invisible
        panelCanvasGroup.alpha = 0;
        //dialoguePanel.SetActive(false);
    }

    public bool CanInteract()
    {
        return !isDialogueActive;
    }

    public void Interact()
    {
        Debug.Log("Interaction triggered");
        if (isDialogueActive)
        {
            NextLine();
        }
        else
        {
            StartDialogue();
        }
    }

    void StartDialogue()
    {
        //isDialogueActive = true;
        dialogueIndex = 0;
        nameText.SetText(dialogueData.name);
        portraitImage.sprite = dialogueData.npcPortrait;

        // DOTween Fade In
        dialoguePanel.SetActive(true);
        panelCanvasGroup.DOKill(); // Stop any current tweening to prevent bugs
        panelCanvasGroup.DOFade(1, fadeDuration).OnComplete(() => {
            StartCoroutine(TypeLine());
        });
    }

    void NextLine()
    {
        if (isTyping)
        {
            StopAllCoroutines();
            dialogueText.SetText(dialogueData.dialogueLines[dialogueIndex]);
            isTyping = false;
        }
        else if (++dialogueIndex < dialogueData.dialogueLines.Length)
        {
            StartCoroutine(TypeLine());
        }
        else
        {
            EndDialogue();
        }
    }

    IEnumerator TypeLine()
    {
        isTyping = true;
        dialogueText.SetText("");
        foreach (char letter in dialogueData.dialogueLines[dialogueIndex])
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(dialogueData.typingSpeed);
        }
        isTyping = false;

        if (dialogueData.autoProgressLines.Length > dialogueIndex && dialogueData.autoProgressLines[dialogueIndex])
        {
            yield return new WaitForSeconds(dialogueData.autoProgressDelay);
            NextLine();
        }
    }

    public void EndDialogue()
    {
        StopAllCoroutines();
        isDialogueActive = false;

        // DOTween Fade Out
        panelCanvasGroup.DOKill();
        panelCanvasGroup.DOFade(0, fadeDuration).OnComplete(() => {
            dialoguePanel.SetActive(false);
            dialogueText.SetText("");
        });
    }
}