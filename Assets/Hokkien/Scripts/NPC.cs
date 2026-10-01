using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;

public class NPC : MonoBehaviour, IInteractable
{
    // Only one vendor conversation can own Escape at a time.
    private static NPC activeConversation;

    [Header("NPC Identity")]
    public string vendorId;
    public Sprite npcPortrait;

    [Header("Settings")]
    public float typingSpeed = 0.05f;
    public float notificationDuration = 2f;
    public float autoAdvanceDelay = 0.5f;

    [Header("UI References")]
    public GameObject dialoguePanel;
    public TMP_Text nameText;
    public Image portraitImage;
    public TMP_Text dialogueText;
    public Transform optionsContainer;
    public GameObject optionButtonPrefab;
    public Button continueButton;
    public WordHoverHandler wordHoverHandler;

    [Header("Audio")]
    public Button audioButton;
    public AudioSource audioSource;

    [Header("Option POJ / Audio Generation")]
    public AdminTranslationImporter translationImporter;

    [Header("Translation Toggle")]
    public Button translationToggleButton;
    public TMP_Text translationToggleLabel;

    [Header("Item Notification UI")]
    public GameObject itemNotificationPanel;
    public Image notificationItemIcon;
    public TMP_Text notificationItemName;

    [Header("Player Control")]
    public frogMove playerMovement;

    private enum TranslationMode { Original, Hokkien, POJ }

    private CanvasGroup panelCanvasGroup;
    private DialogueResponseData currentNode;
    private Coroutine typingRoutine;
    private bool isTyping;
    private bool isCurrentlyPlayerTurn = false;
    private string currentNpcName;
    private int conversationToken;
    private bool conversationActive;

    private TranslationMode translationMode = TranslationMode.Original;
    private AudioClip currentClip;
    private readonly HashSet<string> autoPlayedNodeIds = new HashSet<string>();
    private readonly Dictionary<string, AudioClip> optionAudioClips = new Dictionary<string, AudioClip>();
    private readonly Dictionary<string, TMP_Text> optionTextLabels = new Dictionary<string, TMP_Text>();
    private bool optionSelectionInProgress;

    private void Start()
    {
        StartCoroutine(PrewarmDialogue());
    }

    private void Awake()
    {
        panelCanvasGroup = dialoguePanel.GetComponent<CanvasGroup>();
        panelCanvasGroup.alpha = 0;
        dialoguePanel.SetActive(false);

        if (playerMovement == null)
            playerMovement = FindObjectOfType<frogMove>();

        if (wordHoverHandler == null && dialogueText != null)
            wordHoverHandler = dialogueText.GetComponent<WordHoverHandler>();

        if (continueButton != null)
            continueButton.gameObject.SetActive(false);

        if (itemNotificationPanel != null)
            itemNotificationPanel.SetActive(false);

        if (audioButton != null)
        {
            audioButton.gameObject.SetActive(false);
            audioButton.onClick.AddListener(OnAudioButtonPressed);
        }

        if (translationImporter == null)
        {
            translationImporter = FindObjectOfType<AdminTranslationImporter>();
            if (translationImporter == null)
                translationImporter = gameObject.AddComponent<AdminTranslationImporter>();
        }

        if (translationToggleButton != null)
        {
            translationToggleButton.gameObject.SetActive(false);
            translationToggleButton.onClick.AddListener(OnTranslationTogglePressed);
        }
    }

    public bool CanInteract() => HokkienInteractionState.CurrentMode == HokkienInteractionMode.World
        && !dialoguePanel.activeSelf;

    public static bool TryHandleEscapeForActiveConversation()
    {
        if (activeConversation == null || !activeConversation.conversationActive)
        {
            return false;
        }

        activeConversation.EndDialogue();
        return true;
    }

    private IEnumerator PrewarmDialogue()
    {
        if (string.IsNullOrEmpty(vendorId))
            yield break;

        while (APIManager.Instance == null)
            yield return null;

        APIManager.Instance.GetVendorProfile(vendorId,
            vendor =>
            {
                if (!string.IsNullOrEmpty(vendor.dialogue_node_id))
                    APIManager.Instance.PrefetchDialogueNode(vendor.dialogue_node_id);
            },
            _ => { });
    }

    public void Interact()
    {
        if (isTyping)
        {
            SkipTyping();
            return;
        }

        if (!dialoguePanel.activeSelf)
            BeginConversation();
        else
            EndDialogue();
    }

