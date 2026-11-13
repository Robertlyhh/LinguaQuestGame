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
    [SerializeField] private bool verboseWordLogs = false;

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
        if (dialogueData == null)
        {
            textMesh.text = string.Empty;
            return;
        }

        string formattedText = dialogueData.text ?? string.Empty;

        if (dialogueData.key_words == null || dialogueData.key_words.Length == 0)
        {
            textMesh.text = formattedText;
            return;
        }

        if (verboseWordLogs)
            Debug.Log($"[WordHoverHandler] Processing text: {formattedText}");

        // English phrase -> key word, e.g. "Minced Pork Rice" from "Minced Pork Rice (ló-bah-pn̄g)"
        var targets = new Dictionary<string, KeyWordData>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var kw in dialogueData.key_words)
        {
            if (kw == null || string.IsNullOrEmpty(kw.word) || string.IsNullOrEmpty(kw.translation))
                continue;

            keywordDatabase[kw.word] = kw;

            // "Excuse me / May I ask" lists alternatives; whichever the sentence uses gets the link.
            foreach (string alternative in kw.translation.Split('(')[0].Split('/'))
            {
                string englishTarget = alternative.Trim();
                if (!string.IsNullOrEmpty(englishTarget) && !targets.ContainsKey(englishTarget))
                    targets[englishTarget] = kw;
            }
        }

        if (targets.Count > 0)
        {
            // One pass, longest phrase first: "I am allergic to beef" and "beef" may overlap,
            // and TMP cannot click a link nested inside another link.
            var alternatives = new List<string>(targets.Keys);
            alternatives.Sort((a, b) => b.Length.CompareTo(a.Length));
            string pattern = string.Join("|", alternatives.ConvertAll(Regex.Escape));

            formattedText = Regex.Replace(formattedText, pattern, match =>
            {
                KeyWordData kw = targets[match.Value];
                if (verboseWordLogs)
                    Debug.Log($"[WordHoverHandler] Injected link for: {match.Value}");
                // Keep the sentence's own casing; underline + yellow marks it clickable.
                return $"<link=\"{kw.word}\"><u><color=#FFD700>{match.Value}</color></u></link>";
            }, RegexOptions.IgnoreCase);
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
                int lastCharIndex = firstCharIndex + linkInfo.linkTextLength - 1;
                Vector3 bottomLeft = textMesh.textInfo.characterInfo[firstCharIndex].bottomLeft;
                Vector3 bottomRight = textMesh.textInfo.characterInfo[lastCharIndex].bottomRight;
                Vector3 wordCenter = (bottomLeft + bottomRight) * 0.5f;
                Vector3 wordPosition = textMesh.transform.TransformPoint(wordCenter);

                // Place the tooltip roughly above the underlined word.
                wordPosition.y += 20f;

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