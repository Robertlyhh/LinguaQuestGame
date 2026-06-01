using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class MiniGame_MultipleChoice : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject quizPanel;
    public TextMeshProUGUI questionText;
    public Button[] answerButtons;
    public TextMeshProUGUI[] answerTexts;
    public TextMeshProUGUI feedbackText;
    public AudioSource audioSource;
    public AudioClip correctSound;
    public AudioClip wrongSound;

    [Header("Flash Effect")]
    public Image panelBackground; // drag your quiz panel background image here
    public Color correctFlashColor = new Color(0f, 1f, 0f, 0.4f);
    public Color wrongFlashColor = new Color(1f, 0f, 0f, 0.4f);
    public float flashDuration = 0.3f;
    private Color originalPanelColor;

    [Header("Question")]
    public List<MultipleChoiceQuestion> questionsSet;

    private Action<bool> onMiniGameComplete;
    private int currentQuestionIndex;
    private bool answeredIncorrect;
    private List<MultipleChoiceQuestion> remainingQuestions; // tracks unanswered
    public GameObject PlayerPrefab;
    public GameObject EnemyPrefab;

    void Start()
    {
        quizPanel.SetActive(false);
        if (panelBackground != null)
            originalPanelColor = panelBackground.color;
    }

    public void LaunchQuestion(List<MultipleChoiceQuestion> questions, Action<bool> onComplete)
    {
        Debug.Log("Launching multiple choice quiz with " + questions.Count + " questions.");
        questionsSet = questions;

        // Copy into remaining — wrong answers get re-added here
        remainingQuestions = new List<MultipleChoiceQuestion>(questions);

        currentQuestionIndex = 0;
        answeredIncorrect = false;
        onMiniGameComplete = onComplete;

        quizPanel.SetActive(true);
        feedbackText.text = "";
        ShowQuestion(currentQuestionIndex);
    }

    void ShowQuestion(int index)
    {
        // All questions answered correctly
        if (remainingQuestions.Count == 0)
        {
            FinishQuiz();
            return;
        }

        // Wrap index around remaining list
        currentQuestionIndex = index % remainingQuestions.Count;

        quizPanel.SetActive(true);

        MultipleChoiceQuestion currentQuestion = remainingQuestions[currentQuestionIndex];
        questionText.text = currentQuestion.question;
        feedbackText.text = "";

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

    void SelectAnswer(int index)
    {
        MultipleChoiceQuestion currentQuestion = remainingQuestions[currentQuestionIndex];
        bool isCorrect = index == currentQuestion.correctAnswerIndex;

        if (isCorrect)
        {
            feedbackText.text = "Correct!";

            if (audioSource != null && correctSound != null)
                audioSource.PlayOneShot(correctSound);

            if (PlayerPrefab != null && EnemyPrefab != null)
            {
                PlayerPrefab.GetComponent<PlayerMovement>().AttackEnemy();
                EnemyPrefab.GetComponent<FightingEnemy>().takeDamage(1);
            }

            // Remove correctly answered question from remaining
            remainingQuestions.RemoveAt(currentQuestionIndex);

            StartCoroutine(NextQuestionDelay(true));
        }
        else
        {
            answeredIncorrect = true;
            feedbackText.text = $"Not quite!\n{currentQuestion.explanation}";

            if (audioSource != null && wrongSound != null)
                audioSource.PlayOneShot(wrongSound);

            if (PlayerPrefab != null && EnemyPrefab != null)
            {
                PlayerPrefab.GetComponent<PlayerMovement>().takeDamage(1);
                EnemyPrefab.GetComponent<FightingEnemy>().AttackPlayer();
            }

            // Move wrong question to end of remaining list
            MultipleChoiceQuestion wrongQuestion = remainingQuestions[currentQuestionIndex];
            remainingQuestions.RemoveAt(currentQuestionIndex);
            remainingQuestions.Add(wrongQuestion);

            StartCoroutine(NextQuestionDelay(false));
        }
    }

    private IEnumerator NextQuestionDelay(bool wasCorrect)
    {
        DisableButtons();

        // Flash the panel
        if (panelBackground != null)
            StartCoroutine(FlashPanel(wasCorrect ? correctFlashColor : wrongFlashColor));

        yield return new WaitForSeconds(2.5f);

        // Show next question — panel stays open
        ShowQuestion(currentQuestionIndex);
    }

    private IEnumerator FlashPanel(Color flashColor)
    {
        if (panelBackground == null) yield break;

        panelBackground.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        panelBackground.color = originalPanelColor;
    }

    private void FinishQuiz()
    {
        quizPanel.SetActive(false);
        onMiniGameComplete?.Invoke(true);
    }

    private void DisableButtons()
    {
        foreach (Button b in answerButtons)
            b.gameObject.SetActive(false);
    }
}