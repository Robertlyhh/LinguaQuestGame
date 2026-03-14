using UnityEngine;

public interface ISessionData
{
    void LoadData(GameData data);
    void SaveData(ref GameData data);
}
