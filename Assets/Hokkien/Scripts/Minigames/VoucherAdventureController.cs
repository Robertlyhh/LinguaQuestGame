using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// Runs the walkable Voucher Adventure minigame.
/// </summary>
public sealed class VoucherAdventureController : MonoBehaviour
{
    private const int VoucherValue = 4000;
    private const float HardTimeLimit = 90f;
    private const float TimerCheckpointInterval = 15f;
    private const string VoucherContentVersion = "v1";
    private const float CollapsedStatusCardHeight = 205f;
    private const float PurchasedRowHeight = 32f;

    private static readonly Color32 CardColor = new(244, 237, 243, 255);
    private static readonly Color32 TextColor = new(73, 53, 61, 255);
    private static readonly Color32 AccentColor = new(235, 174, 65, 255);
    private static readonly Color32 SecondaryButtonColor = new(248, 220, 177, 255);
    private static readonly Color32 SecondaryButtonHoverColor = new(255, 235, 204, 255);
    private static readonly Color32 PressedColor = new(207, 142, 45, 255);
    private static readonly Color32 DisabledColor = new(205, 190, 175, 255);
    private static readonly Color32 SuccessColor = new(177, 83, 43, 255);
    private static readonly Color32 ErrorColor = new(164, 62, 62, 255);
    private static readonly KeyCode[] NumberKeys =
    {
        KeyCode.Alpha0, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4,
        KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9
    };
    private static readonly KeyCode[] KeypadKeys =
    {
        KeyCode.Keypad0, KeyCode.Keypad1, KeyCode.Keypad2, KeyCode.Keypad3, KeyCode.Keypad4,
        KeyCode.Keypad5, KeyCode.Keypad6, KeyCode.Keypad7, KeyCode.Keypad8, KeyCode.Keypad9
    };
    private static readonly int[] DetailVendorOrder = { 0, 1, 3, 4, 2 };
    private static readonly string[] VoucherVendorIds =
    {
        "v_va_001", "v_va_002", "v_va_003", "v_va_004", "v_va_005"
    };
    private static readonly string[] VoucherOfferIds =
    {
        "pepper_bun", "chicken_wing_rice_roll", "hsinchu_pork_ball",
        "tube_rice_pudding", "steamed_taro_cake"
    };

    [SerializeField] private Sprite panelFrame;
    [SerializeField] private TMP_FontAsset interfaceFont;
    [SerializeField] private frogMove playerMovement;

    // Keep local display data available while session content loads.
    private readonly VendorOffer[] offers =
    {
        new(
            "Pepper Bun", "胡椒餅", 800, 2, 1200, "Second item half price",
            "第二個半價",
            "It means the first item is full price and the second costs 50%.",
            "$8.00 + ($8.00 × 50%) = ?",
            new[]
            {
                "Vendor: 新鮮的胡椒餅，一個八箍。",
                "You: 今仔日有特價無？",
                "Vendor: 有。頭一個是原價，第二個是半價。"
            },
            new[]
            {
                new OfferQuestion(
                    QuestionSkill.RuleMeaning,
                    "How much does the second bun cost?",
                    400,
                    "Only the second bun is discounted; it costs 50% of the original price.",
                    "$8.00 × 50% = ?",
                    "Correct. The second bun costs $4.00."),
                new OfferQuestion(
                    QuestionSkill.PriceCalculation,
                    "What is the total price for both pepper buns?",
                    1200,
                    "Add the full-price first bun and the half-price second bun.",
                    "$8.00 + $4.00 = ?",
                    "Correct. Both pepper buns cost $12.00 in total.",
                    400,
                    "$4.00 is only the second bun's price. Add the full-price first bun too.")
            },
            new OfferQuestion(
                QuestionSkill.Transfer,
                "Challenge: If each bun cost $10.00 with the same offer, what would two buns cost?",
                1500,
                "Apply the same rule to the new original price.",
                "$10.00 + ($10.00 × 50%) = ?",
                "Correct. The same offer would make two $10.00 buns cost $15.00.")),
        new(
            "Chicken Wing Rice Roll", "雞翅包飯", 1000, 1, 800, "20% off",
            "減兩成",
            "It means you pay 80% of the original price.",
            "$10.00 × 80% = ?",
            new[]
            {
                "Vendor: 雞翅包飯原價一個十箍。",
                "You: 今仔日有特價無？",
                "Vendor: 今仔日減兩成。對原價減去兩成。"
            },
            new[]
            {
                new OfferQuestion(
                    QuestionSkill.RuleMeaning,
                    "How much is taken off the $10.00 original price?",
                    200,
                    "The discount is 20% of the original price.",
                    "$10.00 × 20% = ?",
                    "Correct. The discount amount is $2.00."),
                new OfferQuestion(
                    QuestionSkill.PriceCalculation,
                    "What price do you pay after the discount?",
                    800,
                    "Subtract the $2.00 discount from the $10.00 original price.",
                    "$10.00 − $2.00 = ?",
                    "Correct. You pay $8.00.",
                    200,
                    "$2.00 is the discount amount, not the final price. Subtract it from $10.00.")
            },
            new OfferQuestion(
                QuestionSkill.Transfer,
                "Challenge: A different item costs $15.00 with the same offer. What price would you pay?",
                1200,
                "Apply the same percentage discount to the new price.",
                "$15.00 × 80% = ?",
                "Correct. A $15.00 item would cost $12.00.")),
        new(
            "Hsinchu Pork Ball", "新竹貢丸", 1000, 1, 700, "30% off",
            "減三成",
            "It means you pay 70% of the original price.",
            "$10.00 × 70% = ?",
            new[]
            {
                "Vendor: 新竹貢丸原價一個十箍。",
                "You: 今仔日有特價無？",
                "Vendor: 今仔日減三成。對原價減去三成。"
            },
            new[]
            {
                new OfferQuestion(
                    QuestionSkill.RuleMeaning,
                    "How much is taken off the $10.00 original price?",
                    300,
                    "The discount is 30% of the original price.",
                    "$10.00 × 30% = ?",
                    "Correct. The discount amount is $3.00."),
                new OfferQuestion(
                    QuestionSkill.PriceCalculation,
                    "What price do you pay after the discount?",
                    700,
                    "Subtract the $3.00 discount from the $10.00 original price.",
                    "$10.00 − $3.00 = ?",
                    "Correct. You pay $7.00.",
                    300,
                    "$3.00 is the discount amount, not the final price. Subtract it from $10.00.")
            },
            new OfferQuestion(
                QuestionSkill.Transfer,
                "Challenge: A different item costs $20.00 with the same offer. What price would you pay?",
                1400,
                "Apply the same percentage discount to the new price.",
                "$20.00 × 70% = ?",
                "Correct. A $20.00 item would cost $14.00.")),
        new(
            "Tube Rice Pudding", "筒仔米糕", 600, 2, 600, "Buy one, get one free",
            "買一個送一個",
            "It means you pay for only one item and receive two.",
            "Pay for 1 × $6.00 and receive 2 items.",
            new[]
            {
                "Vendor: 筒仔米糕一個六箍。",
                "You: 今仔日有特價無？",
                "Vendor: 有。買一個送一個。頭一個照原價，第二個免錢。"
            },
            new[]
            {
                new OfferQuestion(
                    QuestionSkill.RuleMeaning,
                    "How much does the second rice pudding cost?",
                    0,
                    "You pay full price for the first item; the second item is free.",
                    "Second item price = $0.00",
                    "Correct. The second rice pudding is free."),
                new OfferQuestion(
                    QuestionSkill.PriceCalculation,
                    "What is the total price for both rice puddings?",
                    600,
                    "Add the $6.00 first item and the $0.00 free item.",
                    "$6.00 + $0.00 = ?",
                    "Correct. Both rice puddings cost $6.00 in total.",
                    0,
                    "$0.00 is only the second pudding's price. You still pay $6.00 for the first one.")
            },
            new OfferQuestion(
                QuestionSkill.Transfer,
                "Challenge: If each pudding cost $8.00 with the same offer, what would two cost?",
                800,
                "Apply the same free-item rule to the new original price.",
                "$8.00 + $0.00 = ?",
                "Correct. The same offer would make two $8.00 puddings cost $8.00.")),
        new(
            "Steamed Taro Cake", "芋粿巧", 500, 1, 300, "$2 off",
            "一個減兩箍",
            "It means subtract $2 from the original price.",
            "$5.00 − $2.00 = ?",
            new[]
            {
                "Vendor: 芋粿巧一個五箍。",
                "You: 今仔日有特價無？",
                "Vendor: 今仔日一個減兩箍。對原價減兩箍。",
                "You: 多謝，我來算看覓。"
            },
            new[]
            {
                new OfferQuestion(
                    QuestionSkill.RuleMeaning,
                    "How much is taken off the $5.00 original price?",
                    200,
                    "The offer subtracts $2.00 from the original price.",
                    "Discount amount = $2.00",
                    "Correct. The discount amount is $2.00."),
                new OfferQuestion(
                    QuestionSkill.PriceCalculation,
                    "What price do you pay after the discount?",
                    300,
                    "Subtract the $2.00 discount from the $5.00 original price.",
                    "$5.00 − $2.00 = ?",
                    "Correct. You pay $3.00.",
                    200,
                    "$2.00 is the discount amount, not the final price. Subtract it from $5.00.")
            },
            new OfferQuestion(
                QuestionSkill.Transfer,
                "Challenge: A different item costs $9.00 with the same offer. What price would you pay?",
                700,
                "Apply the same fixed discount to the new price.",
                "$9.00 − $2.00 = ?",
                "Correct. A $9.00 item would cost $7.00.")),
    };

    private Difficulty selectedDifficulty = Difficulty.Easy;
    private int ShoppingVendorTarget => selectedDifficulty switch
    {
        Difficulty.Easy => 0,
        Difficulty.Medium => 2,
        _ => 3
    };
    private GameObject introRoot;
    private GameObject hudRoot;
    private GameObject purchaseModalRoot;
    private GameObject priceCheckPanel;
    private GameObject basketPanel;
    private GameObject standardDialoguePanel;
    private GameObject resultRoot;
    private GameObject offerDetailsRoot;
    private GameObject purchasedItemsSection;
    private GameObject leaveConfirmRoot;
    private RectTransform statusCardRect;
    private RectTransform purchasedItemsSectionRect;
    private TMP_Text difficultyDescription;
    private TMP_Text startButtonLabel;
    private TMP_Text voucherText;
    private TMP_Text taskText;
    private TMP_Text timerText;
    private TMP_Text standardNameText;
    private TMP_Text standardDialogueText;
    private TMP_Text standardContinueLabel;
    private TMP_Text purchaseTitle;
    private TMP_Text purchaseDeal;
    private TMP_Text purchasePrice;
    private TMP_Text priceFeedback;
    private TMP_Text basketTitle;
    private TMP_Text basketDeal;
    private TMP_Text purchaseQuantity;
    private TMP_Text purchaseBalance;
    private TMP_Text purchaseFeedback;
    private TMP_Text resultTitle;
    private TMP_Text resultMessage;
    private TMP_Text[] detailStatusTexts;
    private TMP_Text[] detailOriginalTexts;
    private TMP_Text[] detailOfferTexts;
    private TMP_Text[] detailUnitTexts;
    private TMP_Text purchasedItemsEmptyText;
    private TMP_Text basketToggleLabel;
    private TMP_Text leaveConfirmationMessage;
    private RectTransform[] purchasedItemRows;
    private TMP_Text[] purchasedItemQuantityTexts;
    private TMP_Text[] purchasedItemUnitPriceTexts;
    private TMP_Text priceInputDisplay;
    private TMP_Text offerAudioButtonLabel;
    private RectTransform priceInputRect;
    private Image priceInputBackground;
    private Outline priceInputOutline;
    private Image standardPortrait;
    private CanvasGroup standardDialogueCanvasGroup;
    private Button standardContinueButton;
    private Button standardCloseButton;
    private Button standardLanguageButton;
    private Button standardAudioButton;
    private TMP_Text standardLanguageLabel;
    private Transform standardOptionsContainer;
    private GameObject standardOptionTemplate;
    private Button[] difficultyButtons;
    private Button startButton;
    private Button priceInputButton;
    private Button checkPriceButton;
    private Button offerAudioButton;
    private Button englishHelpButton;
    private Button closePriceButton;
    private Button addToBasketButton;
    private Button removeButton;
    private Button difficultyResultButton;
    private Button retryButton;
    private Button continueShoppingButton;
    private Button introReturnButton;
    private Button leaveAdventureButton;
    private Button returnToOriginButton;
    private Button stayInAdventureButton;
    private Button confirmReturnButton;
    private readonly List<Button> createdButtons = new();
    private bool basketExpanded;
    private bool[] offerUnlocked;
    private bool[] solvedOffers;
    private int[] quantities;
    private int[] offerQuestionSteps;
    private int[] offerQuestionAttempts;
    private string[] offerAudioPaths;
    private bool[] ruleChecksCompleted;
    private bool[] priceChecksCompleted;
    private bool[] transferChecksCompleted;
    private bool[] ruleFirstTryCorrect;
    private bool[] priceFirstTryCorrect;
    private bool[] transferFirstTryCorrect;
    private bool[] englishHelpUsed;
    private int spent;
    private float timeRemaining;
    private bool gameActive;
    private bool vendorFlowActive;
    private bool speedBonusAvailable;
    private int activeVendorIndex = -1;
    private int priceEntryVendorIndex = -1;
    private int conversationLineIndex;
    private int vendorConversationToken;
    private string enteredPriceText = string.Empty;
    private string activeVendorId = string.Empty;
    private string activeVendorName = string.Empty;
    private bool priceInputFocused;
    private bool usingBackendDialogue;
    private bool backendOptionSelectionInProgress;
    private bool voucherSessionRequestInProgress;
    private bool voucherAttemptInProgress;
    private bool basketSyncInProgress;
    private bool voucherCompletionInProgress;
    private bool abandonRequestInProgress;
    private bool usingVoucherBackendSession;
    private string voucherSessionId = string.Empty;
    private int basketRevision;
    private int sessionInitialRemainingMs;
    private float timerCheckpointElapsed;
    private float questionShownAt;
    private VoucherSessionVendor[] sessionVendorsByIndex;
    private VoucherOfferInstance[] sessionOffersByIndex;
    private Sprite activeVendorPortrait;
    private AudioSource voucherDialogueAudioSource;
    private AudioClip voucherDialogueClip;
    private Coroutine dialogueAudioLoadRoutine;
    private string activeDialogueAudioPath = string.Empty;
    private DialogueResponseData backendDialogueNode;
    private VoucherLanguageMode voucherLanguageMode;

    private enum Difficulty
    {
        Easy,
        Medium,
        Hard
    }

    private enum VoucherLanguageMode
    {
        English,
        Han,
        Poj
    }

    private enum QuestionSkill
    {
        RuleMeaning,
        PriceCalculation,
        Transfer
    }

    [System.Serializable]
    private sealed class VoucherOfferEventMetadata
    {
        public string offer_instance_id = string.Empty;
        public string vendor_id = string.Empty;
        public string offer_id = string.Empty;
        public int vendor_index = -1;
    }

    private sealed class VendorOffer
    {
        public readonly string Name;
        public readonly string Hanzi;
        public readonly int BasePrice;
        public readonly int BundleSize;
        public readonly int BundlePrice;
        public readonly string OfferLabel;
        public readonly string HanOfferLabel;
        public readonly string CalculationHint;
        public readonly string FormulaHint;
        public readonly string[] Conversation;
        public readonly OfferQuestion[] CoreQuestions;
        public readonly OfferQuestion[] HardQuestions;

