using UnityEngine;

public class MenuController : MonoBehaviour
{
    public GameObject menuCanvas;
    public GameObject backpackPanel;
    public GameObject lessonTaskPanel;
    public GameObject helpPanel;
    public GameObject hudButtons;
    public InventoryDisplay inventoryDisplay;
    public HokkienQuizLauncher quizLauncher;
    public frogMove playerMovement;

    [Header("Welcome Reveal")]
    public bool showDialoguePageAfterWelcomeClose;
    public GameObject welcomeDialoguePanel;

    private bool quizActive;
    private GameObject welcomePanel;
    // These flags track panels that may be closed directly by UI buttons.
    private bool menuStateOwned;
    private bool welcomeStateOwned;
    private bool backpackStateOwned;
    private bool lessonTaskStateOwned;
    private bool helpStateOwned;
    private bool welcomeDialoguePanelStateOwned;

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

        if (backpackPanel != null && backpackPanel.activeInHierarchy)
        {
            CloseBackpack();
            return;
        }

        if (lessonTaskPanel != null && lessonTaskPanel.activeInHierarchy)
        {
            CloseLessonTask();
            return;
        }

        if (helpPanel != null && helpPanel.activeInHierarchy)
        {
            CloseHelp();
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
        SyncBackpackState();
        SyncLessonTaskState();
        SyncHelpState();
        SyncWelcomeDialoguePanelState();
        SyncHudButtonsVisibility();
    }

    private void SyncHudButtonsVisibility()
    {
        if (hudButtons == null)
        {
            return;
        }

        bool shouldShow = HokkienInteractionState.CurrentMode == HokkienInteractionMode.World;
        if (hudButtons.activeSelf != shouldShow)
        {
            hudButtons.SetActive(shouldShow);
        }
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
            HandleWelcomePanelClosed();
        }
    }

    private void HandleWelcomePanelClosed()
    {
        welcomeStateOwned = false;
        HokkienInteractionState.Exit(this);
        SetPlayerMovementPaused(false);

        if (showDialoguePageAfterWelcomeClose && welcomeDialoguePanel != null)
        {
            welcomeDialoguePanel.SetActive(true);
        }
    }

    private void SyncWelcomeDialoguePanelState()
    {
        if (welcomeDialoguePanel == null)
        {
            return;
        }

        bool isVisible = welcomeDialoguePanel.activeInHierarchy;
        if (isVisible && !welcomeDialoguePanelStateOwned
            && HokkienInteractionState.TryEnter(HokkienInteractionMode.Help, this))
        {
            welcomeDialoguePanelStateOwned = true;
            SetPlayerMovementPaused(true);
        }
        else if (!isVisible && welcomeDialoguePanelStateOwned)
        {
            welcomeDialoguePanelStateOwned = false;
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

    private void SyncBackpackState()
    {
        if (backpackPanel == null)
        {
            return;
        }

        bool isVisible = backpackPanel.activeInHierarchy;
        if (isVisible && !backpackStateOwned
            && HokkienInteractionState.TryEnter(HokkienInteractionMode.Backpack, this))
        {
            backpackStateOwned = true;
            SetPlayerMovementPaused(true);
        }
        else if (!isVisible && backpackStateOwned)
        {
            backpackStateOwned = false;
            HokkienInteractionState.Exit(this);
            SetPlayerMovementPaused(false);
        }
    }

    private void SyncLessonTaskState()
    {
        if (lessonTaskPanel == null)
        {
            return;
        }

        bool isVisible = lessonTaskPanel.activeInHierarchy;
        if (isVisible && !lessonTaskStateOwned
            && HokkienInteractionState.TryEnter(HokkienInteractionMode.LessonTask, this))
        {
            lessonTaskStateOwned = true;
            SetPlayerMovementPaused(true);
        }
        else if (!isVisible && lessonTaskStateOwned)
        {
            lessonTaskStateOwned = false;
            HokkienInteractionState.Exit(this);
            SetPlayerMovementPaused(false);
        }
    }

    private void SyncHelpState()
    {
        if (helpPanel == null)
        {
            return;
        }

        bool isVisible = helpPanel.activeInHierarchy;
        if (isVisible && !helpStateOwned
            && HokkienInteractionState.TryEnter(HokkienInteractionMode.Help, this))
        {
            helpStateOwned = true;
            SetPlayerMovementPaused(true);
        }
        else if (!isVisible && helpStateOwned)
        {
            helpStateOwned = false;
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

    public void OnMenuButtonPressed()
    {
        OpenMenu();
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

    public bool OpenBackpack()
    {
        if (backpackStateOwned && backpackPanel != null && backpackPanel.activeSelf)
        {
            return true;
        }

        if (backpackPanel == null
            || !HokkienInteractionState.TryEnter(HokkienInteractionMode.Backpack, this))
        {
            return false;
        }

        backpackStateOwned = true;
        backpackPanel.SetActive(true);
        SetPlayerMovementPaused(true);

        if (inventoryDisplay != null)
        {
            inventoryDisplay.LoadInventory();
        }

        return true;
    }

    public void OnBackpackButtonPressed()
    {
        OpenBackpack();
    }

    public void CloseBackpack()
    {
        if (backpackPanel != null)
        {
            backpackPanel.SetActive(false);
        }

        if (backpackStateOwned)
        {
            backpackStateOwned = false;
            HokkienInteractionState.Exit(this);
            SetPlayerMovementPaused(false);
        }
    }

    public bool OpenLessonTask()
    {
        if (lessonTaskStateOwned && lessonTaskPanel != null && lessonTaskPanel.activeSelf)
        {
            return true;
        }

        if (lessonTaskPanel == null
            || !HokkienInteractionState.TryEnter(HokkienInteractionMode.LessonTask, this))
        {
            return false;
        }

        lessonTaskStateOwned = true;
        lessonTaskPanel.SetActive(true);
        SetPlayerMovementPaused(true);

        return true;
    }

    public void OnLessonTaskButtonPressed()
    {
        OpenLessonTask();
    }

    public void OnLessonQuizButtonPressed()
    {
        if (quizLauncher == null)
        {
            Debug.LogWarning("[MenuController] Quiz launcher is not assigned.");
            return;
        }

        CloseLessonTask();
        quizLauncher.StartQuiz();
    }

    public void CloseLessonTask()
    {
        if (lessonTaskPanel != null)
        {
            lessonTaskPanel.SetActive(false);
        }

        if (lessonTaskStateOwned)
        {
            lessonTaskStateOwned = false;
            HokkienInteractionState.Exit(this);
            SetPlayerMovementPaused(false);
        }
    }

    public bool OpenHelp()
    {
        if (helpStateOwned && helpPanel != null && helpPanel.activeSelf)
        {
            return true;
        }

        if (helpPanel == null
            || !HokkienInteractionState.TryEnter(HokkienInteractionMode.Help, this))
        {
            return false;
        }

        helpStateOwned = true;
        helpPanel.SetActive(true);
        SetPlayerMovementPaused(true);

        return true;
    }

    public void OnHelpButtonPressed()
    {
        OpenHelp();
    }

    public void CloseHelp()
    {
        if (helpPanel != null)
        {
            helpPanel.SetActive(false);
        }

        if (helpStateOwned)
        {
            helpStateOwned = false;
            HokkienInteractionState.Exit(this);
            SetPlayerMovementPaused(false);
        }
    }

    private void CloseWelcomePanel()
    {
        welcomePanel.SetActive(false);

        if (welcomeStateOwned)
        {
            HandleWelcomePanelClosed();
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
