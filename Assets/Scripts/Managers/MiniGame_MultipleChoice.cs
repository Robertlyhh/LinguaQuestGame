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
    public AudioSource audioSource; // Optional reference to play sounds
    public AudioClip correctSound; // Sound for correct answer
    public AudioClip wrongSound; // Sound for wrong answer

    [Header("Question")]
    public List<MultipleChoiceQuestion> questionsSet;

    private Action<bool> onMiniGameComplete;
    private int currentQuestionIndex;
    private bool answeredIncorrect;
    public GameObject PlayerPrefab;
    public GameObject EnemyPrefab;

    void Start()
    {
        quizPanel.SetActive(false);
    }

    public void LaunchQuestion(List<MultipleChoiceQuestion> questions, Action<bool> onComplete)
    {
        Debug.Log("Launching multiple choice quiz with " + questions.Count + " questions.");
        questionsSet = questions;
        currentQuestionIndex = 0;
        answeredIncorrect = false;
        onMiniGameComplete = onComplete;

        quizPanel.SetActive(true);
        Debug.Log("Quiz panel activated.");
        feedbackText.text = "";
        ShowQuestion(currentQuestionIndex);
    }

    void ShowQuestion(int index)
    {
        if (index < 0 || index >= questionsSet.Count)
        {
            FinishQuiz();
            return;
        }
        quizPanel.SetActive(true);

        MultipleChoiceQuestion currentQuestion = questionsSet[index];
        questionText.text = currentQuestion.question;
        feedbackText.text = "";
        for (int i = 0; i < answerButtons.Length; i++)
        {
            if (i < currentQuestion.choices.Length)
            {
                answerButtons[i].gameObject.SetActive(true);
                answerTexts[i].text = currentQuestion.choices[i];

                int choiceIndex = i; // Local copy for closure
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
        MultipleChoiceQuestion currentQuestion = questionsSet[currentQuestionIndex];
        Debug.Log($"Selected answer for question {currentQuestionIndex}: {index}");
        bool isCorrect = index == currentQuestion.correctAnswerIndex;
        quizPanel.SetActive(false); // Hide quiz panel immediately after selection

        if (isCorrect)
        {
            feedbackText.text = "Correct!";
            if (audioSource != null && correctSound != null)
            {
                audioSource.PlayOneShot(correctSound);
            }
            if (PlayerPrefab != null && EnemyPrefab != null)
            {
                PlayerPrefab.GetComponent<PlayerMovement>().AttackEnemy(); // Example interaction, adjust as needed
                EnemyPrefab.GetComponent<FightingEnemy>().takeDamage(1); // Example interaction, adjust as needed
            }

            StartCoroutine(NextQuestionDelay());
        }
        else
        {
            answeredIncorrect = true;
            if (audioSource != null && wrongSound != null)
            {
                audioSource.PlayOneShot(wrongSound);
            }
            if (PlayerPrefab != null && EnemyPrefab != null)
            {
                PlayerPrefab.GetComponent<PlayerMovement>().takeDamage(1); // Example interaction, adjust as needed
                EnemyPrefab.GetComponent<FightingEnemy>().AttackPlayer(); // Example interaction, adjust as needed
            }

            feedbackText.text = $"Incorrect.\n{currentQuestion.explanation}";
            StartCoroutine(NextQuestionDelay());
        }
    }

    private IEnumerator NextQuestionDelay()
    {
        DisableButtons();
        yield return new WaitForSeconds(2.5f);
        currentQuestionIndex++;
        ShowQuestion(currentQuestionIndex);
    }

    private void FinishQuiz()
    {
        quizPanel.SetActive(false);
        onMiniGameComplete?.Invoke(!answeredIncorrect); // true if all correct
    }

    private void DisableButtons()
    {
        foreach (Button b in answerButtons)
        {
            b.gameObject.SetActive(false);
        }
    }
}