        public VendorOffer(
            string name, string hanzi, int basePrice, int bundleSize, int bundlePrice,
            string offerLabel, string hanOfferLabel, string calculationHint, string formulaHint,
            string[] conversation, OfferQuestion[] coreQuestions, OfferQuestion hardQuestion)
        {
            Name = name;
            Hanzi = hanzi;
            BasePrice = basePrice;
            BundleSize = bundleSize;
            BundlePrice = bundlePrice;
            OfferLabel = offerLabel;
            HanOfferLabel = hanOfferLabel;
            CalculationHint = calculationHint;
            FormulaHint = formulaHint;
            Conversation = conversation;
            CoreQuestions = coreQuestions;
            HardQuestions = new OfferQuestion[coreQuestions.Length + 1];
            for (int i = 0; i < coreQuestions.Length; i++)
            {
                HardQuestions[i] = coreQuestions[i];
            }

            HardQuestions[HardQuestions.Length - 1] = hardQuestion;
        }

        public OfferQuestion[] QuestionsFor(Difficulty difficulty)
        {
            return difficulty == Difficulty.Hard ? HardQuestions : CoreQuestions;
        }
    }

    private sealed class OfferQuestion
    {
        public readonly QuestionSkill Skill;
        public readonly string Prompt;
        public readonly int CorrectPrice;
        public readonly string CalculationHint;
        public readonly string FormulaHint;
        public readonly string CorrectFeedback;
        public readonly int CommonWrongPrice;
        public readonly string MisconceptionFeedback;

        public OfferQuestion(
            QuestionSkill skill, string prompt, int correctPrice, string calculationHint,
            string formulaHint, string correctFeedback, int commonWrongPrice = -1,
            string misconceptionFeedback = null)
        {
            Skill = skill;
            Prompt = prompt;
            CorrectPrice = correctPrice;
            CalculationHint = calculationHint;
            FormulaHint = formulaHint;
            CorrectFeedback = correctFeedback;
            CommonWrongPrice = commonWrongPrice;
            MisconceptionFeedback = misconceptionFeedback;
        }
    }

    private sealed class ProgressSummary
    {
        public int Solved;
        public int PurchasedVendors;
        public int RuleChecks;
        public int PriceChecks;
        public int TransferChecks;
        public int RuleFirstTry;
        public int PriceFirstTry;
        public int TransferFirstTry;
        public int EnglishHelpUsed;
    }

    private void Awake()
    {
        if (playerMovement == null)
        {
            playerMovement = FindFirstObjectByType<frogMove>();
        }

        ConfigureCanvas();
        EnsureEventSystem();
        BuildView();
        SelectDifficulty(Difficulty.Easy);
        ShowIntro();
    }

    private void Update()
    {
        if (leaveConfirmRoot != null && leaveConfirmRoot.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            CancelLeaveConfirmation();
            return;
        }

        if (basketExpanded && Input.GetKeyDown(KeyCode.Escape))
        {
            SetBasketExpanded(false);
            return;
        }

        if (offerDetailsRoot != null && offerDetailsRoot.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseOfferDetails();
            return;
        }

        if (priceCheckPanel != null && priceCheckPanel.activeSelf)
        {
            if (priceInputFocused
                && Input.GetMouseButtonDown(0)
                && !RectTransformUtility.RectangleContainsScreenPoint(priceInputRect, Input.mousePosition))
            {
                SetPriceInputFocused(false);
            }

            CapturePriceInput();
        }

        if (vendorFlowActive && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseDialogue();
            return;
        }

        if (!gameActive || selectedDifficulty != Difficulty.Hard || resultRoot.activeSelf || !speedBonusAvailable)
        {
            return;
        }

        timeRemaining = Mathf.Max(0f, timeRemaining - Time.unscaledDeltaTime);
        if (usingVoucherBackendSession)
        {
            timerCheckpointElapsed += Time.unscaledDeltaTime;
            if (timerCheckpointElapsed >= TimerCheckpointInterval)
            {
                timerCheckpointElapsed = 0f;
                SendVoucherEvent("TIMER_CHECKPOINT");
            }
        }
        UpdateHud();

        if (timeRemaining <= 0f)
        {
            speedBonusAvailable = false;
            UpdateHud();
        }
    }

    public string GetVendorName(int index)
    {
        return index >= 0 && index < offers.Length ? offers[index].Name : "Vendor";
    }

    public bool CanOpenVendor(int index)
    {
        return gameActive
            && index >= 0
            && index < offers.Length
            && HokkienInteractionState.CurrentMode == HokkienInteractionMode.World;
    }

    public void OpenVendor(int index, string vendorId, Sprite portrait)
    {
        if (!CanOpenVendor(index) || !HokkienInteractionState.TryEnter(HokkienInteractionMode.Dialogue, this))
        {
            return;
        }

        SetBasketExpanded(false);
        activeVendorIndex = index;
        activeVendorId = string.IsNullOrWhiteSpace(vendorId) ? VoucherVendorIds[index] : vendorId;
        activeVendorName = offers[index].Name;
        activeVendorPortrait = portrait;
        conversationLineIndex = 0;
        vendorConversationToken++;
        vendorFlowActive = true;
        usingBackendDialogue = true;
        backendOptionSelectionInProgress = false;
        voucherLanguageMode = VoucherLanguageMode.Han;
        SetPlayerPaused(true);
        standardDialogueCanvasGroup.alpha = 1f;
        standardDialoguePanel.SetActive(true);
        standardNameText.SetText(activeVendorName);
        standardPortrait.sprite = activeVendorPortrait;
        standardDialogueText.SetText("Loading dialogue...");
        standardContinueButton.gameObject.SetActive(false);
        standardLanguageButton.gameObject.SetActive(false);
        StopVoucherDialogueAudio();
        ClearBackendOptions();

        if (activeVendorId != VoucherVendorIds[index])
        {
            Debug.LogWarning(
                $"Voucher vendor {index} uses '{activeVendorId}', expected '{VoucherVendorIds[index]}'.");
        }

        if (offerUnlocked[index] && !usingVoucherBackendSession)
        {
            usingBackendDialogue = false;
            ShowConversationLine();
            return;
        }

        BeginBackendVendorDialogue(vendorConversationToken);
    }

