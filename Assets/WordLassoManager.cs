using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class WordLassoManager : MonoBehaviour
{
    [Header("Setup")]
    public Transform spawnArea;         // Reference to WordSpawnArea
    public GameObject wordPrefab;       // Prefab of the word target
    public WordOrderQuestion currentQuestion;

    [Header("Gameplay")]
    public List<WordTargetController> activeWords = new List<WordTargetController>();

    void Start()
    {
        if (currentQuestion != null)
        {
            SpawnWords(currentQuestion);
        }
    }

    public void SpawnWords(WordOrderQuestion question)
    {
        // Clear old words if any
        foreach (var w in activeWords)
        {
            if (w != null) Destroy(w.gameObject);
        }
        activeWords.Clear();

        currentQuestion = question;

        // Shuffle the word parts
        List<string> shuffledParts = new List<string>(question.wordParts);
        for (int i = 0; i < shuffledParts.Count; i++)
        {
            int randIndex = Random.Range(i, shuffledParts.Count);
            string temp = shuffledParts[i];
            shuffledParts[i] = shuffledParts[randIndex];
            shuffledParts[randIndex] = temp;
        }

        // Determine spawn bounds
        Vector2 center = spawnArea.position;
        Vector2 halfSize = spawnArea.localScale / 2f;

        // Calculate spacing for a horizontal line
        float spacing = (halfSize.x * 2f) / shuffledParts.Count;

        // Spawn words in a horizontal line, evenly spaced
        for (int i = 0; i < shuffledParts.Count; i++)
        {
            Vector2 spawnPos = new Vector2(
                center.x - halfSize.x + spacing / 2f + i * spacing, // x position
                center.y // y position fixed
            );

            GameObject newWord = Instantiate(wordPrefab, spawnPos, Quaternion.identity);
            var controller = newWord.GetComponent<WordTargetController>();
            controller.SetWord(shuffledParts[i], this);

            activeWords.Add(controller);
        }
    }

    // Called when a word is “removed” (e.g. caught by the lasso)
    public void OnWordRemoved(WordTargetController word)
    {
        if (activeWords.Contains(word))
        {
            activeWords.Remove(word);
            Destroy(word.gameObject);

            if (CheckIfOnlyCorrectWordsRemain())
            {
                Debug.Log("Sentence complete!");
            }
        }
    }

    bool CheckIfOnlyCorrectWordsRemain()
    {
        // Placeholder for logic later: compare active words to correct order
        return activeWords.Count == currentQuestion.correctOrderIndices.Length;
    }
}
