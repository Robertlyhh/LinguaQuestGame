using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared completion feedback for Hokkien lessons and quizzes.
/// The view is created at runtime so every Hokkien scene gets the same UI
/// without duplicating scene-specific references.
/// </summary>
public sealed class HokkienFeedbackPanel : MonoBehaviour
{
    private const float FadeDuration = 0.2f;

    private enum EscapeBehavior
    {
        None,
        Primary,
        Secondary
    }

    private static HokkienFeedbackPanel instance;

    private GameObject feedbackRoot;
    private CanvasGroup feedbackCanvasGroup;
    private RectTransform cardRect;
    private TMP_Text titleText;
    private TMP_Text scoreText;
    private TMP_Text messageText;
    private RectTransform primaryButtonRect;
    private Button primaryButton;
    private TMP_Text primaryButtonText;
    private RectTransform secondaryButtonRect;
    private Button secondaryButton;
    private TMP_Text secondaryButtonText;
    private RectTransform reviewButtonRect;
    private Button reviewButton;
    private AudioSource audioSource;
    private AudioClip completionChime;
    private Sprite panelSprite;
    private Action primaryAction;
    private Action secondaryAction;
    private Action reviewAction;
    private EscapeBehavior escapeBehavior;

    public static HokkienFeedbackPanel GetOrCreate()
    {
        if (instance != null)
        {
            return instance;
        }

        instance = FindFirstObjectByType<HokkienFeedbackPanel>();
        if (instance != null)
        {
            return instance;
        }

        GameObject feedbackObject = new GameObject(
            "HokkienFeedbackPanel",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(HokkienFeedbackPanel));

        return feedbackObject.GetComponent<HokkienFeedbackPanel>();
    }

    public static bool TryHandleEscapeIfVisible()
    {
        // A visible feedback panel always consumes Escape.
        if (instance == null || instance.feedbackRoot == null || !instance.feedbackRoot.activeInHierarchy)
        {
            return false;
        }

        instance.HandleEscape();
        return true;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        ConfigureCanvas();
        ConfigureAudio();
        LoadPanelFrame();
        BuildView();
        feedbackRoot.SetActive(false);
    }

    public void ShowQuizResult(
        bool passed,
        int correctCount,
        int totalCount,
        Action onPrimary,
        Action onExit = null,
        Action onReview = null,
        bool playCompletionAudio = true)
    {
        // Successful results ignore Escape; failed results use Exit Quiz.
        string title = passed ? "Lesson Complete!" : "Try Again";
        string message = passed
            ? "Perfect! You answered every question correctly."
            : "Review your answers or try the quiz again.";
        string buttonLabel = passed ? "Continue" : "Retry";

        Show(
            title,
            $"{correctCount} / {totalCount}",
            message,
            buttonLabel,
            passed ? string.Empty : "Exit Quiz",
            passed,
            onPrimary,
            passed ? null : onExit,
            onReview,
            passed ? EscapeBehavior.None : EscapeBehavior.Secondary,
            playCompletionAudio);
    }

    public void ShowLessonComplete(string npcName, Action onContinue)
    {
        string message = string.IsNullOrWhiteSpace(npcName)
            ? "You completed this conversation."
            : $"You completed your lesson with {npcName}.";

        Show(
            "Lesson Complete!",
            string.Empty,
            message,
            "Continue",
            string.Empty,
            true,
            onContinue,
            null,
            null,
            EscapeBehavior.Primary,
            true);
    }

    public void ShowExitConfirmation(Action onConfirm, Action onCancel)
    {
        Show(
            "Exit Quiz?",
            string.Empty,
            "Your current quiz progress will be lost.",
            "Cancel",
            "Exit Quiz",
            false,
            onCancel,
            onConfirm,
            null,
            EscapeBehavior.Primary,
            false);
    }

    private void Show(
        string title,
        string score,
        string message,
        string buttonLabel,
        string secondaryButtonLabel,
        bool success,
        Action onPrimary,
        Action onSecondary,
        Action onReview,
        EscapeBehavior onEscape,
        bool playCompletionAudio)
    {
        primaryAction = onPrimary;
        secondaryAction = onSecondary;
        reviewAction = onReview;
        escapeBehavior = onEscape;
        titleText.text = title;
        titleText.color = success
            ? new Color32(177, 83, 43, 255)
            : new Color32(164, 62, 62, 255);
        bool hasScore = !string.IsNullOrWhiteSpace(score);
        scoreText.text = score;
        scoreText.gameObject.SetActive(hasScore);
        messageText.text = message;
        SetRect(
            messageText.rectTransform,
            new Vector2(70f, hasScore ? -245f : -165f),
            new Vector2(-70f, hasScore ? -340f : -300f));
        primaryButtonText.text = buttonLabel;
        primaryButton.interactable = true;

        bool showSecondaryButton = onSecondary != null && !string.IsNullOrWhiteSpace(secondaryButtonLabel);
        secondaryButton.gameObject.SetActive(showSecondaryButton);
        secondaryButton.interactable = showSecondaryButton;
        secondaryButtonText.text = secondaryButtonLabel;

        bool showReviewButton = onReview != null;
        reviewButton.gameObject.SetActive(showReviewButton);
        reviewButton.interactable = showReviewButton;
        LayoutButtons(showSecondaryButton, showReviewButton);

        if (success && playCompletionAudio && audioSource != null && completionChime != null)
        {
            audioSource.PlayOneShot(completionChime);
        }

        feedbackRoot.SetActive(true);
        feedbackCanvasGroup.alpha = 0f;
        cardRect.localScale = Vector3.one * 0.88f;

        feedbackCanvasGroup.DOKill();
        cardRect.DOKill();
        feedbackCanvasGroup.DOFade(1f, FadeDuration).SetUpdate(true);
        cardRect.DOScale(1f, 0.28f).SetEase(Ease.OutBack).SetUpdate(true);
    }