    private void ConfigureCanvas()
    {
        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 950;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current == null)
        {
            new GameObject("Voucher EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }

    private void BuildView()
    {
        BuildIntro();
        BuildHud();
        BuildOfferDetails();
        BuildPurchasePanel();
        BuildResult();
        BuildNavigationDialogs();
        BuildStandardDialogue();
        ApplyUnifiedButtonStyles();
    }

    private void BuildIntro()
    {
        introRoot = CreateModalRoot("VoucherIntro", 0.7f);
        GameObject card = CreatePanel("IntroCard", introRoot.transform, new Vector2(1080f, 700f));

        TMP_Text title = CreateText("Title", card.transform, "VOUCHER ADVENTURE", 56f, FontStyles.Bold);
        SetRect(title.rectTransform, new Vector2(70f, -55f), new Vector2(-70f, -130f));
        title.alignment = TextAlignmentOptions.Center;
        title.color = SuccessColor;

        TMP_Text description = CreateText(
            "Description", card.transform,
            $"Listen to all five vendors, show what each offer means, and calculate the price. Then choose offers from different stalls while staying within the {Money(VoucherValue)} voucher.",
            27f, FontStyles.Normal);
        SetRect(description.rectTransform, new Vector2(100f, -145f), new Vector2(-100f, -265f));
        description.alignment = TextAlignmentOptions.Center;

        difficultyButtons = new Button[3];
        string[] labels = { "Easy", "Medium", "Hard" };
        for (int i = 0; i < labels.Length; i++)
        {
            int index = i;
            difficultyButtons[i] = CreateButton(
                labels[i] + "Button", card.transform, labels[i], 25f,
                () => SelectDifficulty((Difficulty)index), out RectTransform buttonRect, out _);
            SetCenteredRect(buttonRect, new Vector2((i - 1) * 260f, 55f), new Vector2(220f, 62f));
        }

        difficultyDescription = CreateText("DifficultyDescription", card.transform, string.Empty, 23f, FontStyles.Normal);
        SetRect(difficultyDescription.rectTransform, new Vector2(120f, -390f), new Vector2(-120f, -500f));
        difficultyDescription.alignment = TextAlignmentOptions.Center;

        startButton = CreateButton(
            "StartButton", card.transform, "Enter the Market", 28f, StartGame,
            out RectTransform startRect, out startButtonLabel);
        SetCenteredRect(
            startRect,
            new Vector2(VoucherAdventureNavigation.HasOrigin ? -190f : 0f, -250f),
            new Vector2(330f, 70f));

        introReturnButton = CreateButton(
            "IntroReturnButton", card.transform, "Return to Lesson", 25f, ReturnToOrigin,
            out RectTransform introReturnRect, out _);
        SetCenteredRect(introReturnRect, new Vector2(190f, -250f), new Vector2(330f, 70f));
        introReturnButton.gameObject.SetActive(VoucherAdventureNavigation.HasOrigin);
    }

    private void BuildHud()
    {
        hudRoot = CreateUiObject("VoucherHUD", transform);
        StretchToParent(hudRoot.GetComponent<RectTransform>());

        GameObject statusCard = CreatePanel(
            "StatusCard", hudRoot.transform, new Vector2(790f, CollapsedStatusCardHeight));
        statusCardRect = statusCard.GetComponent<RectTransform>();
        statusCardRect.anchorMin = new Vector2(0f, 1f);
        statusCardRect.anchorMax = new Vector2(0f, 1f);
        statusCardRect.pivot = new Vector2(0f, 1f);
        statusCardRect.anchoredPosition = new Vector2(10f, -10f);

        TMP_Text title = CreateText("Title", statusCard.transform, "VOUCHER ADVENTURE", 31f, FontStyles.Bold);
        SetRect(title.rectTransform, new Vector2(35f, -24f), new Vector2(-360f, -70f));
        title.color = SuccessColor;

        timerText = CreateText("Timer", statusCard.transform, string.Empty, 25f, FontStyles.Bold);
        SetRect(timerText.rectTransform, new Vector2(520f, -26f), new Vector2(-35f, -70f));
        timerText.alignment = TextAlignmentOptions.Right;

        voucherText = CreateText("Voucher", statusCard.transform, string.Empty, 23f, FontStyles.Bold);
        SetRect(voucherText.rectTransform, new Vector2(35f, -75f), new Vector2(-35f, -112f));

        taskText = CreateText("Task", statusCard.transform, string.Empty, 19f, FontStyles.Normal);
        SetRect(taskText.rectTransform, new Vector2(35f, -112f), new Vector2(-35f, -140f));
        taskText.alignment = TextAlignmentOptions.Left;
        taskText.textWrappingMode = TextWrappingModes.NoWrap;
        taskText.overflowMode = TextOverflowModes.Truncate;

        Button detailsButton = CreateButton(
            "OfferDetailsButton", statusCard.transform, "View Offers", 17f, ShowOfferDetails,
            out RectTransform detailsRect, out TMP_Text detailsLabel);
        AnchorTopRight(detailsRect, new Vector2(-575f, -145f), new Vector2(180f, 38f));
        detailsLabel.textWrappingMode = TextWrappingModes.NoWrap;
        detailsLabel.overflowMode = TextOverflowModes.Overflow;

        Button basketButton = CreateButton(
            "BasketToggleButton", statusCard.transform, string.Empty, 17f, TogglePurchasedItems,
            out RectTransform basketRect, out basketToggleLabel);
        AnchorTopRight(basketRect, new Vector2(-35f, -145f), new Vector2(200f, 38f));
        basketToggleLabel.textWrappingMode = TextWrappingModes.NoWrap;
        basketToggleLabel.overflowMode = TextOverflowModes.Overflow;

        BuildPurchasedItems(statusCard.transform);

        Button finishButton = CreateButton(
            "FinishButton", hudRoot.transform, "Finish Shopping", 23f, TryFinishShopping,
            out RectTransform finishRect, out _);
        AnchorTopRight(finishRect, new Vector2(-132f, -48f), new Vector2(290f, 58f));

        leaveAdventureButton = CreateButton(
            "LeaveAdventureButton", hudRoot.transform, "Return to Lesson", 21f,
            ShowLeaveConfirmation, out RectTransform leaveRect, out _);
        AnchorTopRight(leaveRect, new Vector2(-438f, -48f), new Vector2(270f, 58f));
        leaveAdventureButton.gameObject.SetActive(VoucherAdventureNavigation.HasOrigin);
    }

    private void BuildOfferDetails()
    {
        offerDetailsRoot = CreateModalRoot("OfferDetails", 0.55f);
        GameObject card = CreatePanel("OfferDetailsCard", offerDetailsRoot.transform, new Vector2(1120f, 690f));

        TMP_Text title = CreateText("Title", card.transform, "MARKET OFFER DETAILS", 42f, FontStyles.Bold);
        SetCenteredRect(title.rectTransform, new Vector2(0f, 245f), new Vector2(980f, 62f));
        title.alignment = TextAlignmentOptions.Center;
        title.color = SuccessColor;

        TMP_Text hint = CreateText(
            "Hint", card.transform,
            "Listen to vendors to reveal offers. Complete the meaning and price checks to reveal each unit price.",
            19f, FontStyles.Normal);
        SetCenteredRect(hint.rectTransform, new Vector2(0f, 195f), new Vector2(980f, 40f));
        hint.alignment = TextAlignmentOptions.Center;

        CreateTableCell(card.transform, "VendorHeader", "Vendor", new Vector2(-380f, 150f), new Vector2(250f, 44f), true);
        CreateTableCell(card.transform, "StatusHeader", "Status", new Vector2(-160f, 150f), new Vector2(150f, 44f), true);
        CreateTableCell(card.transform, "OriginalHeader", "Original", new Vector2(5f, 150f), new Vector2(130f, 44f), true);
        CreateTableCell(card.transform, "OfferHeader", "Offer", new Vector2(210f, 150f), new Vector2(250f, 44f), true);
        CreateTableCell(card.transform, "UnitHeader", "Unit Price", new Vector2(410f, 150f), new Vector2(150f, 44f), true);

        detailStatusTexts = new TMP_Text[offers.Length];
        detailOriginalTexts = new TMP_Text[offers.Length];
        detailOfferTexts = new TMP_Text[offers.Length];
        detailUnitTexts = new TMP_Text[offers.Length];
        for (int rowIndex = 0; rowIndex < DetailVendorOrder.Length; rowIndex++)
        {
            int offerIndex = DetailVendorOrder[rowIndex];
            float rowY = 85f - rowIndex * 56f;
            GameObject row = CreateUiObject($"OfferRow{rowIndex + 1}", card.transform);
            SetCenteredRect(row.GetComponent<RectTransform>(), new Vector2(0f, rowY), new Vector2(1000f, 57f));
            row.AddComponent<Image>().color = rowIndex % 2 == 0
                ? new Color32(255, 249, 239, 235)
                : new Color32(238, 224, 218, 235);

            CreateTableCell(row.transform, "Vendor", offers[offerIndex].Name, new Vector2(-380f, 0f), new Vector2(250f, 50f), true);
            detailStatusTexts[offerIndex] = CreateTableCell(row.transform, "Status", string.Empty, new Vector2(-160f, 0f), new Vector2(150f, 50f));
            detailOriginalTexts[offerIndex] = CreateTableCell(row.transform, "Original", string.Empty, new Vector2(5f, 0f), new Vector2(130f, 50f));
            detailOfferTexts[offerIndex] = CreateTableCell(row.transform, "Offer", string.Empty, new Vector2(210f, 0f), new Vector2(250f, 50f));
            detailUnitTexts[offerIndex] = CreateTableCell(row.transform, "UnitPrice", string.Empty, new Vector2(410f, 0f), new Vector2(150f, 50f));
        }

        Button closeButton = CreateButton(
            "CloseDetailsButton", card.transform, "Close", 23f, CloseOfferDetails,
            out RectTransform closeRect, out TMP_Text closeLabel);
        SetCenteredRect(closeRect, new Vector2(0f, -270f), new Vector2(190f, 46f));
        offerDetailsRoot.SetActive(false);
    }

    private void BuildPurchasedItems(Transform statusCard)
    {
        purchasedItemsSection = CreateUiObject("PurchasedItems", statusCard);
        purchasedItemsSectionRect = purchasedItemsSection.GetComponent<RectTransform>();

        GameObject divider = CreateUiObject("Divider", purchasedItemsSection.transform);
        SetRect(divider.GetComponent<RectTransform>(), new Vector2(0f, -2f), new Vector2(0f, -5f));
        divider.AddComponent<Image>().color = new Color32(214, 160, 99, 180);

        TMP_Text itemHeader = CreateText("ItemHeader", purchasedItemsSection.transform, "Item", 18f, FontStyles.Bold);
        SetRect(itemHeader.rectTransform, new Vector2(20f, -14f), new Vector2(-400f, -44f));
        itemHeader.alignment = TextAlignmentOptions.Left;

        TMP_Text quantityHeader = CreateText("QuantityHeader", purchasedItemsSection.transform, "Qty", 18f, FontStyles.Bold);
        SetRect(quantityHeader.rectTransform, new Vector2(390f, -14f), new Vector2(-250f, -44f));
        quantityHeader.alignment = TextAlignmentOptions.Center;

        TMP_Text unitPriceHeader = CreateText("UnitPriceHeader", purchasedItemsSection.transform, "Unit Price", 18f, FontStyles.Bold);
        SetRect(unitPriceHeader.rectTransform, new Vector2(535f, -14f), new Vector2(-20f, -44f));
        unitPriceHeader.alignment = TextAlignmentOptions.Right;

        purchasedItemRows = new RectTransform[offers.Length];
        purchasedItemQuantityTexts = new TMP_Text[offers.Length];
        purchasedItemUnitPriceTexts = new TMP_Text[offers.Length];
        for (int i = 0; i < offers.Length; i++)
        {
            GameObject row = CreateUiObject($"PurchasedItemRow{i + 1}", purchasedItemsSection.transform);
            purchasedItemRows[i] = row.GetComponent<RectTransform>();
            row.AddComponent<Image>().color = i % 2 == 0
                ? new Color32(255, 249, 239, 235)
                : new Color32(238, 224, 218, 235);

            TMP_Text item = CreateTableCell(
                row.transform, "Item", offers[i].Name, new Vector2(-180f, 0f), new Vector2(330f, 28f), true);
            item.alignment = TextAlignmentOptions.Left;
            purchasedItemQuantityTexts[i] = CreateTableCell(
                row.transform, "Quantity", string.Empty, new Vector2(115f, 0f), new Vector2(90f, 28f));
            purchasedItemUnitPriceTexts[i] = CreateTableCell(
                row.transform, "UnitPrice", string.Empty, new Vector2(275f, 0f), new Vector2(150f, 28f));
            purchasedItemUnitPriceTexts[i].alignment = TextAlignmentOptions.Right;
        }

        purchasedItemsEmptyText = CreateText(
            "EmptyMessage", purchasedItemsSection.transform, "No items purchased yet.", 18f, FontStyles.Normal);
        SetRect(purchasedItemsEmptyText.rectTransform, new Vector2(20f, -54f), new Vector2(-20f, -90f));
        purchasedItemsEmptyText.alignment = TextAlignmentOptions.Center;
        purchasedItemsSection.SetActive(false);
    }

    private void BuildPurchasePanel()
    {
        purchaseModalRoot = CreateModalRoot("VoucherPurchase", 0.45f);
        priceCheckPanel = CreatePanel("PriceCheckCard", purchaseModalRoot.transform, new Vector2(940f, 680f));

        purchaseTitle = CreateText("Title", priceCheckPanel.transform, string.Empty, 42f, FontStyles.Bold);
        SetRect(purchaseTitle.rectTransform, new Vector2(65f, -45f), new Vector2(-65f, -105f));
        purchaseTitle.alignment = TextAlignmentOptions.Center;
        purchaseTitle.color = SuccessColor;

        purchaseDeal = CreateText("Deal", priceCheckPanel.transform, string.Empty, 24f, FontStyles.Bold);
        SetRect(purchaseDeal.rectTransform, new Vector2(80f, -105f), new Vector2(-80f, -185f));
        purchaseDeal.alignment = TextAlignmentOptions.Center;
        purchaseDeal.overflowMode = TextOverflowModes.Overflow;
        purchaseDeal.enableAutoSizing = true;
        purchaseDeal.fontSizeMin = 20f;
        purchaseDeal.fontSizeMax = 24f;

        offerAudioButton = CreateButton(
            "ReplayOfferAudioButton", priceCheckPanel.transform, "Replay Offer Audio", 19f,
            ReplayCurrentOfferAudio, out RectTransform audioRect, out offerAudioButtonLabel);
        SetCenteredRect(audioRect, new Vector2(0f, 125f), new Vector2(270f, 44f));

        purchasePrice = CreateText("PricePrompt", priceCheckPanel.transform, string.Empty, 22f, FontStyles.Normal);
        SetRect(purchasePrice.rectTransform, new Vector2(90f, -250f), new Vector2(-90f, -340f));
        purchasePrice.alignment = TextAlignmentOptions.Center;
        purchasePrice.textWrappingMode = TextWrappingModes.Normal;
        purchasePrice.overflowMode = TextOverflowModes.Overflow;
        purchasePrice.enableAutoSizing = true;
        purchasePrice.fontSizeMin = 17f;
        purchasePrice.fontSizeMax = 22f;

        CreatePriceInput(priceCheckPanel.transform, out priceInputRect);
        SetCenteredRect(priceInputRect, new Vector2(-170f, -55f), new Vector2(280f, 62f));

        // Keep the amount above the field background in the card's render order.
        priceInputDisplay = CreateText("PriceInputValue", priceCheckPanel.transform, "$0.00", 25f, FontStyles.Normal);
        SetCenteredRect(priceInputDisplay.rectTransform, new Vector2(-170f, -55f), new Vector2(272f, 56f));
        priceInputDisplay.alignment = TextAlignmentOptions.Center;
        priceInputDisplay.textWrappingMode = TextWrappingModes.NoWrap;
        priceInputDisplay.overflowMode = TextOverflowModes.Overflow;
        priceInputDisplay.enableAutoSizing = true;
        priceInputDisplay.fontSizeMin = 18f;
        priceInputDisplay.fontSizeMax = 25f;
        priceInputDisplay.raycastTarget = false;
        UpdatePriceInputDisplay();

        checkPriceButton = CreateButton(
            "CheckPriceButton", priceCheckPanel.transform, "Check Answer", 22f, CheckPrice,
            out RectTransform checkRect, out _);
        SetCenteredRect(checkRect, new Vector2(170f, -55f), new Vector2(300f, 62f));

        priceFeedback = CreateText("Feedback", priceCheckPanel.transform, string.Empty, 20f, FontStyles.Normal);
        SetRect(priceFeedback.rectTransform, new Vector2(75f, -440f), new Vector2(-75f, -515f));
        priceFeedback.alignment = TextAlignmentOptions.Center;
        priceFeedback.textWrappingMode = TextWrappingModes.Normal;
        priceFeedback.overflowMode = TextOverflowModes.Overflow;
        priceFeedback.enableAutoSizing = true;
        priceFeedback.fontSizeMin = 17f;
        priceFeedback.fontSizeMax = 20f;

        englishHelpButton = CreateButton(
            "EnglishHelpButton", priceCheckPanel.transform, "English Help", 20f, ShowEnglishHelp,
            out RectTransform englishHelpRect, out _);
        SetCenteredRect(englishHelpRect, new Vector2(-175f, -275f), new Vector2(250f, 58f));

        closePriceButton = CreateButton(
            "ClosePriceButton", priceCheckPanel.transform, "Return to Market", 21f, CloseDialogue,
            out RectTransform closePriceRect, out _);
        SetCenteredRect(closePriceRect, new Vector2(175f, -275f), new Vector2(280f, 58f));

        basketPanel = CreatePanel("BasketCard", purchaseModalRoot.transform, new Vector2(940f, 680f));
        basketTitle = CreateText("Title", basketPanel.transform, string.Empty, 42f, FontStyles.Bold);
        SetRect(basketTitle.rectTransform, new Vector2(65f, -45f), new Vector2(-65f, -105f));
        basketTitle.alignment = TextAlignmentOptions.Center;
        basketTitle.color = SuccessColor;

        basketDeal = CreateText("DealSummary", basketPanel.transform, string.Empty, 22f, FontStyles.Normal);
        SetRect(basketDeal.rectTransform, new Vector2(80f, -120f), new Vector2(-80f, -250f));
        basketDeal.alignment = TextAlignmentOptions.Center;
        basketDeal.overflowMode = TextOverflowModes.Overflow;

        purchaseQuantity = CreateText("Quantity", basketPanel.transform, string.Empty, 24f, FontStyles.Bold);
        SetRect(purchaseQuantity.rectTransform, new Vector2(90f, -280f), new Vector2(-90f, -330f));
        purchaseQuantity.alignment = TextAlignmentOptions.Center;

        purchaseBalance = CreateText("Balance", basketPanel.transform, string.Empty, 22f, FontStyles.Normal);
        SetRect(purchaseBalance.rectTransform, new Vector2(90f, -335f), new Vector2(-90f, -380f));
        purchaseBalance.alignment = TextAlignmentOptions.Center;

        purchaseFeedback = CreateText("Feedback", basketPanel.transform, string.Empty, 20f, FontStyles.Normal);
        SetRect(purchaseFeedback.rectTransform, new Vector2(75f, -395f), new Vector2(-75f, -465f));
        purchaseFeedback.alignment = TextAlignmentOptions.Center;
        purchaseFeedback.overflowMode = TextOverflowModes.Overflow;

        addToBasketButton = CreateButton(
            "AddToBasketButton", basketPanel.transform, "Add to Basket", 22f, AddToBasket,
            out RectTransform basketRect, out _);
        SetCenteredRect(basketRect, new Vector2(-170f, -175f), new Vector2(280f, 58f));

        removeButton = CreateButton(
            "RemoveButton", basketPanel.transform, "Remove from Basket", 20f, RemoveBundle,
            out RectTransform removeRect, out _);
        SetCenteredRect(removeRect, new Vector2(170f, -175f), new Vector2(280f, 58f));

        Button closeBasketButton = CreateButton(
            "CloseBasketButton", basketPanel.transform, "Return to Market", 21f, CloseDialogue,
            out RectTransform closeBasketRect, out _);
        SetCenteredRect(closeBasketRect, new Vector2(0f, -265f), new Vector2(300f, 58f));

        priceCheckPanel.SetActive(false);
        basketPanel.SetActive(false);
    }

    private void BuildStandardDialogue()
    {
        Transform source = null;
        foreach (Transform candidate in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (candidate.name == "DialoguePanel" && candidate.gameObject.scene == gameObject.scene)
            {
                source = candidate;
                break;
            }
        }

        if (source == null)
        {
            Debug.LogError("Voucher Adventure could not find the standard Hokkien DialoguePanel.");
            return;
        }

        standardDialoguePanel = Instantiate(source.gameObject, transform, false);
        standardDialoguePanel.name = "VoucherDialoguePanel";
        standardDialoguePanel.transform.SetAsLastSibling();

        standardDialogueCanvasGroup = standardDialoguePanel.GetComponent<CanvasGroup>();
        standardNameText = FindChild(standardDialoguePanel.transform, "NPCNameText").GetComponent<TMP_Text>();
        standardPortrait = FindChild(standardDialoguePanel.transform, "DialoguePortrait").GetComponent<Image>();
        standardDialogueText = FindChild(standardDialoguePanel.transform, "DialogueText").GetComponent<TMP_Text>();
        standardContinueButton = FindChild(standardDialoguePanel.transform, "ContinueButton").GetComponent<Button>();
        standardContinueLabel = standardContinueButton.GetComponentInChildren<TMP_Text>(true);
        standardCloseButton = FindChild(standardDialoguePanel.transform, "CloseButton").GetComponent<Button>();
        standardOptionsContainer = FindChild(standardDialoguePanel.transform, "OptionsPanel");
        standardLanguageButton = FindChild(standardDialoguePanel.transform, "Language Button").GetComponent<Button>();
        standardLanguageLabel = standardLanguageButton.GetComponentInChildren<TMP_Text>(true);
        standardAudioButton = FindChild(standardDialoguePanel.transform, "SpeakerButton").GetComponent<Button>();

        voucherDialogueAudioSource = GetComponent<AudioSource>();
        if (voucherDialogueAudioSource == null)
        {
            voucherDialogueAudioSource = gameObject.AddComponent<AudioSource>();
        }
        voucherDialogueAudioSource.playOnAwake = false;

        Button[] optionTemplates = standardOptionsContainer.GetComponentsInChildren<Button>(true);
        if (optionTemplates.Length > 0)
        {
            standardOptionTemplate = optionTemplates[0].gameObject;
            standardOptionTemplate.SetActive(false);
        }
        else
        {
            Debug.LogError("Voucher Adventure could not find the standard dialogue option template.");
        }

        standardOptionsContainer.gameObject.SetActive(false);
        FindChild(standardDialoguePanel.transform, "QuizOptionsPanel").gameObject.SetActive(false);
        standardAudioButton.gameObject.SetActive(false);
        standardLanguageButton.gameObject.SetActive(false);

        standardCloseButton.onClick.RemoveAllListeners();
        standardCloseButton.onClick.AddListener(CloseDialogue);
        standardLanguageButton.onClick.RemoveAllListeners();
        standardLanguageButton.onClick.AddListener(CycleVoucherLanguage);
        standardAudioButton.onClick.RemoveAllListeners();
        standardAudioButton.onClick.AddListener(ToggleVoucherDialogueAudio);
        standardDialoguePanel.SetActive(false);
    }

    private void BuildResult()
    {
        resultRoot = CreateModalRoot("VoucherResult", 0.7f);
        GameObject card = CreatePanel("ResultCard", resultRoot.transform, new Vector2(960f, 760f));

        resultTitle = CreateText("Title", card.transform, string.Empty, 50f, FontStyles.Bold);
        SetRect(resultTitle.rectTransform, new Vector2(65f, -45f), new Vector2(-65f, -120f));
        resultTitle.alignment = TextAlignmentOptions.Center;

        resultMessage = CreateText("Message", card.transform, string.Empty, 21f, FontStyles.Normal);
        SetRect(resultMessage.rectTransform, new Vector2(90f, -135f), new Vector2(-90f, -600f));
        resultMessage.alignment = TextAlignmentOptions.Center;
        resultMessage.overflowMode = TextOverflowModes.Overflow;

        difficultyResultButton = CreateButton(
            "DifficultyButton", card.transform, "Choose Difficulty", 23f, ShowIntro,
            out RectTransform difficultyRect, out _);
        SetCenteredRect(
            difficultyRect,
            new Vector2(VoucherAdventureNavigation.HasOrigin ? -300f : -175f, -335f),
            new Vector2(260f, 62f));

        retryButton = CreateButton(
            "RetryButton", card.transform, "Try Again", 23f, StartGame,
            out RectTransform retryRect, out _);
        SetCenteredRect(
            retryRect,
            new Vector2(VoucherAdventureNavigation.HasOrigin ? 300f : 175f, -335f),
            new Vector2(250f, 62f));

        returnToOriginButton = CreateButton(
            "ReturnToOriginButton", card.transform, "Return to Lesson", 23f, ReturnToOrigin,
            out RectTransform returnRect, out _);
        SetCenteredRect(returnRect, new Vector2(0f, -335f), new Vector2(280f, 62f));
        returnToOriginButton.gameObject.SetActive(false);

        continueShoppingButton = CreateButton(
            "ContinueShoppingButton", card.transform, "Continue Shopping", 23f, ContinueShopping,
            out RectTransform continueRect, out _);
        SetCenteredRect(continueRect, new Vector2(0f, -335f), new Vector2(330f, 62f));
        continueShoppingButton.gameObject.SetActive(false);
    }

    private void BuildNavigationDialogs()
    {
        leaveConfirmRoot = CreateModalRoot("LeaveVoucherConfirmation", 0.75f);
        GameObject leaveCard = CreatePanel("LeaveCard", leaveConfirmRoot.transform, new Vector2(760f, 430f));
        TMP_Text leaveTitle = CreateText(
            "Title", leaveCard.transform, "Return to the Lesson?", 38f, FontStyles.Bold);
        SetCenteredRect(leaveTitle.rectTransform, new Vector2(0f, 115f), new Vector2(650f, 60f));
        leaveTitle.alignment = TextAlignmentOptions.Center;
        leaveTitle.color = SuccessColor;
        leaveConfirmationMessage = CreateText(
            "Message", leaveCard.transform,
            "Leaving will discard this Voucher Adventure run and its progress.",
            23f, FontStyles.Normal);
        SetCenteredRect(leaveConfirmationMessage.rectTransform, new Vector2(0f, 30f), new Vector2(630f, 100f));
        leaveConfirmationMessage.alignment = TextAlignmentOptions.Center;
        stayInAdventureButton = CreateButton(
            "StayButton", leaveCard.transform, "Stay", 23f,
            CancelLeaveConfirmation, out RectTransform stayRect, out _);
        SetCenteredRect(stayRect, new Vector2(-165f, -120f), new Vector2(260f, 62f));
        confirmReturnButton = CreateButton(
            "ConfirmReturnButton", leaveCard.transform, "Return to Lesson", 23f,
            ConfirmReturnToOrigin, out RectTransform leaveRect, out _);
        SetCenteredRect(leaveRect, new Vector2(165f, -120f), new Vector2(280f, 62f));
    }

    private void SelectDifficulty(Difficulty difficulty)
    {
        selectedDifficulty = difficulty;
        string[] descriptions =
        {
            "Audio and Han subtitles, optional English meaning help, and guided feedback. Shopping is optional.",
            "Audio and Han subtitles with rule hints only. No English help or formulas; buying from 2 vendors is a bonus goal.",
            "No English help or answer hints. Each offer adds a transfer challenge; shopping and the 90-second timer are bonus goals."
        };
        difficultyDescription.text = descriptions[(int)difficulty];
        difficultyDescription.color = TextColor;
        if (startButtonLabel != null)
        {
            startButtonLabel.SetText("Enter the Market");
        }

        for (int i = 0; i < difficultyButtons.Length; i++)
        {
            SetDifficultyButtonSelected(difficultyButtons[i], i == (int)difficulty);
        }
    }

    private void ShowIntro()
    {
        SetBasketExpanded(false);
        gameActive = false;
        vendorFlowActive = false;
        activeVendorIndex = -1;
        priceEntryVendorIndex = -1;
        enteredPriceText = string.Empty;
        SetPriceInputFocused(false);
        activeVendorPortrait = null;
        ReleaseInteraction();
        HokkienInteractionState.TryEnter(HokkienInteractionMode.Welcome, this);
        SetPlayerPaused(true);
        introRoot.SetActive(true);
        hudRoot.SetActive(false);
        offerDetailsRoot.SetActive(false);
        purchaseModalRoot.SetActive(false);
        standardDialoguePanel.SetActive(false);
        resultRoot.SetActive(false);
        leaveConfirmRoot?.SetActive(false);
    }

    private void StartGame()
    {
        if (voucherSessionRequestInProgress)
        {
            return;
        }

        if (APIManager.Instance == null)
        {
            ShowSessionRequestError("The game service is unavailable.");
            return;
        }

        CreateVoucherSession();
    }

    private void CreateVoucherSession(Difficulty? requested = null)
    {
        Difficulty requestedDifficulty = requested ?? selectedDifficulty;
        voucherSessionRequestInProgress = true;
        string playerId = SessionManager.Instance?.GameData?.playerId ?? GameData.DefaultPlayerId();
        APIManager.Instance.CreateVoucherSession(
            new VoucherSessionCreateRequest
            {
                player_id = playerId,
                difficulty = requestedDifficulty.ToString().ToLowerInvariant(),
                content_version = VoucherContentVersion
            },
            session =>
            {
                voucherSessionRequestInProgress = false;
                if (session == null)
                {
                    ShowSessionRequestError("The server returned an empty session.");
                    return;
                }

                ConfigureVoucherSession(session);
                InitializeGameState();
                SendVoucherEvent("SESSION_STARTED");
            },
            error =>
            {
                voucherSessionRequestInProgress = false;
                if (error != null && error.code == "ACTIVE_SESSION_EXISTS"
                    && !string.IsNullOrWhiteSpace(error.session_id))
                {
                    DiscardExistingSessionAndRetry(error.session_id, requestedDifficulty);
                    return;
                }

                ShowSessionRequestError(error?.message ?? "The session service is unavailable.");
            });
    }

    private void DiscardExistingSessionAndRetry(string sessionId, Difficulty requestedDifficulty)
    {
        if (string.IsNullOrWhiteSpace(sessionId) || APIManager.Instance == null)
        {
            ShowSessionRequestError("The previous run could not be discarded.");
            return;
        }

        voucherSessionRequestInProgress = true;
        APIManager.Instance.AbandonVoucherSession(
            sessionId,
            _ =>
            {
                voucherSessionRequestInProgress = false;
                CreateVoucherSession(requestedDifficulty);
            },
            error =>
            {
                voucherSessionRequestInProgress = false;
                ShowSessionRequestError(
                    $"The previous run could not be discarded: {error?.message ?? "network error"}");
            });
    }

    private void ShowSessionRequestError(string message)
    {
        voucherSessionRequestInProgress = false;
        if (introRoot != null && introRoot.activeSelf)
        {
            difficultyDescription.text =
                $"Could not connect: {message}\nCheck the server and try again.";
            difficultyDescription.color = ErrorColor;
            startButtonLabel?.SetText("Retry Connection");
            return;
        }

        if (resultRoot != null && resultRoot.activeSelf)
        {
            resultTitle.text = "Could Not Start";
            resultTitle.color = ErrorColor;
            resultMessage.text =
                $"The new session could not be started: {message}\n\nYour previous result is unchanged. Try again when the connection is available.";
            retryButton.gameObject.SetActive(true);
        }
    }

    private void ConfigureVoucherSession(VoucherSessionData session)
    {
        SelectDifficulty(session.difficulty?.ToLowerInvariant() switch
        {
            "medium" => Difficulty.Medium,
            "hard" => Difficulty.Hard,
            _ => Difficulty.Easy
        });
        usingVoucherBackendSession = true;
        voucherSessionId = session.session_id;
        basketRevision = session.basket?.client_revision ?? 0;
        sessionInitialRemainingMs = session.timer?.remaining_ms ?? 0;
        sessionVendorsByIndex = new VoucherSessionVendor[offers.Length];
        sessionOffersByIndex = new VoucherOfferInstance[offers.Length];
        if (session.vendors == null)
        {
            return;
        }

        foreach (VoucherSessionVendor vendor in session.vendors)
        {
            int index = FindVendorIndex(vendor.vendor_id);
            if (index < 0)
            {
                continue;
            }

            sessionVendorsByIndex[index] = vendor;
            sessionOffersByIndex[index] = vendor.offer;
        }
    }

    private void InitializeGameState()
    {
        SetBasketExpanded(false);
        spent = 0;
        timeRemaining = selectedDifficulty == Difficulty.Hard && sessionInitialRemainingMs > 0
            ? sessionInitialRemainingMs / 1000f
            : HardTimeLimit;
        offerUnlocked = new bool[offers.Length];
        quantities = new int[offers.Length];
        solvedOffers = new bool[offers.Length];
        offerQuestionSteps = new int[offers.Length];
        offerQuestionAttempts = new int[offers.Length];
        offerAudioPaths = new string[offers.Length];
        ruleChecksCompleted = new bool[offers.Length];
        priceChecksCompleted = new bool[offers.Length];
        transferChecksCompleted = new bool[offers.Length];
        ruleFirstTryCorrect = new bool[offers.Length];
        priceFirstTryCorrect = new bool[offers.Length];
        transferFirstTryCorrect = new bool[offers.Length];
        englishHelpUsed = new bool[offers.Length];
        voucherAttemptInProgress = false;
        basketSyncInProgress = false;
        voucherCompletionInProgress = false;
        speedBonusAvailable = selectedDifficulty == Difficulty.Hard;
        timerCheckpointElapsed = 0f;
        vendorFlowActive = false;
        activeVendorIndex = -1;
        priceEntryVendorIndex = -1;
        enteredPriceText = string.Empty;
        SetPriceInputFocused(false);
        activeVendorPortrait = null;
        gameActive = true;

        ReleaseInteraction();
        SetPlayerPaused(false);
        introRoot.SetActive(false);
        hudRoot.SetActive(true);
        offerDetailsRoot.SetActive(false);
        purchaseModalRoot.SetActive(false);
        standardDialoguePanel.SetActive(false);
        resultRoot.SetActive(false);
        UpdateHud();
    }

    private void BeginBackendVendorDialogue(int token)
    {
        if (APIManager.Instance == null)
        {
            ShowDialogueRetry(
                "The dialogue service is unavailable.", token,
                () => BeginBackendVendorDialogue(token));
            return;
        }

        if (usingVoucherBackendSession && sessionVendorsByIndex != null)
        {
            VoucherSessionVendor sessionVendor = sessionVendorsByIndex[activeVendorIndex];
            if (sessionVendor == null || string.IsNullOrWhiteSpace(sessionVendor.dialogue_node_id))
            {
                ShowDialogueRetry(
                    "This session does not currently contain the vendor dialogue.", token,
                    () => BeginBackendVendorDialogue(token));
                return;
            }

            FetchBackendDialogueNode(sessionVendor.dialogue_node_id, token);
            return;
        }

        APIManager.Instance.GetVendorProfile(
            activeVendorId,
            vendor =>
            {
                if (!IsVendorConversationValid(token))
                {
                    return;
                }

                if (vendor == null || string.IsNullOrWhiteSpace(vendor.dialogue_node_id))
                {
                    ShowDialogueRetry(
                        "The vendor dialogue could not be loaded.", token,
                        () => BeginBackendVendorDialogue(token));
                    return;
                }

                activeVendorName = string.IsNullOrWhiteSpace(vendor.vendor_name)
                    ? offers[activeVendorIndex].Name
                    : vendor.vendor_name;
                standardNameText.SetText(activeVendorName);
                FetchBackendDialogueNode(vendor.dialogue_node_id, token);
            },
            error => ShowDialogueRetry(
                $"Vendor request failed: {error}", token,
                () => BeginBackendVendorDialogue(token)));
    }

    private void FetchBackendDialogueNode(string nodeId, int token)
    {
        if (!IsVendorConversationValid(token) || APIManager.Instance == null)
        {
            return;
        }

        System.Action<DialogueResponseData> onSuccess =
            node =>
            {
                if (!IsVendorConversationValid(token))
                {
                    return;
                }

                if (node?.dialogue == null)
                {
                    ShowDialogueRetry(
                        $"Dialogue node '{nodeId}' is empty.", token,
                        () => FetchBackendDialogueNode(nodeId, token));
                    return;
                }

                ShowBackendDialogueNode(node, token);
            };

        if (usingVoucherBackendSession)
        {
            APIManager.Instance.GetVoucherDialogueNode(
                nodeId,
                voucherSessionId,
                onSuccess,
                error =>
                {
                    ShowDialogueRetry(
                        $"Dialogue request failed: {error?.message ?? "network error"}", token,
                        () => FetchBackendDialogueNode(nodeId, token));
                });
        }
        else
        {
            APIManager.Instance.GetDialogueNode(
                nodeId, onSuccess,
                error => ShowDialogueRetry(
                    $"Dialogue request failed: {error}", token,
                    () => FetchBackendDialogueNode(nodeId, token)));
        }
    }

    private void ShowDialogueRetry(string message, int token, System.Action retry)
    {
        if (!IsVendorConversationValid(token))
        {
            return;
        }

        usingBackendDialogue = true;
        backendOptionSelectionInProgress = false;
        StopVoucherDialogueAudio();
        ClearBackendOptions();
        standardLanguageButton.gameObject.SetActive(false);
        standardDialogueText.SetText(
            $"Connection issue\n\n{message}\n\nYour current offer and progress have been kept.");
        standardContinueLabel.SetText("Retry");
        standardContinueButton.onClick.RemoveAllListeners();
        standardContinueButton.onClick.AddListener(() => retry?.Invoke());
        standardContinueButton.gameObject.SetActive(true);
    }

    private void ShowBackendDialogueNode(DialogueResponseData node, int token)
    {
        backendDialogueNode = node;
        backendOptionSelectionInProgress = false;
        standardNameText.SetText(activeVendorName);
        standardPortrait.sprite = activeVendorPortrait;
        ClearBackendOptions();
        ApplyVoucherDialogueLanguage();
        CacheOfferAudioPath(node);
        LoadVoucherDialogueAudio(node.dialogue.audio, token);

        DialogueOption[] options = node.options;
        if (options != null && options.Length > 0)
        {
            standardContinueButton.gameObject.SetActive(false);
            ShowBackendOptions(options, token);
            return;
        }

        standardContinueButton.onClick.RemoveAllListeners();
        standardContinueButton.gameObject.SetActive(true);
        if (node.next_nodes != null && node.next_nodes.Length == 1
            && !string.IsNullOrWhiteSpace(node.next_nodes[0]))
        {
            string nextNode = node.next_nodes[0];
            standardContinueLabel.SetText("Continue");
            standardContinueButton.onClick.AddListener(
                () => FetchBackendDialogueNode(nextNode, token));
        }
        else
        {
            standardContinueLabel.SetText("Close");
            standardContinueButton.onClick.AddListener(CloseDialogue);
        }
    }

    private void CacheOfferAudioPath(DialogueResponseData node)
    {
        if (activeVendorIndex < 0 || offerAudioPaths == null
            || node?.dialogue == null || string.IsNullOrWhiteSpace(node.dialogue.audio)
            || node.options == null)
        {
            return;
        }

        foreach (DialogueOption option in node.options)
        {
            if (TryGetRevealMetadata(option, out _))
            {
                // Keep the offer line separate from the vendor's greeting audio.
                offerAudioPaths[activeVendorIndex] = node.dialogue.audio;
                return;
            }
        }
    }

    private void ShowBackendOptions(DialogueOption[] options, int token)
    {
        if (standardOptionTemplate == null)
        {
            ShowDialogueRetry(
                "The dialogue options could not be displayed.", token,
                () => ShowBackendDialogueNode(backendDialogueNode, token));
            return;
        }

        standardOptionsContainer.gameObject.SetActive(true);
        foreach (DialogueOption option in options)
        {
            if (option == null)
            {
                continue;
            }

            GameObject optionObject = Instantiate(standardOptionTemplate, standardOptionsContainer, false);
            optionObject.name = string.IsNullOrWhiteSpace(option.option_id)
                ? "VoucherDialogueOption"
                : option.option_id;
            optionObject.SetActive(true);

            TMP_Text label = optionObject.GetComponentInChildren<TMP_Text>(true);
            label.SetText(option.text ?? string.Empty);

            Button button = optionObject.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            DialogueOption selectedOption = option;
            button.onClick.AddListener(() => SelectBackendOption(selectedOption, token));
        }
    }

    private void SelectBackendOption(DialogueOption option, int token)
    {
        if (!IsVendorConversationValid(token) || backendOptionSelectionInProgress)
        {
            return;
        }

        backendOptionSelectionInProgress = true;
        StopVoucherDialogueAudio();
        ClearBackendOptions();

        if (usingVoucherBackendSession && TryGetRevealMetadata(option, out VoucherOfferEventMetadata metadata))
        {
            RevealBackendOffer(option, metadata, token);
            return;
        }

        bool offerRevealed = ProcessVoucherDialogueEvents(option);
        if (!string.IsNullOrWhiteSpace(option.next_node))
        {
            standardDialogueText.SetText("Loading dialogue...");
            standardContinueButton.gameObject.SetActive(false);
            FetchBackendDialogueNode(option.next_node, token);
            return;
        }

        if (offerRevealed)
        {
            CompleteBackendOfferDialogue();
        }
        else
        {
            CloseDialogue();
        }
    }

    private void RevealBackendOffer(DialogueOption option, VoucherOfferEventMetadata metadata, int token)
    {
        VoucherSessionVendor sessionVendor = sessionVendorsByIndex?[activeVendorIndex];
        string offerInstanceId = !string.IsNullOrWhiteSpace(metadata.offer_instance_id)
            ? metadata.offer_instance_id
            : sessionVendor?.offer_instance_id;
        if (string.IsNullOrWhiteSpace(offerInstanceId))
        {
            ShowDialogueRetry(
                "The offer could not be identified for this session.", token,
                () => RevealBackendOffer(option, metadata, token));
            return;
        }

        standardDialogueText.SetText("Loading offer...");
        standardContinueButton.gameObject.SetActive(false);
        APIManager.Instance.RevealVoucherOffer(
            voucherSessionId,
            offerInstanceId,
            new VoucherRevealRequest
            {
                dialogue_node_id = backendDialogueNode?.dialogue_node,
                option_id = option.option_id,
                remaining_ms = CurrentRemainingMs()
            },
            revealedOffer =>
            {
                if (!IsVendorConversationValid(token) || revealedOffer == null)
                {
                    return;
                }

                if (revealedOffer.questions == null || revealedOffer.questions.Length == 0)
                {
                    ShowDialogueRetry(
                        "The server returned the offer without its questions.", token,
                        () => RevealBackendOffer(option, metadata, token));
                    return;
                }

                sessionOffersByIndex[activeVendorIndex] = revealedOffer;
                offerUnlocked[activeVendorIndex] = true;
                UpdateHud();
                CompleteBackendOfferDialogue();
            },
            error =>
            {
                if (!IsVendorConversationValid(token))
                {
                    return;
                }

                ShowDialogueRetry(
                    $"The offer could not be saved: {error?.message ?? "network error"}", token,
                    () => RevealBackendOffer(option, metadata, token));
            });
    }

    private static bool TryGetRevealMetadata(
        DialogueOption option, out VoucherOfferEventMetadata metadata)
    {
        metadata = null;
        if (option?.events == null)
        {
            return false;
        }

        foreach (DialogueEvent dialogueEvent in option.events)
        {
            if (dialogueEvent?.event_type != "VOUCHER_OFFER_REVEALED")
            {
                continue;
            }

            metadata = string.IsNullOrWhiteSpace(dialogueEvent.metadata)
                ? new VoucherOfferEventMetadata()
                : JsonUtility.FromJson<VoucherOfferEventMetadata>(dialogueEvent.metadata);
            return true;
        }

        return false;
    }

    private bool ProcessVoucherDialogueEvents(DialogueOption option)
    {
        bool offerRevealed = false;
        if (option?.events == null)
        {
            return false;
        }

        foreach (DialogueEvent dialogueEvent in option.events)
        {
            if (dialogueEvent == null || dialogueEvent.event_type != "VOUCHER_OFFER_REVEALED")
            {
                continue;
            }

            VoucherOfferEventMetadata metadata = string.IsNullOrWhiteSpace(dialogueEvent.metadata)
                ? null
                : JsonUtility.FromJson<VoucherOfferEventMetadata>(dialogueEvent.metadata);
            int vendorIndex = FindOfferIndex(metadata?.offer_id);
            if (vendorIndex < 0 && metadata != null
                && metadata.vendor_index >= 0 && metadata.vendor_index < offers.Length)
            {
                vendorIndex = metadata.vendor_index;
            }

            if (vendorIndex != activeVendorIndex)
            {
                Debug.LogWarning(
                    $"Voucher event '{dialogueEvent.event_id}' resolved vendor {vendorIndex}, " +
                    $"but the active vendor is {activeVendorIndex}.");
                continue;
            }

            if (metadata != null && !string.IsNullOrWhiteSpace(metadata.vendor_id)
                && metadata.vendor_id != activeVendorId)
            {
                Debug.LogWarning(
                    $"Voucher event vendor '{metadata.vendor_id}' does not match '{activeVendorId}'.");
                continue;
            }

            offerUnlocked[vendorIndex] = true;
            offerRevealed = true;
        }

        if (offerRevealed)
        {
            UpdateHud();
        }

        return offerRevealed;
    }

    private static int FindOfferIndex(string offerId)
    {
        for (int i = 0; i < VoucherOfferIds.Length; i++)
        {
            if (VoucherOfferIds[i] == offerId)
            {
                return i;
            }
        }

        return -1;
    }

    private static int FindVendorIndex(string vendorId)
    {
        for (int i = 0; i < VoucherVendorIds.Length; i++)
        {
            if (VoucherVendorIds[i] == vendorId)
            {
                return i;
            }
        }

        return -1;
    }

    private bool HasBackendOffer(int index)
    {
        return usingVoucherBackendSession
            && sessionOffersByIndex != null
            && index >= 0
            && index < sessionOffersByIndex.Length
            && sessionOffersByIndex[index]?.questions != null
            && sessionOffersByIndex[index].questions.Length > 0;
    }

    private int CurrentRemainingMs()
    {
        return selectedDifficulty == Difficulty.Hard
            ? Mathf.Max(0, Mathf.RoundToInt(timeRemaining * 1000f))
            : 0;
    }

    private int CurrentBasePrice(int index)
    {
        return HasBackendOffer(index)
            ? sessionOffersByIndex[index].base_price_cents
            : offers[index].BasePrice;
    }

    private int CurrentBundleSize(int index)
    {
        return HasBackendOffer(index)
            ? Mathf.Max(1, sessionOffersByIndex[index].bundle_size)
            : offers[index].BundleSize;
    }

    private int CurrentBundlePrice(int index)
    {
        VoucherResolvedOffer resolved = HasBackendOffer(index)
            ? sessionOffersByIndex[index].offer_resolved
            : null;
        return resolved != null && resolved.bundle_price_cents > 0
            ? resolved.bundle_price_cents
            : offers[index].BundlePrice;
    }

    private int CurrentUnitPrice(int index)
    {
        VoucherResolvedOffer resolved = HasBackendOffer(index)
            ? sessionOffersByIndex[index].offer_resolved
            : null;
        return resolved != null && resolved.effective_unit_price_cents >= 0
            ? resolved.effective_unit_price_cents
            : CurrentBundlePrice(index) / CurrentBundleSize(index);
    }

    private string CurrentHanOfferLabel(int index)
    {
        return HasBackendOffer(index) && !string.IsNullOrWhiteSpace(sessionOffersByIndex[index].label_han)
            ? sessionOffersByIndex[index].label_han
            : offers[index].HanOfferLabel;
    }

    private string CurrentEnglishOfferLabel(int index)
    {
        return HasBackendOffer(index) && !string.IsNullOrWhiteSpace(sessionOffersByIndex[index].label_en)
            ? sessionOffersByIndex[index].label_en
            : offers[index].OfferLabel;
    }

    private void SendVoucherEvent(string eventType, int offerIndex = -1, string dialogueNode = null)
    {
        if (!usingVoucherBackendSession || APIManager.Instance == null
            || string.IsNullOrWhiteSpace(voucherSessionId))
        {
            return;
        }

        string offerInstanceId = offerIndex >= 0 && sessionVendorsByIndex != null
            ? sessionVendorsByIndex[offerIndex]?.offer_instance_id
            : null;
        APIManager.Instance.RecordVoucherEvent(
            voucherSessionId,
            new VoucherEventRequest
            {
                event_id = System.Guid.NewGuid().ToString(),
                event_type = eventType,
                offer_instance_id = offerInstanceId,
                dialogue_node_id = dialogueNode,
                occurred_at = System.DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                metadata = new VoucherEventMetadata { remaining_ms = CurrentRemainingMs() }
            },
            null,
            error => Debug.LogWarning(
                $"[Voucher Adventure] Event '{eventType}' was not saved: {error?.message}"));
    }

    private void OnApplicationPause(bool paused)
    {
        if (selectedDifficulty != Difficulty.Hard || !gameActive)
        {
            return;
        }

        SendVoucherEvent(paused ? "SESSION_PAUSED" : "SESSION_RESUMED");
    }

    private void CompleteBackendOfferDialogue()
    {
        usingBackendDialogue = false;
        backendOptionSelectionInProgress = false;
        backendDialogueNode = null;
        StopVoucherDialogueAudio();
        ClearBackendOptions();
        standardLanguageButton.gameObject.SetActive(false);
        standardDialoguePanel.SetActive(false);
        ShowPurchasePanel();
    }

    private bool IsVendorConversationValid(int token)
    {
        return vendorFlowActive
            && activeVendorIndex >= 0
            && token == vendorConversationToken
            && standardDialoguePanel.activeSelf;
    }

    private void CycleVoucherLanguage()
    {
        if (!usingBackendDialogue || backendDialogueNode?.dialogue == null)
        {
            return;
        }

        voucherLanguageMode = selectedDifficulty == Difficulty.Easy
            ? voucherLanguageMode switch
            {
                VoucherLanguageMode.Han => VoucherLanguageMode.English,
                VoucherLanguageMode.English => VoucherLanguageMode.Poj,
                _ => VoucherLanguageMode.Han
            }
            : voucherLanguageMode == VoucherLanguageMode.Han
                ? VoucherLanguageMode.Poj
                : VoucherLanguageMode.Han;
        if (voucherLanguageMode == VoucherLanguageMode.English
            && activeVendorIndex >= 0 && englishHelpUsed != null)
        {
            bool firstUse = !englishHelpUsed[activeVendorIndex];
            englishHelpUsed[activeVendorIndex] = true;
            if (firstUse)
            {
                SendVoucherEvent("ENGLISH_SUPPORT_USED", activeVendorIndex, backendDialogueNode?.dialogue_node);
            }
        }
        ApplyVoucherDialogueLanguage();
    }

    private void ApplyVoucherDialogueLanguage()
    {
        DialogueContent dialogue = backendDialogueNode?.dialogue;
        if (dialogue == null)
        {
            return;
        }

        string displayText = voucherLanguageMode switch
        {
            VoucherLanguageMode.Han when !string.IsNullOrWhiteSpace(dialogue.translation_HAN)
                => dialogue.translation_HAN,
            VoucherLanguageMode.Poj when !string.IsNullOrWhiteSpace(dialogue.translation_POJ)
                => dialogue.translation_POJ,
            VoucherLanguageMode.Poj when !string.IsNullOrWhiteSpace(dialogue.translation_HAN)
                => dialogue.translation_HAN,
            _ => dialogue.text
        };
        standardDialogueText.SetText(displayText);

        bool hasTranslation = !string.IsNullOrWhiteSpace(dialogue.translation_HAN)
            || !string.IsNullOrWhiteSpace(dialogue.translation_POJ);
        standardLanguageButton.gameObject.SetActive(hasTranslation);
        if (standardLanguageLabel != null)
        {
            standardLanguageLabel.SetText(voucherLanguageMode switch
            {
                VoucherLanguageMode.Han => "漢",
                VoucherLanguageMode.Poj => "POJ",
                _ => "EN"
            });
        }
    }

    private void LoadVoucherDialogueAudio(string audioPath, int token)
    {
        StopVoucherDialogueAudio();
        if (string.IsNullOrWhiteSpace(audioPath) || APIManager.Instance == null)
        {
            return;
        }

        activeDialogueAudioPath = audioPath;
        string audioUrl = BuildVoucherAudioUrl(audioPath);
        dialogueAudioLoadRoutine = StartCoroutine(
            DownloadVoucherDialogueAudio(audioUrl, audioPath, token));
    }

    private IEnumerator DownloadVoucherDialogueAudio(string audioUrl, string audioPath, int token)
    {
        using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(audioUrl, AudioType.UNKNOWN);
        yield return request.SendWebRequest();

        dialogueAudioLoadRoutine = null;
        if (!IsVendorConversationValid(token) || audioPath != activeDialogueAudioPath)
        {
            yield break;
        }

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[Voucher Adventure] Failed to load dialogue audio: {request.error}");
            standardAudioButton.gameObject.SetActive(false);
            yield break;
        }

        voucherDialogueClip = DownloadHandlerAudioClip.GetContent(request);
        if (voucherDialogueClip == null)
        {
            standardAudioButton.gameObject.SetActive(false);
            yield break;
        }

        voucherDialogueAudioSource.clip = voucherDialogueClip;
        standardAudioButton.gameObject.SetActive(true);
        voucherDialogueAudioSource.Play();
    }

    private string BuildVoucherAudioUrl(string audioPath)
    {
        if (audioPath.StartsWith("http://") || audioPath.StartsWith("https://"))
        {
            return audioPath;
        }

        return APIManager.Instance.VoucherBaseUrl.TrimEnd('/') + "/" + audioPath.TrimStart('/');
    }

    private void ReplayCurrentOfferAudio()
    {
        if (activeVendorIndex < 0 || APIManager.Instance == null)
        {
            return;
        }

        string audioPath = GetOfferAudioPath(activeVendorIndex);
        if (string.IsNullOrWhiteSpace(audioPath))
        {
            offerAudioButtonLabel.SetText("Audio Unavailable");
            offerAudioButton.interactable = false;
            return;
        }

        if (voucherDialogueClip != null && activeDialogueAudioPath == audioPath)
        {
            PlayCurrentOfferAudio();
            return;
        }

        StopVoucherDialogueAudio();
        activeDialogueAudioPath = audioPath;
        offerAudioButtonLabel.SetText("Loading Audio...");
        offerAudioButton.interactable = false;
        int vendorIndex = activeVendorIndex;
        dialogueAudioLoadRoutine = StartCoroutine(
            DownloadOfferAudio(BuildVoucherAudioUrl(audioPath), audioPath, vendorIndex));
    }

    private IEnumerator DownloadOfferAudio(string audioUrl, string audioPath, int vendorIndex)
    {
        using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(audioUrl, AudioType.UNKNOWN);
        yield return request.SendWebRequest();

        dialogueAudioLoadRoutine = null;
        if (activeVendorIndex != vendorIndex || priceCheckPanel == null || !priceCheckPanel.activeSelf
            || activeDialogueAudioPath != audioPath)
        {
            yield break;
        }

        offerAudioButton.interactable = true;
        offerAudioButtonLabel.SetText("Replay Offer Audio");
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[Voucher Adventure] Failed to load offer audio: {request.error}");
            offerAudioButtonLabel.SetText("Retry Offer Audio");
            activeDialogueAudioPath = string.Empty;
            yield break;
        }

        voucherDialogueClip = DownloadHandlerAudioClip.GetContent(request);
        if (voucherDialogueClip == null)
        {
            offerAudioButtonLabel.SetText("Retry Offer Audio");
            activeDialogueAudioPath = string.Empty;
            yield break;
        }

        voucherDialogueAudioSource.clip = voucherDialogueClip;
        PlayCurrentOfferAudio();
    }

