using UnityEngine;

public class MenuController : MonoBehaviour
{
    public GameObject menuCanvas;
    public InventoryDisplay inventoryDisplay;
    public HokkienQuizLauncher quizLauncher;

    private bool quizActive;
    private GameObject welcomePanel;

    void Start()
    {
        quizActive = false;
        menuCanvas.SetActive(false);
        welcomePanel = FindSceneObject("Welcome Panel");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleEscape();
            return;
        }

        if (quizActive)
        {
            return;
        }

        // Keep the menu from opening over an active conversation.
        if (HokkienInteractionState.CurrentMode == HokkienInteractionMode.Dialogue)
        {
            if (menuCanvas != null && menuCanvas.activeSelf)
            {
                menuCanvas.SetActive(false);
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            bool isOpening = !menuCanvas.activeSelf;
            menuCanvas.SetActive(isOpening);

            if (isOpening && inventoryDisplay != null)
            {
                inventoryDisplay.LoadInventory();
            }
        }
    }

    private void HandleEscape()
    {
        // Let the topmost UI state consume Escape first.
        if (HokkienFeedbackPanel.TryHandleEscapeIfVisible())
        {
            return;
        }

        if (quizLauncher != null && quizLauncher.TryHandleEscape())
        {
            return;
        }

        if (NPC.TryHandleEscapeForActiveConversation())
        {
            return;
        }

        if (TutorialNPC.TryHandleEscapeForActiveConversation())
        {
            return;
        }

        if (welcomePanel != null && welcomePanel.activeInHierarchy)
        {
            welcomePanel.SetActive(false);
            return;
        }

        if (menuCanvas != null && menuCanvas.activeInHierarchy)
        {
            menuCanvas.SetActive(false);
        }
    }

    private GameObject FindSceneObject(string objectName)
    {
        // Search inactive scene objects without adding scene references.
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform descendant in descendants)
            {
                if (descendant.name == objectName)
                {
                    return descendant.gameObject;
                }
            }
        }

        return null;
    }

    public void OnQuizButtonPressed()
    {
        if (HokkienInteractionState.CurrentMode == HokkienInteractionMode.Dialogue)
        {
            menuCanvas.SetActive(false);
            return;
        }

        if (quizLauncher == null)
        {
            Debug.LogWarning("[MenuController] Quiz launcher is not assigned.");
            return;
        }

        if (!quizLauncher.StartQuiz())
        {
            menuCanvas.SetActive(true); // If launcher fails to run, drops player back safely.
        }
    }

    public void SetQuizActive(bool active)
    {
        quizActive = active;

        if (quizActive)
        {
            menuCanvas.SetActive(false);
        }
    }
}
