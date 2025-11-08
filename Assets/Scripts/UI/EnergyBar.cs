using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class EnergyBar : MonoBehaviour
{
    [Header("Images")]
    [SerializeField] private Image fill;   // Filled type
    [SerializeField] private Image frame;  // optional
    [SerializeField] private Image glow;   // optional subtle additive glow

    [Header("Colors / Gradient")]
    [SerializeField] private Gradient colorByPercent;
    [SerializeField] private Color glowColor = new Color(0.2f,0.7f,1f,0.2f);

    [Header("Smoothing")]
    [SerializeField] private float lerpSpeed = 10f;

    public float Current01 { get; private set; } = 1f;
    float _target01 = 1f;

    public void Set01(float v)
    {
        _target01 = Mathf.Clamp01(v);
    }

    void Update()
    {
        if (!fill) return;
        Current01 = Mathf.Lerp(Current01, _target01, Time.deltaTime * lerpSpeed);
        fill.fillAmount = Current01;

        if (colorByPercent != null)
            fill.color = colorByPercent.Evaluate(Current01);

        if (glow)
        {
            glow.color = glowColor * Mathf.Lerp(0.4f, 1.0f, Current01); // brighter when fuller
        }
    }
}
