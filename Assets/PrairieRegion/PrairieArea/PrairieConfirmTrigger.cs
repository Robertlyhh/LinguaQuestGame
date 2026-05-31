using UnityEngine;
using UnityEngine.SceneManagement;

public class PrairieConfirmTrigger : MonoBehaviour
{
    public string sceneToLoad;
    public string promptMessage;

    private bool hasTriggered = false;

    void OnTriggerEnter2D(Collider2D other)
    {

        Debug.Log("TRIGGER ENTERED");

        if (!PrairieNPCTracker.Instance.AllVisited())
        {
            UIConfirmPrompt.Instance.Show(
                "TALK TO ALL THE PEOPLE IN THE AREA BEFORE ENTERING THE MINIGAME",
                () => { }
            );
            return;
        }

        if (hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            Debug.Log("PLAYER DETECTED");

            hasTriggered = true;

            UIConfirmPrompt.Instance.Show(promptMessage, () =>
            {
                Debug.Log("CALLBACK EXECUTING");
                SceneManager.LoadScene(sceneToLoad);
            });
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            hasTriggered = false;
        }
    }
}