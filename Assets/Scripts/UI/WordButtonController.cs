using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WordButtonController : MonoBehaviour
{
    // public TMP_Text label; 
    /*  Ben: I'm removing this reference field 
        because I was getting "Type Mismatch" when attempting to 
        change the object reference. It is now automatically
        grabbed in the initialization.
    */
    private TMP_Text label;
    private Button button;
    private SyntaxShuffleManager manager;
    public AudioSource clickSound;
    private Color originalColor;

    public void Init(string word, SyntaxShuffleManager mgr)
    {
        if (label == null) label = GetComponentInChildren<TMP_Text>(true);
        label.text = word;

        manager = mgr;
        button = GetComponent<Button>();
        clickSound = GetComponent<AudioSource>();
        button.onClick.AddListener(OnClick);
        originalColor = GetComponent<Image>().color;
        // Debug.Log($"Original color for button '{word}': {originalColor}");
        
    }

    void OnClick()
    {
        manager.OnWordClicked(this);
        clickSound.Play();
    }

    public string GetWord()
    {
        return label.text;
    }

    public void SetWord(string newWord)
    {
        label.text = newWord;
    }

    public Color GetOriginalColor()
    {
        return originalColor;
    }
}
