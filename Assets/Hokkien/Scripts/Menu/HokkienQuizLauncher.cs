using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HokkienQuizLauncher : MonoBehaviour
{
    [Header("Scene References")]
    public MenuController menuController;
    public frogMove playerMovement;
    public HokkienQuizManager quizManager;
    public GameObject quizOptionsPanel;

    [Header("Question Bank")]
    public HokkienQuizQuestionBank questionBank;
    [Tooltip("Load quiz content from the backend and use the local bank only as a fallback.")]
    public bool loadQuizFromBackend = true;

    [Header("Level Configs")]
    public List<HokkienQuizLevelData> levelConfigs = new List<HokkienQuizLevelData>();
    [Min(0)] public int currentLevelIndex;

    [Header("Progression")]
    public string successSceneName;
    public string failureSceneName;

    private bool quizRunning;
    private int quizRunToken;

    private void Awake()
    {
        if (menuController == null)
        {
            menuController = FindObjectOfType<MenuController>();
        }

        if (playerMovement == null)
        {
            playerMovement = FindObjectOfType<frogMove>();
        }
    }

    public bool StartQuiz()
    {
        if (quizRunning)
        {
            return false;
        }

        if (quizManager == null)
        {
            Debug.LogWarning("[HokkienQuizLauncher] Quiz manager is not assigned.");
            RestoreMenuAfterFailedStart();
            return false;
        }

        HokkienQuizLevelData activeLevel = GetActiveLevelData();
        List<MultipleChoiceQuestion> questionsToLaunch = GetQuestionsToLaunch(activeLevel);
        if (questionsToLaunch == null || questionsToLaunch.Count == 0)
        {
            Debug.LogWarning("[HokkienQuizLauncher] No quiz questions are assigned on the question bank or HokkienQuizManager.");
            RestoreMenuAfterFailedStart();
            return false;
        }

        SetQuizRunning(true);
        PausePlayer(true);

        if (quizManager.quizPanel != null)
        {
            quizManager.quizPanel.SetActive(true);

            CanvasGroup cg = quizManager.quizPanel.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }
        }

        if (quizOptionsPanel != null)
        {
            quizOptionsPanel.SetActive(true);
        }

        LaunchQuiz(activeLevel, questionsToLaunch);
        return true;
    }

    private void LaunchQuiz(HokkienQuizLevelData activeLevel, List<MultipleChoiceQuestion> fallbackQuestions)
    {
        int runToken = ++quizRunToken;
        quizManager.SetExitHandler(RequestExitQuiz);

        if (quizManager.quizPanel != null)
        {
            quizManager.quizPanel.SetActive(true);
        }

        if (quizOptionsPanel != null)
        {
            quizOptionsPanel.SetActive(true);
        }

        if (loadQuizFromBackend && APIManager.Instance != null && activeLevel != null)
        {
            string levelId = BuildBackendLevelId(activeLevel.levelName);
            APIManager.Instance.GetHokkienQuiz(
                levelId,
                quiz =>
                {
                    if (!IsQuizRunCurrent(runToken))
                    {
                        return;
                    }

                    quizManager.LaunchQuestion(
                        CreateRuntimeQuestions(quiz),
                        success =>
                        {
                            if (IsQuizRunCurrent(runToken))
                            {
                                OnQuizCompleted(success, activeLevel);
                            }
                        });
                },
                error =>
                {
                    if (!IsQuizRunCurrent(runToken))
                    {
                        return;
                    }

                    Debug.LogWarning($"[HokkienQuizLauncher] Backend quiz '{levelId}' unavailable ({error}); using local fallback.");
                    quizManager.LaunchQuestion(
                        fallbackQuestions,
                        success =>
                        {
                            if (IsQuizRunCurrent(runToken))
                            {
                                OnQuizCompleted(success, activeLevel);
                            }
                        });
                });
        }
        else
        {
            quizManager.LaunchQuestion(
                fallbackQuestions,
                success =>
                {
                    if (IsQuizRunCurrent(runToken))
                    {
                        OnQuizCompleted(success, activeLevel);
                    }
                });
        }
    }

    private bool IsQuizRunCurrent(int runToken)
    {
        return quizRunning && runToken == quizRunToken;
    }

    private static string BuildBackendLevelId(string levelName)
    {
        string normalized = Regex.Replace((levelName ?? string.Empty).Trim().ToLowerInvariant(), @"[^a-z0-9_-]+", "_");
        return int.TryParse(normalized, out int levelNumber) ? $"level_{levelNumber}" : normalized;
    }

    private static List<MultipleChoiceQuestion> CreateRuntimeQuestions(HokkienQuizData quiz)
    {
        List<MultipleChoiceQuestion> result = new List<MultipleChoiceQuestion>();
        foreach (HokkienQuizQuestionData source in quiz.questions)
        {
            if (source == null || source.choices == null || source.choices.Length == 0)
                continue;

            MultipleChoiceQuestion question = ScriptableObject.CreateInstance<MultipleChoiceQuestion>();
            question.question = source.question;
            question.correctAnswerIndex = source.correct_answer_index;
            question.choices = new string[source.choices.Length];
            question.choiceHokkienRomanizedTexts = new string[source.choices.Length];
            question.choiceHokkienAudioUrls = new string[source.choices.Length];
            question.choiceHokkienAudioClips = new AudioClip[source.choices.Length];
            string[] explanations = new string[source.choices.Length];

            for (int i = 0; i < source.choices.Length; i++)
            {
                HokkienQuizChoiceData choice = source.choices[i];
                question.choices[i] = choice?.text ?? string.Empty;
                explanations[i] = choice?.explanation ?? string.Empty;
                string audioPath = choice?.audio ?? string.Empty;
                question.choiceHokkienAudioUrls[i] = string.IsNullOrWhiteSpace(audioPath)
                    ? string.Empty
                    : APIManager.Instance.BaseUrl.TrimEnd('/') + "/" + audioPath.TrimStart('/');
            }

            question.explanation = string.Join("|||", explanations);
            result.Add(question);
        }
        return result;
    }

    public void SetCurrentLevelIndex(int levelIndex)
    {
        currentLevelIndex = Mathf.Max(0, levelIndex);
    }

    public void SetCurrentLevel(HokkienQuizLevelData levelData)
    {
        if (levelData == null)
        {
            return;
        }

        int index = levelConfigs.IndexOf(levelData);
        if (index >= 0)
        {
            currentLevelIndex = index;
        }
    }

    private HokkienQuizLevelData GetActiveLevelData()
    {
        if (levelConfigs != null && levelConfigs.Count > 0)
        {
            int safeIndex = Mathf.Clamp(currentLevelIndex, 0, levelConfigs.Count - 1);
            return levelConfigs[safeIndex];
        }

        return null;
    }

    private List<MultipleChoiceQuestion> GetQuestionsToLaunch(HokkienQuizLevelData activeLevel)
    {
        HokkienQuizQuestionBank activeQuestionBank = activeLevel != null && activeLevel.questionBank != null
            ? activeLevel.questionBank
            : questionBank;

        if (activeQuestionBank != null && activeQuestionBank.questions != null && activeQuestionBank.questions.Count > 0)
        {
            return activeQuestionBank.CreateRuntimeQuestions();
        }

        if (quizManager != null && quizManager.questionsSet != null && quizManager.questionsSet.Count > 0)
        {
            return new List<MultipleChoiceQuestion>(quizManager.questionsSet);
        }

        return null;
    }

    private void OnQuizCompleted(bool success, HokkienQuizLevelData activeLevel)
    {
        if (quizManager.quizPanel != null)
        {
            quizManager.quizPanel.SetActive(false);
        }

        if (quizOptionsPanel != null)
        {
            quizOptionsPanel.SetActive(false);
        }

        int correctCount = quizManager.CorrectAnswerCount;
        int totalCount = quizManager.AnsweredQuestionCount;
        ShowQuizCompletionFeedback(success, activeLevel, correctCount, totalCount);
    }

    private void ShowQuizCompletionFeedback(
        bool success,
        HokkienQuizLevelData activeLevel,
        int correctCount,
        int totalCount)
    {
        // Confirmation is only needed while a quiz is still in progress.
        System.Action onExit = success ? null : ExitQuiz;

        HokkienCompletionFeedback.GetOrCreate().ShowQuizResult(
            success,
            correctCount,
            totalCount,
            success
                ? () => CompleteQuizAndNavigate(activeLevel)
                : () => RetryQuiz(activeLevel),
            onExit);
    }

    public bool TryHandleEscape()
    {
        if (!quizRunning)
        {
            return false;
        }

        // Close quiz sub-panels before offering to exit the quiz.
        if (quizManager != null && quizManager.TryHandleEscape())
        {
            return true;
        }

        RequestExitQuiz();
        return true;
    }

    private void RequestExitQuiz()
    {
        HokkienCompletionFeedback.GetOrCreate().ShowExitConfirmation(ExitQuiz, null);
    }

    private void ExitQuiz()
    {
        quizRunToken++;
        quizManager.CancelQuiz();

        if (quizManager.quizPanel != null)
        {
            quizManager.quizPanel.SetActive(false);
        }

        if (quizOptionsPanel != null)
        {
            quizOptionsPanel.SetActive(false);
        }

        PausePlayer(false);
        SetQuizRunning(false);

        if (menuController != null && menuController.menuCanvas != null)
        {
            menuController.menuCanvas.SetActive(false);
        }
    }

    private void RetryQuiz(HokkienQuizLevelData activeLevel)
    {
        List<MultipleChoiceQuestion> questionsToLaunch = GetQuestionsToLaunch(activeLevel);
        if (questionsToLaunch == null || questionsToLaunch.Count == 0)
        {
            Debug.LogWarning("[HokkienQuizLauncher] Cannot retry because no quiz questions are available.");
            return;
        }

        LaunchQuiz(activeLevel, questionsToLaunch);
    }

    private void CompleteQuizAndNavigate(HokkienQuizLevelData activeLevel)
    {
        quizRunToken++;
        quizManager.SetExitHandler(null);
        PausePlayer(false);
        SetQuizRunning(false);

        string nextSuccessScene = activeLevel != null && !string.IsNullOrWhiteSpace(activeLevel.successSceneName)
            ? activeLevel.successSceneName
            : successSceneName;

        if (!string.IsNullOrWhiteSpace(nextSuccessScene))
        {
            SceneManager.LoadScene(nextSuccessScene);
        }
        else if (menuController != null)
        {
            menuController.SetQuizActive(false);
            menuController.menuCanvas.SetActive(true);
        }
    }

    private void PausePlayer(bool paused)
    {
        if (playerMovement != null)
        {
            playerMovement.SetMovementPaused(paused);
        }
    }

    private void SetQuizRunning(bool isRunning)
    {
        quizRunning = isRunning;

        if (menuController != null)
        {
            menuController.SetQuizActive(isRunning);
        }
    }

    private void RestoreMenuAfterFailedStart()
    {
        quizRunToken++;
        PausePlayer(false);
        SetQuizRunning(false);

        if (quizManager != null)
        {
            quizManager.SetExitHandler(null);
            if (quizManager.quizPanel != null)
            {
                quizManager.quizPanel.SetActive(false);
            }
        }

        if (quizOptionsPanel != null)
        {
            quizOptionsPanel.SetActive(false);
        }

        if (menuController != null)
        {
            menuController.menuCanvas.SetActive(true);
        }
    }
}
