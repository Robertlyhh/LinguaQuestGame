using System.Collections; 
using System.Collections.Generic; 
using UnityEngine;
using UnityEngine.InputSystem; 

public class InteractionDetector : MonoBehaviour
{
    public IInteractable interactableInRange = null; 
    public GameObject interactionIcon; 
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        interactionIcon.SetActive(false);
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

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
        if (collision.TryGetComponent(out IInteractable interactable) && interactable.CanInteract())
        {
            interactableInRange = interactable; 
            interactionIcon.SetActive(true); 
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out IInteractable interactable) && interactable == interactableInRange)
        {
            interactableInRange = null; 
            interactionIcon.SetActive(false); 
        }
    }
}
