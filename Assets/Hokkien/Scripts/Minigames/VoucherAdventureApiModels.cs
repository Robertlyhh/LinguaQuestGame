using System;

// ─── Voucher Adventure ───────────────────────────────────────────────────────

[Serializable]
public class VoucherApiError
{
    public string code;
    public string message;
    public string session_id;
    public int expected_basket_revision;
    public int expected_attempt_number;
}

[Serializable]
public class VoucherSessionCreateRequest
{
    public string player_id;
    public string difficulty;
    public string content_version;
}

[Serializable]
public class VoucherSessionWrapper
{
    public string status;
    public VoucherSessionData data;
}

[Serializable]
public class VoucherSessionData
{
    public string session_id;
    public string difficulty;
    public VoucherSessionVendor[] vendors;
    public VoucherBasketData basket;
    public VoucherTimerData timer;
}

[Serializable]
public class VoucherSessionVendor
{
    public string vendor_id;
    public string offer_instance_id;
    public string dialogue_node_id;
    public VoucherOfferInstance offer;
}

[Serializable]
public class VoucherOfferInstance
{
    public string offer_instance_id;
    public string offer_id;
    public string vendor_id;
    public string offer_type;
    public int base_price_cents;
    public int bundle_size;
    public string label_han;
    public string label_en;
    public VoucherQuestionInstance[] questions;
    public VoucherResolvedOffer offer_resolved;
}

[Serializable]
public class VoucherQuestionInstance
{
    public string question_instance_id;
    public string skill;
    public string prompt_en;
    public string answer_unit;
    public int order;
}

[Serializable]
public class VoucherResolvedOffer
{
    public int bundle_price_cents;
    public int effective_unit_price_cents;
}

[Serializable]
public class VoucherRevealRequest
{
    public string dialogue_node_id;
    public string option_id;
    public int remaining_ms;
}

[Serializable]
public class VoucherOfferWrapper
{
    public string status;
    public VoucherOfferInstance data;
}

[Serializable]
public class VoucherAttemptRequest
{
    public string session_id;
    public string question_instance_id;
    public int answer;
    public string answer_unit;
    public int attempt_number;
    public bool english_support_used;
    public int response_time_ms;
    public int remaining_ms;
}

[Serializable]
public class VoucherAttemptWrapper
{
    public string status;
    public VoucherAttemptResult data;
}

[Serializable]
public class VoucherAttemptResult
{
    public bool correct;
    public string misconception;
    public string feedback_level;
    public string feedback_en;
    public string next_question_instance_id;
    public VoucherResolvedOffer offer_resolved;
}

[Serializable]
public class VoucherBasketRequest
{
    public int client_revision;
    public VoucherBasketItemRequest[] items;
    public int remaining_ms;
}

[Serializable]
public class VoucherBasketItemRequest
{
    public string offer_instance_id;
    public int quantity;
}

[Serializable]
public class VoucherBasketWrapper
{
    public string status;
    public VoucherBasketData data;
}

[Serializable]
public class VoucherBasketData
{
    public int client_revision;
    public VoucherBasketItem[] items;
    public int spent_cents;
    public int remaining_cents;
}

[Serializable]
public class VoucherBasketItem
{
    public string offer_id;
    public string offer_instance_id;
    public string vendor_id;
    public int quantity;
    public int spent_cents;
}

[Serializable]
public class VoucherEventRequest
{
    public string event_id;
    public string event_type;
    public string offer_instance_id;
    public string dialogue_node_id;
    public string occurred_at;
    public VoucherEventMetadata metadata;
}

[Serializable]
public class VoucherEventMetadata
{
    public int remaining_ms;
}

[Serializable]
public class VoucherEventWrapper
{
    public string status;
    public VoucherEventResult data;
}

[Serializable]
public class VoucherEventResult
{
    public string event_id;
    public bool recorded;
    public VoucherTimerEventResult timer;
}

[Serializable]
public class VoucherTimerEventResult
{
    public string state;
    public int remaining_ms;
    public bool accepted;
    public string reason;
}

[Serializable]
public class VoucherTimerData
{
    public string state;
    public int time_limit_ms;
    public int remaining_ms;
    public string last_checkpoint_at;
}

[Serializable]
public class VoucherCompleteRequest
{
    public int basket_revision;
}

[Serializable]
public class VoucherCompleteWrapper
{
    public string status;
    public VoucherCompletionResult data;
}

[Serializable]
public class VoucherCompletionResult
{
    public string session_id;
    public string session_status;
    public VoucherLearningResult learning;
    public VoucherShoppingResult shopping;
    public VoucherBasketItem[] basket;
}

[Serializable]
public class VoucherLearningResult
{
    public int rule_completed;
    public int rule_first_try;
    public int price_completed;
    public int price_first_try;
    public int transfer_completed;
    public int transfer_first_try;
    public int english_support_vendor_count;
}

[Serializable]
public class VoucherShoppingResult
{
    public int spent_cents;
    public int remaining_cents;
    public int purchased_vendor_count;
    public int vendor_target;
    public bool vendor_target_completed;
    public bool speed_bonus_completed;
}

[Serializable]
public class VoucherSimpleWrapper
{
    public string status;
    public VoucherSimpleResult data;
}

[Serializable]
public class VoucherSimpleResult
{
    public string session_id;
    public string session_status;
}
