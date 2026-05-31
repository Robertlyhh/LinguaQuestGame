using System.Collections;
using TMPro;
using UnityEngine;

public class OrbitTossGameManager : MonoBehaviour
{
    [Header("Round")]
    public float roundTime = 60f;
    public int targetScore = 10;
    public int startingObjectCount = 6;
    public float respawnDelay = 0.75f;

    [Header("Scene References")]
    public GameObject[] tossablePrefabs;
    public Transform playerTransform;
    public LassoScript playerLasso;
    public Transform bottomLeftBound;
    public Transform topRightBound;

    [Header("Spawn Bias")]
    public float bottomWeight = 0.45f;
    public float leftWeight = 0.20f;
    public float rightWeight = 0.20f;
    public float topWeight = 0.15f;

    [Tooltip("Keeps spawns slightly inside the arena edges.")]
    public float edgeInset = 0.75f;

    [Tooltip("Prevents spawning too close to the player.")]
    public float minSpawnDistanceFromPlayer = 2.5f;

    [Tooltip("How high up side spawns can go. 0.65 = lower 65% of arena height.")]
    [Range(0.1f, 1f)]
    public float sideUpperLimitPercent = 0.65f;

    [Header("UI")]
    public TextMeshProUGUI scoreTMP;
    public TextMeshProUGUI timerTMP;
    public TextMeshProUGUI goalTMP;
    public TextMeshProUGUI messageTMP;

    private float timeRemaining;
    private int score;
    private bool roundActive;

    void Start()
    {
        timeRemaining = roundTime;
        score = 0;
        roundActive = true;

        RefreshUI();

        for (int i = 0; i < startingObjectCount; i++)
        {
            SpawnOne();
        }

        if (messageTMP != null)
            messageTMP.text = "";
    }

    void Update()
    {
        if (!roundActive)
            return;

        timeRemaining -= Time.deltaTime;
        if (timeRemaining < 0f)
            timeRemaining = 0f;

        RefreshUI();

        if (timeRemaining <= 0f)
        {
            EndRound(score >= targetScore);
        }
    }

    public void OnObjectScored(TossableObject obj, int points)
    {
        if (!roundActive)
            return;

        score += points;
        RefreshUI();

        if (score >= targetScore)
        {
            EndRound(true);
            return;
        }

        StartCoroutine(RespawnAfterDelay());
    }

    public void OnObjectMissed(TossableObject obj)
    {
        if (!roundActive)
            return;

        StartCoroutine(RespawnAfterDelay());
    }

    IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);

        if (roundActive)
            SpawnOne();
    }

  
    public void SpawnOne()
    {
        if (tossablePrefabs == null || tossablePrefabs.Length == 0)
            return;

        Vector2 spawnPos = GetWeightedSpawnPosition();

        GameObject prefabToSpawn = tossablePrefabs[Random.Range(0, tossablePrefabs.Length)];
        GameObject obj = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);

        TossableObject tossable = obj.GetComponent<TossableObject>();
        if (tossable != null)
        {
            tossable.Initialize(this);
        }
    }

    Vector2 GetWeightedSpawnPosition()
    {
        float xMin = Mathf.Min(bottomLeftBound.position.x, topRightBound.position.x) + edgeInset;
        float xMax = Mathf.Max(bottomLeftBound.position.x, topRightBound.position.x) - edgeInset;
        float yMin = Mathf.Min(bottomLeftBound.position.y, topRightBound.position.y) + edgeInset;
        float yMax = Mathf.Max(bottomLeftBound.position.y, topRightBound.position.y) - edgeInset;

        float sideYMax = Mathf.Lerp(yMin, yMax, sideUpperLimitPercent);

        float totalWeight = bottomWeight + leftWeight + rightWeight + topWeight;

        for (int attempt = 0; attempt < 20; attempt++)
        {
            float pick = Random.Range(0f, totalWeight);
            Vector2 pos;

            if (pick < bottomWeight)
            {
                pos = new Vector2(Random.Range(xMin, xMax), yMin);
            }
            else if (pick < bottomWeight + leftWeight)
            {
                pos = new Vector2(xMin, Random.Range(yMin, sideYMax));
            }
            else if (pick < bottomWeight + leftWeight + rightWeight)
            {
                pos = new Vector2(xMax, Random.Range(yMin, sideYMax));
            }
            else
            {
                pos = new Vector2(Random.Range(xMin, xMax), yMax);
            }

            if (playerTransform == null)
                return pos;

            if (Vector2.Distance(pos, playerTransform.position) >= minSpawnDistanceFromPlayer)
                return pos;
        }

        return new Vector2((xMin + xMax) * 0.5f, yMin);
    }

    public bool IsInsidePlayArea(Vector2 pos, float padding = 0f)
    {
        float xMin = Mathf.Min(bottomLeftBound.position.x, topRightBound.position.x) - padding;
        float xMax = Mathf.Max(bottomLeftBound.position.x, topRightBound.position.x) + padding;
        float yMin = Mathf.Min(bottomLeftBound.position.y, topRightBound.position.y) - padding;
        float yMax = Mathf.Max(bottomLeftBound.position.y, topRightBound.position.y) + padding;

        return pos.x >= xMin && pos.x <= xMax && pos.y >= yMin && pos.y <= yMax;
    }

    void RefreshUI()
    {
        if (scoreTMP != null)
            scoreTMP.text = $"Score: {score}";

        if (goalTMP != null)
            goalTMP.text = $"Goal: {targetScore}";

        if (timerTMP != null)
            timerTMP.text = $"Time: {Mathf.CeilToInt(timeRemaining)}";
    }

    void EndRound(bool win)
    {
        roundActive = false;

        if (playerLasso != null)
            playerLasso.enabled = false;

        if (messageTMP != null)
            messageTMP.text = win ? "You Win!" : "Time Up!";
    }
}