using UnityEngine;
using TMPro;

public class WestCoastNPCUI : MonoBehaviour
{
    public static WestCoastNPCUI Instance;
    public TMP_Text counterText;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        UpdateCounter(0, 0);
    }

    public void UpdateCounter(int current, int total)
    {
        counterText.text = "Talked to: " + current + " / " + total;
    }
}