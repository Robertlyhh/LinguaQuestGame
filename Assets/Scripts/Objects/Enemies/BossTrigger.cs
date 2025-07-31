using System.Collections.Generic;
using UnityEngine;

public class BossRoomTrigger : MonoBehaviour
{
    public List<string> bossDialogLines;
    public DialogManager dialogManager;  // Reference to your dialog manager
    public string bossFightSceneName = "BossFightingScene";  // Name of your boss fight scene
    public GameObject player;  // Reference to the player GameObject
    public BoolValue isBossDead;  // Reference to the BoolValue that tracks if the boss is dead

    private bool triggered = false;

    private void Start()
    {
        if (isBossDead.runtimeValue)
        {
            // If the boss is already dead, destroy this trigger
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!triggered && other.CompareTag("Player"))
        {
            triggered = true;
            dialogManager.StartDialog(bossDialogLines, OnDialogComplete);
        }
    }

    private void OnDialogComplete()
    {
        player.GetComponent<PlayerExploring>().StartingPosition.runtimeValue = player.transform.position;
        SceneTracker.Instance.RecordSceneAndPosition(player.transform.position);
        UnityEngine.SceneManagement.SceneManager.LoadScene(bossFightSceneName);
    }
}
