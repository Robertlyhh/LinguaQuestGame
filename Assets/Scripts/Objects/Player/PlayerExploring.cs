using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerState
{
    walk,
    attack,
    interact,
    stagger,
    slip,
    falling
}

public class PlayerExploring : MonoBehaviour
{
    // --- Serialized Fields ---
    [Header("Movement")]
    public float speed = 5f;
    public Rigidbody2D myRigidbody;
    public VectorValue StartingPosition;

    [Header("Animation")]
    private Animator animator;

    [Header("Player State")]
    public PlayerState currentState = PlayerState.walk;
    public UnityEngine.Vector3 change = UnityEngine.Vector3.zero;

    [Header("Sprint")]
    public float sprintSpeed = 9f;
    public float normalSpeed = 5f;

    [Header("Health & Magic")]
    public FloatValue currentHealth;
    public FloatValue magicLevel;

    [Header("Damage")]
    [SerializeField] private float damageInvulnerabilityDuration = 1f;
    [SerializeField] private float damageFlashDuration = 0.2f;
    [SerializeField] private Color damageFlashColor = new Color(1f, 0f, 0f, 0.65f);
    private float nextDamageTime;
    private Coroutine damageFlashCoroutine;
    private SpriteRenderer playerSpriteRenderer;
    private SpriteRenderer damageFlashRenderer;

    [Header("Inventory & Items")]
    public Inventory inventory;
    public SpriteRenderer receiveItemSprite;

    [Header("Signals")]
    public Signal playerHealthSignal;
    public Signal playerAttackSignal;
    [SerializeField] private Signal playerDefeatedSignal;

    [Header("Step Sound")]
    public StepSoundManager stepSoundManager;
    public float stepSoundCooldown = 0.5f;
    private float lastStepSoundTime = 0f;

    [Header("Magic Attacks")]
    public GameObject fireballPrefab;
    public Transform firePoint;
    public GameObject lightningEffectPrefab;
    public float lightningCastOffset = 4.5f;

    [Header("Combat & Slip Mechanics")]
    public float pushCooldown = 1.0f;
    private float lastPushTime = -10f;
    private float currentSlipTimer = 0f;

    [Tooltip("How much player input affects slip time.")]
    public float slipInputInfluence = 0.8f;

    [Tooltip("How much control the player has while sliding.")]
    public float slipSteeringPower = 2.0f;

    [Tooltip("Time added to slide when bouncing off a wall.")]
    public float wallBounceExtension = 0.5f;

    [Header("Slip Limits")]
    [Tooltip("Current multiplier for speed when slipping.")]
    public float slipSpeedMultiplier = 1.6f;
    public float maxSlipSpeedMultiplier = 3.0f;
    public float maxSlipDuration = 4.0f;
    private float defaultSlipSpeedMultiplier;

    [Header("Falling Mechanics")]
    public float fallingDuration = 0.5f;

    [Tooltip("Speed multiplier while falling (0.1 = 10% speed).")]
    public float fallingSpeedPenalty = 0.1f;

    [Tooltip("Grace period cooldown after falling.")]
    public float fallingCooldown = 2.0f;
    private float lastFallingTime = -10f;

    [HideInInspector] public bool isMoving;

    [Header("Visual Effects")]
    public TrailRenderer[] slipTrails;
    public FootprintsFromPlayerExploring footprintSystem;

    [Header("Tutorial")]
    public PetBubble petBubble;

    // --- Unity Methods ---
    void Start()
    {
        animator = GetComponent<Animator>();
        stepSoundManager = FindFirstObjectByType<StepSoundManager>();
        myRigidbody = GetComponent<Rigidbody2D>();
        playerSpriteRenderer = GetComponent<SpriteRenderer>();
        SetupDamageFlashRenderer();

        defaultSlipSpeedMultiplier = slipSpeedMultiplier;

        if (footprintSystem == null)
            footprintSystem = GetComponent<FootprintsFromPlayerExploring>();
        if (footprintSystem != null)
            footprintSystem.enabled = true;

        foreach (var trail in slipTrails)
        {
            if (trail != null) trail.emitting = false;
        }

        animator.SetFloat("moveX", 0);
        animator.SetFloat("moveY", -1);
        myRigidbody.position = StartingPosition.runtimeValue;
        magicLevel.runtimeValue = magicLevel.initialValue;
    }

