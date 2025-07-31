using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class PasteQuestionGenerator : EditorWindow
{
    string pastedText = "";


    [MenuItem("Tools/Paste Question Generator")]
    public static void ShowWindow()
    {
        GetWindow<PasteQuestionGenerator>("Paste Question Generator");
    }

    void OnGUI()
    {
        GUILayout.Label("Paste your question content here:", EditorStyles.boldLabel);
        pastedText = EditorGUILayout.TextArea(pastedText, GUILayout.Height(200));

        if (GUILayout.Button("Generate Scriptable Object"))
        {
            GenerateQuestionFromText(pastedText);
        }
    }

    void GenerateQuestionFromText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            Debug.LogWarning("No text provided.");
            return;
        }

        string[] lines = text.Split('\n');
        string question = "";
        List<string> choices = new List<string>();
        int correctIndex = 0;
        string explanation = "";

        bool isTrueFalse = false;
        bool hasCustomChoices = false;


        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            if (line.StartsWith("Q:"))
                question = line.Substring(2).Trim();
            else if (line.StartsWith("A:"))
                choices.Add(line.Substring(2).Trim());
            else if (line.StartsWith("B:"))
                choices.Add(line.Substring(2).Trim());
            else if (line.StartsWith("C:"))
                choices.Add(line.Substring(2).Trim());
            else if (line.StartsWith("D:"))
                choices.Add(line.Substring(2).Trim());
            else if (line.StartsWith("ANSWER:"))
            {
                string ans = line.Substring(7).Trim().ToLower();

                // True/False support
                if (ans == "true" || ans == "false")
                {
                    isTrueFalse = true;
                    choices = new List<string> { "True", "False" };
                    correctIndex = ans == "true" ? 0 : 1;
                }
                else
                {
                    correctIndex = "ABCD".IndexOf(ans.ToUpper());
                    hasCustomChoices = true;
                }
            }
            else if (line.StartsWith("EXPLANATION:"))
                explanation = line.Substring(12).Trim();

        }

        if (string.IsNullOrEmpty(question) || choices.Count < 2)
        {
            Debug.LogError("Invalid format. Please ensure your input includes a question, at least 2 choices, and an answer.");
            return;
        }

        MultipleChoiceQuestion mcq = ScriptableObject.CreateInstance<MultipleChoiceQuestion>();
        mcq.question = question;
        mcq.choices = choices.ToArray();
        mcq.correctAnswerIndex = correctIndex;
        mcq.explanation = explanation;

        string path = "Assets/ScriptableObjects/QuestionData/MultipleChoice";
        if (isTrueFalse)
        {
            path = "Assets/ScriptableObjects/QuestionData/TrueOrFalse";
        }
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        string assetName = question.Substring(0, Mathf.Min(20, question.Length)).Replace(" ", "_");
        string fullPath = $"{path}/{assetName}_MCQ.asset";
        AssetDatabase.CreateAsset(mcq, fullPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Multiple Choice Question created: {fullPath}");
        Selection.activeObject = mcq;
    }
}
