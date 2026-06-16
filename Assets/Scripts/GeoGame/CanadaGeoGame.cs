using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class CanadaGeoGame : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI feedbackText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;
    public Image questionImage;
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI ratingText;

    [Header("Province Images — for highlight on correct answer")]
    public Image atlantic;
    public Image farNorth;
    public Image maritimes;
    public Image ontario;
    public Image prairies;
    public Image quebec;
    public Image westCoast;

    [Header("Questions")]
    public List<ProvinceQuestion> questions;

    [Header("Settings")]
    public float timePerQuestion = 20f;

    private int currentIndex = 0;
    private int score = 0;
    private float timer;
    private bool canAnswer = true;

    void Start()
    {
        ShuffleQuestions();
        LoadQuestion();
    }

    void Update()
    {
        if (!canAnswer) return;

        timer -= Time.deltaTime;
        timerText.text = "Time Left: " + Mathf.CeilToInt(timer);

        if (timer <= 0)
            StartCoroutine(HandleAnswer(false, "Time's up!"));
    }

    void ShuffleQuestions()
    {
        for (int i = 0; i < questions.Count; i++)
        {
            int rand = Random.Range(i, questions.Count);
            var temp = questions[i];
            questions[i] = questions[rand];
            questions[rand] = temp;
        }
    }

    void LoadQuestion()
    {
        if (currentIndex >= questions.Count)
        {
            EndGame();
            return;
        }

        canAnswer = true;
        timer = timePerQuestion;
        feedbackText.text = "";
        ResetAllHighlights();

        ProvinceQuestion q = questions[currentIndex];
        questionText.text = q.questionText;

        if (questionImage != null && q.questionImage != null)
        {
            questionImage.gameObject.SetActive(true);
            questionImage.sprite = q.questionImage;
        }
        else if (questionImage != null)
        {
            questionImage.gameObject.SetActive(false);
        }
    }

    public void OnProvinceClicked(string provinceName)
{
    Debug.Log("OnProvinceClicked called with: " + provinceName);
    Debug.Log("canAnswer: " + canAnswer);
    Debug.Log("questions count: " + questions.Count);

    if (!canAnswer) return;

    ProvinceQuestion q = questions[currentIndex];
    Debug.Log("Correct province: " + q.correctProvince);

    bool isCorrect = provinceName == q.correctProvince;
    Debug.Log("isCorrect: " + isCorrect);

    StartCoroutine(HandleAnswer(isCorrect, q.funFact));
}

    private IEnumerator HandleAnswer(bool correct, string fact)
    {
        canAnswer = false;
        ProvinceQuestion q = questions[currentIndex];

        if (correct)
        {
            int points = Mathf.RoundToInt(timer * 100);
            score += points;
            feedbackText.text = "Correct! +" + points + " points\n" + fact;
            feedbackText.color = Color.green;
            HighlightRegion(q.correctProvince, Color.green);
        }
        else
        {
            feedbackText.text = "Wrong! It was " + q.correctProvince + "\n" + fact;
            feedbackText.color = Color.red;
            HighlightRegion(q.correctProvince, Color.green); // show correct
        }

        scoreText.text = "Score: " + score;
        yield return new WaitForSeconds(2.5f);

        currentIndex++;
        LoadQuestion();
    }

    private void HighlightRegion(string name, Color color)
    {
        Image target = GetRegionImage(name);
        if (target != null)
            target.color = color;
    }

    public void ResetAllHighlights()
    {
        // Reset to original colors
        if (atlantic) atlantic.color = new Color(1f, 0.4f, 0.4f, 1f); // pink
        if (farNorth) farNorth.color = new Color(0.5f, 0.5f, 0.5f, 1f); // grey
        if (maritimes) maritimes.color = new Color(0.6f, 0.6f, 0.6f, 1f); // light grey
        if (ontario) ontario.color = new Color(0.9f, 0.5f, 0.1f, 1f); // orange
        if (prairies) prairies.color = new Color(0.9f, 0.9f, 0.1f, 1f); // yellow
        if (quebec) quebec.color = new Color(0.4f, 0.7f, 1f, 1f); // blue
        if (westCoast) westCoast.color = new Color(0.1f, 0.7f, 0.5f, 1f); // teal
    }

    private Image GetRegionImage(string name)
    {
        return name switch
        {
            "Atlantic" => atlantic,
            "FarNorth" => farNorth,
            "Maritimes" => maritimes,
            "Ontario" => ontario,
            "Prairies" => prairies,
            "Quebec" => quebec,
            "WestCoast" => westCoast,
            _ => null
        };
    }

    void EndGame()
    {
        resultPanel.SetActive(true);
        resultText.text = "Final Score: " + score;
        ratingText.text = GetRating();
    }

    string GetRating()
    {
        if (score > 10000) return "True Canadian! ??";
        if (score > 6000) return "Pretty good, eh!";
        if (score > 3000) return "Keep exploring Canada!";
        return "Time to study the map!";
    }
}