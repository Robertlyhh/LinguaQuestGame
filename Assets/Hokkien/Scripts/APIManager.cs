using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;

public class APIManager : MonoBehaviour
{
    public static APIManager Instance { get; private set; }
    // Pick Render or Localhost in Assets/Hokkien/Resources/BackendConfig.

    public string VoucherBaseUrl => voucherBaseUrl;

    [Header("Backend")]
   
    [SerializeField] private bool verboseNetworkLogs = false;
    [SerializeField] private int maxPrefetchConcurrency = 2;

    private readonly Dictionary<string, VendorProfile> vendorCache = new Dictionary<string, VendorProfile>();
    private readonly Dictionary<string, DialogueResponseData> dialogueNodeCache = new Dictionary<string, DialogueResponseData>();
    private readonly Dictionary<string, PendingVendorRequest> pendingVendorRequests = new Dictionary<string, PendingVendorRequest>();
    private readonly Dictionary<string, PendingDialogueRequest> pendingDialogueRequests = new Dictionary<string, PendingDialogueRequest>();
    private readonly Queue<string> dialoguePrefetchQueue = new Queue<string>();
    private readonly HashSet<string> queuedDialoguePrefetches = new HashSet<string>();
    private int activeDialoguePrefetchRequests;

    private sealed class PendingVendorRequest
    {
        public readonly List<Action<VendorProfile>> successCallbacks = new List<Action<VendorProfile>>();
        public readonly List<Action<string>> errorCallbacks = new List<Action<string>>();
    }

