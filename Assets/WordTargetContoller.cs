using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class WordTargetController : MonoBehaviour
{
    public TMP_Text label;
    private WordLassoManager manager;
    private bool collected = false;

    public void SetWord(string word, WordLassoManager mgr)
    {
        label.text = word;
        manager = mgr;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collected) return;

        // When word reaches the catch zone
        if (collision.CompareTag("CatchZone"))
        {
            collected = true;
            //manager.OnWordCollected(this); // new method you'll add in manager
            gameObject.SetActive(false);   // hide it after collection
        }
    }

    public string GetWord()
    {
        return label.text;
    }
}

