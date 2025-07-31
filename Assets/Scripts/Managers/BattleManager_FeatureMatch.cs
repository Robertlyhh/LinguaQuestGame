using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BattleManager_FeatureMatch : MonoBehaviour
{
    [Header("Game Managers")]
    public GameObject featureMatchPanel;
    public FeatureMatchManager featureMatchManager;

    [Header("Battle Participants")]
    public GameObject playerPrefab;
    public GameObject enemyPrefab;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip correctSound;
    public AudioClip incorrectSound;
    public AudioClip winSound;
    public AudioClip loseSound;

    [Header("UI Elements")]
    public Canvas winScreen;
    public Canvas loseScreen;
    public TextMeshProUGUI UItext;
    public TextMeshProUGUI feedbackText;

    [Header("Battle Messages")]
    public string[] battleMessages = {
        "Get ready for a challenge!",
        "Can you solve this?",
        "Time to test your skills!",
        "Let's see what you've got!"
    };
    public string[] PerfectAnswerMessages = { "Great job!", "Well done!", "You nailed it!", "Excellent!" };
    public string[] GoodAnswerMessages = { "Good effort!", "Not bad!", "Keep it up!", "Solid attempt!" };
    public string[] NeutralAnswerMessages = { "That's okay!", "That's fine!", "Keep trying!", "You're on the right track!" };
    public string[] WrongAnswerMessages = { "Try again!", "Don't give up!", "You can do better!", "Keep practicing!" };

    [Header("Timings")]
    public float messageDisplayTime = 2f;
    public float messageDelay = 0.5f;
    public float turnDelay = 1.5f;

    private bool isPlayerTurn = true;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        StartCoroutine(RunBattleIntroAndStartTurn());
    }

    private IEnumerator RunBattleIntroAndStartTurn()
    {
        yield return StartCoroutine(ShowBattleMessages());
        yield return StartCoroutine(StartTurn());
    }

    private IEnumerator StartTurn()
    {
        Debug.Log("Starting turn. Is Player's turn: " + isPlayerTurn);
        yield return StartCoroutine(showTurnMessage());
        featureMatchPanel.SetActive(true);
        // Start the feature match challenge
        featureMatchManager.StartNewChallenge(this);
    }

    /// <summary>
    /// Callback method for the FeatureMatchManager to report the player's answer.
    /// </summary>
    public IEnumerator OnPlayerSubmitted(bool success, float timeRemaining, string explanation)
    {

        float damage = 0f;
        string message = "";
        float soundVolume = 1f;

        if (success)
        {
            // Logic for correct answer (damage calculation, etc.)
            // This is kept consistent with your original BattleManager
            if (timeRemaining > featureMatchManager.duration * 0.7f)
            {
                damage = 1f;
                message = PerfectAnswerMessages[Random.Range(0, PerfectAnswerMessages.Length)];
                soundVolume = 2f;
            }
            else if (timeRemaining > featureMatchManager.duration * 0.5f)
            {
                damage = 1f;
                message = GoodAnswerMessages[Random.Range(0, GoodAnswerMessages.Length)];
                soundVolume = 1.5f;
            }
            else
            {
                damage = 0.5f;
                message = NeutralAnswerMessages[Random.Range(0, NeutralAnswerMessages.Length)];
                soundVolume = 1f;
            }

            audioSource.PlayOneShot(correctSound, soundVolume);

            if (isPlayerTurn)
            {
                // Player attacks enemy
                playerPrefab.GetComponent<PlayerMovement>().AttackEnemy();
                Debug.Log("Player dealt " + damage + " damage to the enemy.");
                enemyPrefab.GetComponent<FightingEnemy>().takeDamage(damage);
                isPlayerTurn = (timeRemaining > featureMatchManager.duration * 0.8f); // Keep turn if perfect
            }
            else
            {
                // Enemy attacks player, but player might dodge
                enemyPrefab.GetComponent<FightingEnemy>().AttackPlayer();
                if (damage < 1f) playerPrefab.GetComponent<PlayerMovement>().takeDamage(damage);
                else playerPrefab.GetComponent<PlayerMovement>().Doge();
                isPlayerTurn = true; // Player always regains turn after enemy
            }
        }
        else
        {
            // Logic for incorrect answer
            message = WrongAnswerMessages[Random.Range(0, WrongAnswerMessages.Length)];
            soundVolume = isPlayerTurn ? 1.5f : 1f;
            audioSource.PlayOneShot(incorrectSound, soundVolume);

            if (!isPlayerTurn)
            {
                enemyPrefab.GetComponent<FightingEnemy>().AttackPlayer();
                playerPrefab.GetComponent<PlayerMovement>().takeDamage(1);
            }
            else
            {
                playerPrefab.GetComponent<PlayerMovement>().AttackEnemy();
                enemyPrefab.GetComponent<FightingEnemy>().Doge();
            }
            isPlayerTurn = !isPlayerTurn;
        }

        // Show explanation text and wait for player to continue
        feedbackText.gameObject.SetActive(true);
        feedbackText.text = explanation;
        while (!Input.GetKeyDown(KeyCode.E))
        {
            yield return null;
        }
        feedbackText.gameObject.SetActive(false);
        featureMatchPanel.SetActive(false);

        // Show result message and check if the battle is over
        StartCoroutine(ShowTextWithDelay(message, messageDisplayTime));
        if (CheckBattleOver())
        {
            StartCoroutine(EndBattle());
        }
        else
        {
            Debug.Log("Battle continues. Next turn: " + (isPlayerTurn ? "Player" : "Enemy"));
            //yield return new WaitForSeconds(turnDelay);
            StartCoroutine(StartTurn());
        }
    }

    private bool CheckBattleOver()
    {
        if (enemyPrefab != null && !enemyPrefab.activeInHierarchy)
        {
            Debug.Log("Player Wins!");
            winScreen.gameObject.SetActive(true);
            return true;
        }

        if (playerPrefab != null && !playerPrefab.activeInHierarchy)
        {
            Debug.Log("Player Loses!");
            loseScreen.gameObject.SetActive(true);
            return true;
        }

        return false;
    }

    private IEnumerator EndBattle()
    {
        if (audioSource.isPlaying) audioSource.Stop();

        // Play win or lose sound based on which screen is active
        if (winScreen.gameObject.activeSelf) audioSource.PlayOneShot(winSound);
        if (loseScreen.gameObject.activeSelf) audioSource.PlayOneShot(loseSound);

        while (!Input.GetKeyDown(KeyCode.E))
        {
            yield return null;
        }

        // Assuming you have a SceneTracker script like in the original
        // SceneTracker.Instance.ReturnToPreviousScene(winScreen.gameObject.activeSelf);
    }

    public IEnumerator ShowTextWithDelay(string message, float delay)
    {
        UItext.text = message;
        yield return new WaitForSeconds(delay);
        UItext.text = string.Empty;
    }

    public IEnumerator ShowBattleMessages()
    {
        foreach (string message in battleMessages)
        {
            UItext.text = message;
            audioSource.PlayOneShot(correctSound, 0.5f);
            yield return new WaitForSeconds(messageDisplayTime + messageDelay);
        }
        UItext.text = string.Empty;
    }

    public IEnumerator showTurnMessage()
    {
        UItext.text = isPlayerTurn ? "Your Turn!" : "Enemy's Turn!";
        yield return new WaitForSeconds(messageDisplayTime);
        UItext.text = string.Empty;
    }
}
