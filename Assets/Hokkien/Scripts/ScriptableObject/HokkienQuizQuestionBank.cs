using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HokkienQuizQuestionBank", menuName = "Hokkien/Quiz Question Bank")]
public class HokkienQuizQuestionBank : ScriptableObject
{
    [System.Serializable]
    public class QuizQuestionEntry
    {
        [TextArea]
        public string question;

        public string[] choices = new string[4];

        [Tooltip("Admin dialogue node ID for the question. Required when generating question POJ/audio.")]
        public string questionDialogueNodeId;

        [Tooltip("One admin dialogue node ID per choice. Required when generating each choice's POJ/audio.")]
        public string[] choiceDialogueNodeIds = new string[4];

        [Tooltip("Pre-generated POJ shown immediately without calling the backend.")]
        public string[] choiceHokkienRomanizedTexts = new string[4];

        [Tooltip("Resources-relative AudioClip path for each pre-generated choice audio file.")]
        public string[] choiceAudioResourcePaths = new string[4];

        [TextArea]
        [Tooltip("Provide an explanation for each of the 4 choices above.")]
        public string[] choiceExplanations = new string[4];

        [Range(0, 3)]
        public int correctAnswerIndex;
    }

    [Header("Questions")]
    public List<QuizQuestionEntry> questions = new List<QuizQuestionEntry>();

    public List<MultipleChoiceQuestion> CreateRuntimeQuestions()
    {
        List<MultipleChoiceQuestion> runtimeQuestions = new List<MultipleChoiceQuestion>();

        foreach (QuizQuestionEntry entry in questions)
        {
            if (entry == null)
            {
                continue;
            }

            MultipleChoiceQuestion runtimeQuestion = ScriptableObject.CreateInstance<MultipleChoiceQuestion>();
            runtimeQuestion.question = entry.question;
            runtimeQuestion.choices = entry.choices;
            runtimeQuestion.correctAnswerIndex = entry.correctAnswerIndex;
            runtimeQuestion.dialogueNodeId = entry.questionDialogueNodeId;
            runtimeQuestion.choiceDialogueNodeIds = entry.choiceDialogueNodeIds;

            int choiceCount = entry.choices != null ? entry.choices.Length : 0;
            runtimeQuestion.choiceHokkienRomanizedTexts = CopyToLength(entry.choiceHokkienRomanizedTexts, choiceCount);
            runtimeQuestion.choiceHokkienAudioUrls = new string[choiceCount];
            runtimeQuestion.choiceHokkienAudioClips = new AudioClip[choiceCount];
            for (int i = 0; i < choiceCount; i++)
            {
                if (entry.choiceAudioResourcePaths != null && i < entry.choiceAudioResourcePaths.Length
                    && !string.IsNullOrWhiteSpace(entry.choiceAudioResourcePaths[i]))
                {
                    runtimeQuestion.choiceHokkienAudioClips[i] = Resources.Load<AudioClip>(entry.choiceAudioResourcePaths[i]);
                }
            }

            // TRICK: We pack the 4 explanations into the single string using "|||" as a hidden divider!
            runtimeQuestion.explanation = string.Join("|||", entry.choiceExplanations);
            
            runtimeQuestions.Add(runtimeQuestion);
        }

        return runtimeQuestions;
    }

    private static string[] CopyToLength(string[] source, int length)
    {
        string[] result = new string[length];
        if (source != null)
            System.Array.Copy(source, result, Mathf.Min(source.Length, length));
        return result;
    }
}
