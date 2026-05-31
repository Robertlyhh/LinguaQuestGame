using UnityEngine;

public class PrairieSwitch : MonoBehaviour
{
    public int currentDirection = 1;

    public Transform topCart;
    public Transform middleCart;
    public Transform bottomCart;
    public Transform switchPoint;

    void OnMouseDown()
    {
        if (!PrairieGameManager.Instance.gameStarted)
        {
            PrairieGameManager.Instance.gameStarted = true;
        }

        currentDirection++;
        if (currentDirection > 2)
            currentDirection = 0;

        RotateSwitchPoint();
    }

    void RotateSwitchPoint()
    {
        if (currentDirection == 0)
        {
            transform.rotation = Quaternion.Euler(0, 0, 25);
            transform.position = new Vector3(-1.85f, 3.47f, 0);
            switchPoint.rotation = Quaternion.Euler(0, 0, 60);
        }

        else if (currentDirection == 1)
        {
            transform.rotation = Quaternion.Euler(0, 0, 0);
            transform.position = new Vector3(-1.7f, 3.47f, 0);
            switchPoint.rotation = Quaternion.Euler(0, 0, 0);
        }

        else if (currentDirection == 2)
        {
            transform.rotation = Quaternion.Euler(0, 0, -25);
            transform.position = new Vector3(-1.5f, 3.47f, 0);
            switchPoint.rotation = Quaternion.Euler(0, 0, -40);
        }
    }

    public Transform GetCurrentTarget()
    {
        if (currentDirection == 0) return topCart;
        if (currentDirection == 1) return middleCart;
        return bottomCart;
    }
}