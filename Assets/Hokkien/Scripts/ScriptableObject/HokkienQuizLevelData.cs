using UnityEngine;

[CreateAssetMenu(fileName = "HokkienQuizLevelData", menuName = "Hokkien/Quiz Level Data")]
public class HokkienQuizLevelData : ScriptableObject
{
    [Header("Level Info")]
    public string levelName;

    [Header("Content")]
    public HokkienQuizQuestionBank questionBank;

    [Header("Progression")]
    public string successSceneName;
    public string failureSceneName;
}