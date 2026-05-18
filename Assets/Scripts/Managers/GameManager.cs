using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public PlayerData playerData;
    public AudioSource audioSource;
    public string startingSceneName = "StartingPage";
    public string playerName = "Explorer";
    public BoolValue tutorialIntroShown;
    public BoolValue houseIntroShown;

    private PetSignalManager petSignalManager;

    public PetSignalManager PetSignalManager => petSignalManager;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist between scenes
            playerData = new PlayerData();
        }
        else
        {
            Destroy(gameObject); // Prevent duplicate managers
            return;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        petSignalManager = GetComponent<PetSignalManager>();
        if (petSignalManager == null)
        {
            petSignalManager = gameObject.AddComponent<PetSignalManager>();
        }
    }

    void Start()
    {
        // Initialize player data or load from saved data
        SceneManager.LoadScene(startingSceneName);
        tutorialIntroShown.runtimeValue = false;
        houseIntroShown.runtimeValue = false;

    }

    public void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public void SetPlayerName(string playerName)
    {
        playerData.setPlayerName(playerName);
    }
}
// Add methods to save and load player data here
