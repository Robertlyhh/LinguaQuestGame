using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public class WordBlock : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private string wordText = "run";
    [SerializeField] private string shownLabel = "Noun"; // what the player sees
    [SerializeField] private bool isLabelCorrect = true; // whether shownLabel matches the true POS

    [Header("Points / Timing")]
    [SerializeField] private int pointsOnCorrectSlice = 100; // slicing an INCORRECT label is correct play
    [SerializeField] private int pointsOnWrongSlice = -50; // slicing a CORRECT label is a mistake
    [SerializeField] private float destroyDelay = 0.05f;

    [Header("Refs")]
    [SerializeField] private SpriteRenderer background;
    [SerializeField] private TextMeshPro wordTMP;
    [SerializeField] private TextMeshPro labelTMP;
    [SerializeField] private ParticleSystem sliceParticles; // optional

    [Header("Colors")]
    [SerializeField] private Color neutralColor = Color.white;
    [SerializeField] private Color goodColor = new Color(0.2f, 1f, 0.4f); // correct action (hit wrong label)
    [SerializeField] private Color badColor = new Color(1f, 0.3f, 0.3f); // mistake (hit correct label)

    private bool _hasBeenSliced;

    void Reset()
    {
        if (!background) background = GetComponentInChildren<SpriteRenderer>();
        if (!wordTMP) wordTMP = transform.Find("Text: Word")?.GetComponent<TextMeshPro>();
        if (!labelTMP) labelTMP = transform.Find("Text: Label")?.GetComponent<TextMeshPro>();
    }

    void OnEnable()
    {
        _hasBeenSliced = false;
        ApplyTexts();
        SetNeutral();
        EnableColliders(true);
    }

    // Public init used by the spawner
    public void Initialize(string word, string shownLabel, bool isLabelCorrect)
    {
        this.wordText = word;
        this.shownLabel = shownLabel;
        this.isLabelCorrect = isLabelCorrect;
        ApplyTexts();
        SetNeutral();
        EnableColliders(true);
    }

    private void ApplyTexts()
    {
        if (wordTMP) wordTMP.text = wordText;
        if (labelTMP) labelTMP.text = shownLabel;
    }

    private void SetNeutral()
    {
        if (background) background.color = neutralColor;
        if (wordTMP) wordTMP.color = Color.white;
        if (labelTMP) labelTMP.color = new Color(0.9f, 0.9f, 0.9f);
    }

    private void EnableColliders(bool enabled)
    {
        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = enabled;
    }

    /// <summary>
    /// Call this from the slash system when the swipe overlaps.
    /// Returns the score delta for this slice.
    /// </summary>
    public int Slice()
    {
        if (_hasBeenSliced) return 0;
        _hasBeenSliced = true;

        // “Correct play” = slice blocks whose label is incorrect
        bool correctPlay = !isLabelCorrect;

        // visual feedback
        if (background) background.color = correctPlay ? goodColor : badColor;

        if (sliceParticles)
        {
            var main = sliceParticles.main;
            main.startColor = correctPlay ? goodColor : badColor;
            sliceParticles.Play();
        }

        // disable further hits immediately
        EnableColliders(false);

        // schedule removal (pooling-friendly: SetActive(false))
        StartCoroutine(RemoveAfterDelay());

        return correctPlay ? pointsOnCorrectSlice : pointsOnWrongSlice;
    }

    private System.Collections.IEnumerator RemoveAfterDelay()
    {
        yield return new WaitForSeconds(destroyDelay);
        gameObject.SetActive(false);
    }

    // Optional: gizmos to help layout
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = isLabelCorrect ? new Color(0f, 0.8f, 1f, 0.35f) : new Color(1f, 0.5f, 0f, 0.35f);
        Gizmos.DrawSphere(transform.position, 0.15f);
    }
#endif
}
