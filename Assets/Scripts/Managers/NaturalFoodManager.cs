using UnityEngine;
using System.Collections;

public class NaturalFoodManager : MonoBehaviour
{
    public static NaturalFoodManager Instance;

    private void Awake()
    {
        // Singleton pattern so there's only one manager
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Stay alive between scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Respawn a natural food prefab at a position after a delay.
    /// </summary>
    public void RespawnFood(GameObject foodPrefab, Vector3 position, float delay)
    {
        StartCoroutine(RespawnRoutine(foodPrefab, position, delay));
    }

    private IEnumerator RespawnRoutine(GameObject foodPrefab, Vector3 position, float delay)
    {
        yield return new WaitForSeconds(delay);

        // Respawn new instance
        Instantiate(foodPrefab, position, Quaternion.identity);
    }
}