    private void HandlePrimaryButton()
    {
        if (!primaryButton.interactable)
        {
            return;
        }

        Action callback = primaryAction;
        HideAndInvoke(callback);
    }

    private void HandleSecondaryButton()
    {
        if (!secondaryButton.interactable)
        {
            return;
        }

        Action callback = secondaryAction;
        HideAndInvoke(callback);
    }

    private void HandleReviewButton()
    {
        if (!reviewButton.interactable)
        {
            return;
        }

        Action callback = reviewAction;
        HideAndInvoke(callback);
    }

    private void LayoutButtons(bool showSecondaryButton, bool showReviewButton)
    {
        if (showReviewButton)
        {
            cardRect.sizeDelta = new Vector2(800f, 540f);
            primaryButtonRect.sizeDelta = new Vector2(210f, 58f);
            secondaryButtonRect.sizeDelta = new Vector2(210f, 58f);
            reviewButtonRect.sizeDelta = new Vector2(240f, 58f);

            if (showSecondaryButton)
            {
                secondaryButtonRect.anchoredPosition = new Vector2(-245f, 82f);
                reviewButtonRect.anchoredPosition = new Vector2(0f, 82f);
                primaryButtonRect.anchoredPosition = new Vector2(245f, 82f);
            }
            else
            {
                reviewButtonRect.anchoredPosition = new Vector2(-135f, 82f);
                primaryButtonRect.anchoredPosition = new Vector2(135f, 82f);
            }

            return;
        }

        cardRect.sizeDelta = new Vector2(800f, 520f);
        primaryButtonRect.sizeDelta = new Vector2(250f, 64f);
        secondaryButtonRect.sizeDelta = new Vector2(250f, 64f);
        primaryButtonRect.anchoredPosition = new Vector2(showSecondaryButton ? 140f : 0f, 72f);
        secondaryButtonRect.anchoredPosition = new Vector2(-140f, 72f);
    }

    private void HandleEscape()
    {
        switch (escapeBehavior)
        {
            case EscapeBehavior.Primary:
                HandlePrimaryButton();
                break;
            case EscapeBehavior.Secondary:
                HandleSecondaryButton();
                break;
        }
    }

