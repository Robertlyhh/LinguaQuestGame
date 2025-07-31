using UnityEngine;

public class NaturalFood : MonoBehaviour
{
    public float respawnTime = 5f;
    public GameObject foodPrefab; // Assign in inspector


    private void OnDestroy()
    {
        if (NaturalFoodManager.Instance != null)
        {
            NaturalFoodManager.Instance.RespawnFood(
                foodPrefab, // Prefab reference
                transform.position,
                respawnTime);
        }
        else
        {
            Debug.LogWarning("NaturalFoodManager not found in scene!");
        }
    }
}
