using UnityEngine;
using TMPro;
using System.Collections.Generic;
using JetBrains.Annotations;
using System.Runtime.CompilerServices;

public class SwordWaveManager : MonoBehaviour
{
    public static SwordWaveManager Instance;

    [Header("Data Sources")]
    [SerializeField] private SentenceRuntimeBank runtimeBank;
    [SerializeField] private string filterTopic = null;
    [SerializeField] private int minDifficulty = 1;
    [SerializeField] private int maxDifficulty = 4;
    [SerializeField] private int sentencesPerRound = 10;

    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI sentenceTMP;
    [SerializeField] private TextMeshProUGUI scoreTMP;
    [SerializeField] private TextMeshProUGUI testoverTMP;
    [SerializeField] private TextMeshProUGUI healthTMP;
    [SerializeField] private TextMeshProUGUI progressTMP;
    [SerializeField] private TextMeshProUGUI timerTMP;

    [Header("Win/Lose UI")]
    [SerializeField] private GameObject winUI;
    [SerializeField] private GameObject perfectWinUI;
    [SerializeField] private GameObject loseUI;
    [SerializeField] private TextMeshProUGUI finalScoreTMP;
    [SerializeField] private TextMeshProUGUI perfectScoreTMP;
    [SerializeField] private TextMeshProUGUI loseScoreTMP;
    [SerializeField] private TextMeshProUGUI nextStepTMP;
    [SerializeField]
    private List<string> endMessages = new List<string>()
    {
        "This is the end of our game test.",
        "Press Q to quit the whole game.",
        "Press E to go back to the main world and keep exploring.",
        "Press R to restart the whole game from start.",
    };
    [SerializeField] private AudioClip winSound;
    [SerializeField] private AudioClip loseSound;

    [Header("Stats")]
    public int energy = 100;
    public int maxEnergy = 100;
    public int health = 3;

    [Header("Energy Recovery")]
    [Tooltip("Energy points recovered per second")]
    [SerializeField] private float energyRegenRate = 5f;
    [Tooltip("Delay in seconds before energy starts regenerating after use")]
    [SerializeField] private float energyRegenDelay = 1f;
    private float _timeSinceLastEnergyUse;
    private float _energyAccumulator;

    [Header("Win/Lose Conditions")]
    [Tooltip("Score needed to win the game")]
    [SerializeField] private int winScore = 1000;
    [Tooltip("Score needed for a perfect win result")]
    [SerializeField] private int perfectWinScore = 1500;
    [Tooltip("Time limit in seconds (0 = no limit)")]
    [SerializeField] private float timeLimit = 60f; // 3 minutes
    private float _timeRemaining;
    private bool _gameEnded = false;
    private bool _lastOutcomeWin = false;

    [Header("Block Tracking")]
    private int _totalBlocksInSentence;
    private int _blocksCleared;
    private int _totalBlocksCleared = 0; // Total across all sentences
    private int _totalBlocksSpawned = 0; // Total blocks spawned
    private int _incorrectCuts = 0; // Track mistakes

    [Header("Game Flow")]
    private bool _gameStarted = false;
    public GameObject helpPanel;
    // round state
    private List<SentenceData> _roundQueue = new();
    private int _currentIndex = -1;
    private SentenceSelector _selector;
    private static int _score;

    public static SentenceData CurrentSentence { get; private set; }
    public AudioSource bgmSource;
    public AudioClip bgmClip;
    public bool IsPaused;
    private static readonly Color GoldColor = new Color(1f, 0.84f, 0f);
    public Inventory playerInventory;
    public Item completedItem;

    void Awake()
    {
        string playerName = GameManager.Instance.playerData.getPlayerName();
        if (playerName == null || playerName.Trim() == "")
        {
            playerName = "Player";
        }
        endMessages.Add("Thank you for playing, " + playerName + "!");
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _score = 0;
        _timeSinceLastEnergyUse = 0f;
        _energyAccumulator = 0f;
        _gameStarted = false;
        _gameEnded = false;
        _timeRemaining = timeLimit;
        _incorrectCuts = 0;
        _totalBlocksCleared = 0;
        _totalBlocksSpawned = 0;
        perfectWinScore = Mathf.Max(perfectWinScore, winScore);

        // Hide all UI panels at start
        if (winUI) winUI.SetActive(false);
        if (perfectWinUI) perfectWinUI.SetActive(false);
        if (loseUI) loseUI.SetActive(false);
    }

