using UnityEngine;

/// <summary>
/// Connects a world vendor to the Voucher Adventure dialogue.
/// </summary>
public sealed class VoucherVendorInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private VoucherAdventureController controller;
    [SerializeField] private int vendorIndex;
    [SerializeField] private string vendorId;
    [SerializeField] private Sprite portrait;

    public void Configure(VoucherAdventureController owner, int index, string id)
    {
        controller = owner;
        vendorIndex = index;
        vendorId = id;
    }

    public bool CanInteract()
    {
        return controller != null && controller.CanOpenVendor(vendorIndex);
    }

    public void Interact()
    {
        if (CanInteract())
        {
            // Use the vendor's dialogue portrait, not the world sprite.
            controller.OpenVendor(vendorIndex, vendorId, portrait);
        }
    }
}
