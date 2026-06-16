using UnityEngine;

public class WestCoastNPCTracker : MonoBehaviour
{
    public static WestCoastNPCTracker Instance;

    public int totalNPCs = 0;
    private int talkedTo = 0;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Invoke(nameof(CountNPCs), 0.2f);
    }

    private void CountNPCs()
    {
        totalNPCs = FindObjectsByType<TrackableNPC>(FindObjectsSortMode.None).Length;
        Debug.Log("Total NPCs found: " + totalNPCs);
        UpdateUI(); // NOW called AFTER counting
    }

    public void RegisterNPCTalkedTo()
    {
        talkedTo++;
        Debug.Log("Talked to: " + talkedTo + " / " + totalNPCs);
        UpdateUI();

        if (talkedTo >= totalNPCs)
        {
            Debug.Log("All NPCs talked to! Unlocking Longhouse Greeter.");
            LonghouseGreeter.Instance.Unlock();
        }
    }

    private void UpdateUI()
    {
        if (WestCoastNPCUI.Instance != null)
            WestCoastNPCUI.Instance.UpdateCounter(talkedTo, totalNPCs);
        else
            Debug.LogError("WestCoastNPCUI.Instance is null!");
    }
}