using System;
using NoOneCameBack.Tablet;
using UnityEngine;

namespace NoOneCameBack.Inventory
{
    // Owns the player's inventory model. It lives on the player, not the scene, so it survives real/psychotic world swaps.
    // The shared locker grid is owned here too: one instance for the whole game without a static singleton.
    [DisallowMultipleComponent]
    public class PlayerInventoryHolder : MonoBehaviour, IAmmoSource
    {
        [SerializeField] private ItemDatabase database;
        [Tooltip("Optional. Unlocks storage when the tablet is found and adds unlocked apps to save data.")]
        [SerializeField] private TabletController tablet;

        [Header("Sizes")]
        [SerializeField] private Vector2Int gridSize = new Vector2Int(5, 5);
        // TODO(diseño): shared locker size.
        [SerializeField] private Vector2Int lockerSize = new Vector2Int(8, 6);

        [Header("Start")]
        // TODO(diseño): starting weapon and how many rounds it comes with.
        [SerializeField] private ItemDefinition startingWeapon;
        [SerializeField, Min(0)] private int startingLoadedAmmo = 6;

        [Header("Drop")]
        [Tooltip("Used for items without their own world pickup prefab.")]
        [SerializeField] private ItemPickup genericPickupPrefab;
        [Tooltip("Facing used to place dropped items. Empty = this transform.")]
        [SerializeField] private Transform dropOrigin;
        [SerializeField, Min(0f)] private float dropDistance = 1f;
        [SerializeField] private float dropHeight = 0.2f;

        private IHealth health;
        private IPsychosis psychosis;
        private IBleeding bleeding;

        public event Action<ItemInstance, PickupResult> PickupFailed;
        public event Action<ItemInstance> ItemDropped;

        public PlayerInventoryModel Model { get; private set; }
        public ItemDatabase Database => database;
        public IHealth Health => health;
        public IPsychosis Psychosis => psychosis;
        public IBleeding Bleeding => bleeding;

        private void Awake()
        {
            Model = new PlayerInventoryModel(gridSize, lockerSize);

            health = GetComponentInChildren<IHealth>(true);
            psychosis = GetComponentInChildren<IPsychosis>(true);
            bleeding = GetComponentInChildren<IBleeding>(true);

            if (startingWeapon != null)
            {
                ItemInstance weapon = new ItemInstance(startingWeapon);
                weapon.SetLoadedAmmo(startingLoadedAmmo);
                Model.Equip(weapon);
            }
        }

        private void Start()
        {
            // TabletController.Awake may run after ours, so its debug "start found" flag is only reliable here.
            if (tablet != null && tablet.IsFound) Model.UnlockStorage();
        }

        private void OnEnable()
        {
            if (tablet != null) tablet.Found += Model.UnlockStorage;
        }

        private void OnDisable()
        {
            if (tablet != null) tablet.Found -= Model.UnlockStorage;
        }

        public PickupResult TryPickUp(ItemInstance item)
        {
            PickupResult result = Model.TryPickUp(item);
            if (result == PickupResult.NoSpace || result == PickupResult.PartiallyStored || result == PickupResult.NoStorage)
                PickupFailed?.Invoke(item, result);
            return result;
        }

        // Reserve ammo for reloading comes only from the backpack grid, never from the locker.
        public int GetAmmo(ItemDefinition ammo) => ammo != null ? Model.Grid.CountOf(ammo) : 0;

        public int TakeAmmo(ItemDefinition ammo, int requested) => Model.Grid.Consume(ammo, requested);

        public ItemEffectContext CreateContext(ItemInstance item) =>
            new ItemEffectContext(item, gameObject, health, psychosis, bleeding, Model);

        public bool CanUse(ItemInstance item) => Model.CanUse(CreateContext(item));

        public bool Use(ItemInstance item) => Model.TryUse(CreateContext(item));

        public bool Drop(ItemInstance item)
        {
            if (item == null || !item.Definition.UsesGrid || !Model.Grid.Remove(item)) return false;

            SpawnPickup(item);
            ItemDropped?.Invoke(item);
            return true;
        }

        public InventorySaveData ToSaveData()
        {
            InventorySaveData data = Model.ToSaveData();
            if (tablet != null) data.unlockedApps = tablet.GetUnlockedAppIds();
            return data;
        }

        public void LoadFromSaveData(InventorySaveData data, ItemDatabase itemDatabase)
        {
            Model.LoadFromSaveData(data, itemDatabase != null ? itemDatabase : database);
            if (tablet != null) tablet.LoadUnlocks(data.tabletFound, data.unlockedApps);
        }

        private void SpawnPickup(ItemInstance item)
        {
            GameObject prefab = item.Definition.WorldPickupPrefab != null
                ? item.Definition.WorldPickupPrefab
                : genericPickupPrefab != null ? genericPickupPrefab.gameObject : null;

            if (prefab == null)
            {
                Debug.LogWarning($"[Inventory] No pickup prefab to drop '{item.Definition.Id}'. The item is lost.", this);
                return;
            }

            Transform origin = dropOrigin != null ? dropOrigin : transform;
            Vector3 forward = Vector3.ProjectOnPlane(origin.forward, Vector3.up).normalized;
            Vector3 position = origin.position + forward * dropDistance + Vector3.up * dropHeight;

            GameObject spawned = Instantiate(prefab, position, Quaternion.LookRotation(forward.sqrMagnitude > 0f ? forward : Vector3.forward));
            if (spawned.TryGetComponent(out ItemPickup pickup)) pickup.Initialize(item);
            else Debug.LogWarning($"[Inventory] Pickup prefab '{prefab.name}' has no ItemPickup component.", spawned);
        }
    }
}
