using UnityEngine;

namespace NoOneCameBack.Inventory
{
    [RequireComponent(typeof(Collider))]
    public class ItemPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private ItemDefinition definition;
        [SerializeField, Min(1)] private int quantity = 1;
        [Tooltip("Rounds already in the magazine when the item is a weapon.")]
        [SerializeField, Min(0)] private int loadedAmmo;

        private ItemInstance instance;

        // Built lazily from the serialized fields, or injected by Initialize when the player drops an item.
        public ItemInstance Instance
        {
            get
            {
                if (instance == null && definition != null)
                {
                    instance = new ItemInstance(definition, quantity);
                    instance.SetLoadedAmmo(loadedAmmo);
                }
                return instance;
            }
        }

        public string InteractPrompt
        {
            get
            {
                ItemInstance item = Instance;
                if (item == null) return string.Empty;
                return item.Quantity > 1
                    ? $"Recoger {item.Definition.DisplayName} x{item.Quantity}"
                    : $"Recoger {item.Definition.DisplayName}";
            }
        }

        public void Initialize(ItemInstance item)
        {
            instance = item;
            definition = item.Definition;
            quantity = item.Quantity;
            loadedAmmo = item.LoadedAmmo;
        }

        public bool CanInteract(GameObject interactor) =>
            Instance != null && interactor.GetComponentInParent<PlayerInventoryHolder>() != null;

        public void Interact(GameObject interactor)
        {
            PlayerInventoryHolder holder = interactor.GetComponentInParent<PlayerInventoryHolder>();
            if (holder == null || Instance == null) return;

            switch (holder.TryPickUp(Instance))
            {
                case PickupResult.Stored:
                case PickupResult.StoredAsKeyItem:
                    Destroy(gameObject);
                    break;
                case PickupResult.PartiallyStored:
                    quantity = Instance.Quantity;
                    break;
            }
        }
    }
}
