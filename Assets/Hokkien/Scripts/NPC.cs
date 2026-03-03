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
    public float typingSpeed = 0.03f;
    public float fadeDuration = 0.5f;

    [Header("UI References")]
    public GameObject dialoguePanel;
    public TMP_Text nameText;
    public Image portraitImage;
    public TMP_Text dialogueText;
    public Transform optionsContainer;
    public GameObject optionButtonPrefab;

    private CanvasGroup panelCanvasGroup;
    private VendorProfile vendorProfile;
    private DialogueResponseData currentDialogueNode;
    private Coroutine typingRoutine;

    private bool isTyping;
    private bool isDialogueActive;
    private bool isTransitioning;
    private bool isWaitingForOption;
    private bool isLoading;
 
    private void Awake()
    {
        panelCanvasGroup = dialoguePanel.GetComponent<CanvasGroup>();
        panelCanvasGroup.alpha = 0;
        dialoguePanel.SetActive(false);
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
                nameText.SetText(vendor.name);
                FetchDialogueNode(vendor.starting_node_id);
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

        typingRoutine = StartCoroutine(TypeLine(currentDialogueNode.dialogue.text));
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
