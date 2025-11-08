using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class SwordWaveManager : MonoBehaviour
{
    public static SwordWaveManager Instance;

    [Header("Data Sources")]
    [SerializeField] private SentenceRuntimeBank runtimeBank; // assign ScriptableObject
    [SerializeField] private string filterTopic = null;       // e.g. "word_classes"
    [SerializeField] private int minDifficulty = 1;
    [SerializeField] private int maxDifficulty = 4;
    [SerializeField] private int sentencesPerRound = 5;

    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI sentenceTMP;
    [SerializeField] private TextMeshProUGUI scoreTMP;
    [SerializeField] private TextMeshProUGUI energyTMP;
    [SerializeField] private TextMeshProUGUI healthTMP;

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
    private float _energyAccumulator; // Store fractional energy

    // round state
    private List<SentenceData> _roundQueue = new();
    private int _currentIndex = -1;
    private SentenceSelector _selector;
    private static int _score;

    public static SentenceData CurrentSentence { get; private set; }

    void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _score = 0;
        _timeSinceLastEnergyUse = 0f;
        _energyAccumulator = 0f;
    }

    void Start()
    {
        // Load JSON → runtime list
        runtimeBank.LoadAll();
        _selector = new SentenceSelector(runtimeBank.sentences);

        // Build a round queue
        _roundQueue = _selector.PickSet(sentencesPerRound, filterTopic, minDifficulty, maxDifficulty, avoidRepeats: true);
        NextSentence();
        RefreshUI();
    }

    void Update()
    {
        // Natural energy regeneration
        if (energy < maxEnergy)
        {
            _timeSinceLastEnergyUse += Time.deltaTime;

            // Only regenerate after delay period
            if (_timeSinceLastEnergyUse >= energyRegenDelay)
            {
                // Accumulate fractional energy
                _energyAccumulator += energyRegenRate * Time.deltaTime;

                // Only add full integer points to energy
                int energyToAdd = Mathf.FloorToInt(_energyAccumulator);

                if (energyToAdd > 0)
                {
                    _energyAccumulator -= energyToAdd; // Remove the integer part
                    energy = Mathf.Min(maxEnergy, energy + energyToAdd);
                    RefreshUI();
                }
            }
        }
        else
        {
            // Reset accumulator when at max
            _energyAccumulator = 0f;
        }
    }

    public void NextSentence()
    {
        _currentIndex++;
        if (_currentIndex >= _roundQueue.Count)
        {
            // Round finished: you can show summary, or loop/reset here
            _currentIndex = 0;
        }

        CurrentSentence = _roundQueue[_currentIndex];
        if (sentenceTMP) sentenceTMP.text = CurrentSentence ? CurrentSentence.sentence : "";
        // Notify spawners (simple approach: broadcast)
        SendMessage("OnNewSentence", CurrentSentence, SendMessageOptions.DontRequireReceiver);
    }

    // Energy management
    public bool HasEnergy(int cost) => energy >= cost;

    public void UseEnergy(int amount)
    {
        energy = Mathf.Max(0, energy - amount);
        _timeSinceLastEnergyUse = 0f; // Reset regen timer
        _energyAccumulator = 0f; // Reset accumulator
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
        if (scoreTMP) scoreTMP.text = $"Score: {_score}";
        if (energyTMP) energyTMP.text = $"Energy: {energy}/{maxEnergy}";
        if (healthTMP) healthTMP.text = $"Health: {health}";
    }

    private void GameOver()
    {
        Debug.Log("Game Over!");
        // Add game over logic here (show UI, restart, etc.)
    }
}