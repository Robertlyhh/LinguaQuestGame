using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class SpiritBubble : MonoBehaviour
{
    public Canvas bubbleCanvas;
    public TextMeshProUGUI bubbleText;
    public Vector3 offset = new Vector3(1.5f, 2f, 0f);
    public float displayDuration = 4f;
    public BoolValue hasShownIntroduction;
    public List<string> startMessages = new List<string>() {
        "Dear explorer,",
        "Welcome to the linguistic World!",
        "Here, you will learn different linguistic skills.",
        "and have fun!",
        "I am your guide spirit.",
        "I will help you on your journey.",
        "Let's get started!",
        "Use WASD to move around.",
        "Press E to interact." };


    public List<string> MessagesBeatingFirstEnemy = new List<string>() {
        "Well done, explorer!",
        "You have defeated your first enemy!",
        "Keep practicing your skills.",
        "You will become stronger with each challenge.",
        "Remember S > NP VP!" };

    private float timer = 0f;
    private bool showing = false;

    void Start()
    {
        if (bubbleCanvas == null || bubbleText == null)
        {
            Debug.LogError("Bubble Canvas or Text is not assigned in the inspector.");
            return;
        }
        if (!hasShownIntroduction.runtimeValue)
        {
            StartCoroutine(ShowMessages(startMessages));
            hasShownIntroduction.runtimeValue = true;
        }

    }

    void Update()
    {
        if (showing)
        {
            timer += Time.deltaTime;
            if (timer > displayDuration)
            {
                HideBubble();
            }

            // Keep the bubble above the spirit
            bubbleCanvas.transform.position = transform.position + offset;
            bubbleCanvas.transform.LookAt(Camera.main.transform); // always face camera
            bubbleCanvas.transform.Rotate(0, 180f, 0); // Flip if needed
        }
    }


    public IEnumerator ShowMessages(List<string> messages)
    {
        if (messages == null || messages.Count == 0)
        {
            Debug.LogWarning("No messages to show.");
            yield break;
        }
        Debug.Log("Starting to show messages.");
        foreach (string message in messages)
        {
            ShowMessage(message);

            bool skipToNext = false;
            float elapsed = 0f;
            while (elapsed < displayDuration && !skipToNext)
            {
                if (Input.GetKeyDown(KeyCode.V))
                {
                    skipToNext = true; // Skip to the next message
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            HideBubble();

            // Pause before showing the next message, allow skipping as well
            if (!skipToNext)
            {
                elapsed = 0f;
                while (elapsed < 1f)
                {
                    if (Input.GetKeyDown(KeyCode.V))
                    {
                        break; // Skip the pause
                    }
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }
        }
    }


    public void ShowMessage(string message)
    {
        Debug.Log("Showing message: " + message);
        bubbleText.text = message;
        bubbleCanvas.gameObject.SetActive(true);
        timer = 0f;
        showing = true;
    }

    public void ShowMessagesToPlayer(List<string> messages)
    {
        StartCoroutine(ShowMessages(messages));
    }

    public void HideBubble()
    {
        bubbleCanvas.gameObject.SetActive(false);
        showing = false;
    }
}
