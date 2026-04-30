using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class StartingPageUI : MonoBehaviour
{
    public GameObject namePanel;
    public TMP_InputField nameInputField;

    public void OnComfirmButtonPressed()
    {
        // Handle confirm button press
        Debug.Log("Confirm button pressed!");
        string playerName = nameInputField.text ?? "";
        Debug.Log("nameInputField: " + nameInputField);
        Debug.Log("GameManager: " + GameManager.Instance);
        if (playerName.Length > 20) playerName = playerName.Substring(0, 20);
        GameManager.Instance.SetPlayerName(playerName);
        namePanel.SetActive(false);
    }
    public void StartGame()
    {
        // Load the quiz scene
        Debug.Log("Starting game...");
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
        // Quit the application
        Debug.Log("Quitting game...");
        Application.Quit();
    }

    public void LoadSettings()
    {
        // Load the settings scene
        Debug.Log("Loading settings...");
        //SceneManager.LoadScene("Settings");
    }

    public void LoadHelp()
    {
        // Load the help scene
        Debug.Log("Loading help...");
        //SceneManager.LoadScene("Help");
    }
}
