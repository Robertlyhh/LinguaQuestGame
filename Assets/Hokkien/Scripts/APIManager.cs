using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;
using System.Diagnostics;

public class APIManager : MonoBehaviour
{
    public static APIManager Instance { get; private set; }

    [Header("Backend")]
    [SerializeField] private string baseUrl = "http://localhost:8000";

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    private IEnumerator GetRequest(string url,
        Action<string> onSuccess, Action<string> onError)
    {
        using var req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            UnityEngine.Debug.Log($"[API] GET {url}\n{req.downloadHandler.text}");
            onSuccess?.Invoke(req.downloadHandler.text);
        }
        else
        {
            UnityEngine.Debug.LogError($"[API] GET {url} → {req.error}");
            onError?.Invoke(req.error);
        }
    }

    public void GetVendorProfile(string vendorId,
        Action<VendorProfile> onSuccess,
        Action<string> onError = null)
    {
        StartCoroutine(GetRequest(
            $"{baseUrl}/api/v1/vendors/{vendorId}",
            json =>
            {
                var response = JsonUtility.FromJson<VendorProfileResponse>(json);
                onSuccess?.Invoke(response.data);
            },
            onError
        ));
    }

    public void GetDialogueNode(string nodeId,
    Action<DialogueResponse> onSuccess,
    Action<string> onError = null)
    {
        StartCoroutine(GetRequest(
            $"{baseUrl}/api/v1/dialogue/{nodeId}",
            json =>
            {
                // JsonUtility handles the outer response fine
                var response = JsonUtility.FromJson<DialogueResponse>(json);

                // Manually fix key_words to fix the nested array issue
                // Extract the key_words array from the raw JSON string
                int kwStart = json.IndexOf("\"key_words\":");
                if (kwStart != -1)
                {
                    int arrayStart = json.IndexOf("[", kwStart);
                    int arrayEnd = json.IndexOf("]", arrayStart);
                    if (arrayStart != -1 && arrayEnd != -1)
                    {
                        string kwJson = "{\"items\":" + json.Substring(arrayStart, arrayEnd - arrayStart + 1) + "}";
                        var wrapper = JsonUtility.FromJson<KeyWordWrapper>(kwJson);
                        if (wrapper != null && response.data?.dialogue != null)
                        {
                            response.data.dialogue.key_words = wrapper.items;
                            UnityEngine.Debug.Log($"[API] Manually parsed {wrapper.items.Length} keywords.");
                        }
                    }
                }

                onSuccess?.Invoke(response);
            },
            onError
        ));
    }
}