using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Text.RegularExpressions; // Required for the search-and-replace logic

[RequireComponent(typeof(TMP_Text))]
public class WordHoverHandler : MonoBehaviour, IPointerClickHandler
{
    private TMP_Text textMesh;
    private DialogueAudioHandler audioHandler;
    public WordTooltip wordTooltip;

    // Stores keyword data using the Hokkien word as the dictionary key
    private Dictionary<string, KeyWordData> keywordDatabase = new Dictionary<string, KeyWordData>();

    void Awake()
    {
        textMesh = GetComponent<TMP_Text>();
        audioHandler = FindObjectOfType<DialogueAudioHandler>();
        if (wordTooltip == null)
            wordTooltip = FindObjectOfType<WordTooltip>();
    }

    public void SetupDialogue(DialogueContent dialogueData)
    {
        keywordDatabase.Clear();
        string formattedText = dialogueData.text;

        Debug.Log($"[WordHoverHandler] Processing text: {formattedText}");

        foreach (var kw in dialogueData.key_words)
        {
            keywordDatabase[kw.word] = kw;

            // Extract "Minced Pork Rice" from "Minced Pork Rice (ló-bah-pn̄g)"
            string englishTarget = kw.translation.Split('(')[0].Trim();
            
            // This creates the link, underline, and yellow color
            string replacement = $"<link=\"{kw.word}\"><u><color=#FFD700>{englishTarget}</color></u></link>";

            // USE Regex.Escape() HERE:
            formattedText = Regex.Replace(formattedText, Regex.Escape(englishTarget), replacement, RegexOptions.IgnoreCase);
            
            Debug.Log($"[WordHoverHandler] Injected link for: {englishTarget}");
        }

        textMesh.text = formattedText;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Detects which <link> tag was clicked
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(textMesh, eventData.position, eventData.pressEventCamera);

        if (linkIndex != -1)
        {
            TMP_LinkInfo linkInfo = textMesh.textInfo.linkInfo[linkIndex];
            string clickedWordId = linkInfo.GetLinkID();

            if (keywordDatabase.TryGetValue(clickedWordId, out KeyWordData kwData))
            {
                // Extract romanized text from parenthesis for the tooltip
                string romanized = "";
                int parenStart = kwData.translation.IndexOf('(');
                int parenEnd = kwData.translation.LastIndexOf(')');
                if (parenStart != -1 && parenEnd != -1)
                {
                    romanized = kwData.translation.Substring(parenStart + 1, parenEnd - parenStart - 1);
                }

                // Calculate where to put the tooltip
                int firstCharIndex = linkInfo.linkTextfirstCharacterIndex;
                Vector3 bottomLeft = textMesh.textInfo.characterInfo[firstCharIndex].bottomLeft;
                Vector3 wordPosition = textMesh.transform.TransformPoint(bottomLeft);
                
                wordPosition.y -= 20f; 

                if (wordTooltip != null)
                    wordTooltip.ShowTooltip(kwData.word, romanized, kwData.context, wordPosition);

                /* if (audioHandler != null)
                {
                    UnityEngine.Debug.Log("[WordHoverHandler] Skipping audio for now...");
                    // audioHandler.PlayWord(kwData.word); // COMMENT THIS OUT
                }
                */
            }
        }
        else
        {
            if (wordTooltip != null) wordTooltip.HideTooltip();
        }
    }
}