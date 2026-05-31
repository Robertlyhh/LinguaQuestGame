using UnityEngine;

public class LassoScript : MonoBehaviour
{
    public GameObject Bullet;
    public float BulletSpeed = 25f;
    public Transform ShootPoint;
    public LineRenderer line;

    [Header("Put these from PlayerInScene")]
    public SpringJoint2D spring;
    public Rigidbody2D playerBody;

    private Vector2 direction;
    private Rigidbody2D targetBody;

    [Header("Pull In")]
    public float latchDistance = 1.5f;
    public float springFrequency = 6f;
    public float springDamping = 0.85f;

    [Header("Target While Latched")]
    public float latchedDrag = 3f;
    public float latchedAngularDrag = 5f;

    [Header("Orbit")]
    public float orbitRadius = 1.5f;
    public float orbitStartDistance = 1.7f;
    public float orbitSpeed = 8f;
    public float orbitPullStrength = 12f;
    public bool orbitClockwise = true;

    [Header("Release")]
    public float releaseBoost = 2f;
    public float maxReleaseSpeed = 15f;

    private float originalDrag;
    private float originalAngularDrag;
    private bool isOrbiting = false;

    void Start()
    {
        if (line != null)
        {
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.enabled = false;
        }

        if (spring != null)
        {
            spring.enabled = false;
            spring.autoConfigureDistance = false;
        }
    }

    void Update()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        direction = (mousePos - (Vector2)ShootPoint.position).normalized;

        FaceMouse();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (targetBody == null)
                Shoot();
            else
                ReleaseTarget();
        }

        if (targetBody != null && line != null)
        {
            line.SetPosition(0, ShootPoint.position);
            line.SetPosition(1, targetBody.position);
        }
    }

    void FixedUpdate()
    {
        if (targetBody == null)
            return;

        Vector2 playerPos = playerBody.position;
        Vector2 targetPos = targetBody.position;

        Vector2 radial = targetPos - playerPos;
        float dist = radial.magnitude;

        if (dist < 0.001f)
            radial = Vector2.right;
        else
            radial /= dist;

        // Once close enough, switch from spring pull-in to forced orbit
        if (!isOrbiting && dist <= orbitStartDistance)
        {
            isOrbiting = true;

            if (spring != null)
            {
                spring.enabled = false;
            }
        }

        if (isOrbiting)
        {
            ApplyOrbitMotion(radial, dist);
        }
    }

    void FaceMouse()
    {
        if (direction.sqrMagnitude > 0.0001f)
            transform.right = direction;
    }

    void Shoot()
    {
        GameObject bulletIns = Instantiate(Bullet, ShootPoint.position, Quaternion.identity);

        BulletScriptTwo bulletScript = bulletIns.GetComponent<BulletScriptTwo>();
        if (bulletScript != null)
        {
            bulletScript.Initialize(this);
        }

        Rigidbody2D rb = bulletIns.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = direction * BulletSpeed;
        }

        Collider2D bulletCol = bulletIns.GetComponent<Collider2D>();
        Collider2D[] playerCols = playerBody.GetComponentsInChildren<Collider2D>();

        if (bulletCol != null)
        {
            foreach (Collider2D playerCol in playerCols)
            {
                Physics2D.IgnoreCollision(bulletCol, playerCol);
            }
        }
    }

    public void TargetHitTwo(Rigidbody2D hitBody)
    {
        if (hitBody == null) return;

        targetBody = hitBody;

        TossableObject tossable = targetBody.GetComponent<TossableObject>();
        if (tossable != null)
        {
            tossable.OnLatched();
        }

        isOrbiting = false;

        originalDrag = targetBody.linearDamping;
        originalAngularDrag = targetBody.angularDamping;

        targetBody.linearDamping = latchedDrag;
        targetBody.angularDamping = latchedAngularDrag;

        if (line != null)
            line.enabled = true;

        if (spring != null)
        {
            spring.connectedBody = targetBody;
            spring.autoConfigureDistance = false;
            spring.distance = latchDistance;
            spring.dampingRatio = springDamping;
            spring.frequency = springFrequency;
            spring.enabled = true;
        }
    }

    void ApplyOrbitMotion(Vector2 radial, float dist)
    {
        Vector2 tangent = orbitClockwise
            ? new Vector2(radial.y, -radial.x)
            : new Vector2(-radial.y, radial.x);

        Vector2 tangentialVelocity = tangent * orbitSpeed;

        float radiusError = dist - orbitRadius;
        Vector2 radialCorrection = -radial * radiusError * orbitPullStrength;

        targetBody.linearVelocity = tangentialVelocity + radialCorrection;
    }

    public void ReleaseTarget()
    {
        isOrbiting = false;

        if (targetBody != null)
        {
            Vector2 currentVelocity = targetBody.linearVelocity;

            if (currentVelocity.sqrMagnitude > 0.001f)
            {
                currentVelocity += currentVelocity.normalized * releaseBoost;
            }

            targetBody.linearVelocity = Vector2.ClampMagnitude(currentVelocity, maxReleaseSpeed);

            targetBody.linearDamping = originalDrag;
            targetBody.angularDamping = originalAngularDrag;

            TossableObject tossable = targetBody.GetComponent<TossableObject>();
            if (tossable != null)
            {
                tossable.OnReleased();
            }
        }

        targetBody = null;

        if (line != null)
            line.enabled = false;

        if (spring != null)
        {
            spring.connectedBody = null;
            spring.enabled = false;
        }
    }
}