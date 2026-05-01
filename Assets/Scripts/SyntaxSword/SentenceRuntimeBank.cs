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
        foreach (var requestPath in GetCandidateRequestPaths(fileName))
        {
            using (UnityWebRequest request = UnityWebRequest.Get(requestPath))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    ProcessJson(request.downloadHandler.text, fileName);
                    yield break;
                }

                Debug.LogWarning($"[Bank] Could not load {fileName} from {requestPath}: {request.error}");
            }
        }

        Debug.LogError($"[Bank] Could not load {fileName} from any configured path.");
    }

    private IEnumerable<string> GetCandidateRequestPaths(string fileName)
    {
        string streamingAssetsPath = Path.Combine(Application.streamingAssetsPath, fileName);

#if UNITY_WEBGL && !UNITY_EDITOR
        yield return streamingAssetsPath;
#else
        yield return new System.Uri(streamingAssetsPath).AbsoluteUri;

        string editorPath = Path.Combine(Application.dataPath, "Scripts", "SyntaxSword", fileName);
        if (File.Exists(editorPath))
        {
            yield return new System.Uri(editorPath).AbsoluteUri;
        }
#endif
    }

    [Preserve]
    [System.Serializable]
    public class SentencePack
    {
        public string packName;
        public List<SentenceData> sentences;
    }
}
