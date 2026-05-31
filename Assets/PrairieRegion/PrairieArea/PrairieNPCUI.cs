using UnityEngine;
using TMPro;

public class PrairieNPCUI : MonoBehaviour
{
    public static PrairieNPCUI Instance;

    public TMP_Text counterText;
    void Start()
    {
        UpdateCounter(0, PrairieNPCTracker.Instance.totalNPCs);
    }
    void Awake()
    {
        Instance = this;
    }

    public void UpdateCounter(int current, int total)
    {
        counterText.text = "Talked to: " + current + " / " + total;
    }
}