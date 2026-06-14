using UnityEngine;

public class MenuController : MonoBehaviour
{
    public GameObject menuCanvas;
    public InventoryDisplay inventoryDisplay;
    public HokkienQuizLauncher quizLauncher;

    private bool quizActive;

    void Start()
    {
        quizActive = false;
        menuCanvas.SetActive(false);
    }

    void Update()
    {
        if (quizActive)
        {
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

    public void OnQuizButtonPressed()
    {
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
