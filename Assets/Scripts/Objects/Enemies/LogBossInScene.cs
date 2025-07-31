using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogBossInScene : MonoBehaviour
{
    public BoolValue isBossDead;
    public GameObject coinPrefab;


    void Start()
    {
        if (isBossDead.runtimeValue)
        {
            // Instantiate the coin prefab at the boss's position
            Vector3 bossPosition = transform.position; // Assuming this script is attached to the boss GameObject
            int coinCount = 8;
            float radius = 1.5f; // Adjust the radius as needed
            for (int i = 0; i < coinCount; i++)
            {
                float angle = i * Mathf.PI * 2 / coinCount;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                Vector3 coinPosition = bossPosition + offset;
                Instantiate(coinPrefab, coinPosition, Quaternion.identity);
            }
            Destroy(gameObject); // Destroy the boss GameObject
        }
    }
}