    // ---------------------------------------------------------
    // CHANGE 1: Update the Start Method
    // ---------------------------------------------------------
    void Start()
    {
        bgmSource = GetComponent<AudioSource>();
        if (bgmSource && bgmClip)
        {
            bgmSource.clip = bgmClip;
            bgmSource.loop = true;
            bgmSource.Play();
        }

        // OLD WAY (Crashes WebGL):
        // runtimeBank.LoadAll();
        // ... logic ...

        // NEW WAY (Web Safe):
        // We tell the bank: "Start downloading, and when you are done, run 'OnDataLoaded'"
        StartCoroutine(runtimeBank.LoadAllCoroutine(OnDataLoaded));
    }

    // ---------------------------------------------------------
    // CHANGE 2: Create this new Callback Method
    // ---------------------------------------------------------
    private void OnDataLoaded()
    {
        Debug.Log("[SwordWaveManager] Data download complete. Initializing Game Logic...");

        // This logic is moved here because we can't do it until the JSON arrives
        _selector = new SentenceSelector(runtimeBank.sentences);

        _roundQueue = _selector.PickSet(sentencesPerRound, filterTopic, minDifficulty, maxDifficulty, avoidRepeats: true);

        Debug.Log($"[SwordWaveManager] Loaded {_roundQueue.Count} sentences.");

        // Count total blocks for perfect win calculation
        foreach (var sentence in _roundQueue)
        {
            if (sentence.entries != null)
            {
                _totalBlocksSpawned += sentence.entries.Count;
            }
        }

        Debug.Log($"[SwordWaveManager] Total blocks in game: {_totalBlocksSpawned}");

        // Debug: Print all sentences loaded
        for (int i = 0; i < _roundQueue.Count; i++)
        {
            Debug.Log($"  Sentence {i + 1}: {_roundQueue[i].sentence} ({_roundQueue[i].entries?.Count ?? 0} entries)");
        }

        RefreshUI();

        // Optional: If you want the game to start immediately after loading, call NextSentence() here.
        // If you are waiting for the Beaver, do nothing and let OnBeaverFinished() handle it.
    }

    // Called from editor event (beaver bubble finished)
    public void OnBeaverFinished()
    {
        // Safety Check: Is the round queue empty?
        if (_roundQueue == null || _roundQueue.Count == 0)
        {
            Debug.LogWarning("[SwordWaveManager] Beaver finished, but data is still loading! Waiting...");
            // Optionally: Start a Coroutine here to check again in 0.5 seconds
            return;
        }

        Debug.Log("[SwordWaveManager] Beaver finished talking! Starting game...");
        _gameStarted = true;
        _timeRemaining = timeLimit;
        NextSentence();
    }

    void Update()
    {
        if (_gameEnded)
        {
            HandleEndInputs();
            return;
        }

        if (!_gameStarted) return;

        // Countdown timer
        if (timeLimit > 0)
        {
            _timeRemaining -= Time.deltaTime;

            if (_timeRemaining <= 0)
            {
                _timeRemaining = 0;
                TimeUp();
            }

            RefreshUI();
        }

        // Energy regeneration
        if (energy < maxEnergy)
        {
            _timeSinceLastEnergyUse += Time.deltaTime;

            if (_timeSinceLastEnergyUse >= energyRegenDelay)
            {
                _energyAccumulator += energyRegenRate * Time.deltaTime;
                int energyToAdd = Mathf.FloorToInt(_energyAccumulator);

                if (energyToAdd > 0)
                {
                    _energyAccumulator -= energyToAdd;
                    energy = Mathf.Min(maxEnergy, energy + energyToAdd);
                    RefreshUI();
                }
            }
        }
        else
        {
            _energyAccumulator = 0f;
        }

        // Debug: Press N to manually go to next sentence
        if (Input.GetKeyDown(KeyCode.N))
        {
            Debug.Log("[SwordWaveManager] Manual skip to next sentence (N key)");
            NextSentence();
        }
    }

    public void NextSentence()
    {
        if (!_gameStarted)
        {
            Debug.LogWarning("[SwordWaveManager] Game not started yet!");
            return;
        }

        if (_gameEnded)
        {
            Debug.LogWarning("[SwordWaveManager] Game already ended!");
            return;
        }

        _currentIndex++;

        if (_currentIndex >= _roundQueue.Count)
        {
            _currentIndex = 0;
            Debug.Log("[SwordWaveManager] All sentences completed! Looping back to start.");
        }

        CurrentSentence = _roundQueue[_currentIndex];

        Debug.Log($"[SwordWaveManager] === SWITCHING TO SENTENCE {_currentIndex + 1}/{_roundQueue.Count} ===");
        Debug.Log($"[SwordWaveManager] Sentence: {CurrentSentence?.sentence}");

        if (sentenceTMP) sentenceTMP.text = CurrentSentence != null ? CurrentSentence.sentence : "";

        // Reset block tracking for new sentence
        _blocksCleared = 0;
        _totalBlocksInSentence = 0;

        if (CurrentSentence != null && CurrentSentence.entries != null)
        {
            _totalBlocksInSentence = CurrentSentence.entries.Count;

            Debug.Log($"[SwordWaveManager] Total blocks in sentence: {_totalBlocksInSentence}");
        }

        // Notify spawners
        BroadcastNewSentence();

        RefreshUI();
    }

