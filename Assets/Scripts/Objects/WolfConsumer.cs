using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WolfConsumer : MonoBehaviour
{
    public enum WolfState { Wandering, Hunting, Fleeing }
    private WolfState currentState;

    [Header("General Settings")]
    public float wanderRadius = 10f;
    public float wanderSpeed = 2f;
    public float huntSpeed = 6f;
    public float fleeSpeed = 5f;
    public float fleeDetectRadius = 7f;         // Start fleeing here
    public float fleeSafeRadius = 10f;          // Only stop fleeing when beyond this radius
    public float huntDetectRadius = 8f;         // Start hunting prey within this range
    public float huntSafeRadius = 10f;          // Stay hunting until prey is beyond this
    public float safeDistance = 12f;            // Distance to flee away from threat
    public bool isHungry = true;
    public LayerMask obstacleLayerMask;

    [Header("Target Settings")]
    public string[] preyTags = { "Rabbit", "MouseDeer" };
    public GameObject MeatPrefab; // Drop on death
    public float eatCooldown = 10f; // Time before hunting again
    private Vector2 currentWanderTarget;
    private float wanderTargetTimeout = 3f; // seconds to spend on a wander target
    private float wanderTargetStartTime;


    private Rigidbody2D rb;
    private Animator animator;
    private GameObject player;
    private GameObject currentPrey;
    private Vector2 startPosition;
    private bool isDead = false;
    private float lastHuntTime;
    private float stateMinDuration = 1.5f; // Prevent rapid state switching
    private float lastStateChangeTime;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player");
        startPosition = transform.position;

        ChangeState(WolfState.Wandering);
    }

    void Update()
    {
        if (isDead) return;

        // Check if it's time to evaluate a state change
        if (Time.time - lastStateChangeTime >= stateMinDuration)
        {
            EvaluateState();
        }

        if (Time.time - lastHuntTime > eatCooldown)
        {
            isHungry = true; // Reset hunger after cooldown
        }
        PerformStateBehavior();
    }

    void ChangeState(WolfState newState)
    {
        currentState = newState;
        lastStateChangeTime = Time.time;
        animator.SetBool("isWalking", true);

        if (newState == WolfState.Wandering)
        {
            PickNewWanderTarget();
        }

        Debug.Log($"{gameObject.name} switched to {newState}");
    }

    void PickNewWanderTarget()
    {
        currentWanderTarget = startPosition + Random.insideUnitCircle * wanderRadius;
        wanderTargetStartTime = Time.time;
    }


    void EvaluateState()
    {
        if (ShouldFlee())
        {
            ChangeState(WolfState.Fleeing);
            return;
        }
        //Debug.Log($"{gameObject.name} evaluating state: isHungry={isHungry}, currentState={currentState}");
        if (isHungry && ShouldHunt())
        {
            ChangeState(WolfState.Hunting);
            return;
        }
        if (currentState != WolfState.Wandering)
        {
            ChangeState(WolfState.Wandering);
        }
    }

    bool ShouldFlee()
    {
        // Detect dead wolves or player nearby
        Collider2D[] threats = Physics2D.OverlapCircleAll(transform.position, fleeDetectRadius);
        foreach (var threat in threats)
        {
            if (threat.CompareTag("Wolf") && threat.GetComponent<WolfConsumer>()?.isDead == true)
                return true;

            if (threat.CompareTag("Player"))
                return Vector2.Distance(transform.position, player.transform.position) < fleeDetectRadius;
        }
        return false;
    }

    bool ShouldHunt()
    {
        //Debug.Log($"{gameObject.name} checking for prey.");
        currentPrey = FindClosestPrey(huntDetectRadius);
        if (currentPrey != null)
            return true;

        return false;
    }

    void PerformStateBehavior()
    {
        switch (currentState)
        {
            case WolfState.Fleeing:
                HandleFleeing();
                break;
            case WolfState.Hunting:
                //Debug.Log($"{gameObject.name} is hunting.");
                HandleHunting();
                break;
            case WolfState.Wandering:
                //Debug.Log($"{gameObject.name} is wandering.");
                HandleWandering();
                break;
        }
    }

    void HandleFleeing()
    {
        Vector2 fleeDirection = (transform.position - player.transform.position).normalized;
        Vector2 safeDirection = FindSafeDirection(fleeDirection);
        Move(safeDirection, fleeSpeed);

        // Exit flee only if player is beyond safe radius
        if (Vector2.Distance(transform.position, player.transform.position) > fleeSafeRadius)
        {
            EvaluateState();
        }
    }

    void HandleHunting()
    {
        if (currentPrey == null || !currentPrey.activeInHierarchy)
        {
            EvaluateState();
            return;
        }

        Vector2 directionToPrey = (currentPrey.transform.position - transform.position).normalized;
        Move(directionToPrey, huntSpeed);

        if (Vector2.Distance(transform.position, currentPrey.transform.position) < 0.5f)
        {
            EatPrey();
        }
        else if (Vector2.Distance(transform.position, currentPrey.transform.position) > huntSafeRadius)
        {
            EvaluateState();
        }
    }

    void HandleWandering()
    {
        Vector2 direction = (currentWanderTarget - (Vector2)transform.position).normalized;
        Vector2 safeDirection = FindSafeDirection(direction);
        Move(safeDirection, wanderSpeed);

        if (Vector2.Distance(transform.position, currentWanderTarget) < 0.5f ||
            Time.time - wanderTargetStartTime > wanderTargetTimeout)
        {
            PickNewWanderTarget();
        }
    }


    void EatPrey()
    {
        Debug.Log($"{gameObject.name} hunted {currentPrey.name}");
        Destroy(currentPrey);
        isHungry = false;
        lastHuntTime = Time.time;
        ChangeState(WolfState.Wandering);
    }

    private GameObject FindClosestPrey(float radius)
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, radius);
        GameObject closestPrey = null;
        float closestDistance = float.MaxValue;

        foreach (var collider in colliders)
        {
            foreach (string tag in preyTags)
            {
                if (collider.CompareTag(tag))
                {
                    float dist = Vector2.Distance(transform.position, collider.transform.position);
                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                        closestPrey = collider.gameObject;
                    }
                }
            }
        }
        return closestPrey;
    }

    void Move(Vector2 direction, float speed)
    {
        rb.MovePosition(rb.position + direction * speed * Time.deltaTime);
        animator.SetFloat("moveX", direction.x);
        animator.SetFloat("moveY", direction.y);
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

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        animator.SetTrigger("isDead");
        Instantiate(MeatPrefab, transform.position, Quaternion.identity);
        NotifyNearbyWolves();
        Destroy(gameObject, 0.5f);
    }

    void NotifyNearbyWolves()
    {
        Collider2D[] nearbyWolves = Physics2D.OverlapCircleAll(transform.position, fleeDetectRadius);
        foreach (var wolf in nearbyWolves)
        {
            if (wolf.CompareTag("Wolf"))
            {
                wolf.GetComponent<WolfConsumer>()?.OnNearbyWolfKilled();
            }
        }
    }

    public void OnNearbyWolfKilled()
    {
        if (!isDead)
        {
            ChangeState(WolfState.Fleeing);
        }
    }
}
