using UnityEngine;
using UnityEngine.UI;

public class ScreenSpaceIndicator : MonoBehaviour
{
    [Header("Targets")]
    public Transform targetNPC;          // Drag your NPC 20 here
    public Transform playerMC;           // Drag your main character (Frog) here
    private Camera mainCamera;

    [Header("UI Adjustments")]
    // Positions the arrow slightly above the Frog's head so it doesn't cover them
    public Vector3 playerOffset = new Vector3(0, 1.5f, 0); 
    
    // How close they can get before we stop updating rotation (prevents zero-vector spin)
    public float minDistanceToRotate = 0.05f; 

    void Start()
    {
        mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (targetNPC == null || playerMC == null || mainCamera == null) return;

        // 1. Calculate where the arrow should sit on the screen (above the MC)
        Vector3 playerScreenPos = mainCamera.WorldToScreenPoint(playerMC.position + playerOffset);

        // 2. Lock the UI Arrow directly onto the player's screen position
        transform.position = new Vector3(playerScreenPos.x, playerScreenPos.y, 0);

        // 3. Calculate the exact directional vector in 2D WORLD space for precision
        // We use the raw positions here so UI scaling doesn't distort the angle
        Vector3 worldDirection = targetNPC.position - (playerMC.position + playerOffset);

        // 4. Distance check: Only update rotation if we are far enough away.
        // If we get closer than this threshold, it simply holds its last valid angle pointing at the NPC.
        if (worldDirection.sqrMagnitude > minDistanceToRotate)
        {
            // Calculate the exact angle from the Player to the NPC
            float angle = Mathf.Atan2(worldDirection.y, worldDirection.x) * Mathf.Rad2Deg; 
            
            // Apply the rotation (assuming your PNG natively points right)
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward); 
        }
    }
}