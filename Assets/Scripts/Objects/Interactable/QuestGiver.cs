using TMPro;
using UnityEngine;
using System.Collections;

public class QuestGiver : Interactable
{
    public GameObject dialogBox;
    public TextMeshProUGUI dialogText;
    public string[] introDialogue;
    public string[] dialogueQuestIncomplete;
    public string[] dialogueQuestComplete;
    public bool dialogActive;
    public int currentDialogIndex = 0;

    public Signal checkQuestCompletion;
    public Signal questComplete;

    private string[] currentDialogue;
    private bool isQuestDone = false;
    private bool questGiven = false;
    private bool isChecking = false;

    public override void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
        if (dialogBox == null)
        {
            dialogBox = GameObject.FindGameObjectWithTag("DialogBox");
        }
        if (dialogText == null)
        {
            dialogText = dialogBox.GetComponentInChildren<TextMeshProUGUI>();
        }
        if (firstInteractionDone.runtimeValue == true)
        {
            if (flashingAnimator != null)
            {
                flashingAnimator.SetBool("isFlashing", false);
            }
        }
    }
    public virtual void Update()
    {
        if (dialogActive && Input.GetKeyDown(KeyCode.E))
        {
            TriggerDialogue();
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

    public virtual void TriggerDialogue()
    {
        if(currentDialogIndex == 0)
        {
            if (questGiven)
            {
                if (!isQuestDone) //if the quest isn't marked as done, check to see if the requirements are complete, and wait for an answer
                {
                    StartCoroutine(WaitForCheck());
                }

            
                if (!isQuestDone)
                {
                    currentDialogue = dialogueQuestIncomplete;
                }
                else
                {
                    currentDialogue = dialogueQuestComplete;
                }
            } else
            {
                currentDialogue = introDialogue;
            }
        }


        if (audioSource != null && interactSound != null)
        {
            audioSource.PlayOneShot(interactSound);
        }

        if (!dialogBox.activeSelf)
        {
            dialogBox.SetActive(true);
            currentDialogIndex = 0;
            dialogText.text = currentDialogue.Length > 0 ? currentDialogue[currentDialogIndex] : "";
        }
        else
        {
            currentDialogIndex++;
            if (currentDialogIndex < currentDialogue.Length)
            {
                dialogText.text = currentDialogue[currentDialogIndex];
            }
            else
            {
                dialogBox.SetActive(false);
                dialogActive = false;
                currentDialogIndex = 0;
                if (!questGiven)
                {
                    questGiven = true;
                }
                if (questGiven && isQuestDone)
                {
                    questComplete.Raise();
                }
                Debug.Log("Dialog ended, calling base Interact.");
                base.Interact();
            }
        }
    }

    IEnumerator WaitForCheck()
    {
        Debug.Log("Checking completion...");
        isChecking = true;
        checkQuestCompletion.Raise();

        yield return new WaitWhile(() => isChecking);

        Debug.Log("Check complete.");
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

    public void MarkQuestDone()
    {
        Debug.Log("Quest complete!");
        isQuestDone = true;
    }

    public void SetIsChecking(bool answer)
    {
        isChecking = answer;
    }
}

