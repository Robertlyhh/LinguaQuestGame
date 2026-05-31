using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking; // Required for WebGL
using UnityEngine.Scripting;

[CreateAssetMenu(menuName = "LinguaQuest/Sentence Runtime Bank", fileName = "SentenceRuntimeBank")]
public class SentenceRuntimeBank : ScriptableObject
{
    public List<string> jsonFiles = new() { "S2.json" };
    [HideInInspector] public List<SentenceData> sentences = new();

    // Change 'void' to 'IEnumerator' and add a callback
    public IEnumerator LoadAllCoroutine(System.Action onComplete)
    {
        sentences.Clear();

        foreach (var fileName in jsonFiles)
        {
            yield return LoadFileCoroutine(fileName);
        }

        Debug.Log($"[Bank] Finished loading {sentences.Count} sentences.");

        // Tell the game we are done, so it can start spawning
        onComplete?.Invoke();
    }

    private void ProcessJson(string json, string fileName)
    {
        // Your parsing logic here
        Debug.Log($"[Bank] Processing {fileName}");
        var pack = JsonUtility.FromJson<SentencePack>(json);
        if (pack != null && pack.sentences != null)
        {
            sentences.AddRange(pack.sentences);
        }
    }

    private IEnumerator LoadFileCoroutine(string fileName)
    {
        foreach (var path in GetCandidatePaths(fileName))
        {
            if (!File.Exists(path))
            {
                continue;
            }

            string requestPath = new System.Uri(path).AbsoluteUri;

            using (UnityWebRequest request = UnityWebRequest.Get(requestPath))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    ProcessJson(request.downloadHandler.text, fileName);
                    yield break;
                }

                Debug.LogError($"[Error] Could not load {fileName} from {path}: {request.error}");
            }
        }

        Debug.LogError($"[Error] Could not find {fileName} in StreamingAssets or Assets/Scripts/SyntaxSword.");
    }

    private IEnumerable<string> GetCandidatePaths(string fileName)
    {
        yield return Path.Combine(Application.streamingAssetsPath, fileName);
        yield return Path.Combine(Application.dataPath, "Scripts", "SyntaxSword", fileName);
    }

    [Preserve]
    [System.Serializable]
    public class SentencePack
    {
        public string packName;
        public List<SentenceData> sentences;
    }
}
