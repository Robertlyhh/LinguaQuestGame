using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MiniGame_FeatureMatch : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject panel;
    public Transform matchArea;
    public GameObject matchEntryTemplate;  // Prefab with LanguageText + FeatureDropdown
    public Button submitButton;
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI feedbackText;
    public AudioSource audioSource; // Optional reference to play sounds
    public AudioClip correctSound; // Sound for correct answer
    public AudioClip wrongSound; // Sound for wrong answer

    private List<FeatureMatchQuestion> currentQuestionSet;
    private int currentQuestionIndex = 0;
    private List<TMP_Dropdown> dropdowns = new List<TMP_Dropdown>();
    private Action<bool> onComplete;
    public GameObject PlayerPrefab;
    public GameObject EnemyPrefab;
    private bool answeredIncorrectly = false;

    public void Launch(List<FeatureMatchQuestion> questions, Action<bool> callback)
    {
        currentQuestionSet = questions;
        currentQuestionIndex = 0;
        answeredIncorrectly = false;
        onComplete = callback;

        panel.SetActive(true);
        feedbackText.text = "";
        ShowQuestion();
    }

    private void ShowQuestion()
    {
        panel.SetActive(true);
        ClearMatchArea();
        FeatureMatchQuestion q = currentQuestionSet[currentQuestionIndex];

        questionText.text = q.question;
        for (int i = 0; i < q.languages.Length; i++)
        {
            GameObject entry = Instantiate(matchEntryTemplate, matchArea);
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
        submitButton.onClick.AddListener(CheckAnswer);
    }

    private void CheckAnswer()
    {
        FeatureMatchQuestion q = currentQuestionSet[currentQuestionIndex];
        bool correct = true;

        for (int i = 0; i < dropdowns.Count; i++)
        {
            if (dropdowns[i].value != q.correctFeatureIndices[i])
            {
                correct = false;
                break;
            }
        }

        panel.SetActive(false);

        if (correct)
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
        }
        else
        {
            answeredIncorrectly = true;
            feedbackText.text = $"Incorrect.\n{q.explanation}";
            if (audioSource != null && wrongSound != null)
            {
                audioSource.PlayOneShot(wrongSound);
            }
            if (PlayerPrefab != null && EnemyPrefab != null)
            {
                PlayerPrefab.GetComponent<PlayerMovement>().takeDamage(1); // Example interaction, adjust as needed
                EnemyPrefab.GetComponent<FightingEnemy>().AttackPlayer(); // Example interaction, adjust as needed
            }
        }

        StartCoroutine(NextQuestionDelay());
    }

    private IEnumerator NextQuestionDelay()
    {
        DisableDropdowns();
        yield return new WaitForSeconds(2.5f);

        currentQuestionIndex++;

        if (currentQuestionIndex < currentQuestionSet.Count)
        {
            dropdowns.Clear();
            ShowQuestion();
        }
        else
        {
            FinishGame();
        }
    }

    private void FinishGame()
    {
        panel.SetActive(false);
        onComplete?.Invoke(!answeredIncorrectly);  // Return true only if all correct
    }

    private void ClearMatchArea()
    {
        foreach (Transform child in matchArea)
        {
            Destroy(child.gameObject);
        }
        dropdowns.Clear();
    }

    private void DisableDropdowns()
    {
        foreach (var dd in dropdowns)
            dd.gameObject.SetActive(false);
        submitButton.gameObject.SetActive(false);
    }
}
