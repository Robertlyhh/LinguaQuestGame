using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialPageController : MonoBehaviour
{
    public GameObject[] pages;
    public Button leftArrowButton;
    public Button rightArrowButton;
    public TextMeshProUGUI pageIndicatorText;

    private int currentPage;

    private void OnEnable()
    {
        ShowPage(0);
    }

    public void NextPage()
    {
        ShowPage(currentPage + 1);
    }

    public void PreviousPage()
    {
        ShowPage(currentPage - 1);
    }

    private void ShowPage(int index)
    {
        if (pages == null || pages.Length == 0)
        {
            return;
        }

        currentPage = Mathf.Clamp(index, 0, pages.Length - 1);

        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(i == currentPage);
        }

        if (leftArrowButton != null)
        {
            leftArrowButton.gameObject.SetActive(currentPage > 0);
        }

        if (rightArrowButton != null)
        {
            rightArrowButton.gameObject.SetActive(currentPage < pages.Length - 1);
        }

        if (pageIndicatorText != null)
        {
            pageIndicatorText.text = $"{currentPage + 1}/{pages.Length}";
        }
    }
}
