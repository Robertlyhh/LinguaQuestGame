using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleManager_TorF : MonoBehaviour
{
    public GameObject TorFPanel;
    public TorFManager torFManager;

    public GameObject playerPrefab;
    public GameObject enemyPrefab;
    public AudioSource audioSource;
    public AudioClip correctSound;
    public AudioClip incorrectSound;
    public AudioClip winSound;
    public AudioClip loseSound;
    public Canvas winScreen;
    public Canvas loseScreen;
    public TextMeshProUGUI UItext;
    public TextMeshProUGUI feedbackText;
    public float messageDisplayTime = 2f; // Time to display each message
    public string[] battleMessages = {
        "Get ready for a True or False challenge!",
        "Can you solve this?",
        "Time to test your skills!",
        "Let's see what you've got!",
        "Prepare for a True or False showdown!"
    };

    public float turnDelay = 1.5f;
    private bool isPlayerTurn = true;

    public float battleDuration = 30f; // Total duration for the battle
    public string[] PerfectAnswerMessages = {
        "Great job!",
        "Well done!",
        "Nice work!",
        "You nailed it!",
        "Excellent!"
    };
    public string[] GoodAnswerMessages = {
        "Good effort!",
        "Not bad!",
        "Keep it up!",
        "You're getting there!",
        "Solid attempt!"
    };

    public string[] NeutralAnswerMessages = {
        "That's okay!",
        "That's fine!",
        "Keep trying!",
        "You're on the right track!",
        "Don't worry, you'll improve!"
    };
    public string[] WrongAnswerMessages = {
        "Try again!",
        "Don't give up!",
        "You can do better!",
        "Keep practicing!",
        "Almost there!"
    };

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        StartCoroutine(ShowStartAndBegin());
    }

    IEnumerator ShowStartAndBegin()
    {
        UItext.text = "True or False Battle!";
        yield return new WaitForSeconds(1.5f);
        UItext.text = "";
        StartCoroutine(StartTurn());
    }

    IEnumerator StartTurn()
    {
        UItext.text = isPlayerTurn ? "Your Turn!" : "Enemy's Turn!";
        yield return new WaitForSeconds(1.5f);
        UItext.text = "";
        TorFPanel.SetActive(true);
        torFManager.StartNewQuestion(this);
    }

    public IEnumerator OnPlayerSubmitted(bool success, float timeRemaining, string explanation)
    {
        float damage = 0f;
        string message = "";
        float soundVolume = 1f;

        if (success)
        {
            damage = 0f;
            message = "";
            soundVolume = 1f;

            if (timeRemaining > 0.7f * battleDuration)
            {
                damage = 1f;
                message = PerfectAnswerMessages[Random.Range(0, PerfectAnswerMessages.Length)];
                soundVolume = 2f;
            }
            else if (timeRemaining > 0.5f * battleDuration)
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
                playerPrefab.GetComponent<PlayerMovement>().AttackEnemy();
                Debug.Log("Player dealt " + damage + " damage to the enemy.");
                enemyPrefab.GetComponent<FightingEnemy>().takeDamage(damage);
                isPlayerTurn = (timeRemaining > 0.8f * battleDuration); // Only keep turn if perfect
            }
            else
            {
                enemyPrefab.GetComponent<FightingEnemy>().AttackPlayer();
                if (damage < 1f)
                {
                    playerPrefab.GetComponent<PlayerMovement>().takeDamage(damage);
                }
                else
                {
                    enemyPrefab.GetComponent<FightingEnemy>().takeDamage(damage);
                    playerPrefab.GetComponent<PlayerMovement>().Doge();
                }

                isPlayerTurn = true; // Player regains turn after enemy
            }

        }
        else
        {
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

        feedbackText.gameObject.SetActive(true);
        feedbackText.text = explanation;
        // Wait until player presses 'E' key
        while (!Input.GetKeyDown(KeyCode.E))
        {
            yield return null;
        }
        feedbackText.text = ""; // Clear feedback after 'E' is pressed
        feedbackText.gameObject.SetActive(false);
        TorFPanel.SetActive(false);
        Debug.Log("Player submitted answer: " + (success ? "Correct" : "Incorrect"));




        StartCoroutine(ShowTextWithDelay(message, messageDisplayTime));
        if (CheckBattleOver())
        {
            StartCoroutine(EndBattle());
        }
        else
        {
            Debug.Log("Battle continues. Next turn: " + (isPlayerTurn ? "Player" : "Enemy"));
            StartCoroutine(StartTurn());
        }


    }

    bool CheckBattleOver()
    {
        if (!enemyPrefab.activeInHierarchy)
        {
            winScreen.gameObject.SetActive(true);
            audioSource.PlayOneShot(winSound);
            return true;
        }

        if (!playerPrefab.activeInHierarchy)
        {
            loseScreen.gameObject.SetActive(true);
            audioSource.PlayOneShot(loseSound);
            return true;
        }

        return false;
    }

    IEnumerator EndBattle()
    {
        audioSource.Stop();
        yield return new WaitForSeconds(1f);
        while (!Input.GetKeyDown(KeyCode.E)) yield return null;
        SceneTracker.Instance.ReturnToPreviousScene(winScreen.gameObject.activeSelf);
    }

    public IEnumerator ShowTextWithDelay(string message, float delay)
    {

        UItext.text = message;

        yield return new WaitForSeconds(delay);

        UItext.text = string.Empty; // Clear the text after the delay
    }
}
