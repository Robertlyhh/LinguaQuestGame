using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using DG.Tweening;

public class NPC : MonoBehaviour, IInteractable
{
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

    [Header("Translation Toggle")]
    public Button translationToggleButton;
    public TMP_Text translationToggleLabel;

    [Header("Item Notification UI")]
    public GameObject itemNotificationPanel;
    public Image notificationItemIcon;
    public TMP_Text notificationItemName;

    private enum TranslationMode { Original, Hokkien, POJ }

    private CanvasGroup panelCanvasGroup;
    private DialogueResponseData currentNode;
    private Coroutine typingRoutine;
    private bool isTyping;

    private TranslationMode translationMode = TranslationMode.Original;
    private AudioClip currentClip;

    private void Awake()
    {
        panelCanvasGroup = dialoguePanel.GetComponent<CanvasGroup>();
        panelCanvasGroup.alpha = 0;
        dialoguePanel.SetActive(false);

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

        if (translationToggleButton != null)
        {
            translationToggleButton.gameObject.SetActive(false);
            translationToggleButton.onClick.AddListener(OnTranslationTogglePressed);
        }
    }

    public bool CanInteract() => !dialoguePanel.activeSelf;

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
        dialoguePanel.SetActive(true);
        portraitImage.sprite = npcPortrait;
        panelCanvasGroup.DOFade(1, 0.3f);

        APIManager.Instance.GetVendorProfile(vendorId, vendor =>
        {
            nameText.SetText(vendor.vendor_name);
            FetchNode(vendor.dialogue_node_id);
        }, err => EndDialogue());
    }

    private void FetchNode(string nodeId)
    {
        dialogueText.SetText("...");
        translationMode = TranslationMode.Original;

        if (audioButton != null) audioButton.gameObject.SetActive(false);
        if (translationToggleButton != null) translationToggleButton.gameObject.SetActive(false);

        StopAudio();
        currentClip = null;

        APIManager.Instance.GetDialogueNode(nodeId, OnNodeReceived, err => EndDialogue());
    }

    private void OnNodeReceived(DialogueResponseData response)
    {
        if (response.dialogue == null) { EndDialogue(); return; }

        currentNode = response;
        ClearOptions();

        // Load audio clip from URL if available
        if (!string.IsNullOrEmpty(response.dialogue.audio))
    StartCoroutine(LoadAudioClip($"{APIManager.Instance.BaseUrl}/{response.dialogue.audio}"));


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
            return;
        }

        typingRoutine = StartCoroutine(TypeLine(currentNode.dialogue.text));
    }

    private IEnumerator LoadAudioClip(string url)
    {
        using var req = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(url, AudioType.UNKNOWN);
        yield return req.SendWebRequest();

        if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            currentClip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(req);
            if (audioButton != null)
                audioButton.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"[NPC] Failed to load audio: {req.error}");
        }
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
            btnObj.GetComponentInChildren<TMP_Text>().SetText(opt.text);
            btnObj.GetComponent<Button>().onClick.AddListener(() => OnOptionPicked(opt));
        }
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
        ClearOptions();
        ProcessEvents(option);
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
            StartCoroutine(ShowLessonCompleteAndEnd());
        else
            AdvanceDialogue(option.next_node);
    }

    private IEnumerator ShowLessonCompleteAndEnd()
    {
        yield return new WaitForSeconds(autoAdvanceDelay);

        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(true);
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(() => EndDialogue());
        }
    }

    private void ShowItemNotification(HokkienItem item)
    {
        if (itemNotificationPanel == null) return;

        if (notificationItemIcon != null) notificationItemIcon.sprite = item.icon;
        if (notificationItemName != null) notificationItemName.SetText(item.displayName);

        itemNotificationPanel.SetActive(true);
        panelCanvasGroup.DOFade(0.5f, 0.2f).SetUpdate(true);

        CancelInvoke(nameof(HideItemNotification));
        Invoke(nameof(HideItemNotification), notificationDuration);
    }

    private void HideItemNotification()
    {
        if (itemNotificationPanel == null) return;

        itemNotificationPanel.SetActive(false);
        panelCanvasGroup.DOFade(1f, 0.2f).SetUpdate(true);
    }

    private void AdvanceDialogue(string nextNode)
    {
        if (string.IsNullOrEmpty(nextNode)) { EndDialogue(); return; }
        FetchNode(nextNode);
    }

    public void EndDialogue()
    {
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        ClearOptions();
        StopAudio();

        if (continueButton != null) continueButton.gameObject.SetActive(false);
        if (audioButton != null) audioButton.gameObject.SetActive(false);
        if (translationToggleButton != null) translationToggleButton.gameObject.SetActive(false);

        panelCanvasGroup.DOFade(0, 0.3f).OnComplete(() => dialoguePanel.SetActive(false));
    }
}