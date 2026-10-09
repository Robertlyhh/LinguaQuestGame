using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HokkienQuizManager : MonoBehaviour
{
    [Header("Main Quiz UI")]
    public GameObject quizPanel;
    public TextMeshProUGUI questionText;
    public Button[] answerButtons;
    public TextMeshProUGUI[] answerTexts;

    [Header("Choice Audio Buttons")]
    [Tooltip("Created automatically beside each answer button when left empty.")]
    public Button[] choiceAudioButtons;

    [Header("Explanation Card")]
    public GameObject explanationCardPanel;
    public TextMeshProUGUI explanationTitleText;
    public TextMeshProUGUI explanationBodyText;
    public Button explanationCloseButton;

    [Header("Recap Card")]
    public GameObject recapPanel;
    public TextMeshProUGUI recapText;
    public Button recapCloseButton;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip correctSound;
    public AudioClip wrongSound;
    public AdminTranslationImporter translationImporter;
    [Tooltip("Ask the admin backend to translate and TTS choices that have no audio. Leave off in gameplay: choice audio is pre-generated, and choices without audio (e.g. English) should stay silent.")]
    public bool generateMissingChoiceAudio;

    [Header("Question")]
    public List<MultipleChoiceQuestion> questionsSet;

    private Action<bool> onQuizComplete;
    private Action onRecapClosed;
    private readonly List<QuizAnswerResult> results = new List<QuizAnswerResult>();
    private int currentQuestionIndex;
    private bool answeredIncorrect;
    private bool waitingForCardDismiss;
    private int questionDisplayToken;
    private Button exitQuizButton;
    private Action onExitRequested;

    public int CorrectAnswerCount
    {
        get
        {
            int correctCount = 0;
            foreach (QuizAnswerResult result in results)
            {
                if (result.wasCorrect)
                {
                    correctCount++;
                }
            }

            return correctCount;
        }
    }

    public int AnsweredQuestionCount => results.Count;

    [Serializable]
    private class QuizAnswerResult
    {
        public string question;
        public string selectedAnswer;
        public string correctAnswer;
        public bool wasCorrect;
        public string explanation;
    }

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = 1f;
        }

        EnsureChoiceAudioButtons();
        FindExitQuizButton();

        if (translationImporter == null)
        {
            translationImporter = FindObjectOfType<AdminTranslationImporter>();
            if (translationImporter == null)
            {
                translationImporter = gameObject.AddComponent<AdminTranslationImporter>();
            }
        }

        if (explanationCloseButton != null)
        {
            explanationCloseButton.onClick.AddListener(OnExplanationCardClosed);
        }

        if (recapCloseButton != null)
        {
            recapCloseButton.onClick.AddListener(OnRecapClosed);
        }
    }

    private void Start()
    {
        HideAllOverlays();
        if (quizPanel != null)
        {
            quizPanel.SetActive(false);
        }
    }

    public void LaunchQuestion(List<MultipleChoiceQuestion> questions, Action<bool> onComplete)
    {
        questionsSet = questions;
        currentQuestionIndex = 0;
        answeredIncorrect = false;
        waitingForCardDismiss = false;
        results.Clear();
        onQuizComplete = onComplete;
        onRecapClosed = null;

        HideAllOverlays();
        if (quizPanel != null)
        {
            quizPanel.SetActive(true);
        }

        ShowQuestion(currentQuestionIndex);
    }

    public void SetExitHandler(Action exitHandler)
    {
        onExitRequested = exitHandler;
        if (exitQuizButton != null)
        {
            exitQuizButton.interactable = exitHandler != null;
        }
    }

    public bool TryHandleEscape()
    {
        // Escape follows the close action of the visible quiz card.
        if (explanationCardPanel != null && explanationCardPanel.activeInHierarchy)
        {
            OnExplanationCardClosed();
            return true;
        }

        if (recapPanel != null && recapPanel.activeInHierarchy)
        {
            OnRecapClosed();
            return true;
        }

        return false;
    }

    public void CancelQuiz()
    {
        questionDisplayToken++;
        StopAllCoroutines();

        if (audioSource != null)
        {
            audioSource.Stop();
        }

        onQuizComplete = null;
        onRecapClosed = null;
        onExitRequested = null;
        results.Clear();
        answeredIncorrect = false;
        currentQuestionIndex = 0;
        HideAllOverlays();
        SetAnswerButtonsVisible(false);

        if (exitQuizButton != null)
        {
            exitQuizButton.interactable = false;
        }

        if (quizPanel != null)
        {
            quizPanel.SetActive(false);
        }
    }

    private void ShowQuestion(int index)
    {
        if (questionsSet == null || index < 0 || index >= questionsSet.Count)
        {
            CompleteQuiz();
            return;
        }

        HideAllOverlays();
        SetAnswerButtonsVisible(true);

        MultipleChoiceQuestion currentQuestion = questionsSet[index];
        int displayToken = ++questionDisplayToken;
        questionText.text = BuildQuestionText(currentQuestion);
        StartCoroutine(EnsureQuestionContent(currentQuestion, displayToken));

        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (i < currentQuestion.choices.Length)
            {
                answerButtons[i].gameObject.SetActive(true);
                answerTexts[i].text = BuildChoiceText(currentQuestion, i);

                int choiceIndex = i;
                answerButtons[i].onClick.RemoveAllListeners();
                answerButtons[i].onClick.AddListener(() => SelectAnswer(choiceIndex));
                UpdateChoiceAudioButton(choiceIndex, currentQuestion);
            }
            else
            {
                answerButtons[i].gameObject.SetActive(false);
                if (choiceAudioButtons != null && i < choiceAudioButtons.Length && choiceAudioButtons[i] != null)
                    choiceAudioButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private IEnumerator EnsureQuestionContent(MultipleChoiceQuestion currentQuestion, int displayToken)
    {
        if (currentQuestion == null || translationImporter == null)
        {
            yield break;
        }

        bool needsText = string.IsNullOrWhiteSpace(currentQuestion.hokkienRomanizedText);
        bool needsAudio = currentQuestion.hokkienAudioClip == null && string.IsNullOrWhiteSpace(currentQuestion.hokkienAudioUrl);
        if (currentQuestion.hokkienAudioClip == null && !string.IsNullOrWhiteSpace(currentQuestion.hokkienAudioUrl))
        {
            StartCoroutine(translationImporter.DownloadAudioClip(currentQuestion.hokkienAudioUrl, clip =>
            {
                currentQuestion.hokkienAudioClip = clip;
            }, error => Debug.LogWarning($"[QuizAudio] Failed to load question audio: {error}")));
        }
        if (!string.IsNullOrWhiteSpace(currentQuestion.dialogueNodeId) && (needsText || needsAudio))
        {
            yield return StartCoroutine(translationImporter.TranslateThenTtsThenFetch(
                currentQuestion.dialogueNodeId,
                currentQuestion.question,
                result =>
                {
                    currentQuestion.hokkienRomanizedText = result.pojTranslation;
                    currentQuestion.hokkienAudioUrl = result.audioUrl;
                    if (displayToken == questionDisplayToken)
                    {
                        questionText.text = BuildQuestionText(currentQuestion);
                    }

                    if (!string.IsNullOrEmpty(result.audioUrl))
                    {
                        StartCoroutine(translationImporter.DownloadAudioClip(result.audioUrl, clip =>
                        {
                            currentQuestion.hokkienAudioClip = clip;
                            if (displayToken == questionDisplayToken && clip != null && audioSource != null)
                            {
                                audioSource.PlayOneShot(clip);
                            }
                        }, error => Debug.LogWarning($"[QuizAudio] Failed to load question audio: {error}")));
                    }
                },
                error => Debug.LogWarning($"[QuizTranslation] Question: {error}")));
        }

        EnsureChoiceArrays(currentQuestion);
        if (currentQuestion.choices == null || currentQuestion.choiceDialogueNodeIds == null)
        {
            yield break;
        }

        int count = Mathf.Min(currentQuestion.choices.Length, currentQuestion.choiceDialogueNodeIds.Length);
        for (int i = 0; i < count; i++)
        {
            int choiceIndex = i;
            string nodeId = currentQuestion.choiceDialogueNodeIds[choiceIndex];
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                nodeId = BuildStableQuizOptionId(currentQuestion.question, currentQuestion.choices[choiceIndex]);
                currentQuestion.choiceDialogueNodeIds[choiceIndex] = nodeId;
            }
            bool choiceNeedsText = string.IsNullOrWhiteSpace(currentQuestion.choiceHokkienRomanizedTexts[choiceIndex]);
            bool choiceNeedsAudio = currentQuestion.choiceHokkienAudioClips[choiceIndex] == null
                && string.IsNullOrWhiteSpace(currentQuestion.choiceHokkienAudioUrls[choiceIndex]);

            if (currentQuestion.choiceHokkienAudioClips[choiceIndex] == null
                && !string.IsNullOrWhiteSpace(currentQuestion.choiceHokkienAudioUrls[choiceIndex]))
            {
                string audioUrl = currentQuestion.choiceHokkienAudioUrls[choiceIndex];
                StartCoroutine(translationImporter.DownloadAudioClip(audioUrl, clip =>
                {
                    currentQuestion.choiceHokkienAudioClips[choiceIndex] = clip;
                    if (displayToken == questionDisplayToken && choiceIndex < answerTexts.Length)
                    {
                        answerTexts[choiceIndex].text = BuildChoiceText(currentQuestion, choiceIndex);
                        UpdateChoiceAudioButton(choiceIndex, currentQuestion);
                    }
                }, error => Debug.LogWarning($"[QuizAudio] Choice {choiceIndex + 1}: {error}")));
                continue;
            }

            if (!generateMissingChoiceAudio || (!choiceNeedsText && !choiceNeedsAudio))
            {
                continue;
            }

            yield return StartCoroutine(translationImporter.TranslateOptionThenTtsThenFetch(
                nodeId,
                currentQuestion.choices[choiceIndex],
                result =>
                {
                    currentQuestion.choiceHokkienRomanizedTexts[choiceIndex] = result.pojTranslation;
                    currentQuestion.choiceHokkienAudioUrls[choiceIndex] = result.audioUrl;
                    if (displayToken == questionDisplayToken && choiceIndex < answerTexts.Length)
                    {
                        answerTexts[choiceIndex].text = BuildChoiceText(currentQuestion, choiceIndex);
                    }

                    if (!string.IsNullOrEmpty(result.audioUrl))
                    {
                        StartCoroutine(translationImporter.DownloadAudioClip(result.audioUrl, clip =>
                        {
                            currentQuestion.choiceHokkienAudioClips[choiceIndex] = clip;
                            if (displayToken == questionDisplayToken)
                                UpdateChoiceAudioButton(choiceIndex, currentQuestion);
                        }, error => Debug.LogWarning($"[QuizAudio] Choice {choiceIndex + 1}: {error}")));
                    }
                },
                error => Debug.LogWarning($"[QuizTranslation] Choice {choiceIndex + 1}: {error}")));
        }
    }

    private static string BuildStableQuizOptionId(string question, string choice)
    {
        unchecked
        {
            uint hash = 2166136261;
            string value = (question ?? string.Empty) + "\n" + (choice ?? string.Empty);
            foreach (char character in value)
            {
                hash ^= character;
                hash *= 16777619;
            }
            return $"quiz_opt_{hash:x8}";
        }
    }

    private static void EnsureChoiceArrays(MultipleChoiceQuestion question)
    {
        int count = question.choices != null ? question.choices.Length : 0;
        if (question.choiceDialogueNodeIds == null || question.choiceDialogueNodeIds.Length != count)
            Array.Resize(ref question.choiceDialogueNodeIds, count);
        if (question.choiceHokkienRomanizedTexts == null || question.choiceHokkienRomanizedTexts.Length != count)
            Array.Resize(ref question.choiceHokkienRomanizedTexts, count);
        if (question.choiceHokkienAudioUrls == null || question.choiceHokkienAudioUrls.Length != count)
            Array.Resize(ref question.choiceHokkienAudioUrls, count);
        if (question.choiceHokkienAudioClips == null || question.choiceHokkienAudioClips.Length != count)
            Array.Resize(ref question.choiceHokkienAudioClips, count);
    }

    private static string BuildChoiceText(MultipleChoiceQuestion question, int choiceIndex)
    {
        string original = question.choices[choiceIndex];
        bool hasSeparateRomanization = question.choiceHokkienRomanizedTexts != null
            && choiceIndex < question.choiceHokkienRomanizedTexts.Length
            && !string.IsNullOrWhiteSpace(question.choiceHokkienRomanizedTexts[choiceIndex])
            && !string.Equals(original, question.choiceHokkienRomanizedTexts[choiceIndex], StringComparison.Ordinal);

        if (!hasSeparateRomanization)
            return original;

        return $"{original}\n<color=#FFD966>{question.choiceHokkienRomanizedTexts[choiceIndex]}</color>";
    }

    private void EnsureChoiceAudioButtons()
    {
        int count = answerButtons != null ? answerButtons.Length : 0;
        if (count == 0)
            return;

        if (choiceAudioButtons == null || choiceAudioButtons.Length != count)
            choiceAudioButtons = new Button[count];

        // Choice audio buttons copy the quiz dialog's speaker button so both look the same.
        Button speakerTemplate = FindQuizSpeakerButton();

        for (int i = 0; i < count; i++)
        {
            if (choiceAudioButtons[i] != null || answerButtons[i] == null)
                continue;

            GameObject audioObject = new GameObject(
                $"ChoiceAudioButton_{i + 1}",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            audioObject.transform.SetParent(answerButtons[i].transform, false);

            RectTransform rect = audioObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(8f, 0f);

            Image image = audioObject.GetComponent<Image>();
            Button button = audioObject.GetComponent<Button>();
            button.targetGraphic = image;
            int choiceIndex = i;
            button.onClick.AddListener(() => PlayChoiceAudioOnly(choiceIndex));
            choiceAudioButtons[i] = button;

            if (speakerTemplate != null)
                CopySpeakerButtonStyle(speakerTemplate, button, image, rect);
            else
                ApplyTextAudioButtonStyle(image, rect);
        }
    }

    private Button FindQuizSpeakerButton()
    {
        if (quizPanel == null)
            return null;

        foreach (Button candidate in quizPanel.GetComponentsInChildren<Button>(true))
        {
            if (candidate.name == "SpeakerButton")
                return candidate;
        }

        Debug.LogWarning("[HokkienQuizManager] Could not find the quiz dialog SpeakerButton; using text audio buttons.");
        return null;
    }

    private static void CopySpeakerButtonStyle(Button template, Button button, Image image, RectTransform rect)
    {
        rect.sizeDelta = ((RectTransform)template.transform).rect.size;
        button.transition = template.transition;
        button.colors = template.colors;

        Image templateImage = template.GetComponent<Image>();
        if (templateImage != null)
        {
            image.sprite = templateImage.sprite;
            image.type = templateImage.type;
            image.color = templateImage.color;
            image.pixelsPerUnitMultiplier = templateImage.pixelsPerUnitMultiplier;
        }

        foreach (Image templateIcon in template.GetComponentsInChildren<Image>(true))
        {
            if (templateIcon == templateImage)
                continue;

            Image icon = Instantiate(templateIcon, rect, false);
            icon.name = "Icon";
            icon.raycastTarget = false;
            break;
        }
    }

    private static void ApplyTextAudioButtonStyle(Image image, RectTransform rect)
    {
        rect.sizeDelta = new Vector2(82f, 34f);
        image.color = new Color(1f, 0.84f, 0.15f, 1f);

        GameObject labelObject = new GameObject(
            "Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(rect, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = "AUDIO";
        label.fontSize = 16f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.black;
        label.raycastTarget = false;
    }

    private void FindExitQuizButton()
    {
        if (quizPanel == null || exitQuizButton != null)
        {
            return;
        }

        Button[] closeButtons = quizPanel.GetComponentsInChildren<Button>(true);
        foreach (Button candidate in closeButtons)
        {
            if (candidate.name != "CloseButton")
            {
                continue;
            }

            bool belongsToExplanation = explanationCardPanel != null
                && candidate.transform.IsChildOf(explanationCardPanel.transform);
            bool belongsToRecap = recapPanel != null
                && candidate.transform.IsChildOf(recapPanel.transform);
            if (belongsToExplanation || belongsToRecap)
            {
                continue;
            }

            exitQuizButton = candidate;
            break;
        }

        if (exitQuizButton == null)
        {
            Debug.LogWarning("[HokkienQuizManager] Could not find the quiz dialog CloseButton.");
            return;
        }

        // Replace inconsistent scene-level actions with the shared exit flow.
        exitQuizButton.onClick = new Button.ButtonClickedEvent();
        exitQuizButton.onClick.AddListener(RequestExit);
        exitQuizButton.interactable = false;
    }

    private void RequestExit()
    {
        onExitRequested?.Invoke();
    }

    private void UpdateChoiceAudioButton(int choiceIndex, MultipleChoiceQuestion question)
    {
        if (choiceAudioButtons == null || choiceIndex < 0 || choiceIndex >= choiceAudioButtons.Length
            || choiceAudioButtons[choiceIndex] == null)
            return;

        bool hasClip = question.choiceHokkienAudioClips != null
            && choiceIndex < question.choiceHokkienAudioClips.Length
            && question.choiceHokkienAudioClips[choiceIndex] != null;
        bool hasUrl = question.choiceHokkienAudioUrls != null
            && choiceIndex < question.choiceHokkienAudioUrls.Length
            && !string.IsNullOrWhiteSpace(question.choiceHokkienAudioUrls[choiceIndex]);

        choiceAudioButtons[choiceIndex].gameObject.SetActive(hasClip || hasUrl);
        choiceAudioButtons[choiceIndex].interactable = hasClip;
    }

    private void PlayChoiceAudioOnly(int choiceIndex)
    {
        if (questionsSet == null || currentQuestionIndex < 0 || currentQuestionIndex >= questionsSet.Count
            || audioSource == null)
        {
            Debug.LogWarning("[QuizAudio] Cannot play: quiz question or AudioSource is unavailable.");
            return;
        }

        MultipleChoiceQuestion question = questionsSet[currentQuestionIndex];
        if (question.choiceHokkienAudioClips == null || choiceIndex < 0
            || choiceIndex >= question.choiceHokkienAudioClips.Length
            || question.choiceHokkienAudioClips[choiceIndex] == null)
        {
            Debug.LogWarning($"[QuizAudio] Choice {choiceIndex + 1} audio has not loaded.");
            return;
        }

        audioSource.PlayOneShot(question.choiceHokkienAudioClips[choiceIndex]);
        Debug.Log($"[QuizAudio] Playing choice {choiceIndex + 1}: {question.choiceHokkienAudioClips[choiceIndex].name}");
    }

    private string BuildQuestionText(MultipleChoiceQuestion currentQuestion)
    {
        if (currentQuestion == null)
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(currentQuestion.hokkienRomanizedText))
        {
            return currentQuestion.question;
        }

        return $"{currentQuestion.question}\n\n[Hokkien]\n{currentQuestion.hokkienRomanizedText}";
    }

    private void SelectAnswer(int selectedIndex)
    {
        if (waitingForCardDismiss)
        {
            return;
        }

        MultipleChoiceQuestion currentQuestion = questionsSet[currentQuestionIndex];
        bool isCorrect = selectedIndex == currentQuestion.correctAnswerIndex;
        string selectedAnswer = selectedIndex >= 0 && selectedIndex < currentQuestion.choices.Length
            ? currentQuestion.choices[selectedIndex]
            : string.Empty;
        string correctAnswer = currentQuestion.correctAnswerIndex >= 0 && currentQuestion.correctAnswerIndex < currentQuestion.choices.Length
            ? currentQuestion.choices[currentQuestion.correctAnswerIndex]
            : string.Empty;

        // UNPACK the string to get the specific explanation for the clicked option
        string specificExplanation = "";
        if (!string.IsNullOrEmpty(currentQuestion.explanation))
        {
            string[] splitExplanations = currentQuestion.explanation.Split(new string[] { "|||" }, StringSplitOptions.None);
            if (selectedIndex >= 0 && selectedIndex < splitExplanations.Length)
            {
                specificExplanation = splitExplanations[selectedIndex];
            }
            else
            {
                specificExplanation = currentQuestion.explanation; // Fallback
            }
        }

        results.Add(new QuizAnswerResult
        {
            question = currentQuestion.question,
            selectedAnswer = selectedAnswer,
            correctAnswer = correctAnswer,
            wasCorrect = isCorrect,
            explanation = specificExplanation
        });

        if (!isCorrect)
        {
            answeredIncorrect = true;
        }

        if (audioSource != null)
        {
            if (isCorrect && correctSound != null)
            {
                audioSource.PlayOneShot(correctSound);
            }
            else if (!isCorrect && wrongSound != null)
            {
                audioSource.PlayOneShot(wrongSound);
            }
        }

        SetAnswerButtonsVisible(false);
        ShowExplanationCard(currentQuestion, selectedAnswer, correctAnswer, isCorrect, specificExplanation);
    }

    private void ShowExplanationCard(MultipleChoiceQuestion question, string selectedAnswer, string correctAnswer, bool isCorrect, string specificExplanation)
    {
        waitingForCardDismiss = true;

        if (quizPanel != null)
        {
            quizPanel.SetActive(true);
        }

        if (explanationCardPanel != null)
        {
            explanationCardPanel.SetActive(true);
        }

        if (explanationTitleText != null)
        {
            explanationTitleText.text = isCorrect ? "Correct" : "Incorrect";
        }

        if (explanationBodyText != null)
        {
            explanationBodyText.text = BuildExplanationBody(selectedAnswer, correctAnswer, isCorrect, specificExplanation);
        }
    }

    private string BuildExplanationBody(string selectedAnswer, string correctAnswer, bool isCorrect, string specificExplanation)
    {
        string resultLine = isCorrect
            ? "You got it right."
            : $"Your answer: {selectedAnswer}\nCorrect answer: {correctAnswer}";

        string explanationLine = string.IsNullOrWhiteSpace(specificExplanation)
            ? string.Empty
            : $"\n\nExplanation:\n{specificExplanation}";

        return resultLine + explanationLine + "\n\nPress Close to continue.";
    }

    private void OnExplanationCardClosed()
    {
        if (!waitingForCardDismiss)
        {
            return;
        }

        waitingForCardDismiss = false;

        if (explanationCardPanel != null)
        {
            explanationCardPanel.SetActive(false);
        }

        currentQuestionIndex++;
        if (currentQuestionIndex < questionsSet.Count)
        {
            ShowQuestion(currentQuestionIndex);
        }
        else
        {
            CompleteQuiz();
        }
    }

    public void ShowRecap(Action onClosed)
    {
        HideAllOverlays();
        onRecapClosed = onClosed;

        if (recapPanel == null)
        {
            OnRecapClosed();
            return;
        }

        if (quizPanel != null)
        {
            quizPanel.SetActive(true);
        }

        SetAnswerButtonsVisible(false);

        recapPanel.SetActive(true);

        if (recapText != null)
        {
            recapText.text = BuildRecapText();
        }
    }

    private string BuildRecapText()
    {
        System.Text.StringBuilder recapBuilder = new System.Text.StringBuilder();
        recapBuilder.AppendLine($"Quiz complete: {CorrectAnswerCount}/{results.Count} correct");
        recapBuilder.AppendLine();

        for (int i = 0; i < results.Count; i++)
        {
            QuizAnswerResult result = results[i];
            recapBuilder.AppendLine($"{i + 1}. {result.question}");
            recapBuilder.AppendLine(result.wasCorrect ? "   Correct" : $"   Wrong - Correct: {result.correctAnswer}");

            if (!string.IsNullOrWhiteSpace(result.explanation))
            {
                recapBuilder.AppendLine($"   {result.explanation}");
            }

            recapBuilder.AppendLine();
        }

        if (results.Count == 0)
        {
            recapBuilder.AppendLine("No questions were answered.");
        }

        recapBuilder.AppendLine("Press Close to return.");
        return recapBuilder.ToString();
    }

    private void CompleteQuiz()
    {
        bool quizSuccess = !answeredIncorrect;
        Action<bool> completionCallback = onQuizComplete;
        onQuizComplete = null;
        SetExitHandler(null);
        completionCallback?.Invoke(quizSuccess);
    }

    private void OnRecapClosed()
    {
        if (recapPanel != null)
        {
            recapPanel.SetActive(false);
        }

        Action callback = onRecapClosed;
        onRecapClosed = null;
        callback?.Invoke();
    }

    private void SetAnswerButtonsVisible(bool visible)
    {
        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (answerButtons[i] != null)
            {
                answerButtons[i].gameObject.SetActive(visible);
            }
        }
    }

    private void HideAllOverlays()
    {
        if (explanationCardPanel != null)
        {
            explanationCardPanel.SetActive(false);
        }

        if (recapPanel != null)
        {
            recapPanel.SetActive(false);
        }

        waitingForCardDismiss = false;
    }
}
