using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Breakable : MonoBehaviour
{
    private Animator animator;
    public AudioSource audioSource;
    public AudioClip smashSound;
    public PetBubble petBubble;
    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    public virtual void Break()
    {
        // ---- TUTORIAL FLAG ----
        if (petBubble != null)
            petBubble.playerHasSwungSword = true;
        // -----------------------

        animator.SetBool("smash", true);
        StartCoroutine(BreakCo());
    }

    IEnumerator BreakCo()
    {
        if (audioSource != null && smashSound != null)
        {
            audioSource.PlayOneShot(smashSound);
        }
        yield return new WaitForSeconds(0.5f);
        this.gameObject.SetActive(false);
    }
}
