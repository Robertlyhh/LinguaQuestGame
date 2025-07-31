using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class FeatureMatchManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI questionText;
    public Transform matchArea; // The parent object for the language/dropdown rows
    public GameObject optionRowPrefab; // A prefab with a TextMeshProUGUI for the language and a TMP_Dropdown for features
    public TextMeshProUGUI timerText;
    public Button submitButton;

    [Header("Game Logic")]
    public List<FeatureMatchQuestion> currentQuestionSet;
    public float duration = 45f; // Time limit for this challenge

    private int currentQuestionIndex = 0;
    private FeatureMatchQuestion currentQuestion;
    private BattleManager_FeatureMatch battleManager;
    private List<TMP_Dropdown> dropdowns = new List<TMP_Dropdown>();

    [Header("Timer")]
    private float timeRemaining;
    private bool timerRunning = false;
    private bool answered = false;

    void Start()
    {
        if (submitButton != null)
        {
            submitButton.onClick.AddListener(SubmitAnswer);
        }
        // It's best to have the BattleManager control when the first challenge starts.
    }

    void Update()
    {
        if (timerRunning && !answered)
        {
            timeRemaining -= Time.deltaTime;
            timerText.text = $"Time Remaining: {timeRemaining:F1}";

            // Update timer color based on time left
            if (timeRemaining > duration * 0.6f)
                timerText.color = Color.green;
            else if (timeRemaining > duration * 0.3f)
                timerText.color = Color.yellow;
            else
                timerText.color = Color.red;

            if (timeRemaining <= 0)
            {
                timerRunning = false;
                timerText.gameObject.SetActive(false);
                // The BattleManager handles the timeout logic
                StartCoroutine(battleManager.OnPlayerSubmitted(false, 0f, currentQuestion.explanation));
            }
        }
    }

    /// <summary>
    /// Called by the BattleManager to start a new question round.
    /// </summary>
    public void StartNewChallenge(BattleManager_FeatureMatch bm)
    {
        this.gameObject.SetActive(true);
        battleManager = bm;
        answered = false;

        // Select the next question, looping back to the start if necessary
        currentQuestionIndex = (currentQuestionIndex + 1) % currentQuestionSet.Count;
        currentQuestion = currentQuestionSet[currentQuestionIndex];
        LoadQuestion(currentQuestion);

        // Reset and start the timer
        timerText.gameObject.SetActive(true);
        timeRemaining = duration;
        timerRunning = true;
    }

    /// <summary>
    /// Sets up the UI for the current question.
    /// </summary>
    private void LoadQuestion(FeatureMatchQuestion question)
    {
        this.gameObject.SetActive(true);
        ClearMatchArea();
        FeatureMatchQuestion q = currentQuestion;

        questionText.text = q.question;
        for (int i = 0; i < q.languages.Length; i++)
        {
            GameObject entry = Instantiate(optionRowPrefab, matchArea);
            entry.SetActive(true);

            // Set language label
            entry.transform.Find("LanguageText").GetComponent<TextMeshProUGUI>().text = q.languages[i];

            // Populate dropdown
            TMP_Dropdown dropdown = entry.transform.Find("FeatureDropdown").GetComponent<TMP_Dropdown>();
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(q.features));

            dropdowns.Add(dropdown);
        }

        submitButton.gameObject.SetActive(true);
        submitButton.onClick.RemoveAllListeners();
        submitButton.onClick.AddListener(SubmitAnswer);
    }

    /// <summary>
    /// Called when the player clicks the Submit button.
    /// </summary>
    public void SubmitAnswer()
    {
        if (answered) return; // Prevent submitting more than once
        answered = true;
        timerRunning = false;
        timerText.gameObject.SetActive(false);

        bool isCorrect = CheckAnswer();

        // Pass the result to the BattleManager
        StartCoroutine(battleManager.OnPlayerSubmitted(isCorrect, timeRemaining, currentQuestion.explanation));
    }

    /// <summary>
    /// Checks if the selected dropdown options match the correct answers.
    /// </summary>
    /// <returns>True if all answers are correct, false otherwise.</returns>
    private bool CheckAnswer()
    {
        if (dropdowns.Count != currentQuestion.correctFeatureIndices.Length)
        {
            Debug.LogError("Mismatch between number of dropdowns and correct answers defined in the question.");
            return false;
        }

        for (int i = 0; i < dropdowns.Count; i++)
        {
            // The dropdown's 'value' is the index of the selected option.
            if (dropdowns[i].value != currentQuestion.correctFeatureIndices[i])
            {
                return false; // If any answer is wrong, the whole submission is wrong.
            }
        }

        return true; // All answers were correct.
    }

    private void ClearMatchArea()
    {
        foreach (Transform child in matchArea)
        {
            Destroy(child.gameObject);
        }
        dropdowns.Clear();
    }
}
