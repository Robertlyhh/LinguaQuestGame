using UnityEngine;

public class SpiritFollower : MonoBehaviour
{
    public Transform playerlocation;
    public GameObject player;
    public float WanderRadius = 0.5f;
    public float WanderSpeed = 1.5f;
    public float maxDistanceToStartChase = 3.5f;
    public float minDistanceToStartWander = 2.0f;
    public float followSpeed = 4f;
    public float switchDelay = 0.5f;
    private float stateTimer = 0f;
    private bool isWandering = false;
    private Vector2 velocity;

    private Vector3 wanderOffset;

    void Start()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            playerlocation = player.transform;
        }
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, playerlocation.position);

        stateTimer -= Time.deltaTime;

        if (!isWandering && distance < minDistanceToStartWander && stateTimer <= 0f)
        {
            isWandering = true;
            stateTimer = switchDelay;
        }
        else if (isWandering && distance > maxDistanceToStartChase && stateTimer <= 0f)
        {
            isWandering = false;
            stateTimer = switchDelay;
        }

        if (isWandering)
        {
            OrbitBehindPlayer();
        }
        else
        {
            ChasePlayer();
        }
    }

    void ChasePlayer()
    {
        Vector2 target = playerlocation.position + Vector3.up * 0.8f;
        transform.position = Vector2.SmoothDamp(transform.position, target, ref velocity, 0.1f, followSpeed);
    }

    void OrbitBehindPlayer()
    {
        // Determine player facing direction
        bool isFacingLeft = player.GetComponent<Animator>().GetFloat("moveX") < 0f;
        bool isFacingUp = player.GetComponent<Animator>().GetFloat("moveY") > 0f;

        // Orbit center is slightly behind the player's head
        Vector3 center = playerlocation.position + new Vector3(isFacingLeft ? 2f : -2f, 2.5f, 0f);
        center += new Vector3(0f, isFacingUp ? 2f : -2f, 0f);

        // Add some randomness to the wandering angle and radius
        float randomOffset = Mathf.PerlinNoise(Time.time * 0.5f, transform.position.x) * Mathf.PI * 2f;
        float angle = Time.time * WanderSpeed + randomOffset;
        float randomRadius = WanderRadius + Mathf.PerlinNoise(transform.position.y, Time.time * 0.5f) * 0.3f;

        float x = Mathf.Cos(angle) * randomRadius;
        float y = Mathf.Sin(angle) * randomRadius;
        Vector3 wanderPosition = center + new Vector3(x, y, 0f);

        // Smoothly move towards the wander position
        transform.position = Vector2.SmoothDamp(transform.position, wanderPosition, ref velocity, 0.15f, WanderSpeed);
    }
}
