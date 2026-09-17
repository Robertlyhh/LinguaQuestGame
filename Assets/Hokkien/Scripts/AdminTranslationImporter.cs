using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// Uses the project's existing APIModel types (DialogueNodeWrapper, DialogueResponseData, DialogueContent)
public class AdminTranslationImporter : MonoBehaviour
{
    public string baseUrl = "http://localhost:8000";
    public float pollInterval = 1f;
    public float pollTimeout = 30f;

    [Serializable]
    private class TranslateRequest
    {
        public string input_text;
        public string output_lang;
    }

    [Serializable]
    private class TtsRequest
    {
        public string translation_text;
    }

    [Serializable]
    private class CreateOptionRequest
    {
        public string option_id;
        public string option_text;
        public string next_node_id;
        public string feedback_type;
    }

    [Serializable]
    public class TranslationResult
    {
        public string englishText;
        public string pojTranslation;
        public string audioUrl;
    }

    [Serializable]
    private class AdminOptionWrapper { public string status; public AdminOptionData data; }

    [Serializable]
    private class AdminOptionData
    {
        public string option_id;
        public string option_text;
        public string translation_POJ;
        public string audio;
    }

    // Public entry point. Calls translate -> wait for translation -> call tts -> wait for audio -> returns result
    public IEnumerator TranslateThenTtsThenFetch(string nodeId, string englishText, Action<TranslationResult> onSuccess, Action<string> onError)
    {
        if (string.IsNullOrEmpty(nodeId))
        {
            onError?.Invoke("nodeId is empty");
            yield break;
        }

        // 1) POST translate
        var translateReq = new TranslateRequest { input_text = englishText, output_lang = "POJ" };
        string translateJson = JsonUtility.ToJson(translateReq);
        string translateUrl = $"{baseUrl}/api/admin/model/translate/{nodeId}";

        using (var req = new UnityWebRequest(translateUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(translateJson);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"Translate request failed: {req.error}");
                yield break;
            }
        }

        // 2) Poll GET /api/admin/dialogues/<node_id> until translation_POJ appears
        DialogueNodeWrapper wrapper = null;
        float waited = 0f;
        string poj = null;
        string audioPath = null;

        while (waited < pollTimeout)
        {
            yield return StartCoroutine(GetDialogueNode(nodeId, result => wrapper = result, err => wrapper = null));
            if (wrapper != null && wrapper.data != null && wrapper.data.dialogue != null)
            {
                poj = wrapper.data.dialogue.translation_POJ;
                audioPath = wrapper.data.dialogue.audio; // may be null until TTS step
                if (!string.IsNullOrEmpty(poj)) break;
            }
            yield return new WaitForSeconds(pollInterval);
            waited += pollInterval;
        }

        if (string.IsNullOrEmpty(poj))
        {
            onError?.Invoke("Timed out waiting for POJ translation");
            yield break;
        }

        // 3) POST TTS using the POJ (or translation text)
        var ttsReq = new TtsRequest { translation_text = poj };
        string ttsJson = JsonUtility.ToJson(ttsReq);
        string ttsUrl = $"{baseUrl}/api/admin/model/tts/{nodeId}";

        using (var req2 = new UnityWebRequest(ttsUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(ttsJson);
            req2.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req2.downloadHandler = new DownloadHandlerBuffer();
            req2.SetRequestHeader("Content-Type", "application/json");
            yield return req2.SendWebRequest();

            if (req2.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"TTS request failed: {req2.error}");
                yield break;
            }
        }

        // 4) Poll until audio path appears (or audio blob)
        waited = 0f;
        wrapper = null;
        audioPath = null;

        while (waited < pollTimeout)
        {
            yield return StartCoroutine(GetDialogueNode(nodeId, result => wrapper = result, err => wrapper = null));
            if (wrapper != null && wrapper.data != null && wrapper.data.dialogue != null)
            {
                audioPath = wrapper.data.dialogue.audio;
                if (!string.IsNullOrEmpty(audioPath)) break;
            }
            yield return new WaitForSeconds(pollInterval);
            waited += pollInterval;
        }

        if (string.IsNullOrEmpty(audioPath))
        {
            onError?.Invoke("Timed out waiting for audio to be generated");
            yield break;
        }

        // Construct full audio URL if needed
        string audioUrl = audioPath;
        if (!audioUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            audioUrl = baseUrl.TrimEnd('/') + "/" + audioPath.TrimStart('/');

        var res = new TranslationResult
        {
            englishText = englishText,
            pojTranslation = poj,
            audioUrl = audioUrl
        };

        onSuccess?.Invoke(res);
    }

    public IEnumerator DownloadAudioClip(string audioUrl, Action<AudioClip> onSuccess, Action<string> onError = null)
    {
        if (string.IsNullOrWhiteSpace(audioUrl))
        {
            onError?.Invoke("audioUrl is empty");
            yield break;
        }

        string pathWithoutQuery = audioUrl.Split('?')[0];
        AudioType audioType = pathWithoutQuery.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)
            ? AudioType.WAV
            : AudioType.MPEG;

