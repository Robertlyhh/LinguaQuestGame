using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RewardSpawner : MonoBehaviour
{
    private AudioSource audioSource;
    public List<BoolValue> activateConditions;
    public Camera targetCamera;
    public GameObject rewardPrefab;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = FindObjectOfType<Camera>();
        }

    }


    public void CheckConditions()
    {
        bool allConditionsMet = true;

        foreach (BoolValue condition in activateConditions)
        {
            if (!condition.runtimeValue)
            {
                allConditionsMet = false;
                Debug.Log($"Condition {condition.name} not met.");
                break;
            }
        }

        if (allConditionsMet)
        {
            SpawnReward();
        }
        else
        {
            Debug.Log("Not all conditions met for spawning reward.");
        }

    }

    private void SpawnReward()
    {
        if (rewardPrefab != null)
        {
            rewardPrefab.SetActive(true);
            targetCamera.GetComponent<CameraMovement>().PayAttentionTo(rewardPrefab);
            Debug.Log("Reward spawned successfully.");
        }
        else
        {
            Debug.LogWarning("Reward prefab is not assigned.");
        }
    }
}
