using UnityEngine;

public class TossableObject : MonoBehaviour
{
    public int points = 1;
    public float outOfBoundsPadding = 2f;
    public TruthType truthType;

    private OrbitTossGameManager manager;
    private bool isLatched;
    private bool canBeScored;
    private bool consumed;

    public void Initialize(OrbitTossGameManager newManager)
    {
        manager = newManager;
        isLatched = false;
        canBeScored = false;
        consumed = false;

        Debug.Log("[TossableObject] Initialized: " + name + " | Type: " + truthType);
    }

    void Start()
    {
        if (manager == null)
        {
            manager = FindFirstObjectByType<OrbitTossGameManager>();
            Debug.Log("[TossableObject] Manager auto-found: " + (manager != null));
        }
    }

    public void OnLatched()
    {
        isLatched = true;
        canBeScored = false;
        Debug.Log("[TossableObject] Latched: " + name);
    }

    public void OnReleased()
    {
        isLatched = false;
        canBeScored = true;
        Debug.Log("[TossableObject] Released, can now be scored: " + name);
    }

    public bool CanBeScored()
    {
        Debug.Log("[TossableObject] CanBeScored? " + canBeScored + " | consumed=" + consumed + " | name=" + name);
        return canBeScored && !consumed;
    }

    public void Score(int overridePoints = -1)
    {
        if (!CanBeScored())
        {
            Debug.Log("[TossableObject] Score blocked because object cannot be scored.");
            return;
        }

        if (manager == null)
        {
            Debug.LogError("[TossableObject] No OrbitTossGameManager reference.");
            return;
        }

        consumed = true;

        int award = overridePoints > 0 ? overridePoints : points;
        Debug.Log("[TossableObject] Correct bin. Scored for " + award + " points: " + name + " | Type: " + truthType);

        manager.OnObjectScored(this, award);
        Destroy(gameObject);
    }

    public void Miss()
    {
        if (consumed)
        {
            Debug.Log("[TossableObject] Miss ignored because already consumed: " + name);
            return;
        }

        consumed = true;

        Debug.Log("[TossableObject] Wrong bin / missed: " + name + " | Type: " + truthType);

        if (manager == null)
        {
            Debug.LogError("[TossableObject] No OrbitTossGameManager reference during Miss().");
            Destroy(gameObject);
            return;
        }

        manager.OnObjectMissed(this);
        Destroy(gameObject);
    }
}