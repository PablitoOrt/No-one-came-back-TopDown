using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WeaponPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private WeaponDefinition weaponDefinition;
    [Tooltip("Ammo given along with the weapon (uses the firearm's ammo type). Ignored for melee weapons.")]
    [SerializeField, Min(0)] private int bundledAmmo = 0;

    public string InteractPrompt => weaponDefinition != null
        ? $"Recoger {weaponDefinition.WeaponName}"
        : "Recoger arma";

    public bool CanInteract(GameObject interactor)
    {
        PlayerInventory inventory = interactor.GetComponentInParent<PlayerInventory>();
        return weaponDefinition != null && inventory != null && !inventory.HasWeapon(weaponDefinition);
    }

    public void Interact(GameObject interactor)
    {
        PlayerInventory inventory = interactor.GetComponentInParent<PlayerInventory>();
        if (inventory == null || !inventory.AddWeapon(weaponDefinition)) return;

        if (weaponDefinition is FirearmDefinition firearm) inventory.AddAmmo(firearm.AmmoType, bundledAmmo);

        Destroy(gameObject);
    }
}
