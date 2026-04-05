using System;

// ─── Shared ───────────────────────────────────────────────────────────────────

[Serializable]
public class ResponseMeta
{
    public int processTimeMS;
}

// ─── Dialogue ─────────────────────────────────────────────────────────────────

[Serializable]
public class DialogueNodeWrapper
{
    public string status;
    public DialogueResponseData data;
    public ResponseMeta meta;
}

[Serializable]
public class DialogueResponseData
{
    public DialogueContent dialogue;
    public string dialogue_node;
    public string[] next_nodes;
    public DialogueOption[] options;
}

[Serializable]
public class DialogueContent
{
    public string audio;
    public string dialogue_id;
    public KeyWordData[] key_words;
    public string npc_id;
    public string text;
    public string translation_HAN;
    public string translation_POJ;
}

[Serializable]
public class KeyWordData
{
    public string audio;
    public string context;
    public string translation;
    public string word;
    public string word_id;
}

[Serializable]
public class DialogueOption
{
    public string feedback_type;
    public string next_node;
    public string option_id;
    public string text;
    public DialogueEvent[] events;
}

[Serializable]
public class DialogueEvent
{
    public string event_id;
    public string event_type;
    public string metadata;
}

[Serializable]
public class PurchaseEventMetadata
{
    public string item_id;
    public string challenge_id;
}

// ─── Root Nodes ───────────────────────────────────────────────────────────────

[Serializable]
public class RootNodesWrapper
{
    public string status;
    public RootNodesResponse data;
    public ResponseMeta meta;
}

[Serializable]
public class RootNodesResponse
{
    public string npc_id;
    public string[] root_nodes;
}

// ─── Vendors ──────────────────────────────────────────────────────────────────

[Serializable]
public class VendorProfileResponse
{
    public string status;
    public VendorProfile data;
    public ResponseMeta meta;
}

[Serializable]
public class VendorProfile
{
    public string vendor_id;
    public string dialogue_node_id;
    public string vendor_name;
    public VendorItem[] items;
}

[Serializable]
public class VendorItem
{
    public string item_id;
    public string item_name;
    public string description;
    public int item_value;
}

// ─── Challenges ───────────────────────────────────────────────────────────────

[Serializable]
public class ChallengesWrapper
{
    public string status;
    public ChallengesResponse data;
    public ResponseMeta meta;
}

[Serializable]
public class ChallengesResponse
{
    public ChallengeData[] challenges;
}

[Serializable]
public class ChallengeWrapper
{
    public string status;
    public ChallengeData data;
    public ResponseMeta meta;
}

[Serializable]
public class ChallengeData
{
    public string challenge_id;
    public string challenge_name;
    public string description;
    public string npc_id;
    public string item_id;
}

[Serializable]
public class ChallengeAcceptRequest
{
    public string user_id;
    public string challenge_id;
}

[Serializable]
public class ChallengeAcceptResponse
{
    public string status;
    public ChallengeAcceptData data;
}

[Serializable]
public class ChallengeAcceptData
{
    public string user_id;
    public string challenge_id;
    public string accepted_at;
}

[Serializable]
public class ChallengeVerifyRequest
{
    public string user_id;
    public string challenge_id;
    public string answer;
}

[Serializable]
public class ChallengeVerifyResponse
{
    public string status;
    public ChallengeVerifyData data;
}

[Serializable]
public class ChallengeVerifyData
{
    public bool correct;
    public string feedback;
    public string next_node;
}

// ─── Inventory ────────────────────────────────────────────────────────────────

[Serializable]
public class InventoryAddRequest
{
    public string user_id;
    public string item_id;
    public string challenge_id;
}

[Serializable]
public class InventoryAddResponse
{
    public string status;
    public InventoryAddData data;
}

[Serializable]
public class InventoryAddData
{
    public string user_id;
    public string item_id;
    public string challenge_id;
    public string acquired_at;
}

[Serializable]
public class InventoryWrapper
{
    public string status;
    public InventoryResponse data;
    public ResponseMeta meta;
}

[Serializable]
public class InventoryResponse
{
    public string user_id;
    public string active_challenge_id;
    public InventoryItemResponse[] inventory;
}

[Serializable]
public class InventoryItemResponse
{
    public string item_id;
    public string challenge_id;
    public string acquired_at;
}