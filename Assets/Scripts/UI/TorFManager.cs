using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TorFManager : MonoBehaviour
{
    public TextMeshProUGUI questionText;
    public List<MultipleChoiceQuestion> torfQuestions;
    public Button trueButton;
    public Button falseButton;
    public float duration = 30f; // Time limit for the challenge
    private float timeRemaining;
    private bool timerRunning = false;
    public TextMeshProUGUI timerText; // UI element to display the timer
    public TextMeshProUGUI feedbackText; // UI element to display feedback
    public int currentQuestionIndex = 0; // Track the current question

    private bool answered = false;

    private BattleManager_TorF battleManager;
    private MultipleChoiceQuestion currentQuestion;

    private int currentIndex = -1;

    void Start()
    {
        trueButton.onClick.AddListener(() => OnAnswerSelected(0));
        falseButton.onClick.AddListener(() => OnAnswerSelected(1));
    }

    void Update()
    {
        if (timerRunning && !answered)
        {
            timeRemaining -= Time.deltaTime;
            timerText.text = $"Time Remaining: {timeRemaining:F1}"; // Update timer UI

            // Color changes
            if (timeRemaining > duration * 0.6f)
                timerText.color = Color.green;
            else if (timeRemaining > duration * 0.3f)
                timerText.color = Color.yellow;
            else
                timerText.color = Color.red;

            // Optional: Pulse or flash when under 3 seconds
            if (timeRemaining < 3f)
            {
                float scale = 1.1f + Mathf.PingPong(Time.time * 5f, 0.2f); // subtle pulse
                timerText.transform.localScale = new Vector3(scale, scale, 1f);
            }
            else
            {
                timerText.transform.localScale = Vector3.one;
            }

            if (timeRemaining <= 0)
            {
                Debug.Log("Time's up!");
                timerRunning = false;
                timerText.gameObject.SetActive(false);
                battleManager.OnPlayerSubmitted(false, 0f, torfQuestions[currentQuestionIndex].explanation);
            }
        }
    }

    public void StartNewQuestion(BattleManager_TorF bm)
    {
        battleManager = bm;
        answered = false;
        currentIndex = (currentIndex + 1) % torfQuestions.Count;
        currentQuestion = torfQuestions[currentIndex];

        questionText.text = currentQuestion.question;
        timeRemaining = duration;
        timerRunning = true;
        timerText.gameObject.SetActive(true);
    }

    void OnAnswerSelected(int selectedIndex)
    {
        if (answered) return; // Ignore if already answered
        answered = true;
        bool correct = selectedIndex == currentQuestion.correctAnswerIndex;
        timerText.gameObject.SetActive(false);
        StartCoroutine(battleManager.OnPlayerSubmitted(correct, timeRemaining, currentQuestion.explanation));
    }
}
