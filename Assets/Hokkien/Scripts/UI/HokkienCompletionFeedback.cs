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
public sealed class HokkienCompletionFeedback : MonoBehaviour
{
    private const float FadeDuration = 0.2f;

    private static HokkienCompletionFeedback instance;

    private GameObject feedbackRoot;
    private CanvasGroup feedbackCanvasGroup;
    private RectTransform cardRect;
    private TMP_Text titleText;
    private TMP_Text scoreText;
    private TMP_Text messageText;
    private Button primaryButton;
    private TMP_Text primaryButtonText;
    private AudioSource audioSource;
    private AudioClip completionChime;
    private Action primaryAction;

    public static HokkienCompletionFeedback GetOrCreate()
    {
        if (instance != null)
        {
            return instance;
        }

        instance = FindFirstObjectByType<HokkienCompletionFeedback>();
        if (instance != null)
        {
            return instance;
        }

        GameObject feedbackObject = new GameObject(
            "HokkienCompletionFeedback",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(HokkienCompletionFeedback));

        return feedbackObject.GetComponent<HokkienCompletionFeedback>();
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
        BuildView();
        feedbackRoot.SetActive(false);
    }

    public void ShowQuizResult(bool passed, int correctCount, int totalCount, Action onPrimary)
    {
        string title = passed ? "Lesson Complete!" : "Try Again";
        string message = passed
            ? "Perfect! You answered every question correctly."
            : "Review the explanations, then try the quiz again.";
        string buttonLabel = passed ? "Continue" : "Retry";

        Show(
            title,
            $"{correctCount} / {totalCount}",
            message,
            buttonLabel,
            passed,
            onPrimary);
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
            true,
            onContinue);
    }

    private void Show(
        string title,
        string score,
        string message,
        string buttonLabel,
        bool success,
        Action onPrimary)
    {
        primaryAction = onPrimary;
        titleText.text = title;
        titleText.color = success
            ? new Color32(255, 214, 102, 255)
            : new Color32(255, 183, 77, 255);
        scoreText.text = score;
        scoreText.gameObject.SetActive(!string.IsNullOrWhiteSpace(score));
        messageText.text = message;
        primaryButtonText.text = buttonLabel;
        primaryButton.interactable = true;

        if (success && audioSource != null && completionChime != null)
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

        primaryButton.interactable = false;
        Action callback = primaryAction;
        primaryAction = null;

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
        cardRect.sizeDelta = new Vector2(720f, 470f);
        cardRect.anchoredPosition = Vector2.zero;

        Image cardImage = card.AddComponent<Image>();
        cardImage.color = new Color32(49, 37, 31, 255);
        Outline cardOutline = card.AddComponent<Outline>();
        cardOutline.effectColor = new Color32(235, 174, 65, 255);
        cardOutline.effectDistance = new Vector2(4f, -4f);

        GameObject accent = CreateUiObject("TopAccent", card.transform);
        RectTransform accentRect = accent.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.sizeDelta = new Vector2(0f, 12f);
        accentRect.anchoredPosition = Vector2.zero;
        accent.AddComponent<Image>().color = new Color32(235, 174, 65, 255);

        titleText = CreateText("Title", card.transform, 46f, FontStyles.Bold);
        SetRect(titleText.rectTransform, new Vector2(50f, -52f), new Vector2(-50f, -125f));
        titleText.alignment = TextAlignmentOptions.Center;

        scoreText = CreateText("Score", card.transform, 58f, FontStyles.Bold);
        SetRect(scoreText.rectTransform, new Vector2(70f, -145f), new Vector2(-70f, -230f));
        scoreText.alignment = TextAlignmentOptions.Center;
        scoreText.color = Color.white;

        messageText = CreateText("Message", card.transform, 27f, FontStyles.Normal);
        SetRect(messageText.rectTransform, new Vector2(70f, -245f), new Vector2(-70f, -340f));
        messageText.alignment = TextAlignmentOptions.Center;
        messageText.color = new Color32(245, 238, 225, 255);

        GameObject buttonObject = CreateUiObject("PrimaryButton", card.transform);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.sizeDelta = new Vector2(280f, 72f);
        buttonRect.anchoredPosition = new Vector2(0f, 42f);

        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color32(235, 174, 65, 255);
        primaryButton = buttonObject.AddComponent<Button>();
        primaryButton.targetGraphic = buttonImage;
        ColorBlock colors = primaryButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(255, 236, 178, 255);
        colors.pressedColor = new Color32(207, 142, 45, 255);
        colors.disabledColor = new Color32(120, 105, 84, 255);
        primaryButton.colors = colors;
        primaryButton.onClick.AddListener(HandlePrimaryButton);

        primaryButtonText = CreateText("Label", buttonObject.transform, 28f, FontStyles.Bold);
        StretchToParent(primaryButtonText.rectTransform);
        primaryButtonText.alignment = TextAlignmentOptions.Center;
        primaryButtonText.color = new Color32(45, 32, 25, 255);
        primaryButtonText.raycastTarget = false;
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