    private void PlayCurrentOfferAudio()
    {
        voucherDialogueAudioSource.Stop();
        voucherDialogueAudioSource.clip = voucherDialogueClip;
        voucherDialogueAudioSource.Play();
        SendVoucherEvent("AUDIO_REPLAYED", activeVendorIndex);
    }

    private string GetOfferAudioPath(int vendorIndex)
    {
        if (offerAudioPaths != null && vendorIndex >= 0 && vendorIndex < offerAudioPaths.Length
            && !string.IsNullOrWhiteSpace(offerAudioPaths[vendorIndex]))
        {
            return offerAudioPaths[vendorIndex];
        }

        string dialogueNode = sessionVendorsByIndex != null
            && vendorIndex >= 0 && vendorIndex < sessionVendorsByIndex.Length
                ? sessionVendorsByIndex[vendorIndex]?.dialogue_node_id
                : null;
        if (!string.IsNullOrWhiteSpace(dialogueNode) && dialogueNode.EndsWith("_greet"))
        {
            return $"/audio-clips/{dialogueNode[..^6]}_offer.wav";
        }

        // Current static dialogue nodes use n_va_v1 through n_va_v5.
        return vendorIndex >= 0 && vendorIndex < offers.Length
            ? $"/audio-clips/n_va_v{vendorIndex + 1}_offer.wav"
            : string.Empty;
    }

