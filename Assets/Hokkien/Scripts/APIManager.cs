using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;

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
            Debug.Log($"[API] GET {url}\n{req.downloadHandler.text}");
            onSuccess?.Invoke(req.downloadHandler.text);
        }
        else
        {
            Debug.LogError($"[API] GET {url} → {req.error}");
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
                var vendor = JsonUtility.FromJson<VendorProfile>(json);
                onSuccess?.Invoke(vendor);
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
                var response = JsonUtility.FromJson<DialogueResponse>(json);
                onSuccess?.Invoke(response);
            },
            onError
        ));
    }
}