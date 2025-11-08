using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionFade : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private float fadeDuration = 3f;
    private float fadeTimer = 0f;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        fadeTimer += Time.deltaTime;
        float alpha = Mathf.Lerp(1f, 0f, fadeTimer / fadeDuration);

        Color color = spriteRenderer.color;
        color.a = alpha;
        spriteRenderer.color = color;

        if (fadeTimer >= fadeDuration)
        {
            Destroy(gameObject);
        }
    }
}
