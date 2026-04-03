using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interactable : MonoBehaviour
{
    public bool playerInRange;
    public Signal context;
    public AudioSource audioSource;
    public AudioClip interactSound;
    public Signal interactSignal;
    public BoolValue firstInteractionDone;
    public Animator flashingAnimator;

    protected void ResolveFlashingAnimator()
    {
        Animator localAnimator = GetComponent<Animator>();

        if (localAnimator != null && localAnimator.runtimeAnimatorController != null)
        {
            flashingAnimator = localAnimator;
            return;
        }

        if (flashingAnimator != null && flashingAnimator.runtimeAnimatorController != null)
        {
            return;
        }

        if (localAnimator != null)
        {
            flashingAnimator = localAnimator;
        }
    }

    protected void SetFlashingState(bool isFlashing)
    {
        ResolveFlashingAnimator();

        if (flashingAnimator == null)
        {
            return;
        }

        if (flashingAnimator.runtimeAnimatorController == null)
        {
            Debug.LogWarning($"Animator on {flashingAnimator.gameObject.name} has no AnimatorController assigned.", flashingAnimator);
            return;
        }

        flashingAnimator.SetBool("isFlashing", isFlashing);
    }

    public virtual void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        ResolveFlashingAnimator();

        if (firstInteractionDone != null && firstInteractionDone.runtimeValue == true)
        {
            SetFlashingState(false);
        }
    }

    public virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            context.Raise();
        }
    }

    public virtual void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            context.Raise();
        }
    }

    public virtual void Interact()
    {
        Debug.Log("Base Interact called on " + gameObject.name);
        if (audioSource != null && interactSound != null)
        {
            audioSource.PlayOneShot(interactSound);
        }
        Debug.Log("Base Interact: Checking firstInteractionDone. Current value: " + (firstInteractionDone != null ? firstInteractionDone.runtimeValue.ToString() : "null"));
        if (firstInteractionDone != null && firstInteractionDone.runtimeValue == false)
        {
            Debug.Log("First interaction done.");
            firstInteractionDone.runtimeValue = true;
            Debug.Log("Stopping flashing animation.");
            SetFlashingState(false);
        }

        if (interactSignal != null)
        {
            interactSignal.Raise();
        }
    }
}