        using (var req = UnityWebRequestMultimedia.GetAudioClip(audioUrl, audioType))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke(DownloadHandlerAudioClip.GetContent(req));
            }
            else
            {
                onError?.Invoke(req.error);
            }
        }
    }

    public IEnumerator TranslateOptionThenTtsThenFetch(string optionId, string optionText,
        Action<TranslationResult> onSuccess, Action<string> onError)
    {
        if (string.IsNullOrWhiteSpace(optionId))
        {
            onError?.Invoke("optionId is empty");
            yield break;
        }

        bool optionReady = false;
        yield return StartCoroutine(EnsureAdminOption(optionId, optionText,
            () => optionReady = true, onError));
        if (!optionReady) yield break;

        bool requestSucceeded = false;
        var translateReq = new TranslateRequest { input_text = optionText, output_lang = "POJ" };
        yield return StartCoroutine(PostJson(
            $"{baseUrl}/api/admin/model/translate-option/{optionId}",
            JsonUtility.ToJson(translateReq), () => requestSucceeded = true, onError));
        if (!requestSucceeded) yield break;

        AdminOptionData option = null;
        float waited = 0f;
        while (waited < pollTimeout)
        {
            yield return StartCoroutine(GetAdminOption(optionId, result => option = result, _ => option = null));
            if (option != null && !string.IsNullOrWhiteSpace(option.translation_POJ)) break;
            yield return new WaitForSeconds(pollInterval);
            waited += pollInterval;
        }
        if (option == null || string.IsNullOrWhiteSpace(option.translation_POJ))
        {
            onError?.Invoke("Timed out waiting for option POJ translation");
            yield break;
        }

        requestSucceeded = false;
        var ttsReq = new TtsRequest { translation_text = option.translation_POJ };
        yield return StartCoroutine(PostJson(
            $"{baseUrl}/api/admin/model/tts-option/{optionId}",
            JsonUtility.ToJson(ttsReq), () => requestSucceeded = true, onError));
        if (!requestSucceeded) yield break;

        waited = 0f;
        while (waited < pollTimeout)
        {
            yield return StartCoroutine(GetAdminOption(optionId, result => option = result, _ => option = null));
            if (option != null && !string.IsNullOrWhiteSpace(option.audio)) break;
            yield return new WaitForSeconds(pollInterval);
            waited += pollInterval;
        }
        if (option == null || string.IsNullOrWhiteSpace(option.audio))
        {
            onError?.Invoke("Timed out waiting for option audio");
            yield break;
        }

        string audioUrl = option.audio;
        if (!audioUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            audioUrl = baseUrl.TrimEnd('/') + "/" + audioUrl.TrimStart('/');
        onSuccess?.Invoke(new TranslationResult
        {
            englishText = optionText,
            pojTranslation = option.translation_POJ,
            audioUrl = audioUrl
        });
    }

    private IEnumerator EnsureAdminOption(string optionId, string optionText, Action onSuccess, Action<string> onError)
    {
        AdminOptionData existing = null;
        yield return StartCoroutine(GetAdminOption(optionId, result => existing = result, _ => { }));
        if (existing != null)
        {
            onSuccess?.Invoke();
            yield break;
        }

        var create = new CreateOptionRequest
        {
            option_id = optionId,
            option_text = optionText,
            next_node_id = string.Empty,
            feedback_type = "quiz"
        };
        yield return StartCoroutine(PostJson(
            $"{baseUrl}/api/admin/options/quiz-generated",
            JsonUtility.ToJson(create), onSuccess, onError));
    }

    private IEnumerator PostJson(string url, string json, Action onSuccess, Action<string> onError)
    {
        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();
        if (req.result == UnityWebRequest.Result.Success) onSuccess?.Invoke();
        else onError?.Invoke($"Request failed ({req.responseCode}): {req.error} {req.downloadHandler.text}");
    }

    private IEnumerator GetAdminOption(string optionId, Action<AdminOptionData> onSuccess, Action<string> onError)
    {
        using var req = UnityWebRequest.Get($"{baseUrl}/api/admin/option/{optionId}");
        yield return req.SendWebRequest();
        if (req.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(req.error);
            yield break;
        }
        try
        {
            AdminOptionWrapper wrapper = JsonUtility.FromJson<AdminOptionWrapper>(req.downloadHandler.text);
            onSuccess?.Invoke(wrapper?.data);
        }
        catch (Exception e) { onError?.Invoke(e.Message); }
    }

    // Helper: GET dialogue node and parse into DialogueNodeWrapper
    private IEnumerator GetDialogueNode(string nodeId, Action<DialogueNodeWrapper> onSuccess, Action<string> onError)
    {
        string url = $"{baseUrl}/api/admin/dialogues/{nodeId}";
        using (var req = UnityWebRequest.Get(url))
        {
            req.downloadHandler = new DownloadHandlerBuffer();
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var json = req.downloadHandler.text;
                    var wrapper = JsonUtility.FromJson<DialogueNodeWrapper>(json);
                    onSuccess?.Invoke(wrapper);
                }
                catch (Exception e)
                {
                    onError?.Invoke($"Failed to parse dialogue response: {e.Message}");
                }
            }
            else
            {
                onError?.Invoke(req.error);
            }
        }
    }
}
