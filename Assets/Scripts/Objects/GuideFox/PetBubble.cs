using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class PetBubble : MonoBehaviour
{
    public Canvas bubbleCanvas;
    public TextMeshProUGUI bubbleText;
    public Vector3 offset = new Vector3(1.5f, 2f, 0f);
    public float baseDisplayDuration = 4f;
    public BoolValue hasShownIntroduction;
    public List<string> startMessages = new List<string>() {
        "Dear Explorer,",
        "Welcome to the world of Canada!",
        "Here, you will discover new language skills and embark on a journey filled with adventure and learning.",
        "I am your guiding spirit, and I will be with you every step of the way.",
        "Let us begin!",
        "Use WASD to move around the world.",
        "Press E to interact with characters and objects you encounter.",
        "Left-click to swing your sword.",
        "Some obstacles will block your path — strike them down to continue.",
        "Be careful! Enemies will engage you the moment you get too close.",
        "Stay sharp, stay ready.",
        "That is all from me. Safe travels, and good luck!"
    };

    private float timer = 0f;
    private bool showing = false;
    private Coroutine currentRoutine;
    private PetMovement petMovement;

    void Start()
    {
        if (bubbleCanvas == null || bubbleText == null)
        {
            Debug.LogError("Bubble Canvas or Text is not assigned in the inspector.");
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
            timer += Time.deltaTime;
            if (timer > baseDisplayDuration)
            {
                HideBubble();
            }

            // Keep the bubble above the pet
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

        // Pet appears
        if (petMovement != null) petMovement.Appear();

        foreach (string message in messages)
        {
            ShowMessage(message);

            bool skipToNext = false;
            float elapsed = 0f;

            // Scale duration based on length of the message
            float duration = Mathf.Max(baseDisplayDuration, message.Length * 0.08f);

            while (elapsed < duration && !skipToNext)
            {
                if (Input.GetKeyDown(KeyCode.V))
                {
                    skipToNext = true; // Skip to the next message
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            HideBubble();

            // Short pause before next message (unless skipped)
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

        // Pet disappears when all messages are done
        if (petMovement != null) petMovement.Disappear();
        bubbleCanvas.gameObject.SetActive(false);
        currentRoutine = null;
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
        {
            StopCoroutine(currentRoutine);
        }
        currentRoutine = StartCoroutine(ShowMessages(messages));
    }

    public void HideBubble()
    {
        bubbleCanvas.gameObject.SetActive(false);
        showing = false;
    }
}
