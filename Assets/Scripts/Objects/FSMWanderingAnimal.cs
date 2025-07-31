using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FSMWanderingAnimal : MonoBehaviour
{
    [Header("Movement Settings")]
    public float wanderRadius = 5f;
    public float wanderSpeed = 2.5f;
    public float fleeSpeed = 4.5f;
    public float scareRadius = 3f;
    public float safeRadius = 5f;
    public float safeDistance = 8f;
    public float predatorDetectRadius = 5f;
    public float predatorSafeRadius = 10f;
    public float eatCooldown = 6f;

    [Header("Food Settings")]
    public string[] naturalFoodTags = { "Grass", "Berries", "Mushrooms" };
    public string[] favouriteFoodTags = { "Carrot" };
    public GameObject rewardPrefab; // Spawned when eating favorite food

    [Header("Predator & Player")]
    public string[] predatorTags = { "Wolf" };
    private GameObject predator;
    private GameObject player;

    [Header("Death & Drops")]
    public GameObject meatPrefab;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip eatSound;

    [Header("Misc")]
    public LayerMask obstacleLayerMask;
    public float scareReductionPerEat = 1f;
    public float minScareRadius = 0f;

    private Vector2 targetPosition;
    private Rigidbody2D rb;
    private Animator animator;

    private GameObject targetFood;
    private bool isDead = false;
    private float lastEatTime = 0f;

    private enum State { FleePredator, EatFavourite, FleePlayer, EatNatural, Wander }
    private State currentState;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player");
        StartCoroutine(FSMUpdate());
    }

    IEnumerator FSMUpdate()
    {
        while (true)
        {
            if (isDead) yield break;

            State previousState = currentState;

            if (DetectPredator())
            {
                //Debug.Log("Predator detected, fleeing!");
                currentState = State.FleePredator;
            }
            else if (DetectFavouriteFood())
            {
                //Debug.Log("Favourite food detected, eating!");
                currentState = State.EatFavourite;
            }
            else if (DetectPlayer())
            {
                //Debug.Log("Player detected, fleeing!");
                currentState = State.FleePlayer;
            }
            else if (DetectNaturalFood())
            {
                //Debug.Log("Natural food detected, eating!");
                currentState = State.EatNatural;
            }
            else
            {
                //Debug.Log("No threats or food detected, wandering.");
                currentState = State.Wander;
            }

            if (previousState != currentState)
            {
                animator.SetBool("isWalking", true);
            }

            PerformCurrentState();

            yield return null;
        }
    }

    void PerformCurrentState()
    {
        switch (currentState)
        {
            case State.FleePredator:
                Flee(predator.transform.position);
                break;

            case State.EatFavourite:
                MoveTowards(targetFood.transform.position, wanderSpeed);
                if (IsAtTarget(targetFood.transform.position))
                    EatFood(true);
                break;

            case State.FleePlayer:
                Flee(player.transform.position);
                break;

            case State.EatNatural:
                MoveTowards(targetFood.transform.position, wanderSpeed);
                if (IsAtTarget(targetFood.transform.position))
                    EatFood(false);
                break;

            case State.Wander:
                Wander();
                break;
        }
    }

    bool DetectPredator()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, predatorDetectRadius);
        foreach (var hit in hits)
        {
            foreach (string tag in predatorTags)
            {
                if (hit.CompareTag(tag))
                {
                    float dist = Vector2.Distance(transform.position, hit.transform.position);

                    if (currentState == State.FleePredator)
                    {
                        if (dist > predatorSafeRadius)
                            return false; // Stop fleeing predator
                        else
                        {
                            predator = hit.gameObject;
                            return true;  // Continue fleeing predator
                        }
                    }
                    else
                    {
                        if (dist < predatorDetectRadius)
                        {
                            predator = hit.gameObject;
                            return true; // Start fleeing predator
                        }
                    }
                }
            }
        }
        predator = null;
        return false;
    }

    bool DetectPlayer()
    {
        if (player == null) return false;

        float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);

        if (currentState == State.FleePlayer)
        {
            if (distanceToPlayer > safeDistance)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        else
        {
            if (distanceToPlayer < scareRadius)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    bool DetectFavouriteFood()
    {
        if (Time.time - lastEatTime < eatCooldown) return false;
        return FindClosestFood(favouriteFoodTags, out targetFood);
    }

    bool DetectNaturalFood()
    {
        if (Time.time - lastEatTime < eatCooldown) return false;
        //Debug.Log("Detecting natural food...");
        return FindClosestFood(naturalFoodTags, out targetFood);
    }

    bool FindClosestFood(string[] tags, out GameObject closest)
    {
        Collider2D[] foods = Physics2D.OverlapCircleAll(transform.position, 50f);
        float minDist = Mathf.Infinity;
        closest = null;
        //Debug.Log($"Found {foods.Length} potential foods");

        foreach (var food in foods)
        {
            //Debug.Log($"Found food: {food.name} Tag: {food.tag}");
            foreach (string tag in tags)
            {
                if (food.CompareTag(tag))
                {
                    float dist = Vector2.Distance(transform.position, food.transform.position);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closest = food.gameObject;
                    }
                }
            }
        }
        return closest != null;
    }

    void EatFood(bool isFavourite)
    {

        if (targetFood == null || Time.time - lastEatTime < eatCooldown) return; // Safety check
        lastEatTime = Time.time;
        Destroy(targetFood);

        if (isFavourite)
        {
            if (rewardPrefab != null)
                Instantiate(rewardPrefab, transform.position, Quaternion.identity);

            scareRadius = Mathf.Max(scareRadius - scareReductionPerEat, minScareRadius);
        }

        if (audioSource && eatSound)
            audioSource.PlayOneShot(eatSound);

        targetFood = null;
    }

    void Flee(Vector2 threatPos)
    {
        Vector2 dir = (transform.position - (Vector3)threatPos).normalized;
        MoveTowards(transform.position + (Vector3)dir, fleeSpeed);
    }

    void Wander()
    {
        if (targetPosition == Vector2.zero || IsAtTarget(targetPosition))
            targetPosition = (Vector2)transform.position + Random.insideUnitCircle * wanderRadius;

        Vector2 safeDir = FindSafeDirection((targetPosition - (Vector2)transform.position).normalized);
        MoveTowards(transform.position + (Vector3)safeDir, wanderSpeed);
    }

    void MoveTowards(Vector2 destination, float speed)
    {
        Vector2 dir = (destination - (Vector2)transform.position).normalized;
        rb.MovePosition(rb.position + dir * speed * Time.deltaTime);
        animator.SetFloat("moveX", dir.x);
        animator.SetFloat("moveY", dir.y);
    }

    bool IsAtTarget(Vector2 target)
    {
        return Vector2.Distance(transform.position, target) < 0.5f;
    }

    Vector2 FindSafeDirection(Vector2 desiredDir)
    {
        for (int i = 0; i < 8; i++)
        {
            Vector2 dir = Quaternion.Euler(0, 0, i * 45f) * desiredDir;
            if (!Physics2D.CircleCast(transform.position, 0.2f, dir, 1f, obstacleLayerMask))
                return dir;
        }
        return -desiredDir;
    }


    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("HitBox"))
        {
            Die();
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        animator.SetTrigger("isDead");
        if (meatPrefab != null)
            Instantiate(meatPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject, 0.35f);
    }
}
