using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.Diagnostics;

public class WordHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private TMP_Text textMesh;
    private DialogueAudioHandler audioHandler;
    public WordTooltip wordTooltip;
    private int lastHoveredWordIndex = -1;

    // Stores key word data loaded from dialogue response
    // Key = hokkien word, Value = [romanized, context]
    private Dictionary<string, string[]> keyWordData = new Dictionary<string, string[]>();

    void Awake()
    {
      
        textMesh = GetComponent<TMP_Text>();
        audioHandler = FindObjectOfType<DialogueAudioHandler>();
        // Only use FindObjectOfType as fallback
        if (wordTooltip == null)
            wordTooltip = FindObjectOfType<WordTooltip>();
    }
    void Start()
    {
        UnityEngine.Debug.LogWarning("WordHoverHandler initialized. TextMesh: " + textMesh.text + " | Tooltip: " + wordTooltip);
        //UnityEngine.Debug.LogWarning("THIS IS A WARNING!!!!");
    }


    // Called by NPC when a new dialogue node loads
    public void LoadKeyWords(List<KeyWordEntry> keyWords)
    {
        keyWordData.Clear();
        UnityEngine.Debug.Log("[WordHoverHandler] LoadKeyWords called with " + keyWords.Count + " keywords.");
        foreach (var kw in keyWords)
        {
            keyWordData[kw.word] = new string[] { kw.romanized, kw.context };
            UnityEngine.Debug.Log("[WordHoverHandler] Loaded keyword: '" + kw.word + "' | romanized: '" + kw.romanized + "' | context: '" + kw.context + "'");
        }
        UnityEngine.Debug.Log("[WordHoverHandler] keyWordData now has " + keyWordData.Count + " entries.");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Vector2 adjustedPosition = eventData.position;
        float scaleFactor = textMesh.canvas.scaleFactor;
        adjustedPosition.x -= 1 * scaleFactor;

        int wordIndex = TMP_TextUtilities.FindIntersectingWord(textMesh, adjustedPosition, eventData.enterEventCamera);

        if (wordIndex != -1 && wordIndex != lastHoveredWordIndex)
        {
            ResetHighlight();
            lastHoveredWordIndex = wordIndex;

            string hoveredWord = textMesh.textInfo.wordInfo[wordIndex].GetWord();
            UnityEngine.Debug.Log("Hovered word: " + hoveredWord);//debugging
            // Only highlight if it's a key word
            if (keyWordData.ContainsKey(hoveredWord))
            {
                HighlightWord(wordIndex, Color.yellow);
                UnityEngine.Debug.Log("Key word hovered: " + hoveredWord);
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        UnityEngine.Debug.Log("[WordHoverHandler] OnPointerClick FIRED at position: " + eventData.position);

        // Recalculate word index directly from click position
        // instead of relying on lastHoveredWordIndex which may have been reset
        Vector2 adjustedPosition = eventData.position;
        float scaleFactor = textMesh.canvas.scaleFactor;
        adjustedPosition.x -= 1 * scaleFactor;

        int clickedWordIndex = TMP_TextUtilities.FindIntersectingWord(
            textMesh,
            adjustedPosition,
            eventData.pressEventCamera
        );

        UnityEngine.Debug.Log("[WordHoverHandler] Clicked word index: " + clickedWordIndex);

        if (clickedWordIndex == -1)
        {
            UnityEngine.Debug.Log("[WordHoverHandler] No word found at click position.");
            return;
        }

        string clickedWord = textMesh.textInfo.wordInfo[clickedWordIndex].GetWord();
        UnityEngine.Debug.Log("[WordHoverHandler] Clicked word string: " + clickedWord);

        if (!keyWordData.ContainsKey(clickedWord))
        {
            UnityEngine.Debug.Log("[WordHoverHandler] Word is not a keyword, ignoring: " + clickedWord);
            return;
        }

        string[] data = keyWordData[clickedWord];
        string romanized = data[0];
        string context = data[1];
        UnityEngine.Debug.Log("[WordHoverHandler] Keyword found! Romanized: " + romanized + " | Context: " + context);

        // Recalculate word position from the clicked word index
        TMP_WordInfo wInfo = textMesh.textInfo.wordInfo[clickedWordIndex];
        Vector3 wordPosition = textMesh.transform.TransformPoint(
            textMesh.textInfo.characterInfo[wInfo.firstCharacterIndex].bottomLeft
        );
        wordPosition.y -= 30f;
        wordPosition.x += 180f;
        UnityEngine.Debug.Log("[WordHoverHandler] Tooltip position: " + wordPosition);

        // Show tooltip
        if (wordTooltip != null)
        {
            UnityEngine.Debug.Log("[WordHoverHandler] Calling ShowTooltip...");
            wordTooltip.ShowTooltip(clickedWord, romanized, context, wordPosition);
        }
        else
        {
            UnityEngine.Debug.LogError("[WordHoverHandler] wordTooltip reference is NULL! Make sure WordTooltip is in the scene.");
        }

        // Play audio
        if (audioHandler != null)
        {
            UnityEngine.Debug.Log("[WordHoverHandler] Calling PlayWord...");
            audioHandler.PlayWord(clickedWord);
        }
        else
        {
            UnityEngine.Debug.LogWarning("[WordHoverHandler] audioHandler is NULL, skipping audio.");
        }

        UnityEngine.Debug.Log("[WordHoverHandler] OnPointerClick completed successfully for word: " + clickedWord);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ResetHighlight();
        lastHoveredWordIndex = -1;
    }

    private void HighlightWord(int wordIndex, Color color)
    {
        textMesh.ForceMeshUpdate();
        TMP_WordInfo wInfo = textMesh.textInfo.wordInfo[wordIndex];

        for (int i = 0; i < wInfo.characterCount; i++)
        {
            int charIndex = wInfo.firstCharacterIndex + i;
            int meshIndex = textMesh.textInfo.characterInfo[charIndex].materialReferenceIndex;
            int vertexIndex = textMesh.textInfo.characterInfo[charIndex].vertexIndex;
            Color32[] vertexColors = textMesh.textInfo.meshInfo[meshIndex].colors32;
            vertexColors[vertexIndex + 0] = color;
            vertexColors[vertexIndex + 1] = color;
            vertexColors[vertexIndex + 2] = color;
            vertexColors[vertexIndex + 3] = color;
        }

        textMesh.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        textMesh.ForceMeshUpdate();
    }

    private void ResetHighlight()
    {
        if (lastHoveredWordIndex != -1)
            HighlightWord(lastHoveredWordIndex, Color.white);
    }
}

// Simple data class to hold key word info passed in from NPC
[System.Serializable]
public class KeyWordEntry
{
    public string word;
    public string romanized;
    public string context;
}