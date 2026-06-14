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

        [Range(0, 3)]
        public int correctAnswerIndex;

        [TextArea]
        public string explanation;
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
            runtimeQuestion.explanation = entry.explanation;
            runtimeQuestions.Add(runtimeQuestion);
        }

        return runtimeQuestions;
    }
}