using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;

public enum PlayerState
{
    walk,
    attack,
    interact,
    stagger
}


public class PlayerExploring : MonoBehaviour
{
    // --- Serialized Fields (visible in Inspector) ---
    [Header("Movement")]
    public float speed = 5f;
    public Rigidbody2D myRigidbody;
    public VectorValue StartingPosition;

    [Header("Animation")]
    private Animator animator;

    [Header("Player State")]
    public PlayerState currentState = PlayerState.walk;
    public UnityEngine.Vector3 change = UnityEngine.Vector3.zero;

    [Header("Health & Magic")]
    public FloatValue currentHealth;
    public FloatValue magicLevel;

    [Header("Inventory & Items")]
    public Inventory inventory;
    public SpriteRenderer receiveItemSprite;

    [Header("Signals")]
    public Signal playerHealthSignal;
    public Signal playerAttackSignal;

    [Header("Step Sound")]
    public StepSoundManager stepSoundManager;
    public float stepSoundCooldown = 0.5f;
    private float lastStepSoundTime = 0f;

    [Header("Magic Attacks")]
    public GameObject fireballPrefab;
    public Transform firePoint;
    public GameObject lightningEffectPrefab;
    public float lightningCastOffset = 4.5f;

    [HideInInspector] public bool isMoving;

    // --- Unity Methods ---
    void Start()
    {
        animator = GetComponent<Animator>();
        stepSoundManager = FindObjectOfType<StepSoundManager>();
        myRigidbody = GetComponent<Rigidbody2D>();
        animator.SetFloat("moveX", 0);
        animator.SetFloat("moveY", -1);
        myRigidbody.position = StartingPosition.runtimeValue;
        magicLevel.runtimeValue = magicLevel.initialValue;
    }

    void Update()
    {
        lastStepSoundTime += Time.deltaTime;
        if (currentState == PlayerState.interact)
            return;

        change = UnityEngine.Vector3.zero;
        change.x = Input.GetAxisRaw("Horizontal");
        change.y = Input.GetAxisRaw("Vertical");
        change.z = 0;
        change.Normalize();

        if (Input.GetMouseButtonDown(0) && currentState != PlayerState.attack)
        {
            StartCoroutine(AttackCo());
        }
        else if (Input.GetMouseButtonDown(1) && currentState == PlayerState.attack)
        {
            animator.SetTrigger("swordDance");
        }
        else if (Input.GetKeyDown(KeyCode.F) && magicLevel.runtimeValue >= 1)
        {
            CastFireball();
        }
        else if (Input.GetKeyDown(KeyCode.G) && magicLevel.runtimeValue >= 2)
        {
            CastLightning();
        }
        else if (currentState == PlayerState.walk)
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
            myRigidbody.MovePosition(myRigidbody.position + new UnityEngine.Vector2(change.x, change.y) * speed * Time.fixedDeltaTime);
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

    // --- Item Handling ---
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

    // --- Attack ---
    private IEnumerator AttackCo()
    {
        animator.SetBool("attacking", true);
        currentState = PlayerState.attack;
        yield return new WaitForSeconds(0.26f);
        animator.SetBool("attacking", false);
        currentState = PlayerState.walk;
    }

    public void changeState(PlayerState newState)
    {
        currentState = newState;
        animator.SetBool("attacking", false);
        animator.SetBool("moving", false);
        animator.SetFloat("moveX", 0);
        animator.SetFloat("moveY", 0);
    }

    // --- Magic Attacks ---
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
        if (castDirection.sqrMagnitude < 0.01f)
            castDirection = new Vector2(0, -1);
        Vector2 strikePosition = (Vector2)transform.position + castDirection.normalized * lightningCastOffset;
        Instantiate(lightningEffectPrefab, strikePosition, Quaternion.identity);
    }

    public void UpgradeMagicLevel(int amount)
    {
        magicLevel.runtimeValue += amount;
        magicLevel.initialValue += amount;
    }
}
