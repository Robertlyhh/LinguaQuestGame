using UnityEngine;
[System.Serializable]

public class GameData
{
    public string playerId;
    public string playerName;
    public int playerBalance;
    public Vector3 playerPosition;
    public System.Collections.Generic.List<string> acquiredItemIds;

    public GameData()
    {
        this.playerBalance = 0;
        this.playerPosition = Vector3.zero;
        this.acquiredItemIds = new System.Collections.Generic.List<string>();
    }

    public static string DefaultPlayerId()
    {
        return "player_001";
    }
}
