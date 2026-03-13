using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;

public class NPC : MonoBehaviour, IInteractable
{
    [Header("NPC Identity")]
    public string vendorId;
    public Sprite npcPortrait;

    [Header("Settings")]
    public float typingSpeed = 0.03f;
    public float fadeDuration = 0.5f;

    [Header("UI References")]
    public GameObject dialoguePanel;
    public TMP_Text nameText;
    public Image portraitImage;
    public TMP_Text dialogueText;
    public Transform optionsContainer;
    public GameObject optionButtonPrefab;
    public WordHoverHandler wordHoverHandler;

    private CanvasGroup panelCanvasGroup;
    private VendorProfile vendorProfile;
    private DialogueResponseData currentDialogueNode;
    private Coroutine typingRoutine;

    private bool isTyping;
    private bool isDialogueActive;
    private bool isTransitioning;
    private bool isWaitingForOption;
    private bool isLoading;
    private DialogueAudioHandler audioHandler;

    private void Awake()
    {
        GameObject uiRoot = GameObject.Find("UI");

        if (uiRoot != null)
        {
            if (dialoguePanel == null)
            {
                Transform panelTransform = uiRoot.transform.Find("DialoguePanel");
                if (panelTransform != null) dialoguePanel = panelTransform.gameObject;
            }
        }
        if (dialoguePanel != null)
        {
            Transform infoPanel = dialoguePanel.transform.Find("InfoPanel");

            if (infoPanel != null)
            {
                if (nameText == null) 
                    nameText = infoPanel.Find("NPCNameText").GetComponent<TMP_Text>();
                
                if (portraitImage == null) 
                    portraitImage = infoPanel.Find("DialoguePortrait").GetComponent<Image>();
                
                if (dialogueText == null) 
                    dialogueText = infoPanel.Find("DialogueText").GetComponent<TMP_Text>();
                if (wordHoverHandler == null)
                {
                    Transform dialogueTextTransform = infoPanel.Find("DialogueText");
                    if (dialogueTextTransform != null)
                        wordHoverHandler = dialogueTextTransform.GetComponent<WordHoverHandler>();
                }
            }

            if (optionsContainer == null) 
                optionsContainer = dialoguePanel.transform.Find("OptionsPanel");

            panelCanvasGroup = dialoguePanel.GetComponent<CanvasGroup>();
            panelCanvasGroup.alpha = 0;
            dialoguePanel.SetActive(false);
        }
        else
        {
            Debug.LogError($"[NPC] {gameObject.name} could not find 'DialoguePanel' in the scene!");
        }

        audioHandler = FindObjectOfType<DialogueAudioHandler>();
    }
 

    public bool CanInteract() => !isTransitioning && !isLoading;

    public void Interact()
    {
        if (isTransitioning || isLoading) return;

        if (!isDialogueActive)
        {
            BeginConversation();
        }
        else if (isTyping)
        {
            SkipTyping();
        }
        else if (!isWaitingForOption)
        {
            EndDialogue();
        }
    }

    private void BeginConversation()
    {
        isDialogueActive = true;
        isTransitioning  = true;
        isLoading        = true;

        portraitImage.sprite = npcPortrait;
        dialogueText.SetText("");

        // Fade in panel dialogue panel
        dialoguePanel.SetActive(true);
        panelCanvasGroup.DOKill();
        panelCanvasGroup.DOFade(1, fadeDuration).OnComplete(() =>
        {
            isTransitioning = false;
        });

        // Fetch vendor profile
        APIManager.Instance.GetVendorProfile(vendorId,
            onSuccess: vendor =>
            {
                vendorProfile = vendor;
                nameText.SetText(vendor.vendor_name);
                FetchDialogueNode(vendor.dialogue_node_id);
            },
            onError: err =>
            {
                Debug.LogWarning($"[NPC] Failed to fetch vendor info: {err}");
                nameText.SetText("Failed to fetch NPC Name");
            }
        );
    }

