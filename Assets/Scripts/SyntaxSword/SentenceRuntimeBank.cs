// Assets/Scripts/Data/SentenceRuntimeBank.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "LinguaQuest/Sentence Runtime Bank", fileName = "SentenceRuntimeBank")]
public class SentenceRuntimeBank : ScriptableObject
{
    [Tooltip("JSON files in StreamingAssets to load at runtime")]
    public List<string> jsonFiles = new() { "test1.json" };

    [HideInInspector] public List<SentenceData> sentences = new();

    public void LoadAll()
    {
        sentences.Clear();
        foreach (var file in jsonFiles)
            sentences.AddRange(SentenceJsonLoader.LoadPackFromStreamingAssets(file));
        Debug.Log($"[SentenceRuntimeBank] Loaded {sentences.Count} sentences from JSON.");
    }
}
