using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTracker : MonoBehaviour
{
    public static SceneTracker Instance;

    public string previousSceneName;
    public Vector3 playerReturnPosition;
    public bool isReturningFromBattle = false;
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
        Debug.Log("Recorded scene: " + previousSceneName + " with player position: " + playerReturnPosition);
    }

    public void ReturnToPreviousScene(bool isWin)
    {
        if (string.IsNullOrEmpty(previousSceneName))
        {
            Debug.LogWarning("No previous scene recorded!");
            SceneManager.LoadScene("World1_Revamped");
            //return;
        }

        if (isWin)
        {
            // Just load previous scene
            isReturningFromBattle = true;
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

        if (isReturningFromBattle)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Debug.Log("Returning player to recorded position: " + playerReturnPosition);
                player.transform.position = playerReturnPosition;
            }
            else
            {
                Debug.LogWarning("Player not found in scene after load!");
            }

            isReturningFromBattle = false;
        }

    }
}
