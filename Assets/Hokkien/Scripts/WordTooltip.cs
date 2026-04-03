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
        // 1. Set the text content
        hokkienWordText.text = hokkienWord;
        romanizedText.text = romanized;
        contextText.text = context;

        // 2. Set initial position and enable the panel
        tooltipPanel.transform.position = worldPosition;
        tooltipPanel.SetActive(true);
        isVisible = true;

        // 3. FORCE Unity to calculate the new size from Content Size Fitter/Layout Groups
        Canvas.ForceUpdateCanvases();

        // 4. Get the RectTransform and its dimensions
        RectTransform rect = tooltipPanel.GetComponent<RectTransform>();
        float width = rect.rect.width;
        float height = rect.rect.height;
        
        // We assume your Pivot is X:0.5 (Center) and Y:1 (Top)
        Vector3 pos = rect.position;

        // --- LEFT OVERFLOW ---
        // If the left edge (center - half width) is less than 0, shift right
        float leftEdge = pos.x - (width * 0.5f);
        if (leftEdge < 20f) // 20px padding from edge
        {
            pos.x += Mathf.Abs(leftEdge) + 20f;
        }

        // --- RIGHT OVERFLOW ---
        // If the right edge (center + half width) is past screen width, shift left
        float rightEdge = pos.x + (width * 0.5f);
        if (rightEdge > Screen.width - 20f)
        {
            pos.x -= (rightEdge - Screen.width) + 20f;
        }

        // --- BOTTOM OVERFLOW ---
        // Since pivot Y is 1 (top), the box grows down. 
        // If (Position Y - Height) is less than 0, shift the box UP.
        float bottomEdge = pos.y - height;
        if (bottomEdge < 20f)
        {
            // Shifting it up so the bottom edge is at 20px
            pos.y += Mathf.Abs(bottomEdge) + 20f;
        }

        // 5. Apply the corrected position
        rect.position = pos;
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