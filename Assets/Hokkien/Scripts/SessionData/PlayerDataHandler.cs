using UnityEngine;

public class PlayerDataHandler : MonoBehaviour, ISessionData
{
    public void LoadData(GameData data)
    {

        transform.position = data.playerPosition;
    }

    public void SaveData(ref GameData data)
    {
        if (string.IsNullOrEmpty(data.playerId))
        {
            data.playerId = System.Guid.NewGuid().ToString();
        }

        data.playerPosition = transform.position;
    }
}