    private void ToggleVoucherDialogueAudio()
    {
        if (voucherDialogueAudioSource == null || voucherDialogueClip == null)
        {
            return;
        }

        if (voucherDialogueAudioSource.isPlaying)
        {
            voucherDialogueAudioSource.Stop();
        }
        else
        {
            voucherDialogueAudioSource.clip = voucherDialogueClip;
            voucherDialogueAudioSource.Play();
            SendVoucherEvent("AUDIO_REPLAYED", activeVendorIndex, backendDialogueNode?.dialogue_node);
        }
    }

    private void StopVoucherDialogueAudio()
    {
        if (dialogueAudioLoadRoutine != null)
        {
            StopCoroutine(dialogueAudioLoadRoutine);
            dialogueAudioLoadRoutine = null;
        }

        if (voucherDialogueAudioSource != null)
        {
            voucherDialogueAudioSource.Stop();
            voucherDialogueAudioSource.clip = null;
        }

        if (voucherDialogueClip != null)
        {
            Destroy(voucherDialogueClip);
            voucherDialogueClip = null;
        }

        activeDialogueAudioPath = string.Empty;
        if (standardAudioButton != null)
        {
            standardAudioButton.gameObject.SetActive(false);
        }
    }

    private void ClearBackendOptions()
    {
        if (standardOptionsContainer == null)
        {
            return;
        }

        foreach (Transform child in standardOptionsContainer)
        {
            if (standardOptionTemplate == null || child.gameObject != standardOptionTemplate)
            {
                Destroy(child.gameObject);
            }
        }

        standardOptionsContainer.gameObject.SetActive(false);
    }