    private void BroadcastNewSentence()
    {
        WordSpawnerPoint[] spawners = FindFirstObjectByType<WordSpawnerPoint>().GetComponentsInChildren<WordSpawnerPoint>();
        Debug.Log($"[SwordWaveManager] Broadcasting to {spawners.Length} spawners");

        foreach (var spawner in spawners)
        {
            spawner.SendMessage("OnNewSentence", CurrentSentence, SendMessageOptions.DontRequireReceiver);
        }
    }

    public void OnBlockClearedCorrectly()
    {
        _blocksCleared++;
        _totalBlocksCleared++;

        Debug.Log($"[SwordWaveManager] ✓ Block cleared correctly! Progress: {_blocksCleared}/{_totalBlocksInSentence} (Total: {_totalBlocksCleared}/{_totalBlocksSpawned})");

        RefreshUI();
    }

    public void OnBlockClearedIncorrectly(string word, string label, bool isCorrect)
    {
        _incorrectCuts++;
        Debug.Log($"[SwordWaveManager] ✗ Block cleared incorrectly: {word} ({label}) - Total mistakes: {_incorrectCuts}");

        // Tell spawner to requeue this block
        WordSpawnerPoint spawner = FindFirstObjectByType<WordSpawnerPoint>();
        if (spawner != null)
        {
            spawner.RequeueBlock(word, label, isCorrect);
        }
    }

    public void OnAllBlocksCleared()
    {
        Debug.Log("[SwordWaveManager] 🎉 ALL BLOCKS CLEARED! Moving to next sentence in 1.5s...");
        Invoke(nameof(NextSentence), 1f);
    }

    private void TimeUp()
    {
        if (_gameEnded) return;

        Debug.Log("[SwordWaveManager] ⏰ TIME UP!");

        ResolveOutcomeFromScore();
    }

    private void ResolveOutcomeFromScore()
    {
        playerInventory.AddItem(completedItem);
        if (_score >= perfectWinScore)
        {
            Debug.Log("[SwordWaveManager] Perfect win threshold reached at time up.");
            PerfectWin();
        }
        else if (_score >= winScore)
        {
            Debug.Log("[SwordWaveManager] Win threshold reached at time up.");
            Win();
        }
        else
        {
            Debug.Log("[SwordWaveManager] Score below win threshold at time up.");
            Lose();
        }
    }

    private void Win()
    {
        _gameEnded = true;
        _gameStarted = false;

        Debug.Log($"[SwordWaveManager] 🏆 VICTORY! Final Score: {_score}, Mistakes: {_incorrectCuts}");

        if (winUI) winUI.SetActive(true);
        if (winSound) bgmSource.Stop();
        if (winSound) AudioSource.PlayClipAtPoint(winSound, Camera.main.transform.position);
        if (finalScoreTMP) finalScoreTMP.text = $"Final Score: {_score}\nMistakes: {_incorrectCuts}";

        Time.timeScale = 0f; // Pause game
        _lastOutcomeWin = true;
        ShowEndMessages();
    }

    private void PerfectWin()
    {
        _gameEnded = true;
        _gameStarted = false;

        Debug.Log($"[SwordWaveManager] ✨ PERFECT VICTORY! Final Score: {_score}, No Mistakes!");

        if (perfectWinUI) perfectWinUI.SetActive(true);
        if (winSound) bgmSource.Stop();
        if (winSound) AudioSource.PlayClipAtPoint(winSound, Camera.main.transform.position);
        if (perfectScoreTMP) perfectScoreTMP.text = $"Perfect Score: {_score}\nFlawless Victory!";

        Time.timeScale = 0f; // Pause game
        _lastOutcomeWin = true;
        ShowEndMessages();
    }

    private void Lose()
    {
        _gameEnded = true;
        _gameStarted = false;

        Debug.Log($"[SwordWaveManager] 💀 DEFEAT! Final Score: {_score}");

        if (loseUI) loseUI.SetActive(true);
        if (bgmSource && loseSound) bgmSource.Stop();
        if (loseSound) AudioSource.PlayClipAtPoint(loseSound, Camera.main.transform.position);
        if (loseScoreTMP) loseScoreTMP.text = $"Final Score: {_score}\nTime's Up!";

        Time.timeScale = 0f; // Pause game
        _lastOutcomeWin = false;
        ShowEndMessages();
    }

    private void GameOver()
    {
        if (_gameEnded) return;

        Debug.Log("[SwordWaveManager] 💀 GAME OVER! Health depleted!");
        Lose();
    }

