using UnityEngine;
using TMPro;

public class UIConfirmPrompt : MonoBehaviour
{
    public static UIConfirmPrompt Instance;

    public GameObject panel;
    public TMP_Text promptText;

    private System.Action onYesAction;

    void Awake()
    {
        Instance = this;
        panel.SetActive(false);
    }

    public void Show(string message, System.Action yesAction)
    {
        panel.SetActive(true);
        promptText.text = message;

        onYesAction = yesAction;

        Time.timeScale = 0f;
    }

    public void OnYesPressed()
    {
        Debug.Log("YES pressed");

        Time.timeScale = 1f;
        panel.SetActive(false);

        onYesAction?.Invoke();
    }

    public void OnNoPressed()
    {
        Debug.Log("NO pressed");

        Time.timeScale = 1f;
        panel.SetActive(false);
    }
}