    private void ShowConversationLine()
    {
        VendorOffer offer = offers[activeVendorIndex];
        if (offerUnlocked[activeVendorIndex])
        {
            string reviewText = solvedOffers[activeVendorIndex]
                ? $"優惠已經完成：{CurrentHanOfferLabel(activeVendorIndex)} / {CurrentEnglishOfferLabel(activeVendorIndex)}"
                : $"這個優惠猶原有：{CurrentHanOfferLabel(activeVendorIndex)}";
            ShowStandardDialogueLine(
                false,
                reviewText,
                "View Offer");
            return;
        }

        string line = offer.Conversation[conversationLineIndex];
        bool playerSpeaking = line.StartsWith("You: ");
        string displayText = line
            .Replace("You: ", string.Empty)
            .Replace("Vendor: ", string.Empty);
        string buttonLabel = conversationLineIndex == offer.Conversation.Length - 1
            ? "View Offer"
            : "Continue";

        ShowStandardDialogueLine(playerSpeaking, displayText, buttonLabel);
    }

    private void ShowStandardDialogueLine(bool playerSpeaking, string text, string buttonLabel)
    {
        VendorOffer offer = offers[activeVendorIndex];
        StopVoucherDialogueAudio();
        ClearBackendOptions();
        standardLanguageButton.gameObject.SetActive(false);
        if (playerSpeaking)
        {
            standardNameText.SetText(PlayerIdentity.Instance != null ? PlayerIdentity.Instance.playerName : "You");
            standardPortrait.sprite = PlayerIdentity.Instance != null
                ? PlayerIdentity.Instance.playerPortrait
                : playerMovement != null ? playerMovement.GetComponent<SpriteRenderer>()?.sprite : null;
        }
        else
        {
            standardNameText.SetText(offer.Name);
            standardPortrait.sprite = activeVendorPortrait;
        }

        standardDialogueText.SetText(text);
        standardContinueLabel.SetText(buttonLabel);
        standardContinueButton.onClick.RemoveAllListeners();
        standardContinueButton.onClick.AddListener(AdvanceConversation);
        standardContinueButton.gameObject.SetActive(true);
    }

    private void AdvanceConversation()
    {
        if (activeVendorIndex < 0)
        {
            return;
        }

        VendorOffer offer = offers[activeVendorIndex];
        if (offerUnlocked[activeVendorIndex])
        {
            ShowPurchasePanel();
            return;
        }

        if (conversationLineIndex < offer.Conversation.Length - 1)
        {
            conversationLineIndex++;
            ShowConversationLine();
            return;
        }

        offerUnlocked[activeVendorIndex] = true;
        UpdateHud();
        ShowPurchasePanel();
    }

    private void ShowPurchasePanel()
    {
        standardDialoguePanel.SetActive(false);
        if (priceEntryVendorIndex != activeVendorIndex)
        {
            enteredPriceText = string.Empty;
            priceEntryVendorIndex = activeVendorIndex;
        }

        purchaseModalRoot.SetActive(true);
        if (solvedOffers[activeVendorIndex])
        {
            ShowBasketPanel();
        }
        else
        {
            ShowPriceCheckPanel();
        }
    }

    private void ShowPriceCheckPanel()
    {
        if (activeVendorIndex < 0)
        {
            return;
        }

        SetPriceInputFocused(false);
        basketPanel.SetActive(false);
        priceCheckPanel.SetActive(true);
        purchaseModalRoot.SetActive(true);
        int completedQuestions = offerQuestionSteps[activeVendorIndex];
        priceFeedback.text = completedQuestions > 0
            ? $"{completedQuestions} of {GetQuestionCount(activeVendorIndex)} checks complete. Continue with the next question."
            : "Show that you understand the offer, then calculate its price.";
        priceFeedback.color = SuccessColor;
        RefreshPriceCheckPanel();
    }

    private void ShowBasketPanel()
    {
        if (activeVendorIndex < 0 || !solvedOffers[activeVendorIndex])
        {
            return;
        }

        SetPriceInputFocused(false);
        StopVoucherDialogueAudio();
        priceCheckPanel.SetActive(false);
        basketPanel.SetActive(true);
        purchaseModalRoot.SetActive(true);
        purchaseFeedback.text = quantities[activeVendorIndex] > 0
            ? "This vendor's offer is in your basket. Remove it if you want to change your choice."
            : "You may add this vendor's offer once, or return to the market.";
        purchaseFeedback.color = SuccessColor;
        RefreshPurchasePanel();
    }

    private void CheckPrice()
    {
        if (activeVendorIndex < 0 || solvedOffers[activeVendorIndex] || voucherAttemptInProgress)
        {
            return;
        }

        if (!TryGetEnteredPrice(out int enteredPrice))
        {
            priceFeedback.text = "Enter a price in dollars, such as 6 or 6.00.";
            priceFeedback.color = ErrorColor;
            return;
        }

        if (usingVoucherBackendSession)
        {
            if (!HasBackendOffer(activeVendorIndex))
            {
                priceFeedback.text =
                    "The current offer data is unavailable. Return to the market and retry this vendor.";
                priceFeedback.color = ErrorColor;
                return;
            }

            SubmitBackendPrice(enteredPrice);
            return;
        }

        VendorOffer offer = offers[activeVendorIndex];
        OfferQuestion[] questions = offer.QuestionsFor(selectedDifficulty);
        int questionIndex = offerQuestionSteps[activeVendorIndex];
        OfferQuestion question = questions[questionIndex];
        offerQuestionAttempts[activeVendorIndex]++;
        if (enteredPrice != question.CorrectPrice)
        {
            priceFeedback.text = BuildWrongAnswerFeedback(
                question, enteredPrice, offerQuestionAttempts[activeVendorIndex]);
            priceFeedback.color = ErrorColor;
            return;
        }

        MarkQuestionCompleted(
            activeVendorIndex, question.Skill, offerQuestionAttempts[activeVendorIndex] == 1);
        bool hasAnotherQuestion = questionIndex + 1 < questions.Length;
        if (hasAnotherQuestion)
        {
            offerQuestionSteps[activeVendorIndex]++;
            offerQuestionAttempts[activeVendorIndex] = 0;
            enteredPriceText = string.Empty;
            RefreshPriceCheckPanel();
            priceFeedback.text = question.CorrectFeedback + " Continue with the next question.";
            priceFeedback.color = SuccessColor;
            SetPriceInputFocused(true);
            return;
        }

        solvedOffers[activeVendorIndex] = true;
        SetPriceInputFocused(false);
        UpdateHud();
        ShowBasketPanel();
        purchaseFeedback.text = question.CorrectFeedback + " Choose whether to add this offer to your basket.";
        purchaseFeedback.color = SuccessColor;
    }

    private void SubmitBackendPrice(int enteredPrice)
    {
        int vendorIndex = activeVendorIndex;
        VoucherOfferInstance offer = sessionOffersByIndex[vendorIndex];
        int questionIndex = Mathf.Clamp(offerQuestionSteps[vendorIndex], 0, offer.questions.Length - 1);
        VoucherQuestionInstance question = offer.questions[questionIndex];
        int attemptNumber = offerQuestionAttempts[vendorIndex] + 1;
        voucherAttemptInProgress = true;
        priceInputButton.interactable = false;
        checkPriceButton.interactable = false;
        priceFeedback.text = "Checking your answer...";
        priceFeedback.color = TextColor;

        APIManager.Instance.SubmitVoucherAttempt(
            new VoucherAttemptRequest
            {
                session_id = voucherSessionId,
                question_instance_id = question.question_instance_id,
                answer = enteredPrice,
                answer_unit = question.answer_unit,
                attempt_number = attemptNumber,
                english_support_used = englishHelpUsed[vendorIndex],
                response_time_ms = Mathf.Max(0, Mathf.RoundToInt((Time.unscaledTime - questionShownAt) * 1000f)),
                remaining_ms = CurrentRemainingMs()
            },
            result =>
            {
                voucherAttemptInProgress = false;
                if (vendorIndex != activeVendorIndex || result == null)
                {
                    return;
                }

                priceInputButton.interactable = true;
                checkPriceButton.interactable = true;
                offerQuestionAttempts[vendorIndex] = attemptNumber;
                if (!result.correct)
                {
                    priceFeedback.text = string.IsNullOrWhiteSpace(result.feedback_en)
                        ? "Not quite. Try again."
                        : result.feedback_en;
                    priceFeedback.color = ErrorColor;
                    questionShownAt = Time.unscaledTime;
                    return;
                }

                QuestionSkill skill = ParseQuestionSkill(question.skill);
                MarkQuestionCompleted(vendorIndex, skill, attemptNumber == 1);
                bool hasAnotherQuestion = questionIndex + 1 < offer.questions.Length;
                if (hasAnotherQuestion)
                {
                    offerQuestionSteps[vendorIndex]++;
                    offerQuestionAttempts[vendorIndex] = 0;
                    enteredPriceText = string.Empty;
                    RefreshPriceCheckPanel();
                    priceFeedback.text = (result.feedback_en ?? "Correct.") + " Continue with the next question.";
                    priceFeedback.color = SuccessColor;
                    SetPriceInputFocused(true);
                    return;
                }

                offer.offer_resolved = result.offer_resolved;
                solvedOffers[vendorIndex] = true;
                SetPriceInputFocused(false);
                UpdateHud();
                ShowBasketPanel();
                purchaseFeedback.text = (result.feedback_en ?? "Correct.")
                    + " Choose whether to add this offer to your basket.";
                purchaseFeedback.color = SuccessColor;
            },
            error =>
            {
                voucherAttemptInProgress = false;
                if (vendorIndex != activeVendorIndex)
                {
                    return;
                }

                priceInputButton.interactable = true;
                checkPriceButton.interactable = true;
                if (error?.expected_attempt_number > 0)
                {
                    offerQuestionAttempts[vendorIndex] = error.expected_attempt_number - 1;
                }
                priceFeedback.text = $"Could not check the answer: {error?.message ?? "network error"}";
                priceFeedback.color = ErrorColor;
                questionShownAt = Time.unscaledTime;
            });
    }

    private void AddToBasket()
    {
        if (activeVendorIndex < 0 || !solvedOffers[activeVendorIndex] || basketSyncInProgress)
        {
            return;
        }

        int bundlePrice = CurrentBundlePrice(activeVendorIndex);
        int bundleSize = CurrentBundleSize(activeVendorIndex);
        if (quantities[activeVendorIndex] > 0)
        {
            purchaseFeedback.text = "This vendor's offer is already in your basket.";
            purchaseFeedback.color = TextColor;
            return;
        }

        if (spent + bundlePrice > VoucherValue)
        {
            purchaseFeedback.text = $"This price is correct, but adding it would exceed the {Money(VoucherValue)} voucher.";
            purchaseFeedback.color = ErrorColor;
            return;
        }

        int previousSpent = spent;
        int previousQuantity = quantities[activeVendorIndex];
        spent += bundlePrice;
        quantities[activeVendorIndex] += bundleSize;
        purchaseFeedback.text = "Added this vendor's offer to your basket.";
        purchaseFeedback.color = SuccessColor;
        RefreshPurchasePanel();
        UpdateHud();
        SyncBasket(previousSpent, activeVendorIndex, previousQuantity);
    }

    private void RemoveBundle()
    {
        if (activeVendorIndex < 0 || basketSyncInProgress)
        {
            return;
        }

        int bundleSize = CurrentBundleSize(activeVendorIndex);
        int bundlePrice = CurrentBundlePrice(activeVendorIndex);
        if (quantities[activeVendorIndex] < bundleSize)
        {
            return;
        }

        int previousSpent = spent;
        int previousQuantity = quantities[activeVendorIndex];
        spent -= bundlePrice;
        quantities[activeVendorIndex] -= bundleSize;
        purchaseFeedback.text = "Removed this vendor's offer from your basket.";
        purchaseFeedback.color = TextColor;
        RefreshPurchasePanel();
        UpdateHud();
        SyncBasket(previousSpent, activeVendorIndex, previousQuantity);
    }

    private void SyncBasket(int previousSpent, int changedVendorIndex, int previousQuantity)
    {
        if (!usingVoucherBackendSession || APIManager.Instance == null)
        {
            return;
        }

        List<VoucherBasketItemRequest> items = new();
        for (int i = 0; i < quantities.Length; i++)
        {
            if (quantities[i] <= 0 || sessionVendorsByIndex?[i] == null)
            {
                continue;
            }

            items.Add(new VoucherBasketItemRequest
            {
                offer_instance_id = sessionVendorsByIndex[i].offer_instance_id,
                quantity = quantities[i]
            });
        }

        basketSyncInProgress = true;
        RefreshPurchasePanel();
        APIManager.Instance.UpdateVoucherBasket(
            voucherSessionId,
            new VoucherBasketRequest
            {
                client_revision = basketRevision + 1,
                items = items.ToArray(),
                remaining_ms = CurrentRemainingMs()
            },
            basket =>
            {
                basketSyncInProgress = false;
                if (basket != null)
                {
                    basketRevision = basket.client_revision;
                    spent = basket.spent_cents;
                }
                RefreshPurchasePanel();
                UpdateHud();
            },
            error =>
            {
                basketSyncInProgress = false;
                if (error?.expected_basket_revision > 0)
                {
                    basketRevision = error.expected_basket_revision - 1;
                }
                spent = previousSpent;
                quantities[changedVendorIndex] = previousQuantity;
                purchaseFeedback.text = $"Basket was not saved: {error?.message ?? "network error"}";
                purchaseFeedback.color = ErrorColor;
                RefreshPurchasePanel();
                UpdateHud();
            });
    }

