using System.Collections;
using UnityEngine;


public class PlayerMovement : MonoBehaviour
{

    public float speed = 5f;
    public Rigidbody2D myRigidbody;
    public UnityEngine.Vector3 change = UnityEngine.Vector3.zero; // Use UnityEngine.Vector3 to match the original code
    private Animator animator;
    public FloatValue currentHealth;
    public PlayerState currentState = PlayerState.walk;
    public Signal playerHealthSignal;
    public Signal playerAttackSignal;
    public VectorValue StartingPosition;
    public Inventory inventory;
    public SpriteRenderer receiveItemSprite;
    public bool isFighting; // Flag to indicate if the player is in a fight
    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        myRigidbody = GetComponent<Rigidbody2D>();
        animator.SetFloat("moveX", 0);
        animator.SetFloat("moveY", -1);
    }




    public void takeDamage(float damage)
    {
        currentHealth.runtimeValue -= damage; // Reduce the player's health by the damage amount
        StartCoroutine(BeingHit()); // Trigger stagger animation
        playerHealthSignal.Raise(); // Notify that the player's health has changed
        if (currentHealth.runtimeValue <= 0)
        {
            //DeathEffect(); // Trigger death effect
            this.gameObject.SetActive(false); // Deactivate the player if health is zero or below
        }
    }

    public void AttackEnemy()
    {
        if (currentState != PlayerState.attack && currentState != PlayerState.stagger)
        {
            StartCoroutine(AttackEnemyCo());
        }
    }

    private IEnumerator AttackEnemyCo()
    {
        animator.SetBool("Hitting", true);

        yield return new WaitForSeconds(1.25f); // Wait for the attack animation to play
        animator.SetBool("Hitting", false);
    }

    public void Doge()
    {
        StartCoroutine(DogeCo());
    }

    private IEnumerator DogeCo()
    {
        animator.SetBool("doge", true);
        yield return new WaitForSeconds(0.7f); // Wait for the dodge animation to play
        animator.SetBool("doge", false);
    }

    private IEnumerator BeingHit()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        Color originalColor = spriteRenderer.color;
        // 70% red: interpolate between original and red by 0.7
        Color targetColor = Color.Lerp(originalColor, Color.red, 0.7f);
        float duration = 0.2f;
        float elapsed = 0f;
        yield return new WaitForSeconds(0.2f); // Wait before starting the color change

        // Gradually turn 70% red
        while (elapsed < duration)
        {
            spriteRenderer.color = Color.Lerp(originalColor, targetColor, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        spriteRenderer.color = targetColor;

        yield return new WaitForSeconds(0.2f);

        // Gradually return to original color
        elapsed = 0f;
        while (elapsed < duration)
        {
            spriteRenderer.color = Color.Lerp(targetColor, originalColor, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        spriteRenderer.color = originalColor;
    }
}