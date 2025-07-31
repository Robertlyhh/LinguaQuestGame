using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpiritMessageManager : MonoBehaviour
{
    public GameObject messagePanel;
    public Text messageText;

    [TextArea]
    public List<string> educationalMessages;
    private int currentIndex = 0;

    void Start()
    {
        HideMessage();
    }

    public void ShowNextMessage()
    {
        if (educationalMessages.Count == 0) return;

        messageText.text = educationalMessages[currentIndex];
        messagePanel.SetActive(true);

        currentIndex = (currentIndex + 1) % educationalMessages.Count;
    }

    public void ShowCustomMessage(string message)
    {
        messageText.text = message;
        messagePanel.SetActive(true);
    }

    public void HideMessage()
    {
        messagePanel.SetActive(false);
    }
}
