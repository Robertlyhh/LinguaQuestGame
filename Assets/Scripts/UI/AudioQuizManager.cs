using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AudioQuizManager : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioClip[] tones; // Array of tone audio clips
    public AudioSource audioSourceBGM;   // For background music
    public AudioSource audioSourceTones; // For tone playback

    [Header("UI Elements")]
    public Button playbackButton;
    public Button[] toneButtons; // Tone 1–4 buttons
    public Button submitButton;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI feedbackText;

    [Header("Settings")]
    public List<int> correctSequence = new(); // Predefined correct answers
    public float duration = 15f; // Time for each question
    private float timeRemaining;

    private int currentQuestionIndex = 0;
    private int selectedTone = -1;
    private bool timerRunning = false;
    private bool answered = false;
    private bool lastResultCorrect = false;

    private AudioQuizBattleManager battleManager;

    private float playbackCooldown = 1f;
    private float lastPlaybackTime = 0f;

    void Start()
    {
        feedbackText.text = "";
        submitButton.onClick.AddListener(SubmitAnswer);
        playbackButton.onClick.AddListener(PlayCurrentTone);

        for (int i = 0; i < toneButtons.Length; i++)
        {
            int index = i; // capture index for listener
            toneButtons[i].onClick.AddListener(() => OnToneButtonClicked(index));
        }
    }

    public void StartNewChallenge(AudioQuizBattleManager bm)
    {
        battleManager = bm;
        currentQuestionIndex = 0;
        ResetUI();
        timerRunning = true;
        answered = false;
    }

    void ResetUI()
    {
        feedbackText.text = "";
        ResetButtonColors();
        selectedTone = -1;
        timeRemaining = duration;
        timerText.gameObject.SetActive(true);
        timerRunning = true;
        answered = false;
    }

    void Update()
    {
        if (timerRunning && !answered)
        {
            timeRemaining -= Time.deltaTime;
            timerText.text = $"Time: {timeRemaining:F1}s";

            if (timeRemaining <= 0)
            {
                Debug.Log("Time’s up! Auto submit.");
                SubmitAnswer();
            }

            lastPlaybackTime += Time.deltaTime;
        }
    }

    public void PlayCurrentTone()
    {
        StartCoroutine(PlayToneCoroutine());
    }

    IEnumerator PlayToneCoroutine()
    {
        if (lastPlaybackTime < playbackCooldown)
        {
            feedbackText.text = "Please wait before playing again.";
            yield break;
        }

        if (audioSourceBGM.isPlaying)
        {
            audioSourceBGM.Pause();
        }

        int toneIndex = correctSequence[currentQuestionIndex];
        audioSourceTones.clip = tones[toneIndex];
        audioSourceTones.Play();

        yield return new WaitForSeconds(tones[toneIndex].length);

        if (audioSourceBGM.clip != null)
        {
            audioSourceBGM.Play();
        }

        lastPlaybackTime = 0f;
    }

    void OnToneButtonClicked(int toneIndex)
    {
        selectedTone = toneIndex;

        for (int i = 0; i < toneButtons.Length; i++)
        {
            ColorBlock cb = toneButtons[i].colors;
            if (i == toneIndex)
            {
                cb.normalColor = Color.yellow;
                cb.highlightedColor = Color.yellow;
                cb.selectedColor = Color.yellow;
            }
            else
            {
                cb.normalColor = Color.white;
                cb.highlightedColor = Color.white;
                cb.selectedColor = Color.white;
            }
            toneButtons[i].colors = cb;
        }
    }

    void ResetButtonColors()
    {
        foreach (Button btn in toneButtons)
        {
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = Color.white;
            cb.selectedColor = Color.white;
            btn.colors = cb;
        }
    }

    void SubmitAnswer()
    {
        if (answered) return;

        answered = true;
        timerRunning = false;
        timerText.gameObject.SetActive(false);

        if (selectedTone == -1)
        {
            feedbackText.text = "No tone selected!";
            lastResultCorrect = false;
        }
        else
        {
            lastResultCorrect = CheckAnswer();
            feedbackText.text = lastResultCorrect ? "Correct!" : "Wrong!";
        }

        // Call BattleManager immediately
        battleManager.OnPlayerSubmitted(lastResultCorrect, timeRemaining);

        // Prepare next question
        currentQuestionIndex++;
        if (currentQuestionIndex < correctSequence.Count)
        {
            StartCoroutine(NextQuestionDelay());
        }
        else
        {
            Debug.Log("All tones completed.");
        }
    }

    bool CheckAnswer()
    {
        return selectedTone == correctSequence[currentQuestionIndex];
    }

    IEnumerator NextQuestionDelay()
    {
        yield return new WaitForSeconds(2f); // Wait before next question
        ResetUI();
    }
}
