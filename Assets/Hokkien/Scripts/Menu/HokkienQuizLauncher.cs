using System.Collections.Generic;
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

    [Header("Level Configs")]
    public List<HokkienQuizLevelData> levelConfigs = new List<HokkienQuizLevelData>();
    [Min(0)] public int currentLevelIndex;

    [Header("Progression")]
    public string successSceneName;
    public string failureSceneName;

    private bool quizRunning;

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

        quizManager.LaunchQuestion(questionsToLaunch, success => OnQuizCompleted(success, activeLevel));
        return true;
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
        PausePlayer(false);
        SetQuizRunning(false);

        if (quizManager.quizPanel != null)
        {
            quizManager.quizPanel.SetActive(false);
        }

        if (quizOptionsPanel != null)
        {
            quizOptionsPanel.SetActive(false);
        }

        string nextSuccessScene = activeLevel != null && !string.IsNullOrWhiteSpace(activeLevel.successSceneName)
            ? activeLevel.successSceneName
            : successSceneName;

        string nextFailureScene = activeLevel != null && !string.IsNullOrWhiteSpace(activeLevel.failureSceneName)
            ? activeLevel.failureSceneName
            : failureSceneName;

        if (success && !string.IsNullOrWhiteSpace(nextSuccessScene))
        {
            SceneManager.LoadScene(nextSuccessScene);
        }
        else if (!success && !string.IsNullOrWhiteSpace(nextFailureScene))
        {
            SceneManager.LoadScene(nextFailureScene);
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
        PausePlayer(false);
        SetQuizRunning(false);

        if (quizManager != null && quizManager.quizPanel != null)
        {
            quizManager.quizPanel.SetActive(false);
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