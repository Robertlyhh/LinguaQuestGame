using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;

public class APIManager : MonoBehaviour
{
    public static APIManager Instance { get; private set; }
    public string BaseUrl => baseUrl;

    [Header("Backend")]
    [SerializeField] private string baseUrl = "https://nightmarket-9bb1.onrender.com";
    [SerializeField] private bool verboseNetworkLogs = false;

    private readonly Dictionary<string, VendorProfile> vendorCache = new Dictionary<string, VendorProfile>();
    private readonly Dictionary<string, DialogueResponseData> dialogueNodeCache = new Dictionary<string, DialogueResponseData>();

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    // ─── Core HTTP Helpers ────────────────────────────────────────────────────

    private IEnumerator GetRequest(string url,
        Action<string> onSuccess, Action<string> onError = null)
    {
        using var req = UnityWebRequest.Get(url);
        req.timeout = 60; // Give Render 60 seconds to "wake up"
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            if (verboseNetworkLogs)
                Debug.Log($"[API] GET {url} ({req.downloadHandler.text.Length} chars)");
            onSuccess?.Invoke(req.downloadHandler.text);
        }
        else
        {
            Debug.LogError($"[API] GET {url} → {req.error}");
            onError?.Invoke(req.error);
        }
    }

    private IEnumerator PostRequest<TResponse>(string url, object body,
        Action<TResponse> onSuccess, Action<string> onError = null)
    {
        string jsonBody = JsonUtility.ToJson(body);
        // Use the constructor that sets up the upload handler automatically
        using var req = UnityWebRequest.PostWwwForm(url, jsonBody);

        // Manually override to raw JSON
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.timeout = 60; // Give Render 60 seconds to "wake up"
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            if (verboseNetworkLogs)
                Debug.Log($"[API] POST {url} ({req.downloadHandler.text.Length} chars)");
            onSuccess?.Invoke(JsonUtility.FromJson<TResponse>(req.downloadHandler.text));
        }
        else
        {
            Debug.LogError($"[API] POST {url} → {req.error}\nBody: {req.downloadHandler.text}");
            onError?.Invoke(req.error);
        }
    }

    // ─── Vendors ──────────────────────────────────────────────────────────────

    /// <summary>GET /api/v1/vendors/{vendorId}</summary>
    public void GetVendorProfile(string vendorId,
        Action<VendorProfile> onSuccess, Action<string> onError = null)
    {
        if (string.IsNullOrEmpty(vendorId))
        {
            onError?.Invoke("Vendor id is empty");
            return;
        }

        if (vendorCache.TryGetValue(vendorId, out var cachedVendor))
        {
            onSuccess?.Invoke(cachedVendor);
            return;
        }

        StartCoroutine(GetRequest(
            $"{baseUrl}/api/v1/vendors/{vendorId}",
            json =>
            {
                var wrapper = JsonUtility.FromJson<VendorProfileResponse>(json);
                if (wrapper?.data == null)
                {
                    onError?.Invoke("Failed to parse vendor response");
                    return;
                }

                vendorCache[vendorId] = wrapper.data;
                onSuccess?.Invoke(wrapper.data);
            },
            onError
        ));
    }

    // ─── Dialogue ─────────────────────────────────────────────────────────────

    /// <summary>GET /api/v1/dialogue/{nodeId}</summary>
    public void GetDialogueNode(string nodeId,
        Action<DialogueResponseData> onSuccess, Action<string> onError = null)
    {
        if (string.IsNullOrEmpty(nodeId))
        {
            onError?.Invoke("Dialogue node id is empty");
            return;
        }

        if (dialogueNodeCache.TryGetValue(nodeId, out var cachedNode))
        {
            onSuccess?.Invoke(cachedNode);
            return;
        }

        StartCoroutine(GetRequest(
            $"{baseUrl}/api/v1/dialogue/{nodeId}",
            json =>
            {
                var wrapper = JsonUtility.FromJson<DialogueNodeWrapper>(json);
                if (wrapper?.data != null)
                {
                    dialogueNodeCache[nodeId] = wrapper.data;
                    onSuccess?.Invoke(wrapper.data);
                }
                else
                {
                    onError?.Invoke("Failed to parse dialogue node response");
                }
            },
            onError
        ));
    }

    public bool HasDialogueNodeCached(string nodeId)
    {
        return !string.IsNullOrEmpty(nodeId) && dialogueNodeCache.ContainsKey(nodeId);
    }

    public void PrefetchDialogueNode(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId) || HasDialogueNodeCached(nodeId))
            return;

        GetDialogueNode(nodeId, _ => { }, _ => { });
    }

    /// <summary>GET /api/v1/dialogue/root-nodes/{npcId}</summary>
    public void GetRootNodes(string npcId,
        Action<RootNodesResponse> onSuccess, Action<string> onError = null)
    {
        StartCoroutine(GetRequest(
            $"{baseUrl}/api/v1/dialogue/root-nodes/{npcId}",
            json =>
            {
                var wrapper = JsonUtility.FromJson<RootNodesWrapper>(json);
                if (wrapper != null) onSuccess?.Invoke(wrapper.data);
                else onError?.Invoke("Failed to parse root nodes response");
            },
            onError
        ));
    }

    // ─── Challenges ───────────────────────────────────────────────────────────

    /// <summary>GET /api/v1/challenges</summary>
    public void GetChallenges(
        Action<ChallengesResponse> onSuccess, Action<string> onError = null)
    {
        StartCoroutine(GetRequest(
            $"{baseUrl}/api/v1/challenges",
            json =>
            {
                var wrapper = JsonUtility.FromJson<ChallengesWrapper>(json);
                if (wrapper != null) onSuccess?.Invoke(wrapper.data);
                else onError?.Invoke("Failed to parse challenges response");
            },
            onError
        ));
    }

    /// <summary>GET /api/v1/challenges/{challengeId}</summary>
    public void GetChallenge(string challengeId,
        Action<ChallengeData> onSuccess, Action<string> onError = null)
    {
        StartCoroutine(GetRequest(
            $"{baseUrl}/api/v1/challenges/{challengeId}",
            json =>
            {
                var wrapper = JsonUtility.FromJson<ChallengeWrapper>(json);
                if (wrapper != null) onSuccess?.Invoke(wrapper.data);
                else onError?.Invoke("Failed to parse challenge response");
            },
            onError
        ));
    }

    /// <summary>POST /api/v1/challenges/accept</summary>
    public void AcceptChallenge(string userId, string challengeId,
        Action<ChallengeAcceptResponse> onSuccess, Action<string> onError = null)
    {
        var request = new ChallengeAcceptRequest
        {
            user_id = userId,
            challenge_id = challengeId
        };
        StartCoroutine(PostRequest($"{baseUrl}/api/v1/challenges/accept", request, onSuccess, onError));
    }

    /// <summary>POST /api/v1/challenges/inventory</summary>
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

    /// <summary>POST /api/v1/challenges/verify</summary>
    public void VerifyChallenge(string userId, string challengeId, string answer,
        Action<ChallengeVerifyResponse> onSuccess, Action<string> onError = null)
    {
        var request = new ChallengeVerifyRequest
        {
            user_id = userId,
            challenge_id = challengeId,
            answer = answer
        };
        StartCoroutine(PostRequest($"{baseUrl}/api/v1/challenges/verify", request, onSuccess, onError));
    }

    // ─── Inventory ────────────────────────────────────────────────────────────

    /// <summary>GET /api/v1/user/{userId}/inventory</summary>
    public void GetUserInventory(string userId,
        Action<InventoryResponse> onSuccess, Action<string> onError = null)
    {
        StartCoroutine(GetRequest(
            $"{baseUrl}/api/v1/user/{userId}/inventory",
            json =>
            {
                var wrapper = JsonUtility.FromJson<InventoryWrapper>(json);
                if (wrapper != null) onSuccess?.Invoke(wrapper.data);
                else onError?.Invoke("Failed to parse inventory response");
            },
            onError
        ));
    }
}