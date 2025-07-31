using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BossFightManager : MonoBehaviour
{
    [Header("Boss Settings")]
    public int bossMaxHP = 3;  // One HP per mini-game phase
    private int bossCurrentHP;
    public AudioClip Phase1BgMusic;
    public AudioClip Phase2BgMusic;
    public AudioClip Phase3BgMusic;
    public AudioSource bgMusicSource;

    [Header("UI Elements")]
    public TextMeshProUGUI bossHPText;
    public TextMeshProUGUI phaseFeedbackText;

    [Header("Mini-Game Managers")]
    public MiniGame_MultipleChoice multipleChoiceGame;
    public MiniGame_WordOrder wordOrderGame;
    public MiniGame_FeatureMatch featureMatchGame;

    [Header("Mini-Game Questions")]
    public List<MultipleChoiceQuestion> mcQuestions;
    public List<WordOrderQuestion> wordOrderQuestions;
    public List<FeatureMatchQuestion> featureMatchQuestions;

    private int currentPhase = 0;
    public AudioClip winSound;
    public AudioClip loseSound;
    public Canvas winScreen;
    public Canvas loseScreen;
    public GameObject PlayerPrefab;
    public GameObject EnemyPrefab;

    void Start()
    {
        bossCurrentHP = bossMaxHP;
        bgMusicSource = GetComponent<AudioSource>();
        bgMusicSource.clip = Phase1BgMusic;
        //bgMusicSource.Play();
        UpdateBossHPUI();
        StartCoroutine(StartNextPhase());
    }

    void Update()
    {
        if (PlayerPrefab.GetComponent<PlayerMovement>().currentHealth.runtimeValue <= 0)
        {
            // Handle player defeat
            phaseFeedbackText.text = "You have been defeated!";
            bgMusicSource.Stop();
            bgMusicSource.PlayOneShot(loseSound, 1.5f);
            winScreen.gameObject.SetActive(false);
            loseScreen.gameObject.SetActive(true);
            StartCoroutine(EndBattle());
        }
    }


    private IEnumerator StartNextPhase()
    {
        yield return new WaitForSeconds(1f);

        phaseFeedbackText.text = $"Phase {currentPhase + 1} Starting...";
        bgMusicSource.Stop();
        bgMusicSource.clip = GetPhaseBgMusic();
        bgMusicSource.Play();

        yield return new WaitForSeconds(1.5f);

        switch (currentPhase)
        {
            case 0:
                multipleChoiceGame.LaunchQuestion(mcQuestions, OnMiniGameCompleted);
                break;
            case 1:
                wordOrderGame.Launch(wordOrderQuestions, OnMiniGameCompleted);
                break;
            case 2:
                featureMatchGame.Launch(featureMatchQuestions, OnMiniGameCompleted);
                break;
            default:
                Debug.Log("Battle complete!");
                BossDefeated();
                break;
        }
    }

    private void OnMiniGameCompleted(bool success)
    {
        if (success)
        {
            // Show success feedback with color and scaling effect
            StartCoroutine(ShowPhaseSuccessEffect());
            bossCurrentHP--;
            UpdateBossHPUI();
        }
        else
        {
            phaseFeedbackText.text = "Phase Failed! Boss attacks you back!";
            // Optional: Call player.TakeDamage() if you have a player HP system
        }

        currentPhase++;

        if (bossCurrentHP > 0 && currentPhase < 3)
        {
            StartCoroutine(StartNextPhase());
        }
        else
        {
            BossDefeated();
        }
    }

    private void UpdateBossHPUI()
    {
        bossHPText.text = $"Boss HP: {bossCurrentHP}/{bossMaxHP}";
    }

    private void BossDefeated()
    {
        phaseFeedbackText.text = "Boss Defeated! Congratulations!";
        EnemyPrefab.GetComponent<FightingEnemy>().takeDamage(3); // Assuming this is how you handle boss defeat
        bgMusicSource.Stop();
        bgMusicSource.PlayOneShot(winSound, 1.5f);
        winScreen.gameObject.SetActive(true);
        loseScreen.gameObject.SetActive(false);
        StartCoroutine(EndBattle());
        // TODO: Load ending scene or show reward UI
    }

    System.Collections.IEnumerator EndBattle()
    {
        // Stop the original background music
        if (bgMusicSource.isPlaying)
        {
            bgMusicSource.Stop();
        }

        // Play the win sound
        bgMusicSource.PlayOneShot(winSound);

        // Wait until player presses the "E" key
        while (!Input.GetKeyDown(KeyCode.E))
        {
            yield return null;
        }

        SceneTracker.Instance.ReturnToPreviousScene(winScreen.gameObject.activeSelf);

    }

    private IEnumerator ShowPhaseSuccessEffect()
    {
        phaseFeedbackText.text = "Phase Passed! Boss takes damage!";
        // TODO: Add color and scaling effect
        yield return new WaitForSeconds(2f);
        phaseFeedbackText.text = "";
    }

    private AudioClip GetPhaseBgMusic()
    {
        switch (currentPhase)
        {
            case 0: return Phase1BgMusic;
            case 1: return Phase2BgMusic;
            case 2: return Phase3BgMusic;
            default: return null;
        }
    }

}
