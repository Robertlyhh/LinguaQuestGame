using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System;

public class PetBubble : MonoBehaviour
{
    public Canvas bubbleCanvas;
    public TextMeshProUGUI bubbleText;
    public Vector3 offset = new Vector3(1.5f, 2f, 0f);
    public float baseDisplayDuration = 4f;
    public BoolValue hasShownIntroduction;

    // Tutorial messages with optional wait conditions
    public List<string> startMessages = new List<string>()
    {
        "Before we begin, let us learn the controls!",
        "Use WASD or the arrow keys to move around — give it a try!",
        "WAIT_MOVE",  // sentinel: wait until player moves
        "Well done! Now press E to interact with characters and objects.",
        "Try it out — interact with the sign to your left!",
        "WAIT_INTERACT", // sentinel: wait until player presses E
        "Perfect! Now press F to throw fireballs — go ahead!",
        "WAIT_FIREBALL", // sentinel: wait until player presses F
        "Excellent! Left-click to swing your sword.",
        "You see that pot? Break it by left-clicking your mouse!",
        "WAIT_SWORD", // sentinel: wait until player left-clicks
        "Outstanding! You are ready to begin your adventure!",
        "Press E on the door when you are ready to start exploring!"
    };

    // These are set by external scripts when actions are performed
    [HideInInspector] public bool playerHasMoved = false;
    [HideInInspector] public bool playerHasSprinted = false;
    [HideInInspector] public bool playerHasInteracted = false;
    [HideInInspector] public bool playerHasThrownFireball = false;
    [HideInInspector] public bool playerHasSwungSword = false;
    [HideInInspector] public bool isPaused = false;
    [HideInInspector] public bool isWaitingForAction = false;
    [HideInInspector] public bool playerHasCollectedCoin = false;
    [HideInInspector] public bool playerHasCollectedHeart = false;

    private float timer = 0f;
    private bool showing = false;
    public Coroutine currentRoutine;
    private PetMovement petMovement;


    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (bubbleCanvas == null || bubbleText == null)
        {
            Debug.LogError("Bubble Canvas or Text is not assigned.");
            return;
        }

        petMovement = GetComponent<PetMovement>();

        if (!hasShownIntroduction.runtimeValue)
        {
            ShowMessagesToPlayer(startMessages);
            hasShownIntroduction.runtimeValue = true;
        }
    }

    void Update()
    {
        if (showing)
        {
            // Don't auto-hide while waiting for player to perform an action
            if (!isWaitingForAction && !isPaused)
            {
                timer += Time.deltaTime;
                if (timer > baseDisplayDuration)
                    HideBubble();
            }

            bubbleCanvas.transform.position = transform.position + offset;
            bubbleCanvas.transform.rotation = Quaternion.LookRotation(
                bubbleCanvas.transform.position - Camera.main.transform.position
            );
        }
    }

    public IEnumerator ShowMessages(List<string> messages)
    {
        if (messages == null || messages.Count == 0)
        {
            Debug.LogWarning("No messages to show.");
            yield break;
        }

        if (petMovement != null) petMovement.Appear();

        foreach (string message in messages)
        {
            if (message.StartsWith("WAIT_"))
            {
                isWaitingForAction = true;  // disable timer
                showing = false;
                yield return StartCoroutine(WaitForAction(message));
                isWaitingForAction = false; // re-enable timer after action done
                continue;
            }

            ShowMessage(message);

            bool skipToNext = false;
            float elapsed = 0f;
            float duration = Mathf.Max(baseDisplayDuration, message.Length * 0.08f);

            while (elapsed < duration && !skipToNext)
            {
                if (isPaused)
                {
                    yield return null;
                    continue;
                }
                if (Input.GetKeyDown(KeyCode.V))
                    skipToNext = true;
                elapsed += Time.deltaTime;
                yield return null;
            }

            HideBubble();

            if (!skipToNext)
            {
                elapsed = 0f;
                while (elapsed < 0.5f)
                {
                    if (Input.GetKeyDown(KeyCode.V)) break;
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }
        }

        if (petMovement != null) petMovement.Disappear();
        bubbleCanvas.gameObject.SetActive(false);
        currentRoutine = null;
    }

    private IEnumerator WaitForAction(string actionType)
    {
        // Wait while paused first
        yield return new WaitUntil(() => !isPaused);

        // Show the bubble again while waiting
        bubbleCanvas.gameObject.SetActive(true);
        showing = true;

        switch (actionType)
        {
            case "WAIT_MOVE":
                yield return new WaitUntil(() => playerHasMoved && !isPaused);
                break;
            case "WAIT_SPRINT":
                yield return new WaitUntil(() => playerHasSprinted && !isPaused);
                break;
            case "WAIT_INTERACT":
                yield return new WaitUntil(() => playerHasInteracted && !isPaused);
                break;
            case "WAIT_FIREBALL":
                yield return new WaitUntil(() => playerHasThrownFireball && !isPaused);
                break;
            case "WAIT_SWORD":
                yield return new WaitUntil(() => playerHasSwungSword && !isPaused);
                break;
            case "WAIT_COIN":
                yield return new WaitUntil(() => playerHasCollectedCoin && !isPaused);
                break;
            case "WAIT_HEART":
                yield return new WaitUntil(() => playerHasCollectedHeart && !isPaused);
                break;
        }

        yield return new WaitForSeconds(0.5f);
    }

    public void ShowMessage(string message)
    {
        bubbleText.text = message;
        bubbleCanvas.gameObject.SetActive(true);
        timer = 0f;
        showing = true;
    }

    public void ShowMessagesToPlayer(List<string> messages)
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(ShowMessages(messages));
    }

    public void HideBubble()
    {
        bubbleCanvas.gameObject.SetActive(false);
        showing = false;
    }
}