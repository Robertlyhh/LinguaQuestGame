using System.Collections;
using UnityEngine;

public class WordSpawnerPoint : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject wordBlockPrefab;

    [Header("Spawn Point (scene transform)")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("Vertical spacing between spawned blocks to prevent overlap")]
    [SerializeField] private float verticalSpacing = 0.5f;

    [Header("Initial Launch (slower, goes left)")]

    [Tooltip("Negative X speeds so blocks start moving left.")]
    [SerializeField] private Vector2 leftSpeedRange = new Vector2(-1.5f, -0.6f);

    [Tooltip("Small upward lift so blocks drift a bit.")]
    [SerializeField] private Vector2 upSpeedRange = new Vector2(0.1f, 0.3f);

    [Header("Cadence (less frequent)")]
    [SerializeField] private float spawnInterval = 1.5f;
    [SerializeField] private int blocksPerWave = 1;

    private SentenceData _active;
    private float _lastSpawnY;

    public void startgame()
    {
        _lastSpawnY = spawnPoint ? spawnPoint.position.y : 0f;
        OnNewSentence(SwordWaveManager.CurrentSentence);
        StartCoroutine(SpawnLoop());
    }

    void OnNewSentence(object obj)
    {
        _active = obj as SentenceData;

        if (_active != null)
        {
            Debug.Log($"[WordSpawner] Received new sentence: {_active.sentence}");
        }
        else
        {
            Debug.LogWarning("[WordSpawner] Received null sentence data!");
        }
    }

    private IEnumerator SpawnLoop()
    {
        var wait = new WaitForSeconds(spawnInterval);
        while (true)
        {
            if (_active != null && _active.entries != null && _active.entries.Count > 0 && spawnPoint != null)
            {
                for (int i = 0; i < blocksPerWave; i++)
                    SpawnOneRandomEntry();
            }

            yield return wait;
        }
    }

    private void SpawnOneRandomEntry()
    {
        if (_active == null || _active.entries == null || _active.entries.Count == 0)
        {
            return;
        }

        var e = _active.entries[Random.Range(0, _active.entries.Count)];

        // Spawn with vertical spacing to prevent overlap
        Vector3 basePos = spawnPoint.position;
        _lastSpawnY += verticalSpacing;

        var pos = new Vector3(
            basePos.x,
            _lastSpawnY,
            0f
        );

        // Reset Y position if it goes too high
        if (_lastSpawnY > basePos.y + 10f)
        {
            _lastSpawnY = basePos.y;
        }

        var wb = Instantiate(wordBlockPrefab, pos, Quaternion.identity);

        var wordBlock = wb.GetComponent<WordBlock>();
        if (wordBlock != null)
        {
            wordBlock.Initialize(e.word, e.shownLabel, e.isLabelCorrect);
            Debug.Log($"[WordSpawner] Spawned block: {e.word} ({e.shownLabel})");
        }
        else
        {
            Debug.LogError("[WordSpawner] wordBlockPrefab is missing WordBlock component!");
        }

        var rb = wb.GetComponent<Rigidbody2D>();
        if (rb)
        {
            // NO ROTATION - freeze it
            rb.freezeRotation = true;
            rb.angularVelocity = 0f;

            // Set velocity (left and slightly up)
            float vx = Random.Range(leftSpeedRange.x, leftSpeedRange.y);
            float vy = Random.Range(upSpeedRange.x, upSpeedRange.y);
            rb.velocity = new Vector2(vx, vy);
        }
        else
        {
            Debug.LogWarning("[WordSpawner] wordBlockPrefab is missing Rigidbody2D component!");
        }
    }

    [ContextMenu("Spawn One (Test)")]
    private void SpawnTest()
    {
        if (_active == null || _active.entries == null || _active.entries.Count == 0 || spawnPoint == null)
        {
            Debug.LogWarning("[WordSpawner] Cannot spawn test: missing sentence entries or spawnPoint.");
            return;
        }
        SpawnOneRandomEntry();
    }
}