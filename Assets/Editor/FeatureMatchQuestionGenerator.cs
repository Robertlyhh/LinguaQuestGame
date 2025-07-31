using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class FeatureMatchQuestionGenerator : EditorWindow
{
    private string pastedText = "";

    [MenuItem("Tools/Quiz Generator/Feature Match Question")]
    public static void ShowWindow()
    {
        GetWindow<FeatureMatchQuestionGenerator>("Feature Match Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Paste Question Definition", EditorStyles.boldLabel);
        pastedText = EditorGUILayout.TextArea(pastedText, GUILayout.Height(300));

        if (GUILayout.Button("Generate Feature Match Question"))
        {
            GenerateFeatureMatchQuestion(pastedText);
        }
    }

    private enum ParseMode { None, Languages, Features, Match }

    private void GenerateFeatureMatchQuestion(string text)
    {
        string question = "";
        List<string> languages = new List<string>();
        List<string> features = new List<string>();
        List<int> matches = new List<int>();
        string explanation = "";

        string[] lines = text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

        ParseMode mode = ParseMode.None;

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();
            if (line.StartsWith("Q:"))
            {
                question = line.Substring(2).Trim();
            }
            else if (line.StartsWith("LANGUAGES:"))
            {
                mode = ParseMode.Languages;
            }
            else if (line.StartsWith("FEATURES:"))
            {
                mode = ParseMode.Features;
            }
            else if (line.StartsWith("MATCH:"))
            {
                mode = ParseMode.Match;
            }
            else if (line.StartsWith("EXPLANATION:"))
            {
                explanation = line.Substring("EXPLANATION:".Length).Trim();
                mode = ParseMode.None;
            }
            else
            {
                switch (mode)
                {
                    case ParseMode.Languages:
                        languages.Add(line);
                        break;
                    case ParseMode.Features:
                        features.Add(line);
                        break;
                    case ParseMode.Match:
                        foreach (string part in line.Split(' '))
                        {
                            if (int.TryParse(part.Trim(), out int index))
                            {
                                matches.Add(index);
                            }
                        }
                        break;
                }
            }
        }

        if (string.IsNullOrEmpty(question) || languages.Count == 0 || features.Count == 0 || matches.Count != languages.Count)
        {
            Debug.LogError("Invalid format: Make sure to include question, languages, features, and a matching index for each language.");
            return;
        }

        FeatureMatchQuestion asset = ScriptableObject.CreateInstance<FeatureMatchQuestion>();
        asset.question = question;
        asset.languages = languages.ToArray();
        asset.features = features.ToArray();
        asset.correctFeatureIndices = matches.ToArray();
        asset.explanation = explanation;

        string path = "Assets/ScriptableObjects/QuestionData/FeatureMatchingQuestion";
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);

        string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{path}/Q_{question.Substring(0, Mathf.Min(20, question.Length)).Replace(" ", "_")}.asset");
        AssetDatabase.CreateAsset(asset, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Feature match question created at: {assetPath}");
    }
}
