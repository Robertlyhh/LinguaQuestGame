// Assets/Scripts/Gameplay/WordSpawner.cs
using System.Collections;
using UnityEngine;

public class WordSpawner : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private WordBlock wordBlockPrefab;

    [Header("Spawn Region")]
    [SerializeField] private float xMin = -6f, xMax = 6f;
    [SerializeField] private float ySpawn = -5.5f, yVariance = 0.2f;

    [Header("Launch")]
    [SerializeField] private Vector2 upSpeedRange = new Vector2(2f, 5f);
    [SerializeField] private Vector2 sideSpeedRange = new Vector2(-2f, 2f);
    [SerializeField] private Vector2 torqueRange = new Vector2(-120f, 120f);

    [Header("Cadence")]
    [SerializeField] private float spawnInterval = 0.9f;
    [SerializeField] private int blocksPerWave = 1;

    private SentenceData _active;

    void Start()
    {
        // If GameManager already set a sentence before Start, take it now
        OnNewSentence(SyntaxSwordGameManager.CurrentSentence);
        StartCoroutine(SpawnLoop());
    }

    // Called by GameManager via SendMessage when a new sentence is selected
    void OnNewSentence(object obj)
    {
        _active = obj as SentenceData;
    }

    private IEnumerator SpawnLoop()
    {
        var wait = new WaitForSeconds(spawnInterval);
        while (true)
        {
            if (_active != null && _active.entries != null && _active.entries.Count > 0)
            {
                for (int i = 0; i < blocksPerWave; i++)
                    SpawnOneRandomEntry();
            }
            yield return wait;
        }
    }

    private void SpawnOneRandomEntry()
    {
        var e = _active.entries[Random.Range(0, _active.entries.Count)];
        var pos = new Vector3(Random.Range(xMin, xMax), ySpawn + Random.Range(-yVariance, yVariance), 0f);
        var wb = Instantiate(wordBlockPrefab, pos, Quaternion.identity);
        wb.Initialize(e.word, e.shownLabel, e.isLabelCorrect);

        var rb = wb.GetComponent<Rigidbody2D>();
        if (rb)
        {
            rb.velocity = new Vector2(Random.Range(sideSpeedRange.x, sideSpeedRange.y),
                                      Random.Range(upSpeedRange.x, upSpeedRange.y));
            rb.angularVelocity = Random.Range(torqueRange.x, torqueRange.y);
        }
    }
}
