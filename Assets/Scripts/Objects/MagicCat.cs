using System.Collections;
using UnityEngine;

public class MagicCat : MonoBehaviour
{
    [Header("Wandering Settings")]
    public float wanderRadius = 5f;
    public float wanderSpeed = 1.5f;

    [Header("Meat Detection & Eating")]
    public float detectMeatRadius = 5f;     // Detection radius
    public float eatDelay = 1.0f;           // Time spent eating
    public float eatCooldown = 10f;         // Cooldown between eating
    public LayerMask meatLayerMask;         // LayerMask for meat prefab
    public LayerMask obstacleLayerMask;     // LayerMask for walls, trees, etc.
    public AudioClip meowClip;              // Meow sound when eating
    public GameObject coinPrefab;           // Coin prefab to spawn
    public int minCoins = 1;                // Min coins dropped
    public int maxCoins = 5;                // Max coins dropped

    [Header("Interaction")]
    public FloatValue lovePlayerValue; // Value to increase when eating
    public float loveIncreaseAmount = 1f; // Amount to increase love value per eat
    public GameObject CluePrefab;
    public BoolValue hasGeneratedClue;


    private Vector2 startPosition;
    private Rigidbody2D rb;
    private Animator animator;
    private AudioSource audioSource;
    private bool isEating = false;
    private float nextEatTime = 0f;

    void Start()
    {
        startPosition = transform.position;
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        StartCoroutine(WanderRoutine());
    }

    void Update()
    {
        if (isEating || Time.time < nextEatTime) return;

        GameObject closestMeat = FindClosestMeat();
        if (closestMeat != null)
        {
            StopAllCoroutines();
            StartCoroutine(GoEatMeat(closestMeat));
        }
    }

    IEnumerator WanderRoutine()
    {
        while (true)
        {
            Vector2 targetPosition = FindSafeWanderPosition();

            while (Vector2.Distance(transform.position, targetPosition) > 0.1f && !isEating)
            {
                Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
                Move(direction, wanderSpeed);
                yield return null;
            }

            animator.SetBool("isWalking", false);
            yield return new WaitForSeconds(Random.Range(0.5f, 1.5f));
        }
    }

    void Move(Vector2 direction, float speed)
    {
        rb.MovePosition(rb.position + direction * speed * Time.deltaTime);

        if (direction.magnitude > 0.01f)
        {
            animator.SetBool("isWalking", true);
            animator.SetFloat("moveX", direction.x);
            animator.SetFloat("moveY", direction.y);
        }
        else
        {
            animator.SetBool("isWalking", false);
        }
    }

    GameObject FindClosestMeat()
    {
        Collider2D[] meats = Physics2D.OverlapCircleAll(transform.position, detectMeatRadius, meatLayerMask);

        GameObject closest = null;
        float shortestDist = Mathf.Infinity;

        foreach (Collider2D meat in meats)
        {
            float dist = Vector2.Distance(transform.position, meat.transform.position);
            if (dist < shortestDist)
            {
                shortestDist = dist;
                closest = meat.gameObject;
            }
        }

        return closest;
    }

    IEnumerator GoEatMeat(GameObject meat)
    {
        isEating = true;
        Debug.Log($"{name}: Found meat! Approaching...");

        while (meat != null && Vector2.Distance(transform.position, meat.transform.position) > 0.2f)
        {
            Vector2 direction = (meat.transform.position - transform.position).normalized;
            Move(direction, wanderSpeed * 1.2f);
            yield return null;
        }

        if (meat == null)
        {
            Debug.Log($"{name}: Meat disappeared. Returning to wander.");
            isEating = false;
            StartCoroutine(WanderRoutine());
            yield break;
        }

        // Play eating animation and meow
        animator.SetBool("isWalking", false);
        animator.SetTrigger("isEating");
        if (meowClip && audioSource)
        {
            audioSource.PlayOneShot(meowClip);
        }

        yield return new WaitForSeconds(eatDelay);

        Destroy(meat);
        Debug.Log($"{name}: Ate the meat!");

        SpawnCoins();
        lovePlayerValue.runtimeValue += loveIncreaseAmount;
        if (hasGeneratedClue != null && !hasGeneratedClue.runtimeValue && CluePrefab != null && lovePlayerValue.runtimeValue >= 3f)
        {
            Instantiate(CluePrefab, transform.position, Quaternion.identity);
            hasGeneratedClue.runtimeValue = true;
            Debug.Log($"{name}: Generated a clue for the player!");
        }


        nextEatTime = Time.time + eatCooldown;
        isEating = false;
        StartCoroutine(WanderRoutine());
    }

    void SpawnCoins()
    {
        int coinsToSpawn = Random.Range(minCoins, maxCoins + 1);
        Debug.Log($"{name}: Dropping {coinsToSpawn} coins!");

        for (int i = 0; i < coinsToSpawn; i++)
        {
            Vector2 spawnOffset = Random.insideUnitCircle * 0.5f;
            GameObject coin = Instantiate(coinPrefab, (Vector2)transform.position + spawnOffset, Quaternion.identity);

            Rigidbody2D rb = coin.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 randomForce = new Vector2(Random.Range(-1f, 1f), Random.Range(1f, 2f));
                rb.AddForce(randomForce, ForceMode2D.Impulse);
            }
        }
    }

    Vector2 FindSafeWanderPosition()
    {
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        Vector2 proposedPos = (Vector2)transform.position + randomDir * wanderRadius;

        RaycastHit2D hit = Physics2D.CircleCast(transform.position, 0.2f, randomDir, wanderRadius, obstacleLayerMask);
        if (hit.collider == null)
        {
            return proposedPos;
        }

        for (int angleStep = 45; angleStep <= 360; angleStep += 45)
        {
            Vector2 newDir = Quaternion.Euler(0, 0, angleStep) * randomDir;
            proposedPos = (Vector2)transform.position + newDir * wanderRadius;

            hit = Physics2D.CircleCast(transform.position, 0.2f, newDir, wanderRadius, obstacleLayerMask);
            if (hit.collider == null)
            {
                return proposedPos;
            }
        }

        return transform.position; // Stay still if no clear path
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectMeatRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, wanderRadius);
    }
}
