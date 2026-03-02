using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Networking;

public class SpeakerButton : MonoBehaviour
{
    [Header("References")]
    public TMP_Text dialogueText;        // drag your DialogueText object here
    public AudioSource audioSource;      // attach an AudioSource on this same object

    [Header("API Config — fill these when backend is ready")]
    public string ttsApiUrl = "https://YOUR_BACKEND_URL/api/tts";   //replace with tts api url later

    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnSpeakerClicked);

        // Add AudioSource automatically if missing
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void OnSpeakerClicked()
    {
        string sentence = dialogueText.text;

        if (string.IsNullOrEmpty(sentence))
        {
            Debug.LogWarning("SpeakerButton: No dialogue text found.");
            return;
        }

        Debug.Log("SpeakerButton clicked. Sentence: " + sentence);
        StartCoroutine(FetchAndPlayTTS(sentence));
    }
    //filler code until backend is ready
    private IEnumerator FetchAndPlayTTS(string sentence)
    {
        // Disable button while loading so user can't double click
        button.interactable = false;

        // Build the request body — adjust the JSON fields to match your backend later
        string jsonBody = JsonUtility.ToJson(new TTSRequest { text = sentence, language = "hokkien" });

        using (UnityWebRequest request = new UnityWebRequest(ttsApiUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerAudioClip(ttsApiUrl, AudioType.MPEG);
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                audioSource.clip = clip;
                audioSource.Play();
                Debug.Log("TTS playing successfully.");
            }
            else
            {
                Debug.LogError("TTS API Error: " + request.error);
                // TODO: optionally show a UI error message to the player here (come bacl for later)
            }
        }

        button.interactable = true;
    }

    [System.Serializable]
    private class TTSRequest
    {
        public string text;
        public string language;
    }
}