    private void FetchDialogueNode(string nodeId)
    {
        isLoading = true;
        dialogueText.SetText("...");
        APIManager.Instance.GetDialogueNode(nodeId, OnNodeReceived, OnAPIError);
    }

    private void OnNodeReceived(DialogueResponse response)
    {
        isLoading = false;

        if (response.status != "success" || response.data?.dialogue == null)
        {
            OnAPIError("");
            EndDialogue();
            return;
        }

        currentDialogueNode = response.data;
        ClearOptions();

        UnityEngine.Debug.Log("[NPC] wordHoverHandler is: " + wordHoverHandler);
        UnityEngine.Debug.Log("[NPC] key_words is: " + currentDialogueNode.dialogue.key_words);


        if (wordHoverHandler != null && currentDialogueNode.dialogue.key_words != null)
        {
            UnityEngine.Debug.Log("[NPC] Loading " + currentDialogueNode.dialogue.key_words.Length + " keywords into WordHoverHandler");
            List<KeyWordEntry> entries = new List<KeyWordEntry>();
            foreach (var kw in currentDialogueNode.dialogue.key_words)
            {
                UnityEngine.Debug.Log("[NPC] Processing keyword: " + kw.word);
                entries.Add(new KeyWordEntry
                {
                    word = kw.word,
                    romanized = kw.translation,// mapped romanized to translaation because backend doesn;t return romanzier for now
                    context = kw.context
                });
            }
            wordHoverHandler.LoadKeyWords(entries);
        }
        else
        {
            UnityEngine.Debug.LogError("[NPC] FAILED to load keywords! wordHoverHandler: " + wordHoverHandler + " | key_words: " + currentDialogueNode.dialogue.key_words);
        }

        typingRoutine = StartCoroutine(TypeLine(currentDialogueNode.dialogue.text));
        audioHandler?.ClearCache();
    }

    private void OnAPIError(string error)
    {
        isLoading = false;
        Debug.LogError($"[NPC] API error: {error}");
        EndDialogue();
    }

    private IEnumerator TypeLine(string line)
    {
        isTyping = true;
        dialogueText.SetText("");

        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        OnLineFinished();
    }

    private void SkipTyping()
    {
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        dialogueText.SetText(currentDialogueNode.dialogue.text);
        isTyping = false;
        OnLineFinished();
    }

    private void OnLineFinished()
    {
        if (currentDialogueNode.options is { Length: > 0 }) {
            ShowOptions();
        }
    }

    private void ShowOptions()
    {
        isWaitingForOption = true;

        foreach (var opt in currentDialogueNode.options)
        {
            GameObject OptionButton = Instantiate(optionButtonPrefab, optionsContainer);
            OptionButton.GetComponentInChildren<TMP_Text>().SetText(opt.text);

            string nextNode = opt.next_node;
            OptionButton.GetComponent<Button>().onClick.AddListener(() =>
                OnOptionPicked(nextNode));
        }
    }

    private void ClearOptions()
    {
        for (int i = optionsContainer.childCount - 1; i >= 0; i--)
            Destroy(optionsContainer.GetChild(i).gameObject);
    }

    private void OnOptionPicked(string nextNode)
    {
        isWaitingForOption = false;
        ClearOptions();

        if (string.IsNullOrEmpty(nextNode))
        {
            EndDialogue();
            return;
        }

        FetchDialogueNode(nextNode);
    }

    public void EndDialogue()
    {
        if (typingRoutine != null) StopCoroutine(typingRoutine);

        isTyping           = false;
        isWaitingForOption = false;
        isLoading          = false;
        isTransitioning    = true;
        ClearOptions();

        panelCanvasGroup.DOKill();
        panelCanvasGroup.DOFade(0, fadeDuration).OnComplete(() =>
        {
            dialoguePanel.SetActive(false);
            dialogueText.SetText("");
            isDialogueActive = false;
            isTransitioning  = false;
        });
    }
}
