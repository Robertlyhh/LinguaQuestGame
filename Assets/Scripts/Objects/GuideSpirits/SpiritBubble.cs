using UnityEngine;
using TMPro;

public class SpiritBubble : MonoBehaviour
{
    public Canvas bubbleCanvas;
    public TextMeshProUGUI bubbleText;
    public Vector3 offset = new Vector3(1.5f, 2f, 0f);
    public float displayDuration = 4f;

    private float timer = 0f;
    private bool showing = false;

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
