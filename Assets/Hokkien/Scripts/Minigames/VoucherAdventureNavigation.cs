using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Keeps Voucher Adventure scene changes limited to approved Hokkien lessons.
/// </summary>
public static class VoucherAdventureNavigation
{
    public const string MinigameId = "voucher_adventure";
    public const string MinigameScene = "HokkienVoucherAdventure";

    private const string OriginSceneKey = "VoucherAdventure.OriginScene";
    private const string OriginXKey = "VoucherAdventure.OriginX";
    private const string OriginYKey = "VoucherAdventure.OriginY";
    private const string ReturnPendingKey = "VoucherAdventure.ReturnPending";

    private static bool subscribed;

    public static bool HasOrigin => IsAllowedOrigin(PlayerPrefs.GetString(OriginSceneKey, string.Empty));

    public static string OriginScene => HasOrigin
        ? PlayerPrefs.GetString(OriginSceneKey)
        : string.Empty;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (subscribed)
            return;

        SceneManager.sceneLoaded += RestorePlayerAfterReturn;
        subscribed = true;
    }

    public static bool TryLaunch(Transform player)
    {
        string currentScene = SceneManager.GetActiveScene().name;
        if (!IsAllowedOrigin(currentScene))
        {
            Debug.LogWarning($"[Voucher Adventure] '{currentScene}' is not an approved origin scene.");
            return false;
        }

        PlayerPrefs.SetString(OriginSceneKey, currentScene);
        if (player != null)
        {
            PlayerPrefs.SetFloat(OriginXKey, player.position.x);
            PlayerPrefs.SetFloat(OriginYKey, player.position.y);
        }

        PlayerPrefs.SetInt(ReturnPendingKey, 0);
        PlayerPrefs.Save();
        SceneManager.LoadScene(MinigameScene);
        return true;
    }

    public static bool ReturnToOrigin()
    {
        string origin = OriginScene;
        if (string.IsNullOrEmpty(origin))
            return false;

        PlayerPrefs.SetInt(ReturnPendingKey, 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(origin);
        return true;
    }

    private static void RestorePlayerAfterReturn(Scene scene, LoadSceneMode mode)
    {
        if (PlayerPrefs.GetInt(ReturnPendingKey, 0) != 1 || scene.name != OriginScene)
            return;

        frogMove player = Object.FindFirstObjectByType<frogMove>();
        if (player != null)
        {
            Vector3 position = player.transform.position;
            position.x = PlayerPrefs.GetFloat(OriginXKey, position.x);
            position.y = PlayerPrefs.GetFloat(OriginYKey, position.y);
            player.transform.position = position;
        }

        ClearOrigin();
    }

    private static void ClearOrigin()
    {
        PlayerPrefs.DeleteKey(OriginSceneKey);
        PlayerPrefs.DeleteKey(OriginXKey);
        PlayerPrefs.DeleteKey(OriginYKey);
        PlayerPrefs.DeleteKey(ReturnPendingKey);
        PlayerPrefs.Save();
    }

    private static bool IsAllowedOrigin(string sceneName)
    {
        return sceneName == "Hokkien3" || sceneName == "Hokkien4";
    }
}
