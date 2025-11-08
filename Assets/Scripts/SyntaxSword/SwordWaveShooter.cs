using UnityEngine;

public class SwordWaveShooter : MonoBehaviour
{
    [Header("Wave Settings")]
    [Tooltip("Default wave type (shoots by default)")]
    public GameObject swordWavePrefab1;
    [Tooltip("Alternative wave type (switch to this)")]
    public GameObject swordWavePrefab2;
    public Signal switchtoWave2Signal;
    public Signal switchtoWave1Signal;
    public float energyCost = 10f;

    [Header("Wave Selection")]
    [Tooltip("Current active wave type (1 or 2)")]
    private int currentWaveType = 1;

    [Header("Spawn Points (4 directions)")]
    [Tooltip("Spawn point when facing right")]
    public Transform spawnPointRight;
    [Tooltip("Spawn point when facing left")]
    public Transform spawnPointLeft;
    [Tooltip("Spawn point when facing up")]
    public Transform spawnPointUp;
    [Tooltip("Spawn point when facing down")]
    public Transform spawnPointDown;

    [Header("Spawn Offset (if no spawn points assigned)")]
    [Tooltip("Distance from player center to spawn the wave")]
    public float spawnDistance = 1.5f;

    [Header("References")]
    private PlayerExploring player;
    private Animator animator;
    private Camera mainCamera;

    void Awake()
    {
        // Get PlayerExploring component (same GameObject since script is on player)
        player = GetComponent<PlayerExploring>();
        if (player == null)
        {
            Debug.LogError("[SwordWaveShooter] PlayerExploring component not found!");
        }

        // Get Animator from player
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("[SwordWaveShooter] Animator component not found!");
        }

