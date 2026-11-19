using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Puts each quiz question's choices in a random order.
/// Everything tied to a choice moves with it: its POJ, audio, node id and
/// explanation, and correctAnswerIndex follows the correct choice.
/// Choices such as "I don't know" keep their place.
/// Works on copies, so the question bank assets are never changed.
/// </summary>
public static class QuizChoiceShuffler
{
    private const string ExplanationSeparator = "|||";

    private static readonly string[] PinnedChoices =
    {
        "i don't know",
        "i dont know",
        "all of the above",
        "none of the above",
    };

    public static List<MultipleChoiceQuestion> ShuffleAll(List<MultipleChoiceQuestion> questions)
    {
        if (questions == null)
            return null;

        List<MultipleChoiceQuestion> shuffled = new List<MultipleChoiceQuestion>(questions.Count);
        foreach (MultipleChoiceQuestion question in questions)
            shuffled.Add(Shuffle(question));
        return shuffled;
    }

    public static MultipleChoiceQuestion Shuffle(MultipleChoiceQuestion source)
    {
        if (source == null || source.choices == null || source.choices.Length < 2)
            return source;

        int count = source.choices.Length;
        int[] order = BuildOrder(source.choices);

        // Instantiate copies the serialized fields, arrays included,
        // so the original question and its arrays stay as they were.
        MultipleChoiceQuestion copy = UnityEngine.Object.Instantiate(source);
        copy.name = source.name;

        copy.choices = Reorder(source.choices, order);
        copy.choiceDialogueNodeIds = Reorder(source.choiceDialogueNodeIds, order);
        copy.choiceHokkienRomanizedTexts = Reorder(source.choiceHokkienRomanizedTexts, order);
        copy.choiceHokkienAudioUrls = Reorder(source.choiceHokkienAudioUrls, order);
        copy.choiceHokkienAudioClips = Reorder(source.choiceHokkienAudioClips, order);

        int newCorrect = Array.IndexOf(order, source.correctAnswerIndex);
        copy.correctAnswerIndex = newCorrect >= 0 ? newCorrect : source.correctAnswerIndex;

        // Explanations are packed one per choice with "|||"; reorder them only
        // when there is exactly one per choice.
        if (!string.IsNullOrEmpty(source.explanation))
        {
            string[] explanations = source.explanation.Split(new[] { ExplanationSeparator }, StringSplitOptions.None);
            if (explanations.Length == count)
                copy.explanation = string.Join(ExplanationSeparator, Reorder(explanations, order));
        }

        return copy;
    }

    /// <summary>order[newIndex] = original index of the choice shown there.</summary>
    private static int[] BuildOrder(string[] choices)
    {
        int count = choices.Length;
        int[] order = new int[count];
        List<int> movable = new List<int>(count);

        for (int i = 0; i < count; i++)
        {
            order[i] = i;
            if (!IsPinned(choices[i]))
                movable.Add(i);
        }

        // Fisher-Yates shuffle of the movable choices, placed back into the movable slots.
        List<int> shuffled = new List<int>(movable);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            int tmp = shuffled[i];
            shuffled[i] = shuffled[j];
            shuffled[j] = tmp;
        }

        for (int k = 0; k < movable.Count; k++)
            order[movable[k]] = shuffled[k];

        return order;
    }

    private static bool IsPinned(string choice)
    {
        if (string.IsNullOrWhiteSpace(choice))
            return false;

        string normalized = choice.Trim().TrimEnd('.', '!', '?').Replace('’', '\'').ToLowerInvariant();
        return Array.IndexOf(PinnedChoices, normalized) >= 0;
    }

    private static T[] Reorder<T>(T[] source, int[] order)
    {
        if (source == null)
            return null;

        T[] result = new T[order.Length];
        for (int i = 0; i < order.Length; i++)
        {
            int from = order[i];
            result[i] = from < source.Length ? source[from] : default;
        }
        return result;
    }
}
