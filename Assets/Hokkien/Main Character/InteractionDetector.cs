using System.Collections; 
using System.Collections.Generic; 
using UnityEngine;
using UnityEngine.InputSystem; 

public class InteractionDetector : MonoBehaviour
{
    public IInteractable interactableInRange = null; 
    public GameObject interactionIcon; 

    private void OnEnable()
    {
        HokkienInteractionState.ModeChanged += RefreshInteractionIcon;
    }

    private void OnDisable()
    {
        HokkienInteractionState.ModeChanged -= RefreshInteractionIcon;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        RefreshInteractionIcon();
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        // Dialogue and quiz screens own input while they are open.
        if (HokkienInteractionState.CurrentMode != HokkienInteractionMode.World)
        {
            RefreshInteractionIcon();
            return;
        }

        if (interactableInRange == null)
        {
            Debug.Log("No interactable in range");
            return;
        }

        Debug.Log("Interacting with: " + (interactableInRange as MonoBehaviour).name);

        if (!interactableInRange.CanInteract())
        {
            Debug.Log("Cannot interact");
            interactionIcon.SetActive(false);
            return;
        }

        interactableInRange.Interact();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out IInteractable interactable))
        {
            interactableInRange = interactable; 
            RefreshInteractionIcon();
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out IInteractable interactable) && interactable == interactableInRange)
        {
            interactableInRange = null; 
            RefreshInteractionIcon();
        }
    }

    private void RefreshInteractionIcon()
    {
        if (interactionIcon == null)
        {
            return;
        }

        bool canInteract = HokkienInteractionState.CurrentMode == HokkienInteractionMode.World
            && interactableInRange != null
            && interactableInRange.CanInteract();
        interactionIcon.SetActive(canInteract);
    }
}