    private void BeginConversation()
    {
        if (!HokkienInteractionState.TryEnter(HokkienInteractionMode.Dialogue, this))
        {
            return;
        }

        activeConversation = this;
        conversationToken++;
        conversationActive = true;
        isCurrentlyPlayerTurn = false;
        SetPlayerMovementPaused(true);
        dialoguePanel.SetActive(true);
        portraitImage.sprite = npcPortrait;
        dialogueText.SetText("...");
        nameText.SetText("...");
        ClearOptions();

        if (audioButton != null) audioButton.gameObject.SetActive(false);
        if (translationToggleButton != null) translationToggleButton.gameObject.SetActive(false);

        StopAudio();
        currentClip = null;
        panelCanvasGroup.DOFade(1, 0.3f);

        int token = conversationToken;

        Debug.Log($"[NPC] Interact resolved vendorId '{vendorId}' -> {APIManager.Instance.BaseUrl}/api/v1/vendors/{vendorId}");

        APIManager.Instance.GetVendorProfile(vendorId, vendor =>
        {
            if (!IsConversationValid(token))
                return;

            currentNpcName = vendor.vendor_name;
            nameText.SetText(currentNpcName);

            Debug.Log($"[NPC] Vendor '{currentNpcName}' resolved dialogue_node_id '{vendor.dialogue_node_id}'");
            FetchNode(vendor.dialogue_node_id, token);
        }, err =>
        {
            if (IsConversationValid(token))
                EndDialogue();
        });
    }

    private void FetchNode(string nodeId, int token)
    {
        if (!IsConversationValid(token))
            return;

        if (APIManager.Instance == null)
        {
            EndDialogue();
            return;
        }

        if (!APIManager.Instance.HasDialogueNodeCached(nodeId))
            dialogueText.SetText("...");

        translationMode = TranslationMode.Original;

        if (audioButton != null) audioButton.gameObject.SetActive(false);
        if (translationToggleButton != null) translationToggleButton.gameObject.SetActive(false);

        StopAudio();
        currentClip = null;

        APIManager.Instance.GetDialogueNode(nodeId,
            response =>
            {
                if (IsConversationValid(token))
                    OnNodeReceived(response, token, nodeId);
            },
            err =>
            {
                if (IsConversationValid(token))
                    EndDialogue();
            });
    }

    private void OnNodeReceived(DialogueResponseData response, int token, string nodeId)
    {
        if (!IsConversationValid(token))
            return;

        if (response.dialogue == null) { EndDialogue(); return; }

        currentNode = response;
        optionSelectionInProgress = false;
        optionAudioClips.Clear();
        optionTextLabels.Clear();
        ClearOptions();

        bool forceVendorSpeaker = response.dialogue.text == "Welcome! Our shaved ice is very refreshing! What toppings would you like?"
            || response.dialogue.text == "Here you go! Enjoy!";

        if (!forceVendorSpeaker && isCurrentlyPlayerTurn && PlayerIdentity.Instance != null)
        {
            nameText.SetText(PlayerIdentity.Instance.playerName);
            portraitImage.sprite = PlayerIdentity.Instance.playerPortrait;
        }
        else
        {
            nameText.SetText(currentNpcName);
            portraitImage.sprite = npcPortrait;
        }

        // Option/branch prompts should always be presented by the vendor.
        if (response.options != null && response.options.Length > 0)
        {
            nameText.SetText(currentNpcName);
            portraitImage.sprite = npcPortrait;
        }

        // Load audio clip from URL if available
        if (!string.IsNullOrEmpty(response.dialogue.audio))
            StartCoroutine(LoadAudioClip($"{APIManager.Instance.BaseUrl}/{response.dialogue.audio}", token, nodeId));

        if (response.options != null)
        {
            foreach (DialogueOption option in response.options)
            {
                if (option != null && !string.IsNullOrWhiteSpace(option.option_id)
                    && !string.IsNullOrWhiteSpace(option.audio))
                {
                    StartCoroutine(LoadOptionAudioClip(option, token));
                }
            }
        }


        // Show translation toggle when any translation exists
        bool hasTranslation = !string.IsNullOrEmpty(response.dialogue.translation_HAN)
                   || !string.IsNullOrEmpty(response.dialogue.translation_POJ);
        if (translationToggleButton != null)
        {
            translationToggleButton.gameObject.SetActive(hasTranslation);
            UpdateTranslationToggleLabel();
        }

        WordHoverHandler handler = wordHoverHandler;
        if (handler == null && dialogueText != null)
        {
            handler = dialogueText.GetComponent<WordHoverHandler>();
            wordHoverHandler = handler;
        }

        if (handler != null)
        {
            handler.SetupDialogue(currentNode.dialogue);
            isTyping = false;
            ProcessAfterTyping();
            PrefetchLikelyNextNodes(currentNode);
            return;
        }

        typingRoutine = StartCoroutine(TypeLine(currentNode.dialogue.text));
        PrefetchLikelyNextNodes(currentNode);
    }

