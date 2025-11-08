using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class EnergyBarFX : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private EnergyBar bar;
    [SerializeField] private RectTransform barRect; // EnergyBar Rect
    [SerializeField] private RectTransform fxAnchor; // child under EnergyBar
    [SerializeField] private AnimationCurve shakeCurve; // quick shake for loss

    [Header("Shine Scroll")]
    [SerializeField] private RawImage shine; // use RawImage for easy UV scroll OR Image with material
    [SerializeField] private Vector2 shineSpeed = new Vector2(-0.8f, 0f);

    [Header("Sparkle FX")]
    [SerializeField] private ParticleSystem gainSparkles; // small, blue, short
    [SerializeField] private ParticleSystem lossCracks;   // subtle white shards

    [Header("Flash")]
    [SerializeField] private Image flash; // full-bar white/blue overlay
    [SerializeField] private float flashDuration = 0.15f;
    [SerializeField] private Color gainFlash = new Color(0.6f, 0.9f, 1f, 0.6f);
    [SerializeField] private Color lossFlash = new Color(1f, 0.2f, 0.2f, 0.5f);

    [Header("Low Energy Pulse")]
    [SerializeField] private float lowThreshold = 0.2f;
    [SerializeField] private float pulseSpeed = 6f;
    [SerializeField] private float pulseAmount = 0.06f;

    float _prev01 = 1f;
    float _flashT;
    Color _flashTarget;

    void Reset()
    {
        bar = GetComponent<EnergyBar>();
        barRect = GetComponent<RectTransform>();
        if (transform.Find("FXAnchor")) fxAnchor = transform.Find("FXAnchor").GetComponent<RectTransform>();
    }

    void Update()
    {
        if (!bar) return;

        // Detect change
        float cur = bar.Current01;
        float delta = cur - _prev01;
        if (Mathf.Abs(delta) > 0.0001f)
        {
            if (delta > 0f) OnGain(delta);
            else OnLoss(-delta);
            _prev01 = cur;
        }

        // Shine scroll (if using RawImage)
        if (shine)
        {
            shine.uvRect = new Rect(shine.uvRect.position + shineSpeed * Time.deltaTime, shine.uvRect.size);
            shine.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.12f, 0.32f, cur)); // slightly stronger when full
        }

        // Flash fade
        if (flash && _flashT > 0f)
        {
            _flashT -= Time.deltaTime / flashDuration;
            flash.color = Color.Lerp(new Color(_flashTarget.r, _flashTarget.g, _flashTarget.b, 0f), _flashTarget, Mathf.Clamp01(_flashT));
        }

        // Low-energy pulse (scale)
        if (barRect)
        {
            if (cur < lowThreshold)
            {
                float s = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
                barRect.localScale = new Vector3(s, s, 1f);
            }
            else
            {
                barRect.localScale = Vector3.one;
            }
        }
    }

    void OnGain(float amount)
    {
        // quick sparkle burst near the current fill edge
        if (gainSparkles && fxAnchor)
        {
            var ps = gainSparkles;
            PositionFXAtFillEdge(ps.transform as RectTransform);
            ps.Play();
        }
        // quick blue flash
        TriggerFlash(gainFlash);
    }

    void OnLoss(float amount)
    {
        // tiny shake
        if (shakeCurve != null && barRect) StartCoroutine(DoShake());

        // subtle crack shards
        if (lossCracks && fxAnchor)
        {
            var ps = lossCracks;
            PositionFXAtFillEdge(ps.transform as RectTransform);
            ps.Play();
        }
        // quick red flash
        TriggerFlash(lossFlash);
    }

    System.Collections.IEnumerator DoShake()
    {
        Vector3 basePos = barRect.anchoredPosition3D;
        float t = 0f, dur = 0.12f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float n = shakeCurve.Evaluate(t / dur);
            barRect.anchoredPosition3D = basePos + (Vector3)Random.insideUnitCircle * (2.0f * n);
            yield return null;
        }
        barRect.anchoredPosition3D = basePos;
    }

    void TriggerFlash(Color target)
    {
        if (!flash) return;
        _flashTarget = target;
        _flashT = 1f;
        flash.enabled = true;
    }

    void PositionFXAtFillEdge(RectTransform fx)
    {
        if (!fx || !barRect) return;
        // place FX at the right edge of current fill inside the bar rect
        float cur = bar.Current01;
        var size = barRect.rect.size;
        Vector2 local = new Vector2(Mathf.Lerp(-size.x * 0.5f, size.x * 0.5f, cur), 0f);
        fx.anchoredPosition = local;
    }
}
