using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;
using System.IO;
using UnityEngine.Networking;
using static System.Net.Mime.MediaTypeNames;

public class SpeakerButton : MonoBehaviour
{
    [Header("References")]
    public TMP_Text dialogueText;
    public AudioSource audioSource;

    [Header("API Config")]
    public string ttsApiUrl = "http://localhost:8000/api/v1/generate/audio-blob";

    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnSpeakerClicked);

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void OnSpeakerClicked()
    {
        string sentence = dialogueText.text;

        if (string.IsNullOrEmpty(sentence))
        {
            UnityEngine.Debug.LogWarning("SpeakerButton: No dialogue text found.");
            return;
        }

        UnityEngine.Debug.Log("SpeakerButton clicked. Sentence: " + sentence);
        StartCoroutine(FetchAndPlayTTS(sentence));
    }

    private IEnumerator FetchAndPlayTTS(string sentence)
    {
        button.interactable = false;

        string jsonBody = JsonUtility.ToJson(new TTSRequest
        {
            input_text = sentence,
            source_lang = "hokkien",
            output_lang = "english"
        });

        using (UnityWebRequest request = new UnityWebRequest(ttsApiUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                AudioBlobResponse response = JsonUtility.FromJson<AudioBlobResponse>(jsonResponse);

                if (response == null || string.IsNullOrEmpty(response.data.audio_blob))
                {
                    UnityEngine.Debug.LogError("SpeakerButton: No audio blob found in response.");
                    button.interactable = true;
                    yield break;
                }

                // Decode base64 to bytes and write to temp file
                byte[] audioBytes = Convert.FromBase64String(response.data.audio_blob);
                string tempPath = System.IO.Path.Combine(UnityEngine.Application.temporaryCachePath, "tts_temp.mp3");
                File.WriteAllBytes(tempPath, audioBytes);

                // Load and play the temp MP3 file
                yield return StartCoroutine(LoadMp3FromFile(tempPath));
            }
            else
            {
                UnityEngine.Debug.LogError("TTS API Error: " + request.error);
                // TODO: show UI error message to player later
                button.interactable = true;
            }
        }
    }

    private IEnumerator LoadMp3FromFile(string filePath)
    {
        string url = "file://" + filePath;

        using (UnityWebRequest audioRequest = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
        {
            yield return audioRequest.SendWebRequest();

            if (audioRequest.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(audioRequest);
                audioSource.clip = clip;
                audioSource.Play();
                UnityEngine.Debug.Log("TTS MP3 playing successfully.");
            }
            else
            {
                UnityEngine.Debug.LogError("SpeakerButton: Failed to load MP3. " + audioRequest.error);
            }
        }

        button.interactable = true;
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
        public string audio_blob;
        public string source_lang;
        public string output_lang;
        public string input_text;
    }

    [System.Serializable]
    private class TTSRequest
    {
        public string input_text;
        public string source_lang;
        public string output_lang;
    }
}