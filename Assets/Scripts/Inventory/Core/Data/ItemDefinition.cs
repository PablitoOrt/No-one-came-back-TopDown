using System.Collections.Generic;
using UnityEngine;

namespace NoOneCameBack.Inventory
{
    [CreateAssetMenu(menuName = "Inventory/Item Definition", fileName = "NewItem")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable key used by saves and ItemDatabase lookups. Don't rename it once saves exist. Empty = asset name.")]
        [SerializeField] private string id;
        [SerializeField] private string displayName = "Item";
        [SerializeField, TextArea(2, 6)] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private ItemCategory category = ItemCategory.Misc;

        [Header("Grid")]
        [Tooltip("Width x height in cells. Ignored by Key and Anchor items, which never use the grid.")]
        [SerializeField] private Vector2Int size = Vector2Int.one;
        [SerializeField] private bool canRotate = true;
        [SerializeField, Min(1)] private int maxStack = 1;

        [Header("Use")]
        [SerializeField] private List<ItemEffect> effects = new List<ItemEffect>();
        [Tooltip("Removes one unit after a successful Use.")]
        [SerializeField] private bool consumeOnUse = true;

        [Header("Weapon")]
        [Tooltip("Combat asset equipped with this item (FirearmDefinition or MeleeDefinition). Its ammo item and magazine size are used by the inventory too.")]
        [SerializeField] private ScriptableObject weapon;

        [Header("Durability")]
        [Tooltip("0 = indestructible. Melee weapons and silencers wear out.")]
        [SerializeField, Min(0)] private int maxDurability;

        [Header("World")]
        [Tooltip("Spawned when the item is dropped. Empty = the holder's generic pickup.")]
        [SerializeField] private GameObject worldPickupPrefab;

        public string Id => string.IsNullOrEmpty(id) ? name : id;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public ItemCategory Category => category;

        public Vector2Int Size => new Vector2Int(Mathf.Max(1, size.x), Mathf.Max(1, size.y));
        public bool CanRotate => canRotate;
        public int MaxStack => Mathf.Max(1, maxStack);
        public bool IsStackable => MaxStack > 1;

        public IReadOnlyList<ItemEffect> Effects => effects;
        public bool HasEffects => effects.Exists(effect => effect != null);
        public bool ConsumeOnUse => consumeOnUse;

        public ScriptableObject Weapon => weapon;
        public ItemDefinition AmmoItem => (weapon as IWeaponData)?.AmmoItem;
        public int MagazineCapacity => weapon is IWeaponData data ? data.MagazineCapacity : 0;
        public bool HasMagazine => MagazineCapacity > 0;

        public int MaxDurability => maxDurability;
        public bool HasDurability => maxDurability > 0;

        public GameObject WorldPickupPrefab => worldPickupPrefab;

        public bool UsesGrid => category != ItemCategory.Key && category != ItemCategory.Anchor;
        public bool IsEquippable => category == ItemCategory.Weapon || category == ItemCategory.Throwable;

        public Vector2Int GetSize(bool rotated)
        {
            Vector2Int s = Size;
            return rotated ? new Vector2Int(s.y, s.x) : s;
        }

        private void OnValidate()
        {
            if (weapon == null || weapon is IWeaponData) return;

            Debug.LogWarning($"[ItemDefinition] '{weapon.name}' isn't a weapon asset (FirearmDefinition / MeleeDefinition).", this);
            weapon = null;
        }
    }
}
