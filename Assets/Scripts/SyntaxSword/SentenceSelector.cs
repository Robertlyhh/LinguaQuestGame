// Assets/Scripts/Data/SentenceSelector.cs
using System.Collections.Generic;
using UnityEngine;

public class SentenceSelector
{
    private readonly List<SentenceData> _pool;
    private readonly HashSet<string> _seen = new();
    private System.Random _rng = new System.Random();

    public SentenceSelector(List<SentenceData> source) { _pool = source; }

    public SentenceData PickOne(string topic = null, int minDiff = 1, int maxDiff = 10, bool avoidRepeats = true)
    {
        var candidates = _pool.FindAll(s =>
            s.difficulty >= minDiff && s.difficulty <= maxDiff &&
            (string.IsNullOrEmpty(topic) || s.topic == topic) &&
            (!avoidRepeats || !_seen.Contains(s.guid)));

        if (candidates.Count == 0) return null;
        var pick = candidates[_rng.Next(candidates.Count)];
        if (avoidRepeats) _seen.Add(pick.guid);
        return pick;
    }

    public List<SentenceData> PickSet(int count, string topic = null, int minDiff = 1, int maxDiff = 10, bool avoidRepeats = true)
    {
        var result = new List<SentenceData>();
        for (int i = 0; i < count; i++)
        {
            var s = PickOne(topic, minDiff, maxDiff, avoidRepeats);
            if (s == null) break;
            result.Add(s);
        }
        return result;
    }

    public void ResetSession() => _seen.Clear();
}
