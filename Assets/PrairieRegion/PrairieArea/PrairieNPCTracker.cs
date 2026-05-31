using UnityEngine;

public class PrairieNPCTracker : MonoBehaviour
{
    public static PrairieNPCTracker Instance;

    public int totalNPCs = 6;
    private bool[] visited;
    private int visitedCount = 0;
    public FloatValue NPCsVisited;

    void Awake()
    {
        Instance = this;
        visited = new bool[totalNPCs];
        visitedCount = (int)NPCsVisited.runtimeValue;
    }

    public void MarkVisited(int id)
    {
        if (visited[id]) return;

        visited[id] = true;
        visitedCount++;
        NPCsVisited.runtimeValue = visitedCount;

        PrairieNPCUI.Instance.UpdateCounter(visitedCount, totalNPCs);
    }

    public bool AllVisited()
    {
        return visitedCount >= totalNPCs;
    }
}