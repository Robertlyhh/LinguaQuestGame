using UnityEngine;
using TMPro;
using System.Collections;

[DisallowMultipleComponent]
public class WordBlock : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private string wordText = "run";
    [SerializeField] private string shownLabel = "Noun"; // what the player sees
    [SerializeField] private bool isLabelCorrect = true; // whether shownLabel matches the true POS

    [Header("Points / Timing")]
    [SerializeField] private int pointsOnCorrectSlice = 100; // exploding an INCORRECT label is correct play
    [SerializeField] private int pointsOnWrongSlice = -50; // exploding a CORRECT label is a mistake
    [SerializeField] private int pointsOnMissed = -30; // penalty for missing a block
    [SerializeField] private float destroyDelay = 0.05f;

    [Header("Lifetime & Fade")]
    [Tooltip("How long the block exists before starting to fade")]
    [SerializeField] private float lifetime = 8f;
    [Tooltip("How long the fade-out animation takes")]
    [SerializeField] private float fadeOutDuration = 2f;
    private float _timer;
    private bool _isFading;

    [Header("Boundaries")]
    [Tooltip("World space boundaries (min/max X and Y)")]
    [SerializeField] private Vector2 minBounds = new Vector2(-10f, -6f);
    [SerializeField] private Vector2 maxBounds = new Vector2(10f, 6f);
    [Tooltip("How much to bounce back when hitting boundary")]
    [SerializeField] private float bounceForce = 0.5f;

    [Header("Refs")]
    [SerializeField] private SpriteRenderer background;
    [SerializeField] private TextMeshPro wordTMP;
    [SerializeField] private TextMeshPro labelTMP;
    [SerializeField] private ParticleSystem explodeParticles; // optional explosion effect

    [Header("Colors")]
    [SerializeField] private Color neutralColor = Color.white;
    [SerializeField] private Color goodColor = new Color(0.2f, 1f, 0.4f); // correct action (hit wrong label)
    [SerializeField] private Color badColor = new Color(1f, 0.3f, 0.3f); // mistake (hit correct label)

    [Header("Explosion")]
    [SerializeField] private float explosionForce = 5f;
    [SerializeField] private float explosionRadius = 2f;

    private bool _hasExploded;
    private Rigidbody2D _rb;
    private Color _originalBackgroundColor;
    private Color _originalWordColor;
    private Color _originalLabelColor;

    void Reset()
    {
        if (!background) background = GetComponentInChildren<SpriteRenderer>();
        if (!wordTMP) wordTMP = transform.Find("Text: Word")?.GetComponent<TextMeshPro>();
        if (!labelTMP) labelTMP = transform.Find("Text: Label")?.GetComponent<TextMeshPro>();
    }

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (_rb)
        {
            // Freeze rotation so blocks don't spin
            _rb.freezeRotation = true;
        }
    }

    void OnEnable()
    {
        _hasExploded = false;
        _isFading = false;
        _timer = 0f;
        ApplyTexts();
        SetNeutral();
        EnableColliders(true);

        // Store original colors for fading
        if (background) _originalBackgroundColor = background.color;
        if (wordTMP) _originalWordColor = wordTMP.color;
        if (labelTMP) _originalLabelColor = labelTMP.color;

        // Ensure rotation is always upright
        transform.rotation = Quaternion.identity;
    }

    void Update()
    {
        if (_hasExploded) return;

        // Update lifetime timer
        _timer += Time.deltaTime;

        // Start fading when lifetime is reached
        if (_timer >= lifetime && !_isFading)
        {
            _isFading = true;
            StartCoroutine(FadeOutAndDestroy());
        }

        // Enforce boundaries
        EnforceBoundaries();
    }

    void LateUpdate()
    {
        // Force upright rotation every frame (safety check)
        if (transform.rotation != Quaternion.identity)
        {
            transform.rotation = Quaternion.identity;
        }
    }

    private void EnforceBoundaries()
    {
        if (_rb == null) return;

        Vector2 pos = transform.position;
        Vector2 velocity = _rb.velocity;
        bool hitBoundary = false;

        // Check X boundaries
        if (pos.x < minBounds.x)
        {
            pos.x = minBounds.x;
            velocity.x = Mathf.Abs(velocity.x) * bounceForce; // Bounce right
            hitBoundary = true;
        }
        else if (pos.x > maxBounds.x)
        {
            pos.x = maxBounds.x;
            velocity.x = -Mathf.Abs(velocity.x) * bounceForce; // Bounce left
            hitBoundary = true;
        }

        // Check Y boundaries
        if (pos.y < minBounds.y)
        {
            pos.y = minBounds.y;
            velocity.y = Mathf.Abs(velocity.y) * bounceForce; // Bounce up
            hitBoundary = true;
        }
        else if (pos.y > maxBounds.y)
        {
            pos.y = maxBounds.y;
            velocity.y = -Mathf.Abs(velocity.y) * bounceForce; // Bounce down
            hitBoundary = true;
        }

        if (hitBoundary)
        {
            transform.position = pos;
            _rb.velocity = velocity;
        }
    }

    // Public init used by the spawner
    public void Initialize(string word, string shownLabel, bool isLabelCorrect)
    {
        this.wordText = word;
        this.shownLabel = shownLabel;
        this.isLabelCorrect = isLabelCorrect;
        ApplyTexts();
        SetNeutral();
        EnableColliders(true);

        // Reset timers
        _timer = 0f;
        _isFading = false;

        // Ensure no rotation
        transform.rotation = Quaternion.identity;
        if (_rb)
        {
            _rb.angularVelocity = 0f;
            _rb.freezeRotation = true;
        }
    }

    private void ApplyTexts()
    {
        if (wordTMP) wordTMP.text = wordText;
        if (labelTMP) labelTMP.text = shownLabel;
    }

    private void SetNeutral()
    {
        if (background) background.color = neutralColor;
        if (wordTMP) wordTMP.color = Color.white;
        if (labelTMP) labelTMP.color = new Color(0.9f, 0.9f, 0.9f);
    }

    private void EnableColliders(bool enabled)
    {
        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = enabled;
    }

    /// <summary>
    /// Explode this word block - called by sword wave attacks ONLY.
    /// Now checks if the wave type matches the block's correctness.
    /// </summary>
    /// <param name="wave">The SwordWave that hit this block (optional)</param>
    /// <returns>Score delta for this explosion</returns>
    public int Explode(SwordWave wave = null)
    {
        if (_hasExploded) return 0;
        _hasExploded = true;

        // Stop fading if it was in progress
        StopAllCoroutines();

        bool correctPlay = false;
        int scoreToReturn = 0;

        // If we have wave information, check if wave type matches block correctness
        if (wave != null)
        {
            // Wave 1 (isCorrectWave = true) should hit CORRECT labels
            // Wave 2 (isCorrectWave = false) should hit INCORRECT labels
            bool waveMatchesBlock = (wave.isCorrectWave == isLabelCorrect);
            correctPlay = waveMatchesBlock;

            scoreToReturn = correctPlay ? pointsOnCorrectSlice : pointsOnWrongSlice;

            Debug.Log($"[WordBlock] Wave type: {(wave.isCorrectWave ? "Correct" : "Incorrect")}, " +
                     $"Block label: {(isLabelCorrect ? "Correct" : "Incorrect")}, " +
                     $"Match: {waveMatchesBlock}, Score: {scoreToReturn}");
        }
        else
        {
            // Fallback to old behavior if no wave info provided
            correctPlay = !isLabelCorrect;
            scoreToReturn = correctPlay ? pointsOnCorrectSlice : pointsOnWrongSlice;
            Debug.LogWarning("[WordBlock] Exploded without wave reference, using old scoring logic");
        }

        // Visual feedback
        if (background) background.color = correctPlay ? goodColor : badColor;

        // Play explosion particles
        if (explodeParticles)
        {
            var main = explodeParticles.main;
            main.startColor = correctPlay ? goodColor : badColor;
            explodeParticles.Play();
        }

        // Apply explosion force to nearby blocks
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (var col in nearbyColliders)
        {
            if (col.gameObject == gameObject) continue; // Skip self

            Rigidbody2D rb = col.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 direction = (col.transform.position - transform.position).normalized;
                float distance = Vector2.Distance(transform.position, col.transform.position);
                float forceMagnitude = explosionForce * (1f - distance / explosionRadius);
                rb.AddForce(direction * forceMagnitude, ForceMode2D.Impulse);
            }
        }

        // Disable colliders
        EnableColliders(false);

        // Remove after delay
        StartCoroutine(RemoveAfterDelay());

        // Return score
        return scoreToReturn;
    }

    private IEnumerator FadeOutAndDestroy()
    {
        float elapsed = 0f;

        // Disable colliders so it can't be hit while fading
        EnableColliders(false);

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / fadeOutDuration);

            // Fade all visual elements
            if (background)
            {
                Color c = _originalBackgroundColor;
                c.a = alpha;
                background.color = c;
            }

            if (wordTMP)
            {
                Color c = _originalWordColor;
                c.a = alpha;
                wordTMP.color = c;
            }

            if (labelTMP)
            {
                Color c = _originalLabelColor;
                c.a = alpha;
                labelTMP.color = c;
            }

            yield return null;
        }

        // Apply score penalty for missing the block (only if it wasn't exploded)
        if (!_hasExploded)
        {
            SwordWaveManager.TryAddScore(pointsOnMissed);
            Debug.Log($"[WordBlock] Missed: {wordText} ({pointsOnMissed} score)");
        }

        // Destroy the block
        Destroy(gameObject);
    }

    private IEnumerator RemoveAfterDelay()
    {
        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }

    // Optional: gizmos to help layout
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Draw block type indicator
        Gizmos.color = isLabelCorrect ? new Color(0f, 0.8f, 1f, 0.35f) : new Color(1f, 0.5f, 0f, 0.35f);
        Gizmos.DrawSphere(transform.position, 0.15f);

        // Draw explosion radius
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);

        // Draw boundaries
        Gizmos.color = Color.yellow;
        Vector3 bottomLeft = new Vector3(minBounds.x, minBounds.y, 0);
        Vector3 topLeft = new Vector3(minBounds.x, maxBounds.y, 0);
        Vector3 topRight = new Vector3(maxBounds.x, maxBounds.y, 0);
        Vector3 bottomRight = new Vector3(maxBounds.x, minBounds.y, 0);

        Gizmos.DrawLine(bottomLeft, topLeft);
        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
    }
#endif
}