    private void PrefetchLikelyNextNodes(DialogueResponseData node)
    {
        if (node == null || APIManager.Instance == null)
            return;

        var nextIds = new HashSet<string>();

        if (node.next_nodes != null)
        {
            foreach (var nextId in node.next_nodes)
            {
                if (!string.IsNullOrEmpty(nextId))
                    nextIds.Add(nextId);
            }
        }

        if (node.options != null)
        {
            foreach (var option in node.options)
            {
                if (!string.IsNullOrEmpty(option.next_node))
                    nextIds.Add(option.next_node);
            }
        }

        foreach (var nextId in nextIds)
            APIManager.Instance.PrefetchDialogueNode(nextId);
    }

    private IEnumerator LoadAudioClip(string url, int token, string nodeId)
    {
        using var req = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(url, AudioType.UNKNOWN);
        yield return req.SendWebRequest();

        if (!IsConversationValid(token))
            yield break;

        if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            currentClip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(req);
            if (audioButton != null)
                audioButton.gameObject.SetActive(true);

            if (!string.IsNullOrEmpty(nodeId) && autoPlayedNodeIds.Add(nodeId) && audioSource != null)
            {
                audioSource.clip = currentClip;
                audioSource.Play();
            }
        }
        else
        {
            Debug.LogWarning($"[NPC] Failed to load audio: {req.error}");
        }
    }

    private IEnumerator LoadOptionAudioClip(DialogueOption option, int token)
    {
        string url = BuildAudioUrl(option.audio);
        using var req = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(url, AudioType.UNKNOWN);
        yield return req.SendWebRequest();

        if (!IsConversationValid(token) || currentNode?.options == null)
            yield break;

        if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            optionAudioClips[option.option_id] = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(req);
        }
        else
        {
            Debug.LogWarning($"[NPC] Failed to load option audio '{option.option_id}': {req.error}");
        }
    }

    private string BuildAudioUrl(string audioPath)
    {
        if (audioPath.StartsWith("http://", System.StringComparison.OrdinalIgnoreCase)
            || audioPath.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase))
        {
            return audioPath;
        }

        return APIManager.Instance.BaseUrl.TrimEnd('/') + "/" + audioPath.TrimStart('/');
    }

    private void OnAudioButtonPressed()
    {
        if (audioSource == null || currentClip == null) return;

        if (audioSource.isPlaying)
        {
            audioSource.Stop();
            return;
        }

        audioSource.clip = currentClip;
        audioSource.Play();
    }

    private void StopAudio()
    {
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
    }

    private void OnTranslationTogglePressed()
    {
        if (currentNode?.dialogue == null) return;

        // Cycle: Original → Hokkien → POJ → Original
        translationMode = translationMode switch
        {
            TranslationMode.Original => TranslationMode.Hokkien,
            TranslationMode.Hokkien => TranslationMode.POJ,
            TranslationMode.POJ => TranslationMode.Original,
            _ => TranslationMode.Original
        };

        UpdateTranslationToggleLabel();
        ApplyTranslationMode();
    }

    private void UpdateTranslationToggleLabel()
    {
        if (translationToggleLabel == null) return;

        translationToggleLabel.SetText(translationMode switch
        {
            TranslationMode.Original => "EN",
            TranslationMode.Hokkien => "漢",
            TranslationMode.POJ => "POJ",
            _ => "EN"
        });
    }

    private void ApplyTranslationMode()
    {
        if (currentNode?.dialogue == null) return;

        string displayText = translationMode switch
        {
            TranslationMode.Hokkien => !string.IsNullOrEmpty(currentNode.dialogue.translation_HAN)
                ? currentNode.dialogue.translation_HAN
                : currentNode.dialogue.text,
            TranslationMode.POJ => !string.IsNullOrEmpty(currentNode.dialogue.translation_POJ)
                ? currentNode.dialogue.translation_POJ
                : currentNode.dialogue.text,
            _ => currentNode.dialogue.text
        };

        if (translationMode == TranslationMode.Original && wordHoverHandler != null)
            wordHoverHandler.SetupDialogue(currentNode.dialogue);
        else
            dialogueText.SetText(displayText);
    }

    private IEnumerator TypeLine(string line)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        ProcessAfterTyping();
    }

    private void ProcessAfterTyping()
    {
        if (continueButton != null)
            continueButton.gameObject.SetActive(false);

        if (currentNode.options != null && currentNode.options.Length > 0)
        {
            ShowOptions();
        }
        else if (currentNode.next_nodes == null || currentNode.next_nodes.Length == 0)
        {
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(true);
                continueButton.onClick.RemoveAllListeners();
                continueButton.onClick.AddListener(EndDialogue);
            }
        }
        else if (currentNode.next_nodes != null && currentNode.next_nodes.Length == 1)
        {
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(true);
                continueButton.onClick.RemoveAllListeners();
                continueButton.onClick.AddListener(() => AdvanceDialogue(currentNode.next_nodes[0]));
            }
        }
        else
        {
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(true);
                continueButton.onClick.RemoveAllListeners();
                continueButton.onClick.AddListener(() => EndDialogue());
            }
        }
    }

    private void SkipTyping()
    {
        if (typingRoutine != null) StopCoroutine(typingRoutine);

        WordHoverHandler handler = wordHoverHandler;
        if (handler == null && dialogueText != null)
        {
            handler = dialogueText.GetComponent<WordHoverHandler>();
            wordHoverHandler = handler;
        }

        if (handler != null)
            handler.SetupDialogue(currentNode.dialogue);
        else
            dialogueText.SetText(currentNode.dialogue.text);

        isTyping = false;
        ProcessAfterTyping();
    }

    private void ShowOptions()
    {
        foreach (var opt in currentNode.options)
        {
            var btnObj = Instantiate(optionButtonPrefab, optionsContainer);
            TMP_Text label = btnObj.GetComponentInChildren<TMP_Text>();
            label.SetText(BuildOptionText(opt));
            if (!string.IsNullOrWhiteSpace(opt.option_id))
                optionTextLabels[opt.option_id] = label;
            btnObj.GetComponent<Button>().onClick.AddListener(() => OnOptionPicked(opt));
        }

        if (translationImporter != null)
            StartCoroutine(GenerateMissingOptionContent(currentNode.options, conversationToken));
    }

    private IEnumerator GenerateMissingOptionContent(DialogueOption[] options, int token)
    {
        foreach (DialogueOption option in options)
        {
            if (!IsConversationValid(token))
                yield break;
            if (option == null || string.IsNullOrWhiteSpace(option.option_id)
                || (!string.IsNullOrWhiteSpace(option.translation_POJ) && !string.IsNullOrWhiteSpace(option.audio)))
                continue;

            AdminTranslationImporter.TranslationResult generated = null;
            yield return StartCoroutine(translationImporter.TranslateOptionThenTtsThenFetch(
                option.option_id,
                option.text,
                result => generated = result,
                error => Debug.LogWarning($"[NPC Option Generation] {option.option_id}: {error}")));

            if (!IsConversationValid(token) || generated == null)
                continue;

            option.translation_POJ = generated.pojTranslation;
            option.audio = generated.audioUrl;
            if (optionTextLabels.TryGetValue(option.option_id, out TMP_Text label) && label != null)
                label.SetText(BuildOptionText(option));

            yield return StartCoroutine(translationImporter.DownloadAudioClip(
                generated.audioUrl,
                clip => optionAudioClips[option.option_id] = clip,
                error => Debug.LogWarning($"[NPC Option Audio] {option.option_id}: {error}")));
        }
    }

    private static string BuildOptionText(DialogueOption option)
    {
        if (option == null)
            return string.Empty;

        if (string.IsNullOrWhiteSpace(option.translation_POJ))
            return option.text;

        string audioIndicator = string.IsNullOrWhiteSpace(option.audio) ? string.Empty : "  <size=75%>🔊</size>";
        return $"{option.text}\n<size=80%><color=#FFD966>{option.translation_POJ}</color>{audioIndicator}</size>";
    }

    private void ClearOptions()
    {
        foreach (Transform child in optionsContainer)
            Destroy(child.gameObject);

        if (continueButton != null)
            continueButton.gameObject.SetActive(false);
    }

    private void OnOptionPicked(DialogueOption option)
    {
        if (optionSelectionInProgress)
            return;

        optionSelectionInProgress = true;
        SetOptionButtonsInteractable(false);

        if (option != null && !string.IsNullOrWhiteSpace(option.option_id)
            && optionAudioClips.TryGetValue(option.option_id, out AudioClip optionClip)
            && optionClip != null && audioSource != null)
        {
            StartCoroutine(PlayOptionAudioThenProcess(option, optionClip));
            return;
        }

        ClearOptions();
        isCurrentlyPlayerTurn = false;
        ProcessEvents(option);
    }

    private IEnumerator PlayOptionAudioThenProcess(DialogueOption option, AudioClip clip)
    {
        StopAudio();
        audioSource.clip = clip;
        audioSource.Play();
        yield return new WaitForSeconds(clip.length);

        if (!conversationActive)
            yield break;

        ClearOptions();
        isCurrentlyPlayerTurn = false;
        ProcessEvents(option);
    }

    private void SetOptionButtonsInteractable(bool interactable)
    {
        foreach (Transform child in optionsContainer)
        {
            Button button = child.GetComponent<Button>();
            if (button != null)
                button.interactable = interactable;
        }
    }

    private void ProcessEvents(DialogueOption option)
    {
        if (option.events == null || option.events.Length == 0)
        {
            AdvanceDialogue(option.next_node);
            return;
        }

        bool hasLessonComplete = false;
        foreach (var evt in option.events)
        {
            if (evt.event_type == "ADD_TO_INVENTORY")
            {
                var metadata = JsonUtility.FromJson<PurchaseEventMetadata>(evt.metadata);
                var userId = SessionManager.Instance?.GameData?.playerId ?? GameData.DefaultPlayerId();
                var item = HokkienItemRegistry.GetItem(metadata.item_id);

                if (item != null)
                    ShowItemNotification(item);

                APIManager.Instance.AddToInventory(userId, metadata.item_id, metadata.challenge_id,
                    resp => Debug.Log($"[NPC] Added {metadata.item_id} to inventory"),
                    err => Debug.LogWarning($"[NPC] Inventory error: {err}"));
            }
            else if (evt.event_type == "LESSON_COMPLETE")
            {
                hasLessonComplete = true;
            }
        }

        if (hasLessonComplete)
            StartCoroutine(FinishLessonDialogue());
        else
            AdvanceDialogue(option.next_node);
    }

    private IEnumerator FinishLessonDialogue()
    {
        yield return new WaitForSeconds(autoAdvanceDelay);

        if (!conversationActive)
            yield break;

        ClearOptions();
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(true);
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(EndDialogue);
        }
    }

    private void ShowItemNotification(HokkienItem item)
    {
        if (itemNotificationPanel == null) return;

        if (notificationItemIcon != null) notificationItemIcon.sprite = item.icon;
        if (notificationItemName != null) notificationItemName.SetText(item.displayName);

        itemNotificationPanel.SetActive(true);
        if (panelCanvasGroup != null)
            panelCanvasGroup.alpha = 1f;

        CancelInvoke(nameof(HideItemNotification));
        Invoke(nameof(HideItemNotification), notificationDuration);
    }

    private void HideItemNotification()
    {
        if (itemNotificationPanel == null) return;

        itemNotificationPanel.SetActive(false);
        if (panelCanvasGroup != null)
            panelCanvasGroup.alpha = 1f;
    }

    private void AdvanceDialogue(string nextNode)
    {
        if (string.IsNullOrEmpty(nextNode)) { EndDialogue(); return; }
        isCurrentlyPlayerTurn = !isCurrentlyPlayerTurn;
        FetchNode(nextNode, conversationToken);
    }

    public void EndDialogue()
    {
        conversationActive = false;
        conversationToken++;

        if (activeConversation == this)
        {
            activeConversation = null;
        }

        if (typingRoutine != null) StopCoroutine(typingRoutine);
        ClearOptions();
        StopAudio();
        SetPlayerMovementPaused(false);

        if (continueButton != null) continueButton.gameObject.SetActive(false);
        if (audioButton != null) audioButton.gameObject.SetActive(false);
        if (translationToggleButton != null) translationToggleButton.gameObject.SetActive(false);

        // Input should unlock even if the fade is interrupted.
        HokkienInteractionState.Exit(this);

        panelCanvasGroup.DOKill();
        panelCanvasGroup.DOFade(0, 0.3f)
            .SetUpdate(true)
            .OnComplete(FinishClosingDialogue);
    }

    private void FinishClosingDialogue()
    {
        dialoguePanel.SetActive(false);
        HokkienInteractionState.RefreshWorldInteractions();
    }

    private void OnDestroy()
    {
        if (activeConversation == this)
        {
            activeConversation = null;
        }

        HokkienInteractionState.Exit(this);
    }

    private bool IsConversationValid(int token)
    {
        return conversationActive && token == conversationToken;
    }

    private void SetPlayerMovementPaused(bool paused)
    {
        if (playerMovement != null)
            playerMovement.SetMovementPaused(paused);
    }
}
