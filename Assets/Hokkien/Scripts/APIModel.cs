using System;

// === Dialogue Response ===
// GET /api/v1/dialogue/<node_id>
[Serializable]
public class DialogueResponse
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
    public string translation;
}

[Serializable]
public class KeyWordData
{
    public string audio;
    public string context;
    public string translation;
    public string word;
    public string word_id;
    //public string romanized; we will add this back in once the romanized api works
}

[Serializable]
public class DialogueOption
{
    public string feedback_type;
    public string next_node;
    public string option_id;
    public string text;
}

[Serializable]
public class ResponseMeta
{
    public int processTimeMS;
}

// === Vendor Profile ===
// GET /api/v1/vendors/<vendor_id>
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

[System.Serializable]
public class KeyWordWrapper
{
    public KeyWordData[] items;
}
