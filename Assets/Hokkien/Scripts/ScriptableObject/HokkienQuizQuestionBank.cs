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

            // TRICK: We pack the 4 explanations into the single string using "|||" as a hidden divider!
            runtimeQuestion.explanation = string.Join("|||", entry.choiceExplanations);
            
            runtimeQuestions.Add(runtimeQuestion);
        }

        return runtimeQuestions;
    }
}