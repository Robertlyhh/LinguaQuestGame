using UnityEngine;

[CreateAssetMenu(fileName = "NewQuizQuestion", menuName = "Quiz/MultipleChoiceQuestion")]
public class MultipleChoiceQuestion : ScriptableObject
{
    [TextArea]
    public string question;
    public string[] choices;
    public int correctAnswerIndex; // 0-based index
    public string explanation; // Optional explanation for the correct answer

    [Header("Generated Hokkien Content")]
    public string dialogueNodeId;

    [TextArea]
    public string hokkienRomanizedText;

    public string hokkienAudioUrl;
    public AudioClip hokkienAudioClip;

    [Header("Generated Hokkien Choice Content")]
    public string[] choiceDialogueNodeIds;
    public string[] choiceHokkienRomanizedTexts;
    public string[] choiceHokkienAudioUrls;
    public AudioClip[] choiceHokkienAudioClips;
}
