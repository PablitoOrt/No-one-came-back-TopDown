using UnityEngine;

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

    // No prefab needed yet - the project has no weapon art/prefabs to instantiate.
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
