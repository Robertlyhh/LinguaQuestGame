using UnityEngine;
using TMPro;
using System.Collections;

public class PrairieSack : MonoBehaviour
{
    public enum Category { Clothing, Food, Household }
    public Category category;

    public float speed = 3f;
    float AdjustedSpeed => speed * PrairieGameManager.Instance.difficultyMultiplier;

    public TextMeshProUGUI textLabel;

    private PrairieSwitch railSwitch;
    private bool redirected = false;
    private Transform targetCart;

    private SpriteRenderer[] sackRenderers;

    void Start()
    {
        sackRenderers = GetComponentsInChildren<SpriteRenderer>();
        railSwitch = FindObjectOfType<PrairieSwitch>();
        AssignRandomItem();
    }

    void Update()
    {
        if (!redirected)
        {
            transform.Translate(Vector2.right * AdjustedSpeed * Time.deltaTime);
        }
        else if (targetCart != null)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetCart.position,
                AdjustedSpeed * Time.deltaTime
            );
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("SwitchPoint"))
        {
            Redirect();
        }

        PrairieCart cart = other.GetComponent<PrairieCart>();
        if (cart != null)
        {
            ResolveCart(cart);
        }
        


        
    }

    void Redirect()
    {
        redirected = true;
        targetCart = railSwitch.GetCurrentTarget();
    }

    void ResolveCart(PrairieCart cart)
    {
        bool correct = (cart.cartCategory == category);
        if (correct)
        {
            PrairieGameManager.Instance.RegisterCorrect();
            Debug.Log("Correct!");
        }
        else if (cart.cartCategory != category)
        {
            Debug.Log("Wrong!");
        }

            StartCoroutine(FlashColor(correct ? Color.green : Color.red));
    }

    IEnumerator FlashColor(Color color)
    {
        foreach (SpriteRenderer sr in sackRenderers)
        {
            sr.color = color;
        }

        yield return new WaitForSeconds(0.3f);

        Destroy(gameObject);
    }

    void AssignRandomItem()
    {
        string[] clothing = { "Bunny Hug", "Toque", "Parka" };
        string[] food = { "Double-Double", "Poutine", "Saskatoon Pie" };
        string[] household = { "Scrubby", "Chesterfield", "Snow Brush" };

        int categoryIndex = Random.Range(0, 3);
        int wordIndex = Random.Range(0, 3);

        if (categoryIndex == 0)
        {
            category = Category.Clothing;
            textLabel.text = clothing[wordIndex];
        }
        else if (categoryIndex == 1)
        {
            category = Category.Food;
            textLabel.text = food[wordIndex];
        }
        else
        {
            category = Category.Household;
            textLabel.text = household[wordIndex];
        }
    }
}