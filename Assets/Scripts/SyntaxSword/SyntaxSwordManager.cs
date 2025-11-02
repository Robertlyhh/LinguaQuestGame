// Assets/Scripts/Core/GameManager.cs
using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class SyntaxSwordGameManager : MonoBehaviour
{
    public static SyntaxSwordGameManager Instance;

    [Header("Data Sources")]
    [SerializeField] private SentenceRuntimeBank runtimeBank; // assign ScriptableObject
    [SerializeField] private string filterTopic = null;       // e.g. "word_classes"
    [SerializeField] private int minDifficulty = 1;
    [SerializeField] private int maxDifficulty = 4;
    [SerializeField] private int sentencesPerRound = 5;

    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI sentenceTMP;
    [SerializeField] private TextMeshProUGUI scoreTMP;

    // round state
    private List<SentenceData> _roundQueue = new();
    private int _currentIndex = -1;
    private SentenceSelector _selector;
    private static int _score;

    public static SentenceData CurrentSentence { get; private set; }

    void Awake()
    {
        Instance = this;
        _score = 0;
    }

    void Start()
    {
        // Load JSON → runtime list
        runtimeBank.LoadAll();
        _selector = new SentenceSelector(runtimeBank.sentences);

        // Build a round queue
        _roundQueue = _selector.PickSet(sentencesPerRound, filterTopic, minDifficulty, maxDifficulty, avoidRepeats: true);
        NextSentence();
        RefreshScoreUI();
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

    public static void TryAddScore(int delta)
    {
        _score += delta;
        Instance?.RefreshScoreUI();
    }

    private void RefreshScoreUI()
    {
        if (scoreTMP) scoreTMP.text = $"Score: {_score}";
    }
}
