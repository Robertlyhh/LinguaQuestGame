using UnityEngine;

public class LonghouseGreeter : MonoBehaviour, IInteractable
{
    public static LonghouseGreeter Instance;
    private bool isUnlocked = false;

    public GameObject lockedMessage; // optional "Talk to everyone first!" UI

    void Awake()
    {
        Instance = this;
    }

    public void Unlock()
    {
        isUnlocked = true;
        if (lockedMessage != null)
            lockedMessage.SetActive(false);
        Debug.Log("Longhouse Greeter unlocked!");
    }

    public bool CanInteract() => isUnlocked;

    public void Interact()
    {
        if (!isUnlocked)
        {
            Debug.Log("Talk to all NPCs first!");
            return;
        }

        // Launch your game here
        Debug.Log("Launching Longhouse game!");
    }
}