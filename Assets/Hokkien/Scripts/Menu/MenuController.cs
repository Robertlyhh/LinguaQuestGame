using UnityEngine;

public class MenuController : MonoBehaviour
{
    public GameObject menuCanvas;
    public InventoryDisplay inventoryDisplay;
    public HokkienQuizLauncher quizLauncher;
    public frogMove playerMovement;

    private bool quizActive;
    private GameObject welcomePanel;
    // These flags track panels that may be closed directly by UI buttons.
    private bool menuStateOwned;
    private bool welcomeStateOwned;

    private void Awake()
    {
        if (playerMovement == null)
        {
            playerMovement = FindObjectOfType<frogMove>();
        }
    }

    void Start()
    {
        quizActive = false;
        menuCanvas.SetActive(false);
        welcomePanel = FindSceneObject("Welcome Panel");
        SyncOverlayStates();
    }

    void Update()
    {
        // Close buttons can hide panels without calling this controller.
        SyncOverlayStates();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleEscape();
            return;
        }

        if (quizActive)
        {
            return;
        }

        if (HokkienInteractionState.CurrentMode != HokkienInteractionMode.World
            && HokkienInteractionState.CurrentMode != HokkienInteractionMode.Menu)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (menuCanvas.activeSelf)
            {
                CloseMenu();
            }
            else
            {
                OpenMenu();
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
            CloseWelcomePanel();
            return;
        }

        if (menuCanvas != null && menuCanvas.activeInHierarchy)
        {
            CloseMenu();
        }
    }

    private void SyncOverlayStates()
    {
        SyncWelcomeState();
        SyncMenuState();
    }

    private void SyncWelcomeState()
    {
        if (welcomePanel == null)
        {
            return;
        }

        bool isVisible = welcomePanel.activeInHierarchy;
        if (isVisible && !welcomeStateOwned
            && HokkienInteractionState.TryEnter(HokkienInteractionMode.Welcome, this))
        {
            welcomeStateOwned = true;
            SetPlayerMovementPaused(true);
        }
        else if (!isVisible && welcomeStateOwned)
        {
            welcomeStateOwned = false;
            HokkienInteractionState.Exit(this);
            SetPlayerMovementPaused(false);
        }
    }

    private void SyncMenuState()
    {
        if (menuCanvas == null)
        {
            return;
        }

        bool isVisible = menuCanvas.activeInHierarchy;
        if (isVisible && !menuStateOwned
            && HokkienInteractionState.TryEnter(HokkienInteractionMode.Menu, this))
        {
            menuStateOwned = true;
            SetPlayerMovementPaused(true);
        }
        else if (!isVisible && menuStateOwned)
        {
            menuStateOwned = false;
            HokkienInteractionState.Exit(this);
            SetPlayerMovementPaused(false);
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
        if (quizLauncher == null)
        {
            Debug.LogWarning("[MenuController] Quiz launcher is not assigned.");
            return;
        }

        bool reopenMenuOnFailure = menuCanvas != null && menuCanvas.activeSelf;
        // Let the quiz claim input after the menu releases it.
        CloseMenu();

        if (!quizLauncher.StartQuiz())
        {
            if (reopenMenuOnFailure && !menuCanvas.activeSelf)
            {
                OpenMenu();
            }
        }
    }

    public bool OpenMenu()
    {
        // Repeated calls should not reload the current menu.
        if (menuStateOwned && menuCanvas != null && menuCanvas.activeSelf)
        {
            return true;
        }

        if (menuCanvas == null
            || !HokkienInteractionState.TryEnter(HokkienInteractionMode.Menu, this))
        {
            return false;
        }

        menuStateOwned = true;
        menuCanvas.SetActive(true);
        SetPlayerMovementPaused(true);

        if (inventoryDisplay != null)
        {
            inventoryDisplay.LoadInventory();
        }

        return true;
    }

    public void CloseMenu()
    {
        if (menuCanvas != null)
        {
            menuCanvas.SetActive(false);
        }

        if (menuStateOwned)
        {
            menuStateOwned = false;
            HokkienInteractionState.Exit(this);
            SetPlayerMovementPaused(false);
        }
    }

    private void CloseWelcomePanel()
    {
        welcomePanel.SetActive(false);

        if (welcomeStateOwned)
        {
            welcomeStateOwned = false;
            HokkienInteractionState.Exit(this);
            SetPlayerMovementPaused(false);
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

    private void SetPlayerMovementPaused(bool paused)
    {
        if (playerMovement != null)
        {
            playerMovement.SetMovementPaused(paused);
        }
    }

    private void OnDestroy()
    {
        HokkienInteractionState.Exit(this);
    }
}
