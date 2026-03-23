using UnityEngine;

public class MenuController : MonoBehaviour
{
    public GameObject menuCanvas;
    public InventoryDisplay inventoryDisplay;

    void Start()
    {
        menuCanvas.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            bool isOpening = !menuCanvas.activeSelf;
            menuCanvas.SetActive(isOpening);

            if (isOpening && inventoryDisplay != null)
            {
                inventoryDisplay.LoadInventory();
            }
        }
    }
}
