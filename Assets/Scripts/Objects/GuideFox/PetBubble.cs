using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class PetBubble : MonoBehaviour
{
    public Canvas bubbleCanvas;
    public TextMeshProUGUI bubbleText;
    public Vector3 offset = new Vector3(1.5f, 0f, 0f);
    public float displayDuration = 4f;
    public BoolValue hasShownIntroduction;
    public PetMovement petMovement;

    private float timer = 0f;
    private bool showing = false;

    void Start()
    {
        bubbleCanvas.gameObject.SetActive(false);

        if (!hasShownIntroduction.runtimeValue)
        {
            ShowMessagesToPlayer(new List<string>() {
                "Dear explorer,",
                "Welcome to the linguistic World!",
                "Here, you will learn different linguistic skills.",
                "I am your guide spirit."
            });
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

            bubbleCanvas.transform.position = transform.position + offset;
            bubbleCanvas.transform.LookAt(Camera.main.transform);
            bubbleCanvas.transform.Rotate(0, 180f, 0);
        }
    }

    public void ShowMessagesToPlayer(List<string> messages)
    {
        StartCoroutine(ShowMessages(messages));
    }

    IEnumerator ShowMessages(List<string> messages)
    {
        petMovement.Appear();

        foreach (string message in messages)
        {
            ShowMessage(message);

            float elapsed = 0f;
            bool skip = false;
            while (elapsed < displayDuration && !skip)
            {
                if (Input.GetKeyDown(KeyCode.V))
                    skip = true;
                elapsed += Time.deltaTime;
                yield return null;
            }

            HideBubble();
            yield return new WaitForSeconds(0.5f);
        }

        yield return new WaitForSeconds(0.5f);
        petMovement.Disappear();
    }

    public void ShowMessage(string message)
    {
        bubbleText.text = message;
        bubbleCanvas.gameObject.SetActive(true);
        timer = 0f;
        showing = true;
    }

    public void HideBubble()
    {
        bubbleCanvas.gameObject.SetActive(false);
        showing = false;
    }
}
