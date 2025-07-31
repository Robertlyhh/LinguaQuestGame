using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTracker : MonoBehaviour
{
    public static SceneTracker Instance;

    public string previousSceneName;
    public Vector3 playerReturnPosition;
    private bool shouldRespawnAfterLoad = false;
    private Vector3 pendingRespawnPosition;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RecordSceneAndPosition(Vector3 playerPos)
    {
        previousSceneName = SceneManager.GetActiveScene().name;
        playerReturnPosition = playerPos;
    }

    public void ReturnToPreviousScene(bool isWin)
    {
        if (string.IsNullOrEmpty(previousSceneName))
        {
            Debug.LogWarning("No previous scene recorded!");
            return;
        }

        if (isWin)
        {
            // Just load previous scene
            SceneManager.LoadScene(previousSceneName);
        }
        else
        {
            // Set respawn flag and load previous scene
            if (PlayerRespawnManager.Instance != null)
            {
                pendingRespawnPosition = PlayerRespawnManager.Instance.GetRespawnPoint();
                shouldRespawnAfterLoad = true;
                SceneManager.LoadScene(previousSceneName);
            }
            else
            {
                Debug.LogWarning("PlayerRespawnManager not found!");
                SceneManager.LoadScene(previousSceneName);
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (shouldRespawnAfterLoad)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Debug.Log("Respawning player at: " + pendingRespawnPosition);
                player.transform.position = pendingRespawnPosition;
            }
            else
            {
                Debug.LogWarning("Player not found in scene after load!");
            }

            shouldRespawnAfterLoad = false;
        }
    }
}
