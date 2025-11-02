// Assets/Scripts/Data/SentenceJsonLoader.cs
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public static class SentenceJsonLoader
{
    [System.Serializable] private class EntryDTO { public string word; public string shownLabel; public bool isLabelCorrect; }
    [System.Serializable]
    private class SentenceDTO
    {
        public string guid;
        public string sentence;
        public string language;
        public string topic;
        public string subtopic;
        public int difficulty;
        public List<EntryDTO> entries;
    }
    [System.Serializable]
    private class PackDTO
    {
        public string packName;
        public string language;
        public List<SentenceDTO> sentences;
    }

    public static List<SentenceData> LoadPackFromStreamingAssets(string filename)
    {
        var path = Path.Combine(Application.streamingAssetsPath, filename);
        if (!File.Exists(path)) { Debug.LogWarning($"[SentenceJsonLoader] Missing: {path}"); return new List<SentenceData>(); }

        var json = File.ReadAllText(path);
        var pack = JsonUtility.FromJson<PackDTO>(json);
        var list = new List<SentenceData>();

        if (pack == null || pack.sentences == null) return list;

        foreach (var s in pack.sentences)
        {
            var sd = ScriptableObject.CreateInstance<SentenceData>();
            sd.guid = string.IsNullOrEmpty(s.guid) ? System.Guid.NewGuid().ToString() : s.guid;
            sd.sentence = s.sentence;
            sd.language = string.IsNullOrEmpty(s.language) ? pack.language : s.language;
            sd.topic = s.topic; sd.subtopic = s.subtopic; sd.difficulty = s.difficulty;

            sd.entries = new List<SentenceData.Entry>();
            if (s.entries != null)
            {
                foreach (var e in s.entries)
                    sd.entries.Add(new SentenceData.Entry
                    {
                        word = e.word,
                        shownLabel = e.shownLabel,
                        isLabelCorrect = e.isLabelCorrect
                    });
            }
            list.Add(sd);
        }
        return list;
    }
}
