using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ProvinceButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string provinceName;
    public Color normalColor = new Color(1, 1, 1, 0); // transparent normally
    public Color hoverColor = new Color(1, 1, 0, 0.4f); // yellow highlight on hover
    public Color correctColor = new Color(0, 1, 0, 0.6f); // green when correct
    public Color wrongColor = new Color(1, 0, 0, 0.6f); // red when wrong

    private Image image;
    private CanadaGeoGame gameManager;

    void Start()
    {
        image = GetComponent<Image>();
        gameManager = FindFirstObjectByType<CanadaGeoGame>();

        // This makes clicks only register on visible pixels, not the whole rect
        image.alphaHitTestMinimumThreshold = 0.1f;

        GetComponent<Button>().onClick.AddListener(() =>
        {
            Debug.Log("Clicked: " + provinceName);
            gameManager.OnProvinceClicked(provinceName);
        });
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Lighten the province color on hover
        Color current = image.color;
        image.color = new Color(
            Mathf.Min(current.r + 0.2f, 1f),
            Mathf.Min(current.g + 0.2f, 1f),
            Mathf.Min(current.b + 0.2f, 1f),
            current.a
        );
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Reset to original color — let GameManager handle this
        gameManager.ResetAllHighlights();
    }

    public void ShowCorrect()
    {
        image.color = correctColor;
    }

    public void ShowWrong()
    {
        image.color = wrongColor;
    }
}