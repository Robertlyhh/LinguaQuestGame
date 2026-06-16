using UnityEngine;

public class TrackableNPC : MonoBehaviour
{
    private bool hasBeenTalkedTo = false;
    void Start()
    {
        Debug.Log("TrackableNPC registered: " + gameObject.name);
    }
    public void RegisterInteraction()
    {
        if (hasBeenTalkedTo) return;
        hasBeenTalkedTo = true;
        WestCoastNPCTracker.Instance.RegisterNPCTalkedTo();
    }
}