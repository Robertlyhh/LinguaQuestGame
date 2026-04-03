using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class WordTooltip : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    public GameObject tooltipPanel;
    public TMP_Text hokkienWordText;
    public TMP_Text romanizedText;
    public TMP_Text contextText;

    private bool isVisible = false;

    void Awake()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    public void ShowTooltip(string hokkienWord, string romanized, string context, Vector3 worldPosition)
    {
        if (hokkienWordText != null) hokkienWordText.text = hokkienWord;
        if (romanizedText != null) romanizedText.text = romanized;
        if (contextText != null) contextText.text = context;

        // ← Replace the position line with a fixed screen position
        // Position it on the right side of the screen
        RectTransform rt = tooltipPanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 0.5f);
        rt.anchorMax = new Vector2(1, 0.5f);
        rt.pivot = new Vector2(1, 0.5f);
        rt.anchoredPosition = new Vector2(-10f, 0f); // 10px from right edge, vertically centered

        tooltipPanel.SetActive(true);
        isVisible = true;
    }

    public void HideTooltip()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);

        isVisible = false;
    }

    // Detect clicks outside the tooltip to close it
    void Update()
    {
        if (!isVisible) return;

        if (Input.GetMouseButtonDown(0))
        {
            // Check if the click was outside the tooltip panel
            if (!RectTransformUtility.RectangleContainsScreenPoint(
                tooltipPanel.GetComponent<RectTransform>(),
                Input.mousePosition,
                Camera.main))
            {
                HideTooltip();
            }
        }
    }

    // Required by IPointerClickHandler to prevent clicks inside tooltip from closing it
    public void OnPointerClick(PointerEventData eventData) { }
}