using UnityEngine;

public class questionMark : MonoBehaviour
{
    private Interactable parentInteractable;

    private void Awake()
    {
        parentInteractable = GetComponentInParent<Interactable>();
    }

    private void Start()
    {
        HandleActiveChange();
    }

    public void handleActiveChange()
    {
        HandleActiveChange();
    }

    public void HandleActiveChange()
    {
        if (parentInteractable == null)
        {
            Debug.LogWarning("QuestionMark could not find a parent Interactable.", this);
            return;
        }

        if (parentInteractable.firstInteractionDone == null)
        {
            Debug.LogWarning("QuestionMark parent Interactable is missing firstInteractionDone.", parentInteractable);
            return;
        }

        Debug.Log("QuestionMark: HandleActiveChange called. firstInteractionDone.runtimeValue = " + parentInteractable.firstInteractionDone.runtimeValue);

        if (parentInteractable.firstInteractionDone.runtimeValue)
        {
            gameObject.SetActive(false);
        }
    }
}
