using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class WordTargetController : MonoBehaviour
{
    public TMP_Text label;
    private WordLassoManager manager;

    public void SetWord(string word, WordLassoManager mgr)
    {
        label.text = word;
        manager = mgr;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Bullet")) // or whatever your bullet tag is
        {
            manager.OnWordRemoved(this);
        }
    }
}

