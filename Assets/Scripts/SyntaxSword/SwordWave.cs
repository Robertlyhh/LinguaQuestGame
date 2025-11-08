using UnityEngine;

/// <summary>
/// Jian Qi sword wave projectile. Fired toward the mouse direction.
/// Explodes WordBlocks on contact, rewards score/energy.
/// </summary>
public class SwordWave : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 12f;
    public float lifetime = 2f;
    private Vector2 direction;
    private float timer;

    [Header("Wave Type")]
    [Tooltip("True = Wave 1 (hits correct labels), False = Wave 2 (hits incorrect labels)")]
    public bool isCorrectWave = true;

    [Header("Damage / Energy")]
    public int energyCost = 10;
    public int scoreReward = 50;
    public int energyReward = 15;

    [Header("Area Effect")]
    public float explosionRadius = 2f; // Radius to explode nearby blocks
    public LayerMask wordBlockLayer; // Set to layer that WordBlocks are on

    [Header("Effects")]
    public GameObject hitEffectPrefab; // small flash/explosion
    public AudioClip hitSound;
    private AudioSource audioSource;

    [Header("References (runtime)")]
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
    }

    public void Initialize(Vector2 dir)
    {
        direction = dir.normalized;
        transform.right = direction; // orient sprite to flight direction
    }

    void Update()
    {
        rb.MovePosition(rb.position + direction * speed * Time.deltaTime);
        timer += Time.deltaTime;
        if (timer >= lifetime) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if we hit a WordBlock
        if (other.CompareTag("WordBlock"))
        {
            var word = other.GetComponent<WordBlock>();
            if (word)
            {
                // Explode the hit block and pass this wave for scoring
                int score = word.Explode(this);

                // Add the score from the block
                SwordWaveManager.TryAddScore(score);

                // Explode nearby blocks in radius
                ExplodeNearbyBlocks(other.transform.position);

                // Restore energy
                if (SwordWaveManager.Instance)
                {
                    SwordWaveManager.Instance.RestoreEnergy(energyReward);
                }

                Debug.Log($"[SwordWave] Hit word block! Score: {score}, +{energyReward} energy");
            }

            SpawnHitFX();
            Destroy(gameObject);
        }
    }

    void ExplodeNearbyBlocks(Vector2 hitPosition)
    {
        // Find all nearby word blocks
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(hitPosition, explosionRadius);

        int explodedCount = 0;
        int totalScore = 0;

        foreach (var col in nearbyColliders)
        {
            if (col.CompareTag("WordBlock"))
            {
                WordBlock block = col.GetComponent<WordBlock>();
                if (block != null)
                {
                    int blockScore = block.Explode(this);
                    totalScore += blockScore;
                    explodedCount++;
                }
            }
        }

        if (explodedCount > 1)
        {
            Debug.Log($"[SwordWave] Chain reaction! Exploded {explodedCount} blocks, Total score: {totalScore}");
            // Add the chain reaction score
            SwordWaveManager.TryAddScore(totalScore);
        }
    }

    void SpawnHitFX()
    {
        if (hitEffectPrefab)
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        if (audioSource && hitSound)
            audioSource.PlayOneShot(hitSound);
    }

    // Draw explosion radius in editor
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = isCorrectWave ? new Color(0f, 1f, 1f, 0.3f) : new Color(1f, 0f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
#endif
}