        // Get main camera
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("[SwordWaveShooter] Main Camera not found!");
        }

        // Start with wave type 1
        currentWaveType = 1;
    }

    void Update()
    {
        // Right-click to switch wave type
        if (Input.GetMouseButtonDown(1))
        {
            SwitchWaveType();
        }

        // Left-click to shoot wave
        if (Input.GetMouseButtonDown(0))
        {
            // Check if player has enough energy
            if (SwordWaveManager.Instance != null && SwordWaveManager.Instance.HasEnergy((int)energyCost))
            {
                ShootWave();
            }
            else
            {
                Debug.Log("[SwordWaveShooter] Not enough energy to cast sword wave!");
            }
        }
    }

    void SwitchWaveType()
    {
        // Toggle between wave type 1 and 2
        currentWaveType = (currentWaveType == 1) ? 2 : 1;
        if (currentWaveType == 1)
            switchtoWave1Signal.Raise();
        else
            switchtoWave2Signal.Raise();
        Debug.Log($"[SwordWaveShooter] Switched to Wave Type {currentWaveType}");

        // Optional: Add visual feedback or sound effect here
    }

    void ShootWave()
    {
        // Get the correct prefab based on current wave type
        GameObject wavePrefab = (currentWaveType == 1) ? swordWavePrefab1 : swordWavePrefab2;

        if (wavePrefab == null)
        {
            Debug.LogError($"[SwordWaveShooter] Wave Type {currentWaveType} prefab is not assigned!");
            return;
        }

        // Get mouse position in world space
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        // Calculate direction from player to mouse
        Vector2 shootDirection = (mouseWorldPos - transform.position).normalized;

        // Get the player's current facing direction for spawn point selection
        Vector2 facingDirection = GetPlayerFacingDirection();

        // Get the appropriate spawn position based on facing direction
        Vector3 spawnPosition = GetSpawnPosition(facingDirection);

        // Instantiate the wave
        GameObject wave = Instantiate(
                    wavePrefab,
                    spawnPosition,
                    Quaternion.identity  // Start with no rotation
                );
        wave.transform.rotation = Quaternion.Euler(0, 0, 180);

        // Initialize the wave with the direction towards mouse
        SwordWave waveScript = wave.GetComponent<SwordWave>();
        if (waveScript != null)
        {
            waveScript.Initialize(shootDirection);
            Debug.Log($"[SwordWaveShooter] Fired Wave Type {currentWaveType} towards mouse. Direction: {shootDirection}");
        }
        else
        {
            Debug.LogError("[SwordWaveShooter] SwordWave component not found on prefab!");
        }

        // Consume energy
        if (SwordWaveManager.Instance != null)
        {
            SwordWaveManager.Instance.UseEnergy((int)energyCost);
        }
    }

    /// <summary>
    /// Gets the appropriate spawn position based on player's facing direction
    /// </summary>
    Vector3 GetSpawnPosition(Vector2 facingDirection)
    {
        // Determine which direction the player is facing (cardinal directions)
        if (Mathf.Abs(facingDirection.x) > Mathf.Abs(facingDirection.y))
        {
            // Facing horizontally (left or right)
            if (facingDirection.x > 0)
            {
                // Facing Right
                return spawnPointRight != null ? spawnPointRight.position : transform.position + Vector3.right * spawnDistance;
            }
            else
            {
                // Facing Left
                return spawnPointLeft != null ? spawnPointLeft.position : transform.position + Vector3.left * spawnDistance;
            }
        }
        else
        {
            // Facing vertically (up or down)
            if (facingDirection.y > 0)
            {
                // Facing Up
                return spawnPointUp != null ? spawnPointUp.position : transform.position + Vector3.up * spawnDistance;
            }
            else
            {
                // Facing Down
                return spawnPointDown != null ? spawnPointDown.position : transform.position + Vector3.down * spawnDistance;
            }
        }
    }

    /// <summary>
    /// Gets the player's current facing direction from the animator's moveX/moveY values
    /// </summary>
    Vector2 GetPlayerFacingDirection()
    {
        if (animator == null) return Vector2.down;

        float moveX = animator.GetFloat("moveX");
        float moveY = animator.GetFloat("moveY");

        Vector2 direction = new Vector2(moveX, moveY);

        // If the player isn't moving, use the last facing direction stored in animator
        if (direction.sqrMagnitude < 0.01f)
        {
            // Default to facing down
            direction = Vector2.down;
        }

        return direction.normalized;
    }

    // Public getter for UI or other systems
    public int GetCurrentWaveType()
    {
        return currentWaveType;
    }

    // Optional: Visualize spawn positions in editor
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Draw all 4 spawn points
        Gizmos.color = currentWaveType == 1 ? Color.cyan : Color.magenta;

        if (spawnPointRight != null)
        {
            Gizmos.DrawWireSphere(spawnPointRight.position, 0.2f);
            Gizmos.DrawLine(transform.position, spawnPointRight.position);
        }
        else
        {
            Vector3 pos = transform.position + Vector3.right * spawnDistance;
            Gizmos.DrawWireSphere(pos, 0.2f);
        }

        if (spawnPointLeft != null)
        {
            Gizmos.DrawWireSphere(spawnPointLeft.position, 0.2f);
            Gizmos.DrawLine(transform.position, spawnPointLeft.position);
        }
        else
        {
            Vector3 pos = transform.position + Vector3.left * spawnDistance;
            Gizmos.DrawWireSphere(pos, 0.2f);
        }

        if (spawnPointUp != null)
        {
            Gizmos.DrawWireSphere(spawnPointUp.position, 0.2f);
            Gizmos.DrawLine(transform.position, spawnPointUp.position);
        }
        else
        {
            Vector3 pos = transform.position + Vector3.up * spawnDistance;
            Gizmos.DrawWireSphere(pos, 0.2f);
        }

        if (spawnPointDown != null)
        {
            Gizmos.DrawWireSphere(spawnPointDown.position, 0.2f);
            Gizmos.DrawLine(transform.position, spawnPointDown.position);
        }
        else
        {
            Vector3 pos = transform.position + Vector3.down * spawnDistance;
            Gizmos.DrawWireSphere(pos, 0.2f);
        }

        // Draw mouse direction (only in play mode)
        if (Application.isPlaying && mainCamera != null)
        {
            Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0f;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, mouseWorldPos);
        }
    }
#endif
}