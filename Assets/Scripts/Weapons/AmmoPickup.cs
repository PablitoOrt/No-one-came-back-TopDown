using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AmmoPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private AmmoType ammoType;
    [SerializeField, Min(1)] private int amount = 10;

    public string InteractPrompt => ammoType != null
        ? $"Recoger {ammoType.DisplayName} x{amount}"
        : "Recoger munición";

    public bool CanInteract(GameObject interactor) =>
        ammoType != null && interactor.GetComponentInParent<PlayerInventory>() != null;

    public void Interact(GameObject interactor)
    {
        PlayerInventory inventory = interactor.GetComponentInParent<PlayerInventory>();
        if (inventory == null) return;

        inventory.AddAmmo(ammoType, amount);
        Destroy(gameObject);
    }
}
