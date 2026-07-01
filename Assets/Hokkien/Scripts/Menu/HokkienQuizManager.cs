using System;
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

    [Header("Question")]
    public List<MultipleChoiceQuestion> questionsSet;

    private Action<bool> onQuizComplete;
    private readonly List<QuizAnswerResult> results = new List<QuizAnswerResult>();
    private int currentQuestionIndex;
    private bool answeredIncorrect;
    private bool waitingForCardDismiss;

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

        HideAllOverlays();
        if (quizPanel != null)
        {
            quizPanel.SetActive(true);
        }

        ShowQuestion(currentQuestionIndex);
    }

    private void ShowQuestion(int index)
    {
        if (questionsSet == null || index < 0 || index >= questionsSet.Count)
        {
            ShowRecap();
            return;
        }

        HideAllOverlays();
        SetAnswerButtonsVisible(true);

        MultipleChoiceQuestion currentQuestion = questionsSet[index];
        questionText.text = currentQuestion.question;

        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (i < currentQuestion.choices.Length)
            {
                answerButtons[i].gameObject.SetActive(true);
                answerTexts[i].text = currentQuestion.choices[i];

                int choiceIndex = i;
                answerButtons[i].onClick.RemoveAllListeners();
                answerButtons[i].onClick.AddListener(() => SelectAnswer(choiceIndex));
            }
            else
            {
                answerButtons[i].gameObject.SetActive(false);
            }
        }
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
            ShowRecap();
        }
    }

    private void ShowRecap()
    {
        HideAllOverlays();

        if (recapPanel != null)
        {
            recapPanel.SetActive(true);
        }

        if (recapText != null)
        {
            recapText.text = BuildRecapText();
        }
    }

    private string BuildRecapText()
    {
        int correctCount = 0;
        foreach (QuizAnswerResult result in results)
        {
            if (result.wasCorrect)
            {
                correctCount++;
            }
        }

        System.Text.StringBuilder recapBuilder = new System.Text.StringBuilder();
        recapBuilder.AppendLine($"Quiz complete: {correctCount}/{results.Count} correct");
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

        recapBuilder.AppendLine("Press Close to finish.");
        return recapBuilder.ToString();
    }

    private void OnRecapClosed()
    {
        if (recapPanel != null)
        {
            recapPanel.SetActive(false);
        }

        bool quizSuccess = !answeredIncorrect;
        onQuizComplete?.Invoke(quizSuccess);
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