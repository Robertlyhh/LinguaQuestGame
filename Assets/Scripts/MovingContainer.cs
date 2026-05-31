using UnityEngine;

public class MovingContainer : MonoBehaviour
{
    public Transform bottomLimit;
    public Transform topLimit;
    public float speed = 3f;
    public bool startMovingUp = true;

    private int direction;

    void Start()
    {
        direction = startMovingUp ? 1 : -1;
    }

    void Update()
    {
        if (bottomLimit == null || topLimit == null)
            return;

        Vector3 pos = transform.position;
        pos.y += direction * speed * Time.deltaTime;

        if (pos.y >= topLimit.position.y)
        {
            pos.y = topLimit.position.y;
            direction = -1;
        }
        else if (pos.y <= bottomLimit.position.y)
        {
            pos.y = bottomLimit.position.y;
            direction = 1;
        }

        transform.position = pos;
    }
}