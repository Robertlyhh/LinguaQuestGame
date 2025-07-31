using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class WanderingAnimal : MonoBehaviour
{
    [Header("Movement Settings")]
    public float wanderRadius = 5f;
    public float wanderSpeed = 2.5f;
    public float fleeSpeed = 4.5f;
    public float scareRadius = 3f;
    public float safeDistance = 8f;
    public float backDistance = 10f; // Distance to back away from player when fleeing
    public float predatorDetectRadius = 5f;
    public bool isHungry = true; // Whether the animal is hungry and will seek food
    private bool isFleeing = false;
    private bool isDead = false;
    private bool isScared = false;

    [Header("Food Settings")]
    public string[] naturalFoodTags = { "Grass", "Berries", "Mushrooms" }; // Tags for natural food sources
    public string[] favouriteFoodTags = { "Carrot" }; // Tags for favorite food sources
    public float favouriteFoodDetectRadius = 5f; // Range to detect favorite food
    public float foodDetectRadius = 50f;     // Range to detect food
    public float eatCooldown = 6f;          // Time between each eating

    [Header("Predator")]
    public string[] predatorTags = { "Wolf" }; // Tags for predators that can scare this animal

    [Header("Behavior")]

    public GameObject MeatPrefab;            // Prefab to drop on death

    public GameObject rewardPrefab;         // Prefab to drop after eating (unique per animal)
    public float scareReductionPerEat = 1f; // Amount scareRadius reduces per food eaten
    public float minScareRadius = 0f;       // Lowest scareRadius possible
    public LayerMask obstacleLayerMask;

    private Vector2 startPosition;
    private Vector2 targetPosition;
    private Rigidbody2D rb;
    private Animator animator;
    public AudioSource audioSource;
    public AudioClip eatSound;

    private GameObject targetFood;
    private GameObject predator;
    private GameObject player;
    private float lastEatTime = 0f;

    void Start()
    {
        startPosition = transform.position;
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player");
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
        StartCoroutine(BehaviorLoop());
    }


    IEnumerator BehaviorLoop()
    {
        while (true)
        {
            if (isDead)
            {
                yield break; // Exit if dead
            }

            if (DetectPredator())
            {
                Debug.Log($"{gameObject.name} detected a predator and is fleeing!");
                Flee(predator.transform.position);
            }
            else if (DetectFavouriteFood())
            {
                Debug.Log($"{gameObject.name} detected favorite food and is eating!");
                Move(targetFood.transform.position, wanderSpeed);
                if (Vector2.Distance(transform.position, targetFood.transform.position) < 0.5f)
                {
                    EatFood(targetFood);
                }
            }
            else if (player != null && Vector2.Distance(transform.position, player.transform.position) < scareRadius) // 👤 Priority 3: Flee player
            {
                Debug.Log($"{gameObject.name} detected the player and is fleeing!");
                Flee(player.transform.position);
            }
            else if (DetectNaturalFood()) // 🌿 Priority 4: Eat natural food (grass/mushroom)
            {
                Debug.Log($"{gameObject.name} detected natural food and is eating!");
                Move(targetFood.transform.position, wanderSpeed);
                if (Vector2.Distance(transform.position, targetFood.transform.position) < 0.5f)
                {
                    EatFood(targetFood);
                }
            }
            else // 🌸 Priority 5: Wander
            {
                Debug.Log($"{gameObject.name} is wandering.");
                Wander();
            }
            yield return null;
        }
    }


    bool DetectPredator()
    {
        Collider2D[] predators = Physics2D.OverlapCircleAll(transform.position, predatorDetectRadius);
        foreach (var obj in predators)
        {
            foreach (string predatorTag in predatorTags)
            {
                if (obj.CompareTag(predatorTag))
                {
                    predator = obj.gameObject;
                    isFleeing = true;
                    return true;
                }
            }
        }
        predator = null;
        isFleeing = false;
        return false;
    }

    bool DetectFavouriteFood()
    {
        if (Time.time - lastEatTime < eatCooldown) return false; // Cooldown check
        Collider2D[] foods = Physics2D.OverlapCircleAll(transform.position, favouriteFoodDetectRadius);
        float closestDistance = float.MaxValue;
        GameObject closestFood = null;

        foreach (var food in foods)
        {
            foreach (string tag in favouriteFoodTags)
            {
                if (food.gameObject.CompareTag(tag))
                {
                    float dist = Vector2.Distance(transform.position, food.transform.position);
                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                        closestFood = food.gameObject;
                    }
                }
            }
        }

        if (closestFood != null)
        {
            targetFood = closestFood;
            return true;
        }
        else
        {
            targetFood = null;
            return false;
        }
    }

    bool DetectNaturalFood()
    {
        if (Time.time - lastEatTime < eatCooldown) return false; // Cooldown check
        Collider2D[] foods = Physics2D.OverlapCircleAll(transform.position, foodDetectRadius);
        float closestDistance = float.MaxValue;
        GameObject closestFood = null;

        foreach (var food in foods)
        {
            foreach (string tag in naturalFoodTags)
            {
                if (food.gameObject.CompareTag(tag))
                {
                    float dist = Vector2.Distance(transform.position, food.transform.position);
                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                        closestFood = food.gameObject;
                    }
                }
            }
        }

        if (closestFood != null)
        {
            targetFood = closestFood;
            return true;
        }
        else
        {
            targetFood = null;
            return false;
        }
    }


    void EatFood(GameObject food)
    {
        if (Time.time - lastEatTime < eatCooldown) return; // Safety check
        Debug.Log($"{gameObject.name} ate {food.name}");
        Destroy(food);
        lastEatTime = Time.time;
    }


    void Wander()
    {
        if (isDead) return;

        Vector2 wanderTarget = (Vector2)transform.position + Random.insideUnitCircle * wanderRadius;
        Vector2 safeDirection = FindSafeDirection((wanderTarget - (Vector2)transform.position).normalized);
        if (safeDirection != Vector2.zero)
        {
            Move(safeDirection, wanderSpeed);
        }
    }

    void EatFavouriteFood()
    {
        if (Time.time - lastEatTime < eatCooldown || targetFood == null) return; // Cooldown check
        Debug.Log($"{gameObject.name} is eating {targetFood.name}.");
        Destroy(targetFood);
        lastEatTime = Time.time;

        // Drop reward prefab
        if (rewardPrefab != null)
        {
            Instantiate(rewardPrefab, transform.position, Quaternion.identity);
        }

        // Reduce scareRadius
        scareRadius -= scareReductionPerEat;
        scareRadius = Mathf.Max(scareRadius, minScareRadius);

        Debug.Log($"{gameObject.name} scareRadius reduced to {scareRadius}.");
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

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        animator.SetTrigger("isDead");
        Instantiate(MeatPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject, 0.35f);
    }

    private Vector2 FindSafeDirection(Vector2 initialDirection)
    {
        const int angleStep = 45;
        const int maxAttempts = 8;
        float checkDistance = 1.0f;

        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 dirToCheck = Quaternion.Euler(0, 0, angleStep * i) * initialDirection;
            RaycastHit2D hit = Physics2D.CircleCast(transform.position, 0.2f, dirToCheck, checkDistance, obstacleLayerMask);
            if (!hit.collider)
            {
                return dirToCheck;
            }
        }

        return -initialDirection;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("HitBox"))
        {
            Die();
            return;
        }
        foreach (string favouriteFoodTag in favouriteFoodTags)
        {
            if (other.CompareTag(favouriteFoodTag))
            {
                EatFavouriteFood();
                return;
            }
        }
        foreach (string naturalFoodTag in naturalFoodTags)
        {
            if (other.CompareTag(naturalFoodTag))
            {
                EatFood(other.gameObject);
                return;
            }
        }


    }

    void Flee(Vector2 threatPosition)
    {
        Vector2 fleeDirection = (transform.position - (Vector3)threatPosition).normalized;
        Vector2 safeDirection = FindSafeDirection(fleeDirection);
        if (safeDirection != Vector2.zero)
        {
            Move(safeDirection, fleeSpeed);
        }
    }
}
