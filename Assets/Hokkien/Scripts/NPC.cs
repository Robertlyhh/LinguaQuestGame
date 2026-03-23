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

    [Header("UI References")]
    public GameObject dialoguePanel;
    public TMP_Text nameText;
    public Image portraitImage;
    public TMP_Text dialogueText;
    public Transform optionsContainer;
    public GameObject optionButtonPrefab;
    public WordHoverHandler wordHoverHandler;

    [Header("Item Notification UI")]
    public GameObject itemNotificationPanel;
    public Image notificationItemIcon;
    public TMP_Text notificationItemName;

    private CanvasGroup panelCanvasGroup;
    private DialogueResponseData currentNode;
    private Coroutine typingRoutine;
    private bool isTyping;

    private void Awake()
    {
        panelCanvasGroup = dialoguePanel.GetComponent<CanvasGroup>();
        panelCanvasGroup.alpha = 0;
        dialoguePanel.SetActive(false);
        
        if (itemNotificationPanel != null)
            itemNotificationPanel.SetActive(false);
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
        APIManager.Instance.GetDialogueNode(nodeId, OnNodeReceived, err => EndDialogue());
    }

    private void OnNodeReceived(DialogueResponseData response)
    {
        if (response.dialogue == null) { EndDialogue(); return; }

        currentNode = response;
        ClearOptions();

        if (wordHoverHandler != null && currentNode.dialogue.key_words != null)
        {
            var entries = new System.Collections.Generic.List<KeyWordEntry>();
            foreach (var kw in currentNode.dialogue.key_words)
            {
                entries.Add(new KeyWordEntry { word = kw.word, romanized = kw.translation, context = kw.context });
            }
            wordHoverHandler.LoadKeyWords(entries);
        }

        typingRoutine = StartCoroutine(TypeLine(currentNode.dialogue.text));
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
        if (currentNode.options?.Length > 0) ShowOptions();
    }

    private void SkipTyping()
    {
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        dialogueText.SetText(currentNode.dialogue.text);
        isTyping = false;
        if (currentNode.options?.Length > 0) ShowOptions();
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
        }

        AdvanceDialogue(option.next_node);
    }

    private void ShowItemNotification(HokkienItem item)
    {
        if (itemNotificationPanel == null) return;

        if (notificationItemIcon != null)
            notificationItemIcon.sprite = item.icon;
        if (notificationItemName != null)
            notificationItemName.SetText(item.displayName);

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
        panelCanvasGroup.DOFade(0, 0.3f).OnComplete(() => dialoguePanel.SetActive(false));
    }
}
