using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionZone : MonoBehaviour
{
    public string sceneToLoad;
    public PetBubble petBubble;

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Something entered trigger: " + other.name + " tag: " + other.tag);

        if (other.CompareTag("Player") && !other.isTrigger)
        {
            Debug.Log("Player entered transition zone!");

            if (petBubble != null && petBubble.currentRoutine != null)
            {
                Debug.Log("Tutorial not finished, blocking transition.");
                return;
            }

            Debug.Log("Loading scene: " + sceneToLoad);
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}