using UnityEngine;

public class BannerController : MonoBehaviour
{
    [Tooltip("Drag the Welcome Panel here in the Inspector")]
    public GameObject welcomePanel;

    // This is the method we will call from the Button
    public void CloseWelcomeBanner()
    {
        if (welcomePanel != null)
        {
            welcomePanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Welcome Panel is not assigned in the UIController script!");
        }
    }
}