    private void RefreshPurchasePanel()
    {
        VendorOffer offer = offers[activeVendorIndex];
        basketTitle.text = $"Purchase {offer.Name}";
        int bundleSize = CurrentBundleSize(activeVendorIndex);
        int bundlePrice = CurrentBundlePrice(activeVendorIndex);
        string itemLabel = bundleSize == 1 ? "item" : "items";
        basketDeal.text =
            $"{offer.Hanzi}\n" +
            $"優惠：{CurrentHanOfferLabel(activeVendorIndex)} / {CurrentEnglishOfferLabel(activeVendorIndex)}\n" +
            $"One offer costs {Money(bundlePrice)} for {bundleSize} {itemLabel}  •  " +
            $"Unit price: {Money(CurrentUnitPrice(activeVendorIndex))}";
        purchaseQuantity.text = quantities[activeVendorIndex] > 0
            ? $"In basket: {quantities[activeVendorIndex]}"
            : "Not in basket";
        purchaseBalance.text = $"Voucher remaining: {Money(VoucherValue - spent)}";
        addToBasketButton.interactable = solvedOffers[activeVendorIndex]
            && quantities[activeVendorIndex] == 0
            && !basketSyncInProgress;
        removeButton.interactable = quantities[activeVendorIndex] >= bundleSize && !basketSyncInProgress;
    }

    private void RefreshPriceCheckPanel()
    {
        VendorOffer offer = offers[activeVendorIndex];
        int questionCount = GetQuestionCount(activeVendorIndex);
        int questionIndex = Mathf.Clamp(offerQuestionSteps[activeVendorIndex], 0, questionCount - 1);
        QuestionSkill skill = GetQuestionSkill(activeVendorIndex, questionIndex);
        purchaseTitle.text = offer.Name;
        purchaseDeal.text =
            $"{offer.Hanzi}\nOriginal price: {Money(CurrentBasePrice(activeVendorIndex))} each";
        offerAudioButtonLabel.SetText("Replay Offer Audio");
        offerAudioButton.interactable = APIManager.Instance != null && dialogueAudioLoadRoutine == null;
        purchasePrice.text =
            $"{QuestionSkillLabel(skill)}  •  Question {questionIndex + 1} of {questionCount}\n" +
            GetQuestionPrompt(activeVendorIndex, questionIndex);
        priceInputButton.interactable = !solvedOffers[activeVendorIndex] && !voucherAttemptInProgress;
        checkPriceButton.interactable = !voucherAttemptInProgress;
        checkPriceButton.gameObject.SetActive(!solvedOffers[activeVendorIndex]);
        questionShownAt = Time.unscaledTime;

        bool allowEnglishHelp = selectedDifficulty == Difficulty.Easy;
        englishHelpButton.gameObject.SetActive(allowEnglishHelp && !solvedOffers[activeVendorIndex]);
        RectTransform closeRect = closePriceButton.GetComponent<RectTransform>();
        SetCenteredRect(
            closeRect,
            allowEnglishHelp ? new Vector2(175f, -275f) : new Vector2(0f, -275f),
            new Vector2(280f, 58f));
    }

    private int GetQuestionCount(int vendorIndex)
    {
        return HasBackendOffer(vendorIndex)
            ? sessionOffersByIndex[vendorIndex].questions.Length
            : offers[vendorIndex].QuestionsFor(selectedDifficulty).Length;
    }

    private string GetQuestionPrompt(int vendorIndex, int questionIndex)
    {
        return HasBackendOffer(vendorIndex)
            ? sessionOffersByIndex[vendorIndex].questions[questionIndex].prompt_en
            : offers[vendorIndex].QuestionsFor(selectedDifficulty)[questionIndex].Prompt;
    }

    private QuestionSkill GetQuestionSkill(int vendorIndex, int questionIndex)
    {
        return HasBackendOffer(vendorIndex)
            ? ParseQuestionSkill(sessionOffersByIndex[vendorIndex].questions[questionIndex].skill)
            : offers[vendorIndex].QuestionsFor(selectedDifficulty)[questionIndex].Skill;
    }

    private static QuestionSkill ParseQuestionSkill(string skill)
    {
        return skill switch
        {
            "PRICE_CALCULATION" => QuestionSkill.PriceCalculation,
            "TRANSFER" => QuestionSkill.Transfer,
            _ => QuestionSkill.RuleMeaning
        };
    }

    private void ShowEnglishHelp()
    {
        if (activeVendorIndex < 0 || selectedDifficulty == Difficulty.Hard)
        {
            return;
        }

        bool firstUse = !englishHelpUsed[activeVendorIndex];
        englishHelpUsed[activeVendorIndex] = true;
        if (firstUse)
        {
            SendVoucherEvent("ENGLISH_SUPPORT_USED", activeVendorIndex);
        }
        priceFeedback.text = offers[activeVendorIndex].CalculationHint;
        priceFeedback.color = TextColor;
    }

    private void MarkQuestionCompleted(int vendorIndex, QuestionSkill skill, bool firstTry)
    {
        switch (skill)
        {
            case QuestionSkill.RuleMeaning:
                ruleChecksCompleted[vendorIndex] = true;
                ruleFirstTryCorrect[vendorIndex] = firstTry;
                break;
            case QuestionSkill.PriceCalculation:
                priceChecksCompleted[vendorIndex] = true;
                priceFirstTryCorrect[vendorIndex] = firstTry;
                break;
            case QuestionSkill.Transfer:
                transferChecksCompleted[vendorIndex] = true;
                transferFirstTryCorrect[vendorIndex] = firstTry;
                break;
        }
    }

    private static string QuestionSkillLabel(QuestionSkill skill)
    {
        return skill switch
        {
            QuestionSkill.RuleMeaning => "Offer meaning",
            QuestionSkill.PriceCalculation => "Price calculation",
            _ => "Transfer challenge"
        };
    }

    private string BuildWrongAnswerFeedback(OfferQuestion question, int enteredPrice, int attempts)
    {
        if (selectedDifficulty == Difficulty.Hard)
        {
            return "Not quite. Try again.";
        }

        if (enteredPrice == question.CommonWrongPrice
            && !string.IsNullOrWhiteSpace(question.MisconceptionFeedback))
        {
            return $"Not quite. {question.MisconceptionFeedback}";
        }

        if (selectedDifficulty == Difficulty.Medium || attempts == 1)
        {
            return $"Not quite. {question.CalculationHint}";
        }

        return $"Try this setup: {question.FormulaHint}";
    }

