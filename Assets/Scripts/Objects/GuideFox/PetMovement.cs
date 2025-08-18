using UnityEngine;

public class PetMovement : MonoBehaviour
{
    public Transform player;
    public float moveSpeed = 1f;
    public float followDistance = 4f;

    [Header("Orbit Offset")]
    public Vector3 behindHeadOffsetRight = new Vector3(-1f, 1f, 0); // if facing right
    public Vector3 behindHeadOffsetLeft = new Vector3(1f, 1f, 0);   // if facing left

    [Header("Wander Settings")]
    public float wanderRadius = 1.5f;
    public float wanderCooldownMin = 2f;
    public float wanderCooldownMax = 4f;

    [Header("State Transition Smoothing")]
    public float stateSwitchDelay = 0.5f; // seconds to wait before switching state

    private Vector3 orbitCenter;
    private Vector3 wanderTarget;
    private float wanderTimer = 0f;
    private float nextWanderTime = 0f;

    public Animator animator;
    private Vector3 lastMoveDir;

    private enum PetState { Wandering, Chasing }
    private PetState currentState = PetState.Wandering;
    private PetState targetState = PetState.Wandering;
    private float stateTimer = 0f;

    void Start()
    {
        //animator = this.GetComponent<Animator>();
        PickNewWanderTarget();
    }

    void Update()
    {
        if (player == null) return;

        // Decide orbit center based on player facing direction
        Vector3 offset = (player.localScale.x > 0) ? behindHeadOffsetRight : behindHeadOffsetLeft;
        orbitCenter = player.position + offset;

        float dist = Vector3.Distance(transform.position, orbitCenter);

        // Desired state based on distance
        targetState = (dist > followDistance) ? PetState.Chasing : PetState.Wandering;

        // Smooth switching between states
        if (targetState != currentState)
        {
            stateTimer += Time.deltaTime;
            if (stateTimer >= stateSwitchDelay)
            {
                currentState = targetState;
                stateTimer = 0f;
            }
        }
        else
        {
            stateTimer = 0f;
        }

        // Execute behavior based on state
        if (currentState == PetState.Chasing)
        {
            MoveTowards(orbitCenter);
        }
        else
        {
            WanderAround();
        }
    }

    void WanderAround()
    {
        wanderTimer += Time.deltaTime;

        if (wanderTimer >= nextWanderTime)
        {
            PickNewWanderTarget();
        }

        MoveTowards(wanderTarget);
    }

    void PickNewWanderTarget()
    {
        Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
        wanderTarget = orbitCenter + new Vector3(randomCircle.x, randomCircle.y, 0);

        wanderTimer = 0f;
        nextWanderTime = Random.Range(wanderCooldownMin, wanderCooldownMax);
    }

    void MoveTowards(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);

        UpdateAnimator(direction);
    }

    void UpdateAnimator(Vector3 moveDir)
    {
        if (moveDir.magnitude > 0.01f)
        {
            animator.SetBool("isMoving", true);
            animator.SetFloat("moveX", moveDir.x);
            animator.SetFloat("moveY", moveDir.y);
            lastMoveDir = moveDir;
        }
        else
        {
            animator.SetBool("isMoving", false);
            animator.SetFloat("moveX", lastMoveDir.x);
            animator.SetFloat("moveY", lastMoveDir.y);
        }
    }

    public void Appear()
    {
        animator.SetTrigger("Appear");
    }

    public void Disappear()
    {
        animator.SetTrigger("Disappear");
    }
}
