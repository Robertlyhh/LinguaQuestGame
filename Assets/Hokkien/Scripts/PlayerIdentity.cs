using UnityEngine;

public class PlayerIdentity : MonoBehaviour
{
    public static PlayerIdentity Instance { get; private set; }

    [Header("Player Info")]
    public string playerName = "You";
    public Sprite playerPortrait; // Drag your Frog portrait here in the Inspector!

    private void Awake()
    {
        // Standard singleton setup
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }
}