using UnityEngine;

public class BulletScriptTwo : MonoBehaviour
{
    private LassoScript gun;
    public float lifeTime = 4f;

    public void Initialize(LassoScript owner)
    {
        gun = owner;
        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player") || col.CompareTag("Gun"))
            return;

        if (col.CompareTag("box"))
        {
            Rigidbody2D hitBody = col.attachedRigidbody != null
                ? col.attachedRigidbody
                : col.GetComponent<Rigidbody2D>();

            if (gun != null)
            {
                gun.TargetHitTwo(hitBody);
            }

            Destroy(gameObject);
        }
    }
}