    void LateUpdate()
    {
        if (damageFlashRenderer == null) return;
        if (!damageFlashRenderer.enabled) return;

        SyncDamageFlashRenderer();
    }

    void OnDisable()
    {
        if (damageFlashRenderer != null)
        {
            damageFlashRenderer.enabled = false;
        }

        damageFlashCoroutine = null;
    }

    void Update()
    {
        lastStepSoundTime += Time.deltaTime;

        if (currentState == PlayerState.interact) return;

        // Slipping Logic takes priority
        if (currentState == PlayerState.slip)
        {
            UpdateAnimationSlip();
            return;
        }
        else
        {
            animator.SetBool("slipping", false);
        }

        // Standard Input Processing
        change = UnityEngine.Vector3.zero;
        change.x = Input.GetAxisRaw("Horizontal");
        change.y = Input.GetAxisRaw("Vertical");
        change.z = 0;
        change.Normalize();


        if (petBubble != null)
        {
            if (change != Vector3.zero)
                petBubble.playerHasMoved = true;

            if (change != Vector3.zero && Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) )
                petBubble.playerHasSprinted = true;

            if (Input.GetKeyDown(KeyCode.F))
                petBubble.playerHasThrownFireball = true;

        }

        if (Input.GetMouseButtonDown(0) && currentState != PlayerState.attack && currentState != PlayerState.falling)
        {
            StartCoroutine(AttackCo());
        }
        else if (Input.GetMouseButtonDown(1) && currentState == PlayerState.attack)
        {
            animator.SetTrigger("swordDance");
        }
        else if (Input.GetKeyDown(KeyCode.F) && magicLevel.runtimeValue >= 1 && currentState != PlayerState.falling)
        {
            CastFireball();
        }
        else if (Input.GetKeyDown(KeyCode.G) && magicLevel.runtimeValue >= 2 && currentState != PlayerState.falling)
        {
            CastLightning();
        }
        // Allow movement logic for both Walk AND Falling
        else if (currentState == PlayerState.walk || currentState == PlayerState.falling)
        {
            UpdateAnimationAndMove();
        }
    }

    // --- Movement & Animation ---
    private void UpdateAnimationAndMove()
    {
        if (change != UnityEngine.Vector3.zero)
        {
            isMoving = true;
            animator.SetFloat("moveX", change.x);
            animator.SetFloat("moveY", change.y);
            animator.SetBool("moving", true);
            foreach (var trail in slipTrails) if (trail) trail.emitting = false;

            float currentMoveSpeed = speed;

            // APPLY PENALTY IF FALLING
            if (currentState == PlayerState.falling)
            {
                currentMoveSpeed *= fallingSpeedPenalty;
            }
            // SPRINT IF HOLDING SHIFT
            else if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                currentMoveSpeed = sprintSpeed;
            }

            myRigidbody.MovePosition(myRigidbody.position + (Vector2)change * currentMoveSpeed * Time.fixedDeltaTime);

            if (stepSoundManager != null && lastStepSoundTime >= stepSoundCooldown)
            {
                lastStepSoundTime = 0f;
                stepSoundManager.PlayStepSound(transform.position);
            }
        }
        else
        {
            isMoving = false;
            animator.SetBool("moving", false);
        }
    }
    private void UpdateAnimationSlip()
    {
        currentSlipTimer -= Time.deltaTime;
        Vector2 inputDir = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;

        if (change != Vector3.zero)
        {
            if (inputDir != Vector2.zero)
            {
                float alignment = Vector2.Dot(inputDir, (Vector2)change.normalized);
                currentSlipTimer += alignment * slipInputInfluence * Time.deltaTime;
                currentSlipTimer = Mathf.Min(currentSlipTimer, maxSlipDuration);
                change = Vector3.Lerp(change, inputDir, slipSteeringPower * Time.deltaTime);
                change.Normalize();
            }
        }

        if (currentSlipTimer <= 0)
        {
            currentSlipTimer = 0;
            animator.SetBool("slipping", false);
            changeState(PlayerState.walk);
            return;
        }

        if (change != UnityEngine.Vector3.zero)
        {
            animator.SetFloat("moveX", change.x);
            animator.SetFloat("moveY", change.y);
            animator.SetBool("slipping", true);
            // Same fixedDeltaTime reasoning as walking movement to avoid frame-rate dependent slowdown.
            myRigidbody.MovePosition(myRigidbody.position + (Vector2)change * speed * slipSpeedMultiplier * Time.fixedDeltaTime);

            if (stepSoundManager != null && lastStepSoundTime >= stepSoundCooldown)
            {
                lastStepSoundTime = 0f;
                stepSoundManager.PlayStepSound(transform.position);
            }
        }
    }

    public void BoostSlip(float addedTime, float speedBoost)
    {
        currentSlipTimer += addedTime;
        currentSlipTimer = Mathf.Min(currentSlipTimer, maxSlipDuration);
        slipSpeedMultiplier += speedBoost;
        slipSpeedMultiplier = Mathf.Min(slipSpeedMultiplier, maxSlipSpeedMultiplier);

        if (currentState != PlayerState.slip)
        {
            changeState(PlayerState.slip);
        }
    }

    public void changeState(PlayerState newState)
    {
        if (newState != PlayerState.falling)
        {
            animator.SetBool("falling", false);
        }

        currentState = newState;

        if (newState == PlayerState.falling) animator.SetBool("falling", true);

        if (newState == PlayerState.slip)
        {
            if (slipTrails != null) foreach (var trail in slipTrails) if (trail) trail.emitting = true;
            if (footprintSystem != null) footprintSystem.enabled = false;
        }
        else if (newState == PlayerState.walk)
        {
            if (slipTrails != null) foreach (var trail in slipTrails) if (trail) trail.emitting = false;
            if (footprintSystem != null) footprintSystem.enabled = true;
            slipSpeedMultiplier = defaultSlipSpeedMultiplier;
        }
        else
        {
            if (slipTrails != null) foreach (var trail in slipTrails) if (trail) trail.emitting = false;
            if (footprintSystem != null) footprintSystem.enabled = true;
        }

        if (newState != PlayerState.attack) animator.SetBool("attacking", false);
        if (newState != PlayerState.walk) animator.SetBool("moving", false);
        if (newState != PlayerState.slip) animator.SetBool("slipping", false);
    }

    // --- Falling Logic ---
    public void TripPlayer(float duration)
    {
        if (Time.time < lastFallingTime + fallingCooldown) return;

        if (currentState != PlayerState.falling && currentState != PlayerState.interact)
        {
            lastFallingTime = Time.time;
            StartCoroutine(FallingCo(duration));
        }
    }

    private IEnumerator FallingCo(float duration)
    {
        changeState(PlayerState.falling);

        // Kill initial velocity from impact
        myRigidbody.linearVelocity = Vector2.zero;

        // Note: We do NOT set isMoving=false or block Input here anymore, 
        // because UpdateAnimationAndMove() will now handle slow movement.

        yield return new WaitForSeconds(duration);

        changeState(PlayerState.walk);
    }

    // --- Other Interaction Methods ---
    public void pushed(Vector3 direction, float slipping_time, bool ignoreCooldown = false)
    {
        if (Time.time < lastPushTime + pushCooldown && !ignoreCooldown) return;
        lastPushTime = Time.time;
        changeState(PlayerState.slip);
        currentSlipTimer = Mathf.Min(slipping_time, maxSlipDuration);
        change.x = direction.x;
        change.y = direction.y;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (currentState == PlayerState.slip)
        {
            if (Input.GetKey(KeyCode.G))
            {
                currentSlipTimer = 0;
                change = Vector3.zero;
                if (slipTrails != null) foreach (var trail in slipTrails) if (trail) trail.emitting = false;
                changeState(PlayerState.walk);
            }
            else
            {
                Vector2 surfaceNormal = collision.contacts[0].normal;
                change = Vector2.Reflect(change, surfaceNormal).normalized;
                currentSlipTimer += wallBounceExtension;
                currentSlipTimer = Mathf.Min(currentSlipTimer, maxSlipDuration);
            }
        }
    }

    public void RaiseItem()
    {
        if (currentState != PlayerState.interact)
        {
            animator.SetBool("receive_item", true);
            currentState = PlayerState.interact;
            receiveItemSprite.sprite = inventory.currentItem.itemSprite;
        }
        else
        {
            animator.SetBool("receive_item", false);
            currentState = PlayerState.walk;
            receiveItemSprite.sprite = null;
        }
    }

    private IEnumerator AttackCo()
    {
        animator.SetBool("attacking", true);
        currentState = PlayerState.attack;
        yield return new WaitForSeconds(0.26f);
        animator.SetBool("attacking", false);
        currentState = PlayerState.walk;
    }

    private void CastFireball()
    {
        animator.SetTrigger("castFireball");
        GameObject fireball = Instantiate(fireballPrefab, firePoint.position, Quaternion.identity);
        fireball.GetComponent<Fireball>().SetDirection(new Vector2(animator.GetFloat("moveX"), animator.GetFloat("moveY")));
    }

    private void CastLightning()
    {
        animator.SetTrigger("castFireball");
        Vector2 castDirection = new Vector2(animator.GetFloat("moveX"), animator.GetFloat("moveY"));
        if (castDirection.sqrMagnitude < 0.01f) castDirection = new Vector2(0, -1);
        Vector2 strikePosition = (Vector2)transform.position + castDirection.normalized * lightningCastOffset;
        Instantiate(lightningEffectPrefab, strikePosition, Quaternion.identity);
    }

    public void UpgradeMagicLevel(int amount)
    {
        magicLevel.runtimeValue += amount;
        magicLevel.initialValue += amount;
    }

    public Vector2 GetCurrentMovementDirection()
    {
        return new Vector2(animator.GetFloat("moveX"), animator.GetFloat("moveY")).normalized;
    }

    public void TakeDamage(float damage)
    {
        if (Time.time < nextDamageTime) return;

        nextDamageTime = Time.time + damageInvulnerabilityDuration;
        currentHealth.runtimeValue -= damage;
        playerHealthSignal.Raise();

        if (currentHealth.runtimeValue <= 0)
        {
            // Handle player death (e.g., trigger death animation, disable controls, etc.)
            Debug.Log("Player has died.");

            if (playerDefeatedSignal != null)
            {
                playerDefeatedSignal.Raise();
                currentHealth.runtimeValue = currentHealth.initialValue;
            }
            else
            {
                // For now, we just reset health for testing purposes
                currentHealth.runtimeValue = currentHealth.initialValue;
                nextDamageTime = 0f;
                playerHealthSignal.Raise();
            }
        }

        if (damageFlashCoroutine != null)
        {
            StopCoroutine(damageFlashCoroutine);
        }

        damageFlashCoroutine = StartCoroutine(TakeDamageCo());
    }

    private IEnumerator TakeDamageCo()
    {
        Debug.Log("Player took damage, current health: " + currentHealth.runtimeValue);
        if (damageFlashRenderer == null)
        {
            damageFlashCoroutine = null;
            yield break;
        }

        SyncDamageFlashRenderer();
        damageFlashRenderer.enabled = true;

        yield return new WaitForSeconds(damageFlashDuration);

        damageFlashRenderer.enabled = false;
        damageFlashCoroutine = null;
    }

    private void SetupDamageFlashRenderer()
    {
        if (playerSpriteRenderer == null) return;

        var overlayObject = new GameObject("DamageFlashOverlay");
        overlayObject.transform.SetParent(transform, false);

        damageFlashRenderer = overlayObject.AddComponent<SpriteRenderer>();
        damageFlashRenderer.enabled = false;
        damageFlashRenderer.color = damageFlashColor;
        SyncDamageFlashRenderer();
    }

    private void SyncDamageFlashRenderer()
    {
        if (playerSpriteRenderer == null || damageFlashRenderer == null) return;

        damageFlashRenderer.sprite = playerSpriteRenderer.sprite;
        damageFlashRenderer.flipX = playerSpriteRenderer.flipX;
        damageFlashRenderer.flipY = playerSpriteRenderer.flipY;
        damageFlashRenderer.drawMode = playerSpriteRenderer.drawMode;
        damageFlashRenderer.size = playerSpriteRenderer.size;
        damageFlashRenderer.maskInteraction = playerSpriteRenderer.maskInteraction;
        damageFlashRenderer.spriteSortPoint = playerSpriteRenderer.spriteSortPoint;
        damageFlashRenderer.sortingLayerID = playerSpriteRenderer.sortingLayerID;
        damageFlashRenderer.sortingOrder = playerSpriteRenderer.sortingOrder + 1;
        damageFlashRenderer.sharedMaterial = playerSpriteRenderer.sharedMaterial;
        damageFlashRenderer.color = damageFlashColor;
    }
}