    private void HideAndInvoke(Action callback)
    {
        primaryButton.interactable = false;
        secondaryButton.interactable = false;
        reviewButton.interactable = false;
        primaryAction = null;
        secondaryAction = null;
        reviewAction = null;
        escapeBehavior = EscapeBehavior.None;

        feedbackCanvasGroup.DOKill();
        cardRect.DOKill();
        feedbackCanvasGroup.DOFade(0f, FadeDuration)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                feedbackRoot.SetActive(false);
                callback?.Invoke();
            });
        cardRect.DOScale(0.94f, FadeDuration).SetEase(Ease.InQuad).SetUpdate(true);
    }

    private void ConfigureCanvas()
    {
        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    private void ConfigureAudio()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 0.35f;
        completionChime = CreateCompletionChime();
    }

    private static AudioClip CreateCompletionChime()
    {
        const int sampleRate = 44100;
        const float noteDuration = 0.18f;
        const int noteCount = 2;
        int sampleCount = Mathf.CeilToInt(sampleRate * noteDuration * noteCount);
        float[] samples = new float[sampleCount];
        float[] frequencies = { 523.25f, 659.25f };

        for (int i = 0; i < sampleCount; i++)
        {
            float totalTime = i / (float)sampleRate;
            int noteIndex = Mathf.Min(Mathf.FloorToInt(totalTime / noteDuration), noteCount - 1);
            float noteTime = totalTime - noteIndex * noteDuration;
            float noteProgress = noteTime / noteDuration;
            float envelope = Mathf.Sin(Mathf.PI * noteProgress);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequencies[noteIndex] * noteTime) * envelope;
        }

        AudioClip clip = AudioClip.Create("HokkienCompletionChime", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void LoadPanelFrame()
    {
        panelSprite = FindLoadedAsset<Sprite>("DialogueBoxSimple_0");
    }

    private static T FindLoadedAsset<T>(string assetName) where T : UnityEngine.Object
    {
        foreach (T asset in Resources.FindObjectsOfTypeAll<T>())
        {
            if (asset.name == assetName)
            {
                return asset;
            }
        }

        return null;
    }

    private void BuildView()
    {
        feedbackRoot = CreateUiObject("FeedbackRoot", transform);
        RectTransform rootRect = feedbackRoot.GetComponent<RectTransform>();
        StretchToParent(rootRect);
        feedbackCanvasGroup = feedbackRoot.AddComponent<CanvasGroup>();

        Image overlay = feedbackRoot.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.72f);

        GameObject card = CreateUiObject("CompletionCard", feedbackRoot.transform);
        cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(800f, 520f);
        cardRect.anchoredPosition = Vector2.zero;

        Image cardImage = card.AddComponent<Image>();
        if (panelSprite != null)
        {
            cardImage.sprite = panelSprite;
            // Match the menu panel's stretched pixel frame.
            cardImage.type = Image.Type.Simple;
            cardImage.color = Color.white;
        }
        else
        {
            // Keep the fallback light so the old dark-gold panel never returns.
            cardImage.color = new Color32(244, 237, 243, 255);
            Outline cardOutline = card.AddComponent<Outline>();
            cardOutline.effectColor = new Color32(239, 153, 81, 255);
            cardOutline.effectDistance = new Vector2(6f, -6f);
        }

        titleText = CreateText("Title", card.transform, 46f, FontStyles.Bold);
        SetRect(titleText.rectTransform, new Vector2(50f, -52f), new Vector2(-50f, -125f));
        titleText.alignment = TextAlignmentOptions.Center;

        scoreText = CreateText("Score", card.transform, 58f, FontStyles.Bold);
        SetRect(scoreText.rectTransform, new Vector2(70f, -145f), new Vector2(-70f, -230f));
        scoreText.alignment = TextAlignmentOptions.Center;
        scoreText.color = new Color32(73, 53, 61, 255);

        messageText = CreateText("Message", card.transform, 27f, FontStyles.Normal);
        SetRect(messageText.rectTransform, new Vector2(70f, -245f), new Vector2(-70f, -340f));
        messageText.alignment = TextAlignmentOptions.Center;
        messageText.color = new Color32(73, 53, 61, 255);

        primaryButton = CreateButton(
            "PrimaryButton",
            card.transform,
            string.Empty,
            28f,
            new Color32(235, 174, 65, 255),
            new Color32(207, 142, 45, 255),
            new Color32(120, 105, 84, 255),
            HandlePrimaryButton,
            out primaryButtonRect,
            out primaryButtonText);

        reviewButton = CreateButton(
            "ReviewButton",
            card.transform,
            "Review Answers",
            24f,
            new Color32(244, 192, 113, 255),
            new Color32(220, 165, 92, 255),
            new Color32(150, 135, 120, 255),
            HandleReviewButton,
            out reviewButtonRect,
            out _);
        reviewButton.gameObject.SetActive(false);

        secondaryButton = CreateButton(
            "SecondaryButton",
            card.transform,
            string.Empty,
            28f,
            new Color32(235, 174, 65, 255),
            new Color32(207, 142, 45, 255),
            new Color32(120, 105, 84, 255),
            HandleSecondaryButton,
            out secondaryButtonRect,
            out secondaryButtonText);
        secondaryButton.gameObject.SetActive(false);

        LayoutButtons(false, false);
    }

    private static Button CreateButton(
        string objectName,
        Transform parent,
        string label,
        float fontSize,
        Color32 backgroundColor,
        Color32 pressedColor,
        Color32 disabledColor,
        UnityEngine.Events.UnityAction onClick,
        out RectTransform buttonRect,
        out TMP_Text buttonText)
    {
        GameObject buttonObject = CreateUiObject(objectName, parent);
        buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);

        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = backgroundColor;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(255, 236, 178, 255);
        colors.pressedColor = pressedColor;
        colors.disabledColor = disabledColor;
        button.colors = colors;
        button.onClick.AddListener(onClick);

        buttonText = CreateText("Label", buttonObject.transform, fontSize, FontStyles.Bold);
        buttonText.text = label;
        StretchToParent(buttonText.rectTransform);
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = new Color32(73, 53, 61, 255);
        buttonText.raycastTarget = false;
        return button;
    }

    private static GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject result = new GameObject(objectName, typeof(RectTransform));
        result.transform.SetParent(parent, false);
        return result;
    }

    private static TMP_Text CreateText(string objectName, Transform parent, float fontSize, FontStyles style)
    {
        GameObject textObject = CreateUiObject(objectName, parent);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetRect(RectTransform rect, Vector2 topLeft, Vector2 bottomRight)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(topLeft.x, bottomRight.y);
        rect.offsetMax = new Vector2(bottomRight.x, topLeft.y);
    }

    private void OnDestroy()
    {
        feedbackCanvasGroup?.DOKill();
        cardRect?.DOKill();

        if (completionChime != null)
        {
            Destroy(completionChime);
        }

        if (instance == this)
        {
            instance = null;
        }
    }
}
