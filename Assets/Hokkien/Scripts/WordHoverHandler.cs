using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Collections;

public class WordHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Highlight Settings")]
    [Tooltip("Base color for keywords when not hovered")]
    public Color keywordBaseColor = new Color(1f, 0.85f, 0.3f, 0.6f);   // soft yellow

    [Tooltip("Color when keyword is hovered")]
    public Color keywordHoverColor = new Color(1f, 0.92f, 0.1f, 1f);    // bright yellow

    [Tooltip("How fast the keyword pulses")]
    public float pulseSpeed = 2f;

    [Tooltip("Min alpha during pulse (0-1)")]
    [Range(0f, 1f)]
    public float pulseMinAlpha = 0.4f;

    [Tooltip("Max alpha during pulse (0-1)")]
    [Range(0f, 1f)]
    public float pulseMaxAlpha = 0.85f;

    private TMP_Text textMesh;
    private DialogueAudioHandler audioHandler;
    public WordTooltip wordTooltip;
    private int lastHoveredWordIndex = -1;
    private bool isPulsing = false;
    private Coroutine pulseCoroutine;

    // Stores key word data loaded from dialogue response
    // Key = hokkien word, Value = [romanized, context]
    private Dictionary<string, string[]> keyWordData = new Dictionary<string, string[]>();
    // Tracks which word indices are keywords for pulse animation
    private HashSet<int> keywordWordIndices = new HashSet<int>();

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
        keywordWordIndices.Clear();
        UnityEngine.Debug.Log("[WordHoverHandler] LoadKeyWords called with " + keyWords.Count + " keywords.");
        foreach (var kw in keyWords)
        {
            keyWordData[kw.word] = new string[] { kw.romanized, kw.context, kw.chinese_word };
            UnityEngine.Debug.Log("[WordHoverHandler] Loaded keyword: '" + kw.word + "' | romanized: '" + kw.romanized + "' | context: '" + kw.context + "'");
        }
        UnityEngine.Debug.Log("[WordHoverHandler] keyWordData now has " + keyWordData.Count + " entries.");

        // Start pulsing after a short delay to let the text render first
        if (pulseCoroutine != null)
            StopCoroutine(pulseCoroutine);

        pulseCoroutine = StartCoroutine(StartPulseAfterDelay(0.1f));
    }
    // this function applies the effect instaintly to the keyword instead of waiting for th dialgue to finnish
    public void RefreshKeywordHighlights()
    {
        if (pulseCoroutine != null)
            StopCoroutine(pulseCoroutine);

        pulseCoroutine = StartCoroutine(StartPulseAfterDelay(0.05f));
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Vector2 adjustedPosition = eventData.position;
        float scaleFactor = textMesh.canvas.scaleFactor;
        adjustedPosition.x -= 1 * scaleFactor;

        int wordIndex = TMP_TextUtilities.FindIntersectingWord(textMesh, adjustedPosition, eventData.enterEventCamera);

        if (wordIndex != -1 && wordIndex != lastHoveredWordIndex)
        {
            // Reset previous hover highlight back to pulse color
            if (lastHoveredWordIndex != -1
                && keywordWordIndices.Contains(lastHoveredWordIndex))
            {
                SetWordColor(lastHoveredWordIndex, GetCurrentPulseColor());
            }

            lastHoveredWordIndex = wordIndex;
            string hoveredWord = textMesh.textInfo.wordInfo[wordIndex].GetWord();
            UnityEngine.Debug.Log("[WordHoverHandler] Hovered word: " + hoveredWord);

            if (keyWordData.ContainsKey(hoveredWord))
            {
                // Brighten to full hover color
                SetWordColor(wordIndex, keywordHoverColor);
                UnityEngine.Debug.Log("[WordHoverHandler] Keyword hovered: " + hoveredWord);
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
        string chineseWord = data[2];
        // Update keyWordData to store 3 values instead of 2:
        // Key = english word, Value = [romanized, context, chinese_word]
        UnityEngine.Debug.Log("[WordHoverHandler] Keyword found! Romanized: " + romanized + " | Context: " + context);

        // Recalculate word position from the clicked word index
        TMP_WordInfo wInfo = textMesh.textInfo.wordInfo[clickedWordIndex];
        Vector3 wordPosition = textMesh.transform.TransformPoint(
            textMesh.textInfo.characterInfo[wInfo.firstCharacterIndex].bottomLeft
        );
        wordPosition.y -= 13f;
        wordPosition.x += 180f;
        UnityEngine.Debug.Log("[WordHoverHandler] Tooltip position: " + wordPosition);

        // Show tooltip
        if (wordTooltip != null)
        {
            UnityEngine.Debug.Log("[WordHoverHandler] Calling ShowTooltip...");
            wordTooltip.ShowTooltip(chineseWord, romanized, context, wordPosition);
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
        // Reset hovered word back to pulse color
        if (lastHoveredWordIndex != -1
            && keywordWordIndices.Contains(lastHoveredWordIndex))
        {
            SetWordColor(lastHoveredWordIndex, GetCurrentPulseColor());
        }
        lastHoveredWordIndex = -1;
    }

    //pusle animation
    private IEnumerator StartPulseAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Find all keyword word indices in the current text
        FindKeywordIndices();

        // Apply initial color to all keywords
        ApplyKeywordBaseColors();

        // Start pulsing
        isPulsing = true;
        yield return StartCoroutine(PulseKeywords());
    }
    private IEnumerator PulseKeywords()
    {
        while (isPulsing && keywordWordIndices.Count > 0)
        {
            // Calculate pulsing alpha using a sine wave
            float alpha = Mathf.Lerp(
                pulseMinAlpha,
                pulseMaxAlpha,
                (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f
            );

            Color pulseColor = new Color(
                keywordBaseColor.r,
                keywordBaseColor.g,
                keywordBaseColor.b,
                alpha
            );

            // Apply pulse color to all keywords except the hovered one
            foreach (int wordIndex in keywordWordIndices)
            {
                if (wordIndex != lastHoveredWordIndex)
                {
                    SetWordColor(wordIndex, pulseColor);
                }
            }

            yield return null; // wait one frame
        }
    }
    private void FindKeywordIndices()
    {
        keywordWordIndices.Clear();

        if (textMesh.textInfo == null) return;

        textMesh.ForceMeshUpdate();

        int wordCount = textMesh.textInfo.wordCount;
        for (int i = 0; i < wordCount; i++)
        {
            string word = textMesh.textInfo.wordInfo[i].GetWord();
            if (keyWordData.ContainsKey(word))
            {
                keywordWordIndices.Add(i);
                UnityEngine.Debug.Log("[WordHoverHandler] Found keyword in text at index "
                    + i + ": " + word);
            }
        }

        UnityEngine.Debug.Log("[WordHoverHandler] Found "
            + keywordWordIndices.Count + " keyword indices in text.");
    }

    private void ApplyKeywordBaseColors()
    {
        foreach (int wordIndex in keywordWordIndices)
        {
            SetWordColor(wordIndex, keywordBaseColor);
        }
    }

    private Color GetCurrentPulseColor()
    {
        float alpha = Mathf.Lerp(
            pulseMinAlpha,
            pulseMaxAlpha,
            (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f
        );
        return new Color(
            keywordBaseColor.r,
            keywordBaseColor.g,
            keywordBaseColor.b,
            alpha
        );
    }

    // Color Helpers 

    private void SetWordColor(int wordIndex, Color color)
    {
        if (textMesh.textInfo == null) return;
        if (wordIndex >= textMesh.textInfo.wordCount) return;

        TMP_WordInfo wInfo = textMesh.textInfo.wordInfo[wordIndex];

        for (int i = 0; i < wInfo.characterCount; i++)
        {
            int charIndex = wInfo.firstCharacterIndex + i;

            if (charIndex >= textMesh.textInfo.characterCount) continue;

            int meshIndex = textMesh.textInfo.characterInfo[charIndex].materialReferenceIndex;
            int vertexIndex = textMesh.textInfo.characterInfo[charIndex].vertexIndex;

            if (meshIndex >= textMesh.textInfo.meshInfo.Length) continue;

            Color32[] vertexColors = textMesh.textInfo.meshInfo[meshIndex].colors32;

            if (vertexIndex + 3 >= vertexColors.Length) continue;

            Color32 c32 = color;
            vertexColors[vertexIndex + 0] = c32;
            vertexColors[vertexIndex + 1] = c32;
            vertexColors[vertexIndex + 2] = c32;
            vertexColors[vertexIndex + 3] = c32;
        }

        textMesh.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    // Cleanup 

    void OnDisable()
    {
        isPulsing = false;
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }

        /*
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
        */
    }

    // Simple data class to hold key word info passed in from NPC
    [System.Serializable]
    public class KeyWordEntry
    {
        public string word;
        public string romanized;
        public string context;
        public string english_word;
        public string chinese_word; //this si what is shown in the wordtooltip
    }
}