    private sealed class PendingDialogueRequest
    {
        public readonly List<Action<DialogueResponseData>> successCallbacks = new List<Action<DialogueResponseData>>();
        public readonly List<Action<string>> errorCallbacks = new List<Action<string>>();
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log($"[API] Using backend {BaseUrl}");
        }
        else { Destroy(gameObject); }
    }

    // ─── Core HTTP Helpers ────────────────────────────────────────────────────

    private IEnumerator GetRequest(string url,
        Action<string> onSuccess, Action<string> onError = null)
    {
        using var req = UnityWebRequest.Get(url);
        req.timeout = 60; // Allow time for a slow backend response.
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
        req.timeout = 60; // Allow time for a slow backend response.
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

    private IEnumerator VoucherJsonRequest<TResponse>(
        string method, string url, object body, string idempotencyKey,
        Action<TResponse> onSuccess, Action<VoucherApiError> onError)
    {
        using var req = new UnityWebRequest(url, method);
        req.downloadHandler = new DownloadHandlerBuffer();
        if (body != null)
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(body));
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.SetRequestHeader("Content-Type", "application/json");
        }

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
            req.SetRequestHeader("Idempotency-Key", idempotencyKey);

        req.timeout = 60;
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            if (verboseNetworkLogs)
                Debug.Log($"[API] {method} {url} ({req.downloadHandler.text.Length} chars)");
            onSuccess?.Invoke(JsonUtility.FromJson<TResponse>(req.downloadHandler.text));
            yield break;
        }

        VoucherApiError apiError = null;
        if (!string.IsNullOrWhiteSpace(req.downloadHandler.text))
        {
            try
            {
                apiError = JsonUtility.FromJson<VoucherApiError>(req.downloadHandler.text);
            }
            catch (Exception)
            {
                // Preserve the transport error when the server did not return JSON.
            }
        }

        apiError ??= new VoucherApiError();
        if (string.IsNullOrWhiteSpace(apiError.message))
            apiError.message = req.error;
        string logMessage =
            $"[API] {method} {url} → {req.responseCode} {apiError.code}: {apiError.message}";
        if (apiError.code == "ACTIVE_SESSION_EXISTS")
        {
            if (verboseNetworkLogs)
                Debug.Log(logMessage);
        }
        else
            Debug.LogError(logMessage);
        onError?.Invoke(apiError);
    }

    private static string NewIdempotencyKey()
    {
        return Guid.NewGuid().ToString();
    }

    public string GetVendorBaseUrl(string vendorId)
    {
        return IsVoucherId(vendorId, "v_va_") ? voucherBaseUrl : baseUrl;
    }

    public string GetDialogueBaseUrl(string nodeId)
    {
        return IsVoucherId(nodeId, "n_va_") ? voucherBaseUrl : baseUrl;
    }

    private static bool IsVoucherId(string value, string prefix)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
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

        string requestBaseUrl = GetVendorBaseUrl(vendorId).TrimEnd('/');
        Debug.Log($"[API] GET vendor profile: {requestBaseUrl}/api/v1/vendors/{vendorId}");

        if (vendorCache.TryGetValue(vendorId, out var cachedVendor))
        {
            onSuccess?.Invoke(cachedVendor);
            return;
        }

        if (pendingVendorRequests.TryGetValue(vendorId, out var pendingVendor))
        {
            if (onSuccess != null) pendingVendor.successCallbacks.Add(onSuccess);
            if (onError != null) pendingVendor.errorCallbacks.Add(onError);
            return;
        }

        pendingVendor = new PendingVendorRequest();
        if (onSuccess != null) pendingVendor.successCallbacks.Add(onSuccess);
        if (onError != null) pendingVendor.errorCallbacks.Add(onError);
        pendingVendorRequests[vendorId] = pendingVendor;

        StartCoroutine(GetRequest(
            $"{requestBaseUrl}/api/v1/vendors/{vendorId}",
            json =>
            {
                var wrapper = JsonUtility.FromJson<VendorProfileResponse>(json);
                if (wrapper?.data == null)
                {
                    CompleteVendorRequestError(vendorId, "Failed to parse vendor response");
                    return;
                }

                vendorCache[vendorId] = wrapper.data;
                CompleteVendorRequestSuccess(vendorId, wrapper.data);
            },
            error => CompleteVendorRequestError(vendorId, error)
        ));
    }

    // ─── Dialogue ─────────────────────────────────────────────────────────────

    /// <summary>GET /api/v1/quizzes/{levelId}</summary>
    public void GetHokkienQuiz(string levelId,
        Action<HokkienQuizData> onSuccess, Action<string> onError = null)
    {
        if (string.IsNullOrWhiteSpace(levelId))
        {
            onError?.Invoke("Quiz level id is empty");
            return;
        }

        StartCoroutine(GetRequest(
            $"{BaseUrl}/api/v1/quizzes/{levelId}",
            json =>
            {
                HokkienQuizResponse response = JsonUtility.FromJson<HokkienQuizResponse>(json);
                if (response?.data?.questions == null || response.data.questions.Length == 0)
                {
                    onError?.Invoke("Backend returned an empty quiz");
                    return;
                }

                onSuccess?.Invoke(response.data);
            },
            onError));
    }

    /// <summary>GET /api/v1/dialogue/{nodeId}</summary>
    public void GetDialogueNode(string nodeId,
        Action<DialogueResponseData> onSuccess, Action<string> onError = null)
    {
        if (string.IsNullOrEmpty(nodeId))
        {
            onError?.Invoke("Dialogue node id is empty");
            return;
        }

        string requestBaseUrl = GetDialogueBaseUrl(nodeId).TrimEnd('/');
        Debug.Log($"[API] GET dialogue node: {requestBaseUrl}/api/v1/dialogue/{nodeId}");

        if (dialogueNodeCache.TryGetValue(nodeId, out var cachedNode))
        {
            onSuccess?.Invoke(cachedNode);
            return;
        }

        if (pendingDialogueRequests.TryGetValue(nodeId, out var pendingDialogue))
        {
            if (onSuccess != null) pendingDialogue.successCallbacks.Add(onSuccess);
            if (onError != null) pendingDialogue.errorCallbacks.Add(onError);
            return;
        }

        pendingDialogue = new PendingDialogueRequest();
        if (onSuccess != null) pendingDialogue.successCallbacks.Add(onSuccess);
        if (onError != null) pendingDialogue.errorCallbacks.Add(onError);
        pendingDialogueRequests[nodeId] = pendingDialogue;

        StartCoroutine(GetRequest(
            $"{requestBaseUrl}/api/v1/dialogue/{nodeId}",
            json =>
            {
                var wrapper = JsonUtility.FromJson<DialogueNodeWrapper>(json);
                if (wrapper?.data != null)
                {
                    dialogueNodeCache[nodeId] = wrapper.data;
                    CompleteDialogueRequestSuccess(nodeId, wrapper.data);
                }
                else
                {
                    CompleteDialogueRequestError(nodeId, "Failed to parse dialogue node response");
                }
            },
            error => CompleteDialogueRequestError(nodeId, error)
        ));
    }

    /// <summary>GET a session-rendered Voucher Adventure dialogue node.</summary>
    public void GetVoucherDialogueNode(string nodeId, string sessionId,
        Action<DialogueResponseData> onSuccess, Action<VoucherApiError> onError = null)
    {
        if (string.IsNullOrWhiteSpace(nodeId) || string.IsNullOrWhiteSpace(sessionId))
        {
            onError?.Invoke(new VoucherApiError { code = "INVALID_REQUEST", message = "Dialogue node and session are required" });
            return;
        }

        string url = $"{voucherBaseUrl.TrimEnd('/')}/api/v1/dialogue/{UnityWebRequest.EscapeURL(nodeId)}" +
                     $"?session_id={UnityWebRequest.EscapeURL(sessionId)}";
        StartCoroutine(VoucherJsonRequest<DialogueNodeWrapper>(
            UnityWebRequest.kHttpVerbGET, url, null, null,
            wrapper =>
            {
                if (wrapper?.data != null) onSuccess?.Invoke(wrapper.data);
                else onError?.Invoke(new VoucherApiError { code = "INVALID_RESPONSE", message = "Dialogue response is empty" });
            },
            onError));
    }

    // ─── Voucher Adventure ──────────────────────────────────────────────────

    public void CreateVoucherSession(VoucherSessionCreateRequest request,
        Action<VoucherSessionData> onSuccess, Action<VoucherApiError> onError)
    {
        StartCoroutine(VoucherJsonRequest<VoucherSessionWrapper>(
            UnityWebRequest.kHttpVerbPOST,
            $"{voucherBaseUrl.TrimEnd('/')}/api/v1/voucher-adventure/sessions",
            request, NewIdempotencyKey(),
            wrapper => onSuccess?.Invoke(wrapper?.data), onError));
    }

    public void RevealVoucherOffer(string sessionId, string offerInstanceId, VoucherRevealRequest request,
        Action<VoucherOfferInstance> onSuccess, Action<VoucherApiError> onError)
    {
        StartCoroutine(VoucherJsonRequest<VoucherOfferWrapper>(
            UnityWebRequest.kHttpVerbPOST,
            $"{voucherBaseUrl.TrimEnd('/')}/api/v1/voucher-adventure/sessions/{UnityWebRequest.EscapeURL(sessionId)}" +
            $"/offers/{UnityWebRequest.EscapeURL(offerInstanceId)}/reveal",
            request, NewIdempotencyKey(), wrapper => onSuccess?.Invoke(wrapper?.data), onError));
    }

    public void SubmitVoucherAttempt(VoucherAttemptRequest request,
        Action<VoucherAttemptResult> onSuccess, Action<VoucherApiError> onError)
    {
        StartCoroutine(VoucherJsonRequest<VoucherAttemptWrapper>(
            UnityWebRequest.kHttpVerbPOST,
            $"{voucherBaseUrl.TrimEnd('/')}/api/v1/voucher-adventure/attempts",
            request, NewIdempotencyKey(), wrapper => onSuccess?.Invoke(wrapper?.data), onError));
    }

    public void UpdateVoucherBasket(string sessionId, VoucherBasketRequest request,
        Action<VoucherBasketData> onSuccess, Action<VoucherApiError> onError)
    {
        StartCoroutine(VoucherJsonRequest<VoucherBasketWrapper>(
            UnityWebRequest.kHttpVerbPUT,
            $"{voucherBaseUrl.TrimEnd('/')}/api/v1/voucher-adventure/sessions/{UnityWebRequest.EscapeURL(sessionId)}/basket",
            request, NewIdempotencyKey(), wrapper => onSuccess?.Invoke(wrapper?.data), onError));
    }

    public void RecordVoucherEvent(string sessionId, VoucherEventRequest request,
        Action<VoucherEventResult> onSuccess = null, Action<VoucherApiError> onError = null)
    {
        StartCoroutine(VoucherJsonRequest<VoucherEventWrapper>(
            UnityWebRequest.kHttpVerbPOST,
            $"{voucherBaseUrl.TrimEnd('/')}/api/v1/voucher-adventure/sessions/{UnityWebRequest.EscapeURL(sessionId)}/events",
            request, NewIdempotencyKey(), wrapper => onSuccess?.Invoke(wrapper?.data), onError));
    }

    public void CompleteVoucherSession(string sessionId, int basketRevision,
        Action<VoucherCompletionResult> onSuccess, Action<VoucherApiError> onError)
    {
        StartCoroutine(VoucherJsonRequest<VoucherCompleteWrapper>(
            UnityWebRequest.kHttpVerbPOST,
            $"{voucherBaseUrl.TrimEnd('/')}/api/v1/voucher-adventure/sessions/{UnityWebRequest.EscapeURL(sessionId)}/complete",
            new VoucherCompleteRequest { basket_revision = basketRevision }, NewIdempotencyKey(),
            wrapper => onSuccess?.Invoke(wrapper?.data), onError));
    }

    public void AbandonVoucherSession(string sessionId,
        Action<VoucherSimpleResult> onSuccess = null, Action<VoucherApiError> onError = null)
    {
        StartCoroutine(VoucherJsonRequest<VoucherSimpleWrapper>(
            UnityWebRequest.kHttpVerbPOST,
            $"{voucherBaseUrl.TrimEnd('/')}/api/v1/voucher-adventure/sessions/{UnityWebRequest.EscapeURL(sessionId)}/abandon",
            new object(), NewIdempotencyKey(), wrapper => onSuccess?.Invoke(wrapper?.data), onError));
    }

    public bool HasDialogueNodeCached(string nodeId)
    {
        return !string.IsNullOrEmpty(nodeId) && dialogueNodeCache.ContainsKey(nodeId);
    }

    public void PrefetchDialogueNode(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId)
            || HasDialogueNodeCached(nodeId)
            || pendingDialogueRequests.ContainsKey(nodeId)
            || queuedDialoguePrefetches.Contains(nodeId))
            return;

        queuedDialoguePrefetches.Add(nodeId);
        dialoguePrefetchQueue.Enqueue(nodeId);
        PumpDialoguePrefetchQueue();
    }

    private void PumpDialoguePrefetchQueue()
    {
        int concurrencyLimit = Mathf.Max(1, maxPrefetchConcurrency);

        while (activeDialoguePrefetchRequests < concurrencyLimit && dialoguePrefetchQueue.Count > 0)
        {
            string nodeId = dialoguePrefetchQueue.Dequeue();

            if (string.IsNullOrEmpty(nodeId) || HasDialogueNodeCached(nodeId) || pendingDialogueRequests.ContainsKey(nodeId))
            {
                queuedDialoguePrefetches.Remove(nodeId);
                continue;
            }

            activeDialoguePrefetchRequests++;
            GetDialogueNode(nodeId,
                _ => CompleteDialoguePrefetch(nodeId),
                _ => CompleteDialoguePrefetch(nodeId));
        }
    }

    private void CompleteDialoguePrefetch(string nodeId)
    {
        queuedDialoguePrefetches.Remove(nodeId);
        activeDialoguePrefetchRequests = Mathf.Max(0, activeDialoguePrefetchRequests - 1);
        PumpDialoguePrefetchQueue();
    }

    private void CompleteVendorRequestSuccess(string vendorId, VendorProfile profile)
    {
        if (!pendingVendorRequests.TryGetValue(vendorId, out var pending))
            return;

        pendingVendorRequests.Remove(vendorId);
        foreach (var callback in pending.successCallbacks)
            callback?.Invoke(profile);
    }

    private void CompleteVendorRequestError(string vendorId, string error)
    {
        if (!pendingVendorRequests.TryGetValue(vendorId, out var pending))
            return;

        pendingVendorRequests.Remove(vendorId);
        foreach (var callback in pending.errorCallbacks)
            callback?.Invoke(error);
    }

    private void CompleteDialogueRequestSuccess(string nodeId, DialogueResponseData data)
    {
        if (!pendingDialogueRequests.TryGetValue(nodeId, out var pending))
            return;

        pendingDialogueRequests.Remove(nodeId);
        foreach (var callback in pending.successCallbacks)
            callback?.Invoke(data);
    }

    private void CompleteDialogueRequestError(string nodeId, string error)
    {
        if (!pendingDialogueRequests.TryGetValue(nodeId, out var pending))
            return;

        pendingDialogueRequests.Remove(nodeId);
        foreach (var callback in pending.errorCallbacks)
            callback?.Invoke(error);
    }

    /// <summary>GET /api/v1/dialogue/root-nodes/{npcId}</summary>
    public void GetRootNodes(string npcId,
        Action<RootNodesResponse> onSuccess, Action<string> onError = null)
    {
        Debug.Log($"[API] GET dialogue root nodes: {BaseUrl}/api/v1/dialogue/root-nodes/{npcId}");

        StartCoroutine(GetRequest(
            $"{BaseUrl}/api/v1/dialogue/root-nodes/{npcId}",
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
            $"{BaseUrl}/api/v1/challenges",
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
            $"{BaseUrl}/api/v1/challenges/{challengeId}",
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
        StartCoroutine(PostRequest($"{BaseUrl}/api/v1/challenges/accept", request, onSuccess, onError));
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
        StartCoroutine(PostRequest($"{BaseUrl}/api/v1/challenges/inventory", request, onSuccess, onError));
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
        StartCoroutine(PostRequest($"{BaseUrl}/api/v1/challenges/verify", request, onSuccess, onError));
    }

    // ─── Inventory ────────────────────────────────────────────────────────────

    /// <summary>GET /api/v1/user/{userId}/inventory</summary>
    public void GetUserInventory(string userId,
        Action<InventoryResponse> onSuccess, Action<string> onError = null)
    {
        StartCoroutine(GetRequest(
            $"{BaseUrl}/api/v1/user/{userId}/inventory",
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
