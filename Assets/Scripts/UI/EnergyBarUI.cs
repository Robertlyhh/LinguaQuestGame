using UnityEngine;
using UnityEngine.UI;

public class EnergyBarUI : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private SwordWaveManager source;   // if left empty, will try SwordWaveManager.Instance

    [Header("UI Refs")]
    [SerializeField] private Image fillImage;           // the inner blue bar (Image Type = Filled, Horizontal)
    [SerializeField] private Image glowImage;           // optional overlay/glow image
                                                        // can be null

    [Header("Behavior")]
    [SerializeField] private float smoothSpeed = 10f;   // how fast the bar interpolates
    [SerializeField, Range(0f, 1f)]
    private float lowEnergyThreshold = 0.25f;           // under 25% -> pulse glow
    [SerializeField] private Color lowEnergyColor = Color.red;
    [SerializeField] private Color normalGlowColor = Color.white;

    private float _currentFill = 1f;

    private void Awake()
    {
        // auto resolve source if not set in Inspector
        if (!source)
        {
            source = SwordWaveManager.Instance;
        }
    }

    private void Start()
    {
        // initialize fill to correct value
        if (source)
        {
            _currentFill = GetTargetFill();
            ApplyFill(_currentFill);
        }
        else
        {
            // no source? hide bar or keep full
            ApplyFill(1f);
        }

        // init glow
        if (glowImage)
        {
            glowImage.color = normalGlowColor;
        }
    }

    private void Update()
    {
        if (!source || !fillImage)
            return;

        float target = GetTargetFill();

        // smooth interpolate
        _currentFill = Mathf.Lerp(_currentFill, target, Time.deltaTime * smoothSpeed);
        ApplyFill(_currentFill);

        // optional low-energy feedback
        HandleGlow(target);
    }

    private float GetTargetFill()
    {
        if (!source || source.maxEnergy <= 0)
            return 0f;

        return (float)source.energy / source.maxEnergy;
    }

    private void ApplyFill(float value)
    {
        value = Mathf.Clamp01(value);

        // for "Filled" image
        fillImage.fillAmount = value;
    }

    private void HandleGlow(float targetFill)
    {
        if (!glowImage) return;

        if (targetFill < lowEnergyThreshold)
        {
            // pulse between transparent and red
            float pulse = (Mathf.Sin(Time.time * 6f) + 1f) * 0.5f; // 0..1
            Color c = Color.Lerp(Color.clear, lowEnergyColor, pulse);
            c.a = 1f; // ensure visible
            glowImage.color = c;
        }
        else
        {
            // gentle return to normal
            glowImage.color = Color.Lerp(glowImage.color, normalGlowColor, Time.deltaTime * 8f);
        }
    }

    // optional: call this from other scripts when you know energy just jumped
    public void SnapToCurrent()
    {
        _currentFill = GetTargetFill();
        ApplyFill(_currentFill);
    }

    public void SetFillColorToRed()
    {
        if (fillImage)
        {
            fillImage.color = Color.red;
            glowImage.color = Color.red;
        }
    }

    public void ResetFillColor()
    {
        if (fillImage)
        {
            fillImage.color = Color.white;
            glowImage.color = normalGlowColor;
        }
    }
}
