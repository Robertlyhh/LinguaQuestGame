// Assets/Scripts/Data/SentenceData.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "LinguaQuest/Sentence", fileName = "SentenceData")]
public class SentenceData : ScriptableObject
{
    [TextArea(1, 3)] public string sentence = "I went for a run.";
    public string language = "en";
    public string topic = "word_classes";
    public string subtopic = "noun_vs_verb";
    [Range(1, 10)] public int difficulty = 2;
    public string sourceId;
    public string guid = System.Guid.NewGuid().ToString();

    [System.Serializable]
    public struct Entry
    {
        public string word;
        public string shownLabel;
        public bool isLabelCorrect;
    }
    public List<Entry> entries = new();
}
