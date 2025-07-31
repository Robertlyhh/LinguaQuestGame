using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

public class MagicStone : Sign
{
    public GameObject WizardQuizManager;
    public SpriteRenderer spriteRenderer;
    public Animator animator;
    public bool hasGivenInstructions = false;

    public override void Update()
    {
        if (dialogActive && Input.GetKeyDown(KeyCode.E))
        {


            if (hasGivenInstructions)
            {
                deactivateStone();
                return;
            }

            if (audioSource != null && interactSound != null)
            {
                audioSource.PlayOneShot(interactSound);
            }

            activateStone();

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
                    //dialogBox.SetActive(false);
                    dialogActive = false;
                    currentDialogIndex = 0;
                    hasGivenInstructions = true;
                    WizardQuizManager.GetComponent<WizardQuizManager>().StartQuizDialogue();
                }
            }
        }
    }

    public override void Start()
    {
        base.Start();
        if (WizardQuizManager == null)
        {
            WizardQuizManager = GameObject.Find("WizardQuizManager");
        }
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    public void activateStone()
    {
        animator.SetBool("isActive", true);
        audioSource.PlayOneShot(interactSound);
    }

    public void deactivateStone()
    {
        animator.SetBool("isActive", false);
        audioSource.PlayOneShot(interactSound);
    }
}

