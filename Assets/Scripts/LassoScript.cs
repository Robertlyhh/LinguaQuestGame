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

        if (line != null)
            line.enabled = true;

        if (spring != null)
        {
            spring.connectedBody = targetBody;
            spring.distance = Vector2.Distance(playerBody.position, targetBody.position) * 0.5f;
            spring.dampingRatio = 0.7f;
            spring.frequency = 4f;
            spring.enabled = true;
        }
    }

    public void ReleaseTarget()
    {
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