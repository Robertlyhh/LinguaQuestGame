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

    private IEnumerator PostRequest<T>(string url, object body,
        Action<T> onSuccess, Action<string> onError)
    {
        string jsonBody = JsonUtility.ToJson(body);
        using var req = new UnityWebRequest(url, "POST");
        req.SetRequestHeader("Content-Type", "application/json");
        req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonBody));
        req.downloadHandler = new DownloadHandlerBuffer();
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            UnityEngine.Debug.Log($"[API] POST {url}\n{req.downloadHandler.text}");
            var response = JsonUtility.FromJson<T>(req.downloadHandler.text);
            onSuccess?.Invoke(response);
        }
        else
        {
            UnityEngine.Debug.LogError($"[API] POST {url} → {req.error}\nResponse: {req.downloadHandler.text}");
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
    Action<DialogueResponseData> onSuccess,
    Action<string> onError = null)
    {
        StartCoroutine(GetRequest(
            $"{baseUrl}/api/v1/dialogue/{nodeId}",
            json =>
            {
                var wrapper = JsonUtility.FromJson<DialogueNodeWrapper>(json);
                if (wrapper != null)
                {
                    onSuccess?.Invoke(wrapper.data);
                }
                else
                {
                    onError?.Invoke("Failed to parse response");
                }
            },
            onError
        ));
    }

    public void AddToInventory(string userId, string itemId, string challengeId,
        Action<InventoryAddResponse> onSuccess, Action<string> onError = null)
    {
        var request = new InventoryAddRequest
        {
            user_id = userId,
            item_id = itemId,
            challenge_id = string.IsNullOrEmpty(challengeId) ? null : challengeId
        };
        StartCoroutine(PostRequest($"{baseUrl}/api/v1/challenges/inventory", request, onSuccess, onError));
    }

    public void GetUserInventory(string userId,
        Action<InventoryResponse> onSuccess, Action<string> onError = null)
    {
        StartCoroutine(GetRequest(
            $"{baseUrl}/api/v1/user/{userId}/inventory",
            json =>
            {
                var wrapper = JsonUtility.FromJson<InventoryWrapper>(json);
                onSuccess?.Invoke(wrapper.data);
            },
            onError
        ));
    }
}
