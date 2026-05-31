using UnityEngine;

public class CatchZone : MonoBehaviour
{
    [Tooltip("Which type this bin accepts.")]
    public TruthType acceptedType;

    [Tooltip("Optional point override for this specific bin.")]
    public int pointsOverride = -1;

    [Tooltip("If true, wrong objects are removed and count as a miss.")]
    public bool destroyWrongObject = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("[CatchZone] Trigger entered by: " + other.name);

        TossableObject tossable = null;

        if (other.attachedRigidbody != null)
        {
            tossable = other.attachedRigidbody.GetComponent<TossableObject>();
            Debug.Log("[CatchZone] attachedRigidbody found: " + other.attachedRigidbody.name);
        }

        if (tossable == null)
        {
            tossable = other.GetComponent<TossableObject>();
        }

        if (tossable == null)
        {
            Debug.Log("[CatchZone] No TossableObject found on entering object.");
            return;
        }

        Debug.Log("[CatchZone] TossableObject found: " + tossable.name + " | Object Type: " + tossable.truthType + " | Bin Accepts: " + acceptedType);

        if (!tossable.CanBeScored())
        {
            Debug.Log("[CatchZone] TossableObject cannot be scored yet.");
            return;
        }

        if (tossable.truthType == acceptedType)
        {
            Debug.Log("[CatchZone] Correct bin. Scoring object now.");
            tossable.Score(pointsOverride);
        }
        else
        {
            Debug.Log("[CatchZone] Wrong bin. No score.");

            if (destroyWrongObject)
            {
                tossable.Miss();
            }
        }
    }
}