    private bool TryGetEnteredPrice(out int cents)
    {
        cents = 0;
        string value = enteredPriceText.Trim();
        if (string.IsNullOrEmpty(value))
        {
            return true;
        }

        bool parsed = decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal dollars)
            || decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out dollars);
        if (!parsed || dollars < 0m)
        {
            return false;
        }

        cents = Mathf.RoundToInt((float)(dollars * 100m));
        return true;
    }

    private void CapturePriceInput()
    {
        if (!priceInputFocused)
        {
            return;
        }

        string value = enteredPriceText;
        bool changed = false;

        if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete))
        {
            if (value.Length > 0)
            {
                value = value[..^1];
                changed = true;
            }
        }
        else
        {
            for (int digit = 0; digit <= 9 && value.Length < 6; digit++)
            {
                if (Input.GetKeyDown(NumberKeys[digit]) || Input.GetKeyDown(KeypadKeys[digit]))
                {
                    value += digit.ToString(CultureInfo.InvariantCulture);
                    changed = true;
                    break;
                }
            }

            bool decimalPressed = Input.GetKeyDown(KeyCode.Period)
                || Input.GetKeyDown(KeyCode.Comma)
                || Input.GetKeyDown(KeyCode.KeypadPeriod);
            if (decimalPressed && !value.Contains('.') && value.Length < 5)
            {
                value += value.Length == 0 ? "0." : ".";
                changed = true;
            }
        }

        if (changed)
        {
            enteredPriceText = value;
            UpdatePriceInputDisplay();
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            CheckPrice();
        }
    }

    private void UpdatePriceInputDisplay()
    {
        bool hasValue = !string.IsNullOrEmpty(enteredPriceText);
        string amount = hasValue ? $"${enteredPriceText}" : "$0.00";
        priceInputDisplay.text = amount;
        priceInputDisplay.color = hasValue || priceInputFocused
            ? TextColor
            : new Color32(130, 115, 120, 180);
    }

    private void FocusPriceInput()
    {
        SetPriceInputFocused(true);
    }

    private void SetPriceInputFocused(bool focused)
    {
        priceInputFocused = focused;
        priceInputBackground.color = focused
            ? new Color32(255, 246, 225, 255)
            : Color.white;
        priceInputOutline.effectColor = focused ? SuccessColor : AccentColor;
        priceInputOutline.effectDistance = focused ? new Vector2(3f, -3f) : new Vector2(2f, -2f);
        UpdatePriceInputDisplay();
    }

    private void CloseDialogue()
    {
        if (!vendorFlowActive)
        {
            return;
        }

        vendorFlowActive = false;
        vendorConversationToken++;
        usingBackendDialogue = false;
        backendOptionSelectionInProgress = false;
        backendDialogueNode = null;
        SetPriceInputFocused(false);
        StopVoucherDialogueAudio();
        ClearBackendOptions();
        standardLanguageButton.gameObject.SetActive(false);
        standardDialoguePanel.SetActive(false);
        purchaseModalRoot.SetActive(false);
        activeVendorIndex = -1;
        activeVendorId = string.Empty;
        activeVendorName = string.Empty;
        activeVendorPortrait = null;
        ReleaseInteraction();
        SetPlayerPaused(false);
    }

    private void UpdateHud()
    {
        voucherText.text = $"Voucher {Money(VoucherValue)}    Spent {Money(spent)}    Remaining {Money(VoucherValue - spent)}";
        ProgressSummary progress = GetProgress();
        taskText.text = selectedDifficulty == Difficulty.Hard
            ? $"Meaning {progress.RuleChecks}/{offers.Length}    Price {progress.PriceChecks}/{offers.Length}    Transfer {progress.TransferChecks}/{offers.Length}"
            : $"Meaning {progress.RuleChecks}/{offers.Length}    Price {progress.PriceChecks}/{offers.Length}";
        RefreshOfferDetails();
        RefreshPurchasedItems();

        timerText.gameObject.SetActive(selectedDifficulty == Difficulty.Hard);
        if (selectedDifficulty == Difficulty.Hard)
        {
            timerText.text = speedBonusAvailable
                ? $"Speed bonus {Mathf.CeilToInt(timeRemaining)}s"
                : "Speed bonus missed";
            timerText.color = !speedBonusAvailable || timeRemaining <= 15f ? ErrorColor : TextColor;
        }
    }

    private void ShowOfferDetails()
    {
        if (!gameActive || vendorFlowActive || resultRoot.activeSelf
            || !HokkienInteractionState.TryEnter(HokkienInteractionMode.Menu, this))
        {
            return;
        }

        SetBasketExpanded(false);
        RefreshOfferDetails();
        SetPlayerPaused(true);
        offerDetailsRoot.SetActive(true);
    }

    private void CloseOfferDetails()
    {
        if (!offerDetailsRoot.activeSelf)
        {
            return;
        }

        offerDetailsRoot.SetActive(false);
        HokkienInteractionState.Exit(this);
        SetPlayerPaused(false);
    }

    private void RefreshPurchasedItems()
    {
        if (purchasedItemRows == null || quantities == null)
        {
            if (basketToggleLabel != null)
            {
                basketToggleLabel.text = "Basket: 0 items ▼";
            }

            return;
        }

        int visibleRow = 0;
        int totalItems = 0;
        for (int i = 0; i < offers.Length; i++)
        {
            bool purchased = quantities[i] > 0;
            purchasedItemRows[i].gameObject.SetActive(basketExpanded && purchased);
            if (!purchased)
            {
                continue;
            }

            // Pack purchased items together so removed rows leave no gaps.
            float rowTop = -48f - visibleRow * PurchasedRowHeight;
            SetRect(
                purchasedItemRows[i], new Vector2(0f, rowTop),
                new Vector2(0f, rowTop - 30f));
            purchasedItemQuantityTexts[i].text = quantities[i].ToString();
            purchasedItemUnitPriceTexts[i].text = Money(CurrentUnitPrice(i));
            totalItems += quantities[i];
            visibleRow++;
        }

        string itemWord = totalItems == 1 ? "item" : "items";
        basketToggleLabel.text = basketExpanded
            ? $"Basket: {totalItems} {itemWord} ▲"
            : $"Basket: {totalItems} {itemWord} ▼";
        purchasedItemsEmptyText.gameObject.SetActive(basketExpanded && visibleRow == 0);

        float sectionHeight = visibleRow == 0 ? 115f : 68f + visibleRow * PurchasedRowHeight;
        SetRect(
            purchasedItemsSectionRect,
            new Vector2(25f, -CollapsedStatusCardHeight),
            new Vector2(-25f, -CollapsedStatusCardHeight - sectionHeight));
        purchasedItemsSection.SetActive(basketExpanded);
        statusCardRect.sizeDelta = new Vector2(
            790f, CollapsedStatusCardHeight + (basketExpanded ? sectionHeight : 0f));
    }

    private void TogglePurchasedItems()
    {
        if (!gameActive || vendorFlowActive || resultRoot.activeSelf)
        {
            return;
        }

        SetBasketExpanded(!basketExpanded);
    }

    private void SetBasketExpanded(bool expanded)
    {
        basketExpanded = expanded;
        RefreshPurchasedItems();
    }

    private void RefreshOfferDetails()
    {
        if (detailStatusTexts == null || offerUnlocked == null)
        {
            return;
        }

        for (int i = 0; i < offers.Length; i++)
        {
            bool discovered = offerUnlocked[i];
            bool solved = solvedOffers[i];
            detailStatusTexts[i].text = quantities[i] > 0
                ? "IN BASKET"
                : solved ? "SOLVED" : discovered ? "HEARD" : "NOT VISITED";
            detailOriginalTexts[i].text = discovered ? Money(CurrentBasePrice(i)) : "—";
            detailOfferTexts[i].text = solved
                ? CurrentEnglishOfferLabel(i)
                : discovered && selectedDifficulty != Difficulty.Hard
                    ? CurrentHanOfferLabel(i)
                    : "—";
            detailUnitTexts[i].text = solved
                ? Money(CurrentUnitPrice(i))
                : "—";
        }
    }

    private void TryFinishShopping()
    {
        if (!gameActive || vendorFlowActive || voucherCompletionInProgress)
        {
            return;
        }

        SetBasketExpanded(false);
        ProgressSummary progress = GetProgress();
        if (progress.Solved < offers.Length)
        {
            resultTitle.text = "Keep Learning";
            resultTitle.color = ErrorColor;
            resultMessage.text =
                $"Offer meanings: {progress.RuleChecks} / {offers.Length}\n" +
                $"Price calculations: {progress.PriceChecks} / {offers.Length}\n" +
                (selectedDifficulty == Difficulty.Hard
                    ? $"Transfer challenges: {progress.TransferChecks} / {offers.Length}\n"
                    : string.Empty) +
                $"\nComplete the remaining checks at {offers.Length - progress.Solved} vendor(s). " +
                "Basket choices are optional learning bonuses.";
            difficultyResultButton.gameObject.SetActive(false);
            retryButton.gameObject.SetActive(false);
            continueShoppingButton.gameObject.SetActive(true);
            returnToOriginButton.gameObject.SetActive(false);
            ReleaseInteraction();
            HokkienInteractionState.TryEnter(HokkienInteractionMode.Menu, this);
            SetPlayerPaused(true);
            resultRoot.SetActive(true);
            return;
        }

        if (usingVoucherBackendSession && APIManager.Instance != null)
        {
            CompleteBackendSession();
        }
        else
        {
            ShowResult();
        }
    }

    private void CompleteBackendSession()
    {
        voucherCompletionInProgress = true;
        SetPlayerPaused(true);
        APIManager.Instance.CompleteVoucherSession(
            voucherSessionId,
            basketRevision,
            result =>
            {
                voucherCompletionInProgress = false;
                if (result == null)
                {
                    ShowCompletionRequestError("The server returned an empty result.");
                    return;
                }

                ShowBackendResult(result);
            },
            error =>
            {
                voucherCompletionInProgress = false;
                ShowCompletionRequestError(error?.message ?? "network error");
            });
    }

    private void ShowCompletionRequestError(string message)
    {
        resultTitle.text = "Could Not Finish";
        resultTitle.color = ErrorColor;
        resultMessage.text =
            $"The session could not be completed: {message}\n\n" +
            "Your progress is still available. Return to the market and try again.";
        difficultyResultButton.gameObject.SetActive(false);
        retryButton.gameObject.SetActive(false);
        continueShoppingButton.gameObject.SetActive(true);
        returnToOriginButton.gameObject.SetActive(false);
        ReleaseInteraction();
        HokkienInteractionState.TryEnter(HokkienInteractionMode.Menu, this);
        SetPlayerPaused(true);
        resultRoot.SetActive(true);
    }

    private void ShowBackendResult(VoucherCompletionResult result)
    {
        gameActive = false;
        usingVoucherBackendSession = false;
        voucherSessionId = string.Empty;
        VoucherLearningResult learning = result.learning ?? new VoucherLearningResult();
        VoucherShoppingResult shopping = result.shopping ?? new VoucherShoppingResult();
        string learningSummary =
            "LEARNING\n" +
            $"Offer meaning: {learning.rule_completed}/{offers.Length}  •  first try {learning.rule_first_try}/{offers.Length}\n" +
            $"Price calculation: {learning.price_completed}/{offers.Length}  •  first try {learning.price_first_try}/{offers.Length}\n" +
            (selectedDifficulty == Difficulty.Hard
                ? $"Transfer: {learning.transfer_completed}/{offers.Length}  •  first try {learning.transfer_first_try}/{offers.Length}\n"
                : string.Empty) +
            $"English support used: {learning.english_support_vendor_count} vendor(s)";
        string shoppingSummary =
            "SHOPPING\n" +
            $"Spent: {Money(shopping.spent_cents)} / {Money(VoucherValue)}\n" +
            $"Vendors purchased from: {shopping.purchased_vendor_count}" +
            (shopping.vendor_target > 0 ? $" / {shopping.vendor_target} bonus target" : " (optional)") + "\n" +
            (shopping.vendor_target > 0
                ? $"Offer Explorer bonus: {(shopping.vendor_target_completed ? "Completed" : "Not completed")}"
                : "Offer Explorer: Optional") +
            (selectedDifficulty == Difficulty.Hard
                ? $"\nSpeed bonus: {(shopping.speed_bonus_completed ? "Completed" : "Missed")}"
                : string.Empty);

        resultTitle.text = "Learning Complete!";
        resultTitle.color = SuccessColor;
        resultMessage.text = $"{learningSummary}\n\n{shoppingSummary}";
        difficultyResultButton.gameObject.SetActive(true);
        retryButton.gameObject.SetActive(true);
        continueShoppingButton.gameObject.SetActive(false);
        returnToOriginButton.gameObject.SetActive(VoucherAdventureNavigation.HasOrigin);
        vendorFlowActive = false;
        SetPriceInputFocused(false);
        standardDialoguePanel.SetActive(false);
        purchaseModalRoot.SetActive(false);
        activeVendorIndex = -1;
        activeVendorPortrait = null;
        ReleaseInteraction();
        HokkienInteractionState.TryEnter(HokkienInteractionMode.Menu, this);
        SetPlayerPaused(true);
        resultRoot.SetActive(true);
    }

    private void ContinueShopping()
    {
        resultRoot.SetActive(false);
        HokkienInteractionState.Exit(this);
        SetPlayerPaused(false);
    }

    private void ShowLeaveConfirmation()
    {
        if (!gameActive || vendorFlowActive || !VoucherAdventureNavigation.HasOrigin)
            return;

        SetBasketExpanded(false);
        if (!HokkienInteractionState.TryEnter(HokkienInteractionMode.Menu, this))
            return;

        SetPlayerPaused(true);
        abandonRequestInProgress = false;
        stayInAdventureButton.interactable = true;
        confirmReturnButton.interactable = true;
        leaveConfirmationMessage.SetText(
            "Leaving will discard this Voucher Adventure run and its progress.");
        leaveConfirmRoot.SetActive(true);
    }

    private void CancelLeaveConfirmation()
    {
        if (abandonRequestInProgress)
            return;

        leaveConfirmRoot.SetActive(false);
        ReleaseInteraction();
        SetPlayerPaused(false);
    }

    private void ConfirmReturnToOrigin()
    {
        if (abandonRequestInProgress)
            return;

        if (!usingVoucherBackendSession || string.IsNullOrWhiteSpace(voucherSessionId))
        {
            ReturnToOrigin();
            return;
        }

        if (APIManager.Instance == null)
        {
            Debug.LogWarning("[Voucher Adventure] Returning without abandoning the current session.");
            FinishReturnToOrigin();
            return;
        }

        abandonRequestInProgress = true;
        stayInAdventureButton.interactable = false;
        confirmReturnButton.interactable = false;
        leaveConfirmationMessage.SetText("Ending this run...");

        APIManager.Instance.AbandonVoucherSession(
            voucherSessionId,
            _ => FinishReturnToOrigin(),
            error =>
            {
                Debug.LogWarning(
                    $"[Voucher Adventure] Session abandon failed: {error?.message ?? "network error"}");
                FinishReturnToOrigin();
            });
    }

    private void FinishReturnToOrigin()
    {
        abandonRequestInProgress = false;
        usingVoucherBackendSession = false;
        voucherSessionId = string.Empty;
        ReturnToOrigin();
    }

    private void ReturnToOrigin()
    {
        if (!VoucherAdventureNavigation.ReturnToOrigin())
            Debug.LogWarning("[Voucher Adventure] No lesson origin was available.");
    }

    private void ShowResult()
    {
        gameActive = false;
        ProgressSummary progress = GetProgress();
        int shoppingTarget = ShoppingVendorTarget;
        bool shoppingTargetMet = shoppingTarget == 0 || progress.PurchasedVendors >= shoppingTarget;
        string learningSummary =
            "LEARNING\n" +
            $"Offer meaning: {progress.RuleChecks}/{offers.Length}  •  first try {progress.RuleFirstTry}/{offers.Length}\n" +
            $"Price calculation: {progress.PriceChecks}/{offers.Length}  •  first try {progress.PriceFirstTry}/{offers.Length}\n" +
            (selectedDifficulty == Difficulty.Hard
                ? $"Transfer: {progress.TransferChecks}/{offers.Length}  •  first try {progress.TransferFirstTry}/{offers.Length}\n"
                : string.Empty) +
            $"English support used: {progress.EnglishHelpUsed} vendor(s)";
        string shoppingSummary =
            "SHOPPING\n" +
            $"Spent: {Money(spent)} / {Money(VoucherValue)}\n" +
            $"Vendors purchased from: {progress.PurchasedVendors}" +
            (shoppingTarget > 0 ? $" / {shoppingTarget} bonus target" : " (optional)") + "\n" +
            (shoppingTarget > 0
                ? $"Offer Explorer bonus: {(shoppingTargetMet ? "Completed" : "Not completed")}"
                : "Offer Explorer: Optional") +
            (selectedDifficulty == Difficulty.Hard
                ? $"\nSpeed bonus: {(speedBonusAvailable ? "Completed" : "Missed")}"
                : string.Empty);

        resultTitle.text = "Learning Complete!";
        resultTitle.color = SuccessColor;
        resultMessage.text = $"{learningSummary}\n\n{shoppingSummary}";

        difficultyResultButton.gameObject.SetActive(true);
        retryButton.gameObject.SetActive(true);
        continueShoppingButton.gameObject.SetActive(false);
        returnToOriginButton.gameObject.SetActive(VoucherAdventureNavigation.HasOrigin);
        vendorFlowActive = false;
        SetPriceInputFocused(false);
        standardDialoguePanel.SetActive(false);
        purchaseModalRoot.SetActive(false);
        activeVendorIndex = -1;
        activeVendorPortrait = null;
        ReleaseInteraction();
        HokkienInteractionState.TryEnter(HokkienInteractionMode.Menu, this);
        SetPlayerPaused(true);
        resultRoot.SetActive(true);
    }

    private ProgressSummary GetProgress()
    {
        ProgressSummary progress = new();
        if (offerUnlocked == null)
        {
            return progress;
        }

        for (int i = 0; i < offers.Length; i++)
        {
            if (solvedOffers[i]) progress.Solved++;
            if (quantities[i] > 0) progress.PurchasedVendors++;
            if (ruleChecksCompleted[i]) progress.RuleChecks++;
            if (priceChecksCompleted[i]) progress.PriceChecks++;
            if (transferChecksCompleted[i]) progress.TransferChecks++;
            if (ruleFirstTryCorrect[i]) progress.RuleFirstTry++;
            if (priceFirstTryCorrect[i]) progress.PriceFirstTry++;
            if (transferFirstTryCorrect[i]) progress.TransferFirstTry++;
            if (englishHelpUsed[i]) progress.EnglishHelpUsed++;
        }

        return progress;
    }

    private void ReleaseInteraction()
    {
        HokkienInteractionState.Exit(this);
    }

    private void SetPlayerPaused(bool paused)
    {
        if (playerMovement != null)
        {
            playerMovement.SetMovementPaused(paused);
        }
    }

    private void OnDestroy()
    {
        StopVoucherDialogueAudio();
        ReleaseInteraction();
    }

    private GameObject CreateModalRoot(string objectName, float opacity)
    {
        GameObject root = CreateUiObject(objectName, transform);
        StretchToParent(root.GetComponent<RectTransform>());
        root.AddComponent<Image>().color = new Color(0f, 0f, 0f, opacity);
        root.SetActive(false);
        return root;
    }

    private GameObject CreatePanel(string objectName, Transform parent, Vector2 size)
    {
        GameObject panel = CreateUiObject(objectName, parent);
        SetCenteredRect(panel.GetComponent<RectTransform>(), Vector2.zero, size);
        Image image = panel.AddComponent<Image>();

        if (panelFrame != null)
        {
            image.sprite = panelFrame;
            image.type = Image.Type.Simple;
            image.color = Color.white;
        }
        else
        {
            image.color = CardColor;
            Outline outline = panel.AddComponent<Outline>();
            outline.effectColor = AccentColor;
            outline.effectDistance = new Vector2(4f, -4f);
        }

        return panel;
    }

    private TMP_Text CreateText(string objectName, Transform parent, string content, float size, FontStyles style)
    {
        GameObject textObject = CreateUiObject(objectName, parent);
        TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = TextColor;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
        if (interfaceFont != null)
        {
            text.font = interfaceFont;
        }

        return text;
    }

    private void CreatePriceInput(Transform parent, out RectTransform rect)
    {
        GameObject inputObject = CreateUiObject("PriceInput", parent);
        rect = inputObject.GetComponent<RectTransform>();
        priceInputBackground = inputObject.AddComponent<Image>();
        priceInputBackground.color = Color.white;

        priceInputOutline = inputObject.AddComponent<Outline>();
        priceInputOutline.effectColor = AccentColor;
        priceInputOutline.effectDistance = new Vector2(2f, -2f);

        priceInputButton = inputObject.AddComponent<Button>();
        priceInputButton.targetGraphic = priceInputBackground;
        priceInputButton.transition = Selectable.Transition.None;
        priceInputButton.onClick.AddListener(FocusPriceInput);
    }

    private TMP_Text CreateTableCell(
        Transform parent, string objectName, string content, Vector2 position, Vector2 size,
        bool bold = false)
    {
        TMP_Text cell = CreateText(
            objectName, parent, content, bold ? 19f : 18f,
            bold ? FontStyles.Bold : FontStyles.Normal);
        SetCenteredRect(cell.rectTransform, position, size);
        cell.alignment = TextAlignmentOptions.Center;
        cell.textWrappingMode = TextWrappingModes.NoWrap;
        cell.overflowMode = TextOverflowModes.Truncate;
        cell.enableAutoSizing = true;
        cell.fontSizeMin = 13f;
        cell.fontSizeMax = bold ? 19f : 18f;
        return cell;
    }

    private Button CreateButton(
        string objectName, Transform parent, string label, float fontSize,
        UnityEngine.Events.UnityAction onClick, out RectTransform rect, out TMP_Text labelText)
    {
        GameObject buttonObject = CreateUiObject(objectName, parent);
        rect = buttonObject.GetComponent<RectTransform>();
        Image image = buttonObject.AddComponent<Image>();
        image.color = AccentColor;

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = AccentColor;
        colors.highlightedColor = new Color32(246, 192, 86, 255);
        colors.pressedColor = PressedColor;
        colors.selectedColor = AccentColor;
        colors.disabledColor = DisabledColor;
        button.colors = colors;
        button.onClick.AddListener(onClick);

        labelText = CreateText("Label", buttonObject.transform, label, fontSize, FontStyles.Bold);
        StretchToParent(labelText.rectTransform, 8f);
        labelText.alignment = TextAlignmentOptions.Center;
        createdButtons.Add(button);
        return button;
    }

    private void ApplyUnifiedButtonStyles()
    {
        foreach (Button button in createdButtons)
        {
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                StyleSecondaryButton(button, label);
            }
        }
    }

    private static void StyleSecondaryButton(Button button, TMP_Text label)
    {
        Image image = button.GetComponent<Image>();
        image.color = SecondaryButtonColor;

        ColorBlock colors = button.colors;
        colors.normalColor = SecondaryButtonColor;
        colors.highlightedColor = SecondaryButtonHoverColor;
        colors.pressedColor = AccentColor;
        colors.selectedColor = SecondaryButtonColor;
        button.colors = colors;

        Outline outline = button.GetComponent<Outline>() ?? button.gameObject.AddComponent<Outline>();
        outline.effectColor = SuccessColor;
        outline.effectDistance = new Vector2(2f, -2f);

        RectTransform labelRect = label.rectTransform;
        RectTransform buttonRect = button.GetComponent<RectTransform>();
        float horizontalPadding = Mathf.Clamp(buttonRect.sizeDelta.x * 0.04f, 8f, 12f);
        float verticalPadding = Mathf.Clamp(buttonRect.sizeDelta.y * 0.1f, 4f, 6f);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.offsetMin = new Vector2(horizontalPadding, verticalPadding);
        labelRect.offsetMax = new Vector2(-horizontalPadding, -verticalPadding);
        label.color = TextColor;
    }

    private static void SetDifficultyButtonSelected(Button button, bool selected)
    {
        Color fillColor = selected ? SecondaryButtonHoverColor : SecondaryButtonColor;
        Image image = button.GetComponent<Image>();
        image.color = fillColor;

        ColorBlock colors = button.colors;
        colors.normalColor = fillColor;
        colors.selectedColor = fillColor;
        button.colors = colors;

        Outline outline = button.GetComponent<Outline>();
        if (outline != null)
        {
            outline.effectDistance = selected ? new Vector2(4f, -4f) : new Vector2(2f, -2f);
        }

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.color = selected ? SuccessColor : TextColor;
        }
    }

    private static GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject uiObject = new(objectName, typeof(RectTransform));
        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }

    private static Transform FindChild(Transform root, string objectName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == objectName)
            {
                return child;
            }
        }

        throw new MissingReferenceException($"{objectName} was not found under {root.name}.");
    }

    private static void StretchToParent(RectTransform rect, float inset = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    private static void SetCenteredRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void AnchorTopRight(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = Vector2.one;
        rect.anchorMax = Vector2.one;
        rect.pivot = Vector2.one;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void SetRect(RectTransform rect, Vector2 topLeft, Vector2 bottomRight)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(topLeft.x, bottomRight.y);
        rect.offsetMax = new Vector2(bottomRight.x, topLeft.y);
    }

    private static string Money(int cents)
    {
        return $"${cents / 100f:0.00}";
    }
}
