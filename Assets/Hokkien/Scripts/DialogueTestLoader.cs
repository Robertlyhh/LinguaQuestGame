using UnityEngine;
using System.Collections.Generic;

public class DialogueTestLoader : MonoBehaviour
{
    public AudioClip testClip; // Drag your audio file here
    private DialogueAudioHandler audioHandler;

    void Start()
    {
        audioHandler = FindObjectOfType<DialogueAudioHandler>();

        //Testing
        // Format: "Word", new float[] { StartTime, EndTime }
        Dictionary<string, float[]> mockMap = new Dictionary<string, float[]>
        {
            { "This", new float[] { 0.0f, 0.5f } },
            { "is", new float[] { 0.6f, 0.9f } },
            { "a", new float[] { 1.0f, 1.2f } },
            { "basic", new float[] { 1.3f, 1.8f } }
        };

        //Loads into the handler
        audioHandler.LoadAudioClip(testClip, mockMap);
    }
}