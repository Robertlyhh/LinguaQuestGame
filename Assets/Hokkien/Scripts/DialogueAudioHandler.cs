using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class DialogueAudioHandler : MonoBehaviour
{
    private AudioSource audioSource;
    public bool useTestAudio = true;

    // Dictionary to map a word to its [start_time, end_time]
    private Dictionary<string, float[]> wordTimestampMap = new Dictionary<string, float[]>();

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    // Used by NPC to pass a hardcoded audio clip and its word map
    public void LoadAudioClip(AudioClip clip, Dictionary<string, float[]> timestamps)
    {
        if (clip == null) return;

        audioSource.clip = clip;
        wordTimestampMap = timestamps;
        UnityEngine.Debug.Log("Audio Clip and Word Map Loaded: " + clip.name);
    }

    // This is for the Speaker Button (plays the full sentence)
    public void ReplayCurrentAudio()
    {
        if (audioSource.clip != null)
        {
            audioSource.time = 0; //starts from the beginning
            audioSource.Play();
            UnityEngine.Debug.Log("Replaying full dialogue audio.");
        }
    }

    // For future API use
    public void PlayDialogueAudio(string audioUrl)
    {
        if (useTestAudio)
        {
            if (audioSource.clip != null) audioSource.Play();
            UnityEngine.Debug.Log("Playing Test Audio");
        }
        else
        {
            StartCoroutine(DownloadAndPlay(audioUrl));
        }
    }
    // This function plays a specific word based on the word name
    public void PlayWord(string word)
    {
        // Check if the word exists in our map
        if (wordTimestampMap.ContainsKey(word))
        {
            float[] times = wordTimestampMap[word];
            float startTime = times[0];
            float endTime = times[1];

            //Stop any current audio and jump to the start time
            audioSource.Stop();
            audioSource.time = startTime;
            audioSource.Play();

            //Start a timer to stop the audio exactly when the word ends
            Invoke("StopSegment", endTime - startTime);
            UnityEngine.Debug.Log("Playing segment for: " + word);
        }
        else
        {
            UnityEngine.Debug.LogWarning("Word not found in map: " + word);
        }
    }

    private void StopSegment()
    {
        audioSource.Stop();
    }
    IEnumerator DownloadAndPlay(string url)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                audioSource.clip = clip;
                audioSource.Play();
            }
            else
            {
                UnityEngine.Debug.LogError("Audio download failed: " + www.error);
            }
        }
    }
}