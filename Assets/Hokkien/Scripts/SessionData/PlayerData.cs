using UnityEngine;
[System.Serializable]

public class GameData
{
    public string playerId;
    public string playerName;
    public int playerBalance;
    public Vector3 playerPosition;

    public GameData()
    {
        // initial values for new game
        this.playerBalance = 0;
        this.playerPosition = Vector3.zero;
    }
}
