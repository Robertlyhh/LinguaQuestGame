using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;

[RequireComponent(typeof(AudioSource))]
public class DialogueAudioHandler : MonoBehaviour
{
    private AudioSource audioSource;

    [Header("API Config")]
    public string ttsApiUrl = "http://localhost:8000/audio-test";

    // Cache so we don't hit the backend twice for the same word
    private Dictionary<string, AudioClip> wordAudioCache = new Dictionary<string, AudioClip>();

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    // Called by WordHoverHandler when a word is clicked
    public void PlayWord(string word)
    {
        // If we already fetched this word before, just play it directly
        if (wordAudioCache.ContainsKey(word))
        {
            UnityEngine.Debug.Log("Playing cached audio for: " + word);
            PlayClip(wordAudioCache[word]);
            return;
        }

        // Otherwise fetch it from the backend
        StartCoroutine(FetchAndPlayWord(word));
    }

    // Plays the full sentence audio — used by SpeakerButton
    public void ReplayCurrentAudio()
    {
        if (audioSource.clip != null)
        {
            audioSource.time = 0;
            audioSource.Play();
            UnityEngine.Debug.Log("Replaying full dialogue audio.");
        }
    }

    private void PlayClip(AudioClip clip)
    {
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }

    private IEnumerator FetchAndPlayWord(string word)
    {
        UnityEngine.Debug.Log("Fetching audio for word: " + word);

        using (UnityWebRequest request = UnityWebRequest.Get(ttsApiUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                AudioBlobResponse response = JsonUtility.FromJson<AudioBlobResponse>(jsonResponse);

                // Check both fields since audio-test returns "audio_clip" not "audio_blob"
                string base64Audio = string.IsNullOrEmpty(response.data.audio_clip)
                    ? response.data.audio_blob
                    : response.data.audio_clip;

                if (string.IsNullOrEmpty(base64Audio))
                {
                    UnityEngine.Debug.LogError("DialogueAudioHandler: No audio found in response for word: " + word);
                    yield break;
                }

                // Decode base64 and write to temp file
                byte[] audioBytes = Convert.FromBase64String(base64Audio);
                string tempPath = System.IO.Path.Combine(UnityEngine.Application.temporaryCachePath, "word_temp.mp3");
                File.WriteAllBytes(tempPath, audioBytes);

                yield return StartCoroutine(LoadAndCacheWord(word, tempPath));
            }
            else
            {
                UnityEngine.Debug.LogError("DialogueAudioHandler: API error for word: " + word + " → " + request.error);
            }
        }
    }
    private IEnumerator LoadAndCacheWord(string word, string filePath)
    {
        string url = "file://" + filePath;

        using (UnityWebRequest audioRequest = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
        {
            yield return audioRequest.SendWebRequest();

            if (audioRequest.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(audioRequest);

                // Cache it so we never fetch this word again
                wordAudioCache[word] = clip;

                PlayClip(clip);
                UnityEngine.Debug.Log("Word audio playing and cached: " + word);
            }
            else
            {
                UnityEngine.Debug.LogError("DialogueAudioHandler: Failed to load MP3 for word: " + word + " → " + audioRequest.error);
            }
        }
    }

    // Clear cache when dialogue changes so words get fresh audio
    public void ClearCache()
    {
        wordAudioCache.Clear();
        UnityEngine.Debug.Log("Word audio cache cleared.");
    }

    [System.Serializable]
    private class TTSRequest
    {
        public string input_text;
        public string source_lang;
        public string output_lang;
    }

    [System.Serializable]
    private class AudioBlobResponse
    {
        public string status;
        public AudioBlobData data;
    }

    [System.Serializable]
    private class AudioBlobData
    {
        public string audio_clip; // matches /audio-test response
        public string audio_blob; // matches /audio-blob response later
    }
}