    // Energy management
    public bool HasEnergy(int cost) => energy >= cost;

    public void UseEnergy(int amount)
    {
        energy = Mathf.Max(0, energy - amount);
        _timeSinceLastEnergyUse = 0f;
        _energyAccumulator = 0f;
        RefreshUI();
    }

    public void RestoreEnergy(int amount)
    {
        energy = Mathf.Min(maxEnergy, energy + amount);
        RefreshUI();
    }

    // Health management
    public void TakeDamage(int amount)
    {
        health = Mathf.Max(0, health - amount);
        RefreshUI();
        if (health <= 0)
        {
            GameOver();
        }
    }

    public void RestoreHealth(int amount)
    {
        health += amount;
        RefreshUI();
    }

    // Score management
    public static void TryAddScore(int delta)
    {
        _score += delta;
        Instance?.RefreshUI();
    }

    private void RefreshUI()
    {
        if (scoreTMP) scoreTMP.text = $"Score: {_score}/{winScore}";
        //if (energyTMP) energyTMP.text = $"Energy: {energy}/{maxEnergy}";
        if (healthTMP) healthTMP.text = $"Health: {health}";

        UpdateScoreColor();
        // Timer display
        if (timerTMP && timeLimit > 0)
        {
            int minutes = Mathf.FloorToInt(_timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(_timeRemaining % 60f);
            timerTMP.text = $"Time: {minutes:00}:{seconds:00}";

            UpdateTimerColor();
        }

        // Show queue count instead of blocks cleared
        WordSpawnerPoint spawner = FindFirstObjectByType<WordSpawnerPoint>();
        int queueCount = spawner != null ? spawner.GetQueueCount() : 0;
        if (progressTMP) progressTMP.text = $"Remaining: {queueCount} | Total: {_totalBlocksCleared}/{_totalBlocksSpawned}";
    }

    private void UpdateTimerColor()
    {
        float t = Mathf.InverseLerp(60f, 30f, _timeRemaining); // 60 -> yellow, 30 -> red
        Color target = Color.Lerp(Color.yellow, Color.red, t);

        // Work on an instance so other labels aren't affected
        var mat = timerTMP.fontMaterial; // this clones the shared material at runtime
        mat.SetColor(ShaderUtilities.ID_FaceColor, target);
        timerTMP.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    private void UpdateScoreColor()
    {
        if (scoreTMP == null) return;

        Color target;
        if (_score >= 0)
        {
            float t = Mathf.Clamp01(_score / 1500f); // 0 -> 1500 maps white -> gold
            target = Color.Lerp(Color.white, GoldColor, t);
        }
        else
        {
            // Below 0, fade from white toward red as it goes negative
            float t = Mathf.Clamp01(-_score / 500f); // adjust 500f to control how fast it reddens
            target = Color.Lerp(Color.white, Color.red, t);
        }

        var mat = scoreTMP.fontMaterial; // instance material so other TMPs aren’t affected
        mat.SetColor(TMPro.ShaderUtilities.ID_FaceColor, target);
        scoreTMP.UpdateVertexData(TMPro.TMP_VertexDataUpdateFlags.Colors32);
    }

    // Public methods for UI buttons
    public void RestartGame()
    {
        Time.timeScale = 1f; // Resume time
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }

    public void QuitToMainMenu()
    {
        Time.timeScale = 1f; // Resume time
        // Load your main menu scene
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    private void HandleEndInputs()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            QuitGame();
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            ReturnToWorld();
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }
    }

    private void ShowEndMessages()
    {
        if (nextStepTMP == null || endMessages == null || endMessages.Count == 0) return;
        nextStepTMP.gameObject.SetActive(true);
        nextStepTMP.text = string.Join("\n", endMessages);
    }

    private void ReturnToWorld()
    {
        Time.timeScale = 1f;
        if (SceneTracker.Instance != null)
        {
            SceneTracker.Instance.ReturnToPreviousScene(_lastOutcomeWin);
        }
        else
        {
            Debug.LogWarning("SceneTracker.Instance not found; cannot return to previous scene.");
        }
    }

    private void QuitGame()
    {
        Debug.Log("[SwordWaveManager] Quitting game.");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // Public getter for game state
    public bool IsGameStarted() => _gameStarted;
    public bool IsGameEnded() => _gameEnded;
    public float GetTimeRemaining() => _timeRemaining;
    public void ShowHelpPanel()
    {
        if (helpPanel != null)
        {
            helpPanel.SetActive(true);
            IsPaused = true;
            Time.timeScale = 0f; // Pause game

        }
    }

    public void HideHelpPanel()
    {
        if (helpPanel != null)
        {
            helpPanel.SetActive(false);
            IsPaused = false;
            Time.timeScale = 1f; // Resume game
        }
    }
}