using UnityEngine;

// World object that grants a WeaponDefinition to whatever Weapon component
// it finds on the interactor. Used both for pickups placed by hand in the
// editor (weaponDefinition assigned in the Inspector) and for weapons the
// player drops when swapping (via SpawnDropped).
[RequireComponent(typeof(Collider))]
public class WeaponPickup : MonoBehaviour, IInteractable
{
    private const string InteractableLayerName = "Interactable";
    private const float DroppedPickupRadius = 0.5f;

    [SerializeField] private WeaponDefinition weaponDefinition;

    public string InteractPrompt => weaponDefinition != null
        ? $"Recoger {weaponDefinition.WeaponName}"
        : "Recoger arma";

    public bool CanInteract(GameObject interactor) => weaponDefinition != null;

    public void Interact(GameObject interactor)
    {
        Weapon weapon = interactor.GetComponentInParent<Weapon>();
        if (weapon == null) return;

        weapon.EquipDefinition(weaponDefinition);
        Destroy(gameObject);
    }

    // Creates a pickup for a weapon the player is dropping at runtime (no
    // prefab needed - the project doesn't have weapon prefabs/art yet).
    public static WeaponPickup SpawnDropped(WeaponDefinition definition, Vector3 position)
    {
        var pickupObject = new GameObject($"Dropped_{definition.WeaponName}");
        pickupObject.transform.position = position;

        int interactableLayer = LayerMask.NameToLayer(InteractableLayerName);
        if (interactableLayer >= 0) pickupObject.layer = interactableLayer;

        SphereCollider collider = pickupObject.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = DroppedPickupRadius;

        WeaponPickup pickup = pickupObject.AddComponent<WeaponPickup>();
        pickup.weaponDefinition = definition;
        return pickup;
    }
}
