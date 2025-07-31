using UnityEngine;

[CreateAssetMenu(fileName = "NewWordOrderQuestion", menuName = "Quiz/WordOrderQuestion")]
public class WordOrderQuestion : ScriptableObject
{
    [TextArea]
    public string questionText;  // A clear question prompt, e.g., "Reorder the sentence to follow correct phrase structure rules."

    public string[] wordParts;       // The shuffled or initial word/phrase parts displayed to the user

    public int[] correctOrderIndices;   // Correct order of indices from 'parts'. E.g., [2, 0, 1] if "Mary", "had", "a little lamb" is 2nd, 0th, then 1st.

    [TextArea]
    public string explanation;   // Optional explanation displayed when answer is incorrect
}
