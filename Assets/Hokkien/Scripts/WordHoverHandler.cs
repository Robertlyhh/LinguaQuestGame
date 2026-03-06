using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WordHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private TMP_Text textMesh;
    private DialogueAudioHandler audioHandler;
    private int lastHoveredWordIndex = -1;
    public Text subtext;

    void Awake()
    {
        textMesh = GetComponent<TMP_Text>();
        audioHandler = FindObjectOfType<DialogueAudioHandler>();
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        //Compensate for the -200 left margin offset
        Vector2 adjustedPosition = eventData.position;

        // Convert the margin offset from local space to screen space
        float scaleFactor = textMesh.canvas.scaleFactor;
        adjustedPosition.x -= 1 * scaleFactor; // fine-tuned offset for DialogueText margin

        int wordIndex = TMP_TextUtilities.FindIntersectingWord(textMesh, adjustedPosition, eventData.enterEventCamera);

        if (wordIndex != -1 && wordIndex != lastHoveredWordIndex)
        {
            ResetHighlight();
            lastHoveredWordIndex = wordIndex;

            HighlightWord(wordIndex, Color.yellow);

            string hoveredWord = textMesh.textInfo.wordInfo[wordIndex].GetWord();
            UnityEngine.Debug.Log("Accurate Hover: " + hoveredWord);
        }
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (lastHoveredWordIndex != -1)
        {
            string clickedWord = textMesh.textInfo.wordInfo[lastHoveredWordIndex].GetWord();
            UnityEngine.Debug.Log("Word clicked: " + clickedWord);

            if (audioHandler != null)
                audioHandler.PlayWord(clickedWord);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ResetHighlight();
        lastHoveredWordIndex = -1;
    }

    private void HighlightWord(int wordIndex, Color color)
    {
        // 1. Force a mesh update to ensure character data is current
        textMesh.ForceMeshUpdate();

        TMP_WordInfo wInfo = textMesh.textInfo.wordInfo[wordIndex];

        // 2. Loop through characters
        for (int i = 0; i < wInfo.characterCount; i++)
        {
            int charIndex = wInfo.firstCharacterIndex + i;
            int meshIndex = textMesh.textInfo.characterInfo[charIndex].materialReferenceIndex;
            int vertexIndex = textMesh.textInfo.characterInfo[charIndex].vertexIndex;

            // Get the color array for this specific mesh (handling sub-meshes)
            Color32[] vertexColors = textMesh.textInfo.meshInfo[meshIndex].colors32;

            // Apply color to all 4 vertices of the character quad
            vertexColors[vertexIndex + 0] = color;
            vertexColors[vertexIndex + 1] = color;
            vertexColors[vertexIndex + 2] = color;
            vertexColors[vertexIndex + 3] = color;
        }

        // 3. Push the new colors back to the actual mesh component
        textMesh.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    private void ResetHighlight()
    {
        if (lastHoveredWordIndex != -1)
        {
            HighlightWord(lastHoveredWordIndex, Color.white); //original is white
        }
    }
}