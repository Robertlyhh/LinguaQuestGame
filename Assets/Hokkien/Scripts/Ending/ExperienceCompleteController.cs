using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class ExperienceCompleteController : MonoBehaviour
{
    private const float FadeDuration = 0.35f;
    private static readonly Color32 ButtonColor = new(248, 220, 177, 255);
    private static readonly Color32 ButtonHoverColor = new(255, 235, 204, 255);
    private static readonly Color32 ButtonPressedColor = new(235, 174, 65, 255);
    private static readonly Color32 ButtonOutlineColor = new(177, 83, 43, 255);
    private static readonly Color32 ButtonTextColor = new(73, 53, 61, 255);

    [SerializeField] private ExperienceEndingConfig config;
    [Header("Hokkien menu visual assets")]
    [SerializeField] private Sprite dialogueBoxSprite;

    private CanvasGroup screenCanvasGroup;
    private RectTransform cardRect;
    private TMP_Text titleText;
    private TMP_Text messageText;
    private Button primaryButton;
    private TMP_Text primaryButtonText;
    private Button restartButton;
    private GameObject confirmationRoot;
    private TMP_Text confirmationTitleText;
    private TMP_Text confirmationMessageText;
    private TMP_Text confirmationCancelText;
    private TMP_Text confirmationConfirmText;
    private TMP_Text confirmationErrorText;
    private bool actionInProgress;

    public ExperienceEndingConfig Config => config;
    public Sprite DialogueBoxSprite => dialogueBoxSprite;

    private void Awake()
    {
        EnsureEventSystem();
        BuildView();
        ApplyConfig();
    }

    private IEnumerator Start()
    {
        screenCanvasGroup.alpha = 0f;
        cardRect.localScale = Vector3.one * 0.92f;

        float elapsed = 0f;
        while (elapsed < FadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / FadeDuration);
            screenCanvasGroup.alpha = progress;
            cardRect.localScale = Vector3.LerpUnclamped(
                Vector3.one * 0.92f,
                Vector3.one,
                EaseOutBack(progress));
            yield return null;
        }

        screenCanvasGroup.alpha = 1f;
        cardRect.localScale = Vector3.one;
        PlayCompletionChime();
    }

    private void Update()
    {
        if (confirmationRoot.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            HideRestartConfirmation();
        }
    }

    public static bool IsValidSurveyUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri parsed))
        {
            return false;
        }

        return parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps;
    }

    private void ApplyConfig()
    {
        if (config == null)
        {
            Debug.LogError("[ExperienceComplete] No ExperienceEndingConfig is assigned.");
            titleText.text = "Thanks for Playing!";
            messageText.text = "You may now close this page.";
            primaryButton.gameObject.SetActive(false);
            restartButton.gameObject.SetActive(false);
            return;
        }

        titleText.text = config.title;
        messageText.text = config.message;
        primaryButtonText.text = config.mode == ExperienceEndingMode.Survey
            ? config.surveyButtonLabel
            : config.finishButtonLabel;
        restartButton.GetComponentInChildren<TMP_Text>().text = config.restartButtonLabel;
        confirmationTitleText.text = config.restartConfirmationTitle;
        confirmationMessageText.text = config.restartConfirmationMessage;
        confirmationCancelText.text = config.restartCancelLabel;
        confirmationConfirmText.text = config.restartConfirmLabel;
    }

    private void HandlePrimaryAction()
    {
        if (actionInProgress || config == null)
        {
            return;
        }

        if (config.mode == ExperienceEndingMode.Survey)
        {
            if (!IsValidSurveyUrl(config.surveyUrl))
            {
                ShowTerminalState(config.finishedTitle, config.invalidSurveyMessage);
                return;
            }

            Application.OpenURL(config.surveyUrl);
            ShowTerminalState(config.surveyOpenedTitle, config.surveyOpenedMessage);
            return;
        }

        ShowTerminalState(config.finishedTitle, config.finishedMessage);
    }

    private void ShowTerminalState(string title, string message)
    {
        actionInProgress = true;
        titleText.text = title;
        messageText.text = message;
        primaryButton.gameObject.SetActive(false);
        restartButton.gameObject.SetActive(false);
    }

    private void ShowRestartConfirmation()
    {
        if (actionInProgress || config == null)
        {
            return;
        }

        confirmationErrorText.gameObject.SetActive(false);
        confirmationRoot.SetActive(true);
    }

    private void HideRestartConfirmation()
    {
        confirmationRoot.SetActive(false);
    }

    private void ConfirmRestart()
    {
        if (actionInProgress || config == null)
        {
            return;
        }

        actionInProgress = true;
        SetConfirmationButtonsInteractable(false);

        if (!Application.CanStreamedLevelBeLoaded(config.restartSceneName))
        {
            ShowRestartError("The restart scene is not available in this build.");
            return;
        }

        if (!HokkienLocalProgress.TryReset(config.saveFileName, out string error))
        {
            ShowRestartError("Progress could not be cleared. Please try again.\n" + error);
            return;
        }

        SceneManager.LoadScene(config.restartSceneName);
    }

    private void ShowRestartError(string message)
    {
        actionInProgress = false;
        confirmationErrorText.text = message;
        confirmationErrorText.gameObject.SetActive(true);
        SetConfirmationButtonsInteractable(true);
    }

    private void SetConfirmationButtonsInteractable(bool interactable)
    {
        foreach (Button button in confirmationRoot.GetComponentsInChildren<Button>(true))
        {
            button.interactable = interactable;
        }
    }

    private void BuildView()
    {
        GameObject canvasObject = new GameObject(
            "ExperienceCompleteCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CanvasGroup));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        screenCanvasGroup = canvasObject.GetComponent<CanvasGroup>();

        GameObject overlay = CreateUiObject("Overlay", canvasObject.transform);
        StretchToParent(overlay.GetComponent<RectTransform>());
        overlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.58f);

        GameObject card = CreateUiObject("EndingCard", overlay.transform);
        cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = cardRect.anchorMax = cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(920f, 560f);

        Image cardImage = card.AddComponent<Image>();
        ApplyDialogueBoxStyle(cardImage);

        titleText = CreateText("Title", card.transform, 52f, FontStyles.Bold);
        SetTopRect(titleText.rectTransform, 55f, 55f, 145f);
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.black;

        messageText = CreateText("Message", card.transform, 29f, FontStyles.Normal);
        SetTopRect(messageText.rectTransform, 90f, 165f, 350f);
        messageText.alignment = TextAlignmentOptions.Center;
        messageText.color = Color.black;

        primaryButton = CreateButton(
            "PrimaryButton", card.transform, string.Empty, new Vector2(205f, 76f),
            new Vector2(360f, 68f), HandlePrimaryAction, out primaryButtonText);

        restartButton = CreateButton(
            "RestartButton", card.transform, "Restart Experience", new Vector2(-205f, 76f),
            new Vector2(360f, 68f), ShowRestartConfirmation, out _);

        BuildConfirmation(overlay.transform);
    }

    private void BuildConfirmation(Transform parent)
    {
        confirmationRoot = CreateUiObject("RestartConfirmation", parent);
        StretchToParent(confirmationRoot.GetComponent<RectTransform>());
        confirmationRoot.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);

        GameObject modal = CreateUiObject("ConfirmationCard", confirmationRoot.transform);
        RectTransform modalRect = modal.GetComponent<RectTransform>();
        modalRect.anchorMin = modalRect.anchorMax = modalRect.pivot = new Vector2(0.5f, 0.5f);
        modalRect.sizeDelta = new Vector2(720f, 420f);
        ApplyDialogueBoxStyle(modal.AddComponent<Image>());

        confirmationTitleText = CreateText("Title", modal.transform, 40f, FontStyles.Bold);
        SetTopRect(confirmationTitleText.rectTransform, 45f, 45f, 105f);
        confirmationTitleText.alignment = TextAlignmentOptions.Center;
        confirmationTitleText.color = Color.black;

        confirmationMessageText = CreateText("Message", modal.transform, 27f, FontStyles.Normal);
        SetTopRect(confirmationMessageText.rectTransform, 65f, 125f, 225f);
        confirmationMessageText.alignment = TextAlignmentOptions.Center;
        confirmationMessageText.color = Color.black;

        confirmationErrorText = CreateText("Error", modal.transform, 19f, FontStyles.Normal);
        SetTopRect(confirmationErrorText.rectTransform, 65f, 225f, 285f);
        confirmationErrorText.alignment = TextAlignmentOptions.Center;
        confirmationErrorText.color = new Color32(164, 62, 62, 255);
        confirmationErrorText.gameObject.SetActive(false);

        CreateButton(
            "CancelButton", modal.transform, "Cancel", new Vector2(-155f, 48f),
            new Vector2(260f, 62f), HideRestartConfirmation, out confirmationCancelText);
        CreateButton(
            "ConfirmButton", modal.transform, "Restart", new Vector2(155f, 48f),
            new Vector2(260f, 62f), ConfirmRestart, out confirmationConfirmText);

        confirmationRoot.SetActive(false);
    }

    private Button CreateButton(
        string name,
        Transform parent,
        string label,
        Vector2 position,
        Vector2 size,
        UnityEngine.Events.UnityAction onClick,
        out TMP_Text labelText)
    {
        GameObject buttonObject = CreateUiObject(name, parent);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = buttonObject.AddComponent<Image>();
        image.color = ButtonColor;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = ButtonColor;
        colors.highlightedColor = ButtonHoverColor;
        colors.pressedColor = ButtonPressedColor;
        colors.selectedColor = ButtonColor;
        colors.disabledColor = new Color32(205, 190, 175, 255);
        button.colors = colors;
        button.onClick.AddListener(onClick);

        Outline outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = ButtonOutlineColor;
        outline.effectDistance = new Vector2(2f, -2f);

        labelText = CreateText("Label", buttonObject.transform, 25f, FontStyles.Bold);
        StretchToParent(labelText.rectTransform);
        labelText.text = label;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = ButtonTextColor;
        return button;
    }

    private TMP_Text CreateText(string name, Transform parent, float size, FontStyles style)
    {
        GameObject textObject = CreateUiObject(name, parent);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.fontStyle = style;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private void ApplyDialogueBoxStyle(Image image)
    {
        image.sprite = dialogueBoxSprite;
        image.type = Image.Type.Simple;
        image.color = dialogueBoxSprite != null ? Color.white : new Color32(244, 237, 243, 255);
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject result = new GameObject(name, typeof(RectTransform));
        result.transform.SetParent(parent, false);
        return result;
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetTopRect(RectTransform rect, float sideMargin, float top, float bottom)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(sideMargin, -bottom);
        rect.offsetMax = new Vector2(-sideMargin, -top);
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }

    private static float EaseOutBack(float value)
    {
        const float overshoot = 1.70158f;
        float shifted = value - 1f;
        return 1f + (overshoot + 1f) * shifted * shifted * shifted
               + overshoot * shifted * shifted;
    }

    private void PlayCompletionChime()
    {
        const int sampleRate = 44100;
        const float noteDuration = 0.18f;
        float[] frequencies = { 523.25f, 659.25f };
        int sampleCount = Mathf.CeilToInt(sampleRate * noteDuration * frequencies.Length);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float totalTime = i / (float)sampleRate;
            int noteIndex = Mathf.Min(Mathf.FloorToInt(totalTime / noteDuration), frequencies.Length - 1);
            float noteTime = totalTime - noteIndex * noteDuration;
            float envelope = Mathf.Sin(Mathf.PI * noteTime / noteDuration);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequencies[noteIndex] * noteTime) * envelope * 0.25f;
        }

        AudioClip clip = AudioClip.Create("ExperienceCompleteChime", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = 0.65f;
        source.clip = clip;
        source.Play();
        Destroy(source, clip.length + 0.1f);
        Destroy(clip, clip.length + 0.1f);
    }
}
