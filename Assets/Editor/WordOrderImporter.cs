#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class WordOrderImporter : EditorWindow
{
    private string inputText = "";

    [MenuItem("Tools/Quiz Generator/Word Order Question")]
    public static void ShowWindow()
    {
        GetWindow<WordOrderImporter>("Word Order Importer");
    }

    void OnGUI()
    {
        GUILayout.Label("Paste your Word Order Question here:", EditorStyles.boldLabel);
        inputText = EditorGUILayout.TextArea(inputText, GUILayout.Height(200));

        if (GUILayout.Button("Create WordOrderQuestion Asset"))
        {
            CreateWordOrderQuestion(inputText);
        }
    }

    void CreateWordOrderQuestion(string text)
    {
        string[] lines = text.Split('\n');

        string questionText = "";
        string[] wordParts = null;
        int[] correctOrderIndices = null;
        string explanation = "";

        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();
            if (trimmedLine.StartsWith("Question:", System.StringComparison.OrdinalIgnoreCase))
                questionText = trimmedLine.Substring("Question:".Length).Trim();

            else if (trimmedLine.StartsWith("Words:", System.StringComparison.OrdinalIgnoreCase))
                wordParts = trimmedLine.Substring("Words:".Length).Trim().Split(' ');

            else if (trimmedLine.StartsWith("CorrectOrder:", System.StringComparison.OrdinalIgnoreCase))
                correctOrderIndices = trimmedLine.Substring("CorrectOrder:".Length).Trim()
                    .Split(' ')
                    .Select(int.Parse)
                    .ToArray();

            else if (trimmedLine.StartsWith("Explanation:", System.StringComparison.OrdinalIgnoreCase))
                explanation = trimmedLine.Substring("Explanation:".Length).Trim();
        }

        if (string.IsNullOrEmpty(questionText) || wordParts == null || correctOrderIndices == null)
        {
            Debug.LogError("Missing fields. Make sure your text includes Question, Words, and CorrectOrder.");
            return;
        }

        WordOrderQuestion asset = ScriptableObject.CreateInstance<WordOrderQuestion>();
        asset.questionText = questionText;
        asset.wordParts = wordParts;
        asset.correctOrderIndices = correctOrderIndices;
        asset.explanation = explanation;

        string path = "Assets/ScriptableObjects/QuestionData/WordOrderQuestion";
        Directory.CreateDirectory(path);
        string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{path}/WordOrder_{wordParts[0]}.asset");

        AssetDatabase.CreateAsset(asset, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Created WordOrderQuestion at {assetPath}");
    }
}
#endif
