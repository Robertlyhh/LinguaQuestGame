using UnityEngine;

public class FishingMiniGame : MonoBehaviour
{
    [SerializeField] Transform topPivot;
    [SerializeField] Transform bottomPivot;
    [SerializeField] Transform fish;

    float fishPosotion;
    float fishDestionation;

    float fishTimer;
    [SerializeField] float timerMutiplicator = 3f;

    float fishSpeed;
    [SerializeField] float smoothMotion = 1f;

    [SerializeField] Transform hook;
    float hookPosition;
    [SerializeField] float hookSize = 0.1f;
    [SerializeField] float hookPower = 0.5f;
    float hookProgress;
    float hookPullvelocity;
    [SerializeField] float hookPullPower = 8f;
    [SerializeField] float hookGravityPower = 5f;
    [SerializeField] float hookProgressDegradationPower = 0.1f;

    [SerializeField] SpriteRenderer hookspriteRenderer;
    [SerializeField] Transform progressbarCan;
    [SerializeField] Signal winSignal;
    [SerializeField] Signal loseSignal;

    bool pause = false;

    [SerializeField] float failtimer = 10f;

    public GameObject Chest;
    public GameObject MiniG;

    private FishingSpot currentSpot;

    private void Start()
    {
        Resize();
    }

    public void SetCurrentSpot(FishingSpot spot)
    {
        currentSpot = spot;

        // reset minigame values each time it starts
        pause = false;
        failtimer = 10f;
        hookProgress = 0f;
        hookPosition = 0.5f;
        hookPullvelocity = 0f;
        fishPosotion = 0.5f;
        fishDestionation = Random.value;
    }

    private void Update()
    {
        if (pause) return;

        Fish();
        Hook();
        ProgressCheck();
    }

    void ProgressCheck()
    {
        Vector3 ls = progressbarCan.localScale;
        ls.y = hookProgress;
        progressbarCan.localScale = ls;

        float mins = hookPosition - hookSize / 2;
        float max = hookPosition + hookSize / 2;

        if (mins < fishPosotion && fishPosotion < max)
        {
            hookProgress += hookPower * Time.deltaTime;
        }
        else
        {
            hookProgress -= hookProgressDegradationPower * Time.deltaTime;

            failtimer -= Time.deltaTime;
            if (failtimer < 0f)
            {
                Lose();
            }
        }

        if (hookProgress >= 0.5f)
        {
            Win();
        }

        hookProgress = Mathf.Clamp(hookProgress, 0f, 1f);
    }

    void Win()
    {
        pause = true;
        Debug.Log("You Win");
        winSignal.Raise();

        if (currentSpot != null)
        {
            if (currentSpot.isCorrectSpot)
            {
                if (currentSpot.rockToRemove != null && currentSpot.rockToRemove.activeInHierarchy)
                {
                    currentSpot.rockToRemove.SetActive(false);
                }

                if (Chest != null && !Chest.activeInHierarchy)
                {
                    Chest.SetActive(true);
                }
            }
            else
            {
                if (currentSpot.objectToSpawn != null)
                {
                    currentSpot.objectToSpawn.SetActive(true);
                }
            }
        }

        if (MiniG.activeInHierarchy)
        {
            MiniG.SetActive(false);
            Invoke(nameof(Rest), 1f);
        }
    }

    void Lose()
    {
        pause = true;
        Debug.Log("You Lose");
        loseSignal.Raise();
        failtimer = 10f;
        hookProgress = 0f;
        Invoke(nameof(Rest), 1f);
    }

    void Rest()
    {
        if (MiniG.activeInHierarchy)
        {
            MiniG.SetActive(false);
        }

        pause = false;
    }

    void Hook()
    {
        if (Input.GetMouseButton(0))
        {
            hookPullvelocity += hookPullPower * Time.deltaTime;
        }

        hookPullvelocity -= hookGravityPower * Time.deltaTime;

        if (hookPosition - hookSize / 2 <= 0f && hookPullvelocity < 0f)
        {
            hookPullvelocity = 0f;
        }

        if (hookPosition + hookSize / 2 >= 1f && hookPullvelocity > 0f)
        {
            hookPullvelocity = 0f;
        }

        hookPosition += hookPullvelocity * Time.deltaTime;
        hookPosition = Mathf.Clamp(hookPosition, hookSize / 2f, 1f - hookSize / 2f);
        hook.position = Vector3.Lerp(bottomPivot.position, topPivot.position, hookPosition);
    }

    void Fish()
    {
        fishTimer -= Time.deltaTime;

        if (fishTimer < 0f)
        {
            fishTimer = Random.value * timerMutiplicator;
            fishDestionation = Random.value;
        }

        fishPosotion = Mathf.SmoothDamp(fishPosotion, fishDestionation, ref fishSpeed, smoothMotion);
        fish.position = Vector3.Lerp(bottomPivot.position, topPivot.position, fishPosotion);
    }

    void Resize()
    {
        Bounds b = hookspriteRenderer.bounds;
        float ySize = b.size.y;
        Vector3 ls = hook.localScale;
        float distance = Vector3.Distance(topPivot.position, bottomPivot.position);
        ls.y = (distance / ySize * hookSize);
        hook.localScale = ls;
    }
}