using System;
using System.Collections.Generic;
using UnityEngine;

namespace NoOneCameBack.Inventory
{
    public sealed class PlayerInventoryModel : IEquipHandler
    {
        public PlayerInventoryModel(Vector2Int gridSize, Vector2Int lockerSize)
        {
            Grid = new InventoryGrid(gridSize.x, gridSize.y);
            Locker = new InventoryGrid(lockerSize.x, lockerSize.y);
            KeyItems = new KeyItemCollection();

            Grid.Changed += OnGridChanged;
        }

        public event Action<ItemInstance> EquippedChanged;
        public event Action<bool> StorageChanged;
        public event Action<ItemInstance> InventoryFull;

        public InventoryGrid Grid { get; }
        public InventoryGrid Locker { get; }
        public KeyItemCollection KeyItems { get; }
        public ItemInstance EquippedWeapon { get; private set; }

        // False until the tablet is found: only the equipped weapon and key items can be carried.
        public bool HasStorage { get; private set; }

        public PickupResult TryPickUp(ItemInstance item)
        {
            if (item == null) return PickupResult.Invalid;
            if (!item.Definition.UsesGrid) return KeyItems.Add(item) ? PickupResult.StoredAsKeyItem : PickupResult.Invalid;
            if (!HasStorage) return PickupResult.NoStorage;

            int before = item.Quantity;
            if (Grid.TryAutoAdd(item)) return PickupResult.Stored;

            InventoryFull?.Invoke(item);
            return item.Quantity < before ? PickupResult.PartiallyStored : PickupResult.NoSpace;
        }

        public void UnlockStorage()
        {
            if (HasStorage) return;

            HasStorage = true;
            if (EquippedWeapon != null && !Grid.Contains(EquippedWeapon) && !Grid.TryAutoAdd(EquippedWeapon))
                Debug.LogWarning($"[Inventory] Equipped weapon '{EquippedWeapon.Definition.Id}' doesn't fit in the grid.");

            StorageChanged?.Invoke(true);
        }

        public bool CanEquip(ItemInstance item) =>
            item != null && item.Definition.IsEquippable && (!HasStorage || Grid.Contains(item));

        public bool Equip(ItemInstance item)
        {
            if (!CanEquip(item)) return false;
            if (EquippedWeapon == item) return true;

            EquippedWeapon = item;
            EquippedChanged?.Invoke(item);
            return true;
        }

        public void Unequip()
        {
            // Without storage there's nowhere to put it away.
            if (EquippedWeapon == null || !HasStorage) return;

            EquippedWeapon = null;
            EquippedChanged?.Invoke(null);
        }

        public bool IsEquipped(ItemInstance item) => item != null && item == EquippedWeapon;

        public bool CanUse(ItemEffectContext context)
        {
            ItemInstance item = context?.Item;
            if (item == null) return false;

            foreach (ItemEffect effect in item.Definition.Effects)
            {
                if (effect != null && effect.CanApply(context)) return true;
            }
            return false;
        }

        public bool TryUse(ItemEffectContext context)
        {
            if (!CanUse(context)) return false;

            ItemInstance item = context.Item;
            foreach (ItemEffect effect in item.Definition.Effects)
            {
                if (effect != null && effect.CanApply(context)) effect.Apply(context);
            }

            if (item.Definition.ConsumeOnUse) item.TakeQuantity(1);
            return true;
        }

        public bool Remove(ItemInstance item) => Grid.Remove(item) || Locker.Remove(item) || KeyItems.Remove(item);

        public InventorySaveData ToSaveData()
        {
            InventorySaveData data = new InventorySaveData { tabletFound = HasStorage };

            WriteGrid(Grid, data.grid);
            WriteGrid(Locker, data.locker);
            foreach (ItemInstance item in KeyItems.Items) data.keyItems.Add(ToSaveData(item, -1, -1, false));

            if (EquippedWeapon != null)
            {
                data.hasEquippedWeapon = true;
                data.equippedInGrid = Grid.TryGetPlacement(EquippedWeapon, out GridPlacement placement);
                data.equippedWeapon = data.equippedInGrid
                    ? ToSaveData(EquippedWeapon, placement.Position.x, placement.Position.y, placement.Rotated)
                    : ToSaveData(EquippedWeapon, -1, -1, false);
            }

            return data;
        }

        public void LoadFromSaveData(InventorySaveData data, ItemDatabase database)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (database == null) throw new ArgumentNullException(nameof(database));

            // Cleared first so clearing the grid doesn't trigger an auto-unequip event.
            EquippedWeapon = null;
            HasStorage = data.tabletFound;

            Grid.Clear();
            Locker.Clear();
            KeyItems.Clear();

            ReadGrid(Grid, data.grid, database);
            ReadGrid(Locker, data.locker, database);
            foreach (ItemSaveData saved in data.keyItems)
            {
                ItemInstance item = CreateInstance(saved, database);
                if (item != null) KeyItems.Add(item);
            }

            if (data.hasEquippedWeapon && data.equippedWeapon != null)
            {
                EquippedWeapon = data.equippedInGrid
                    ? Grid.GetItemAt(new Vector2Int(data.equippedWeapon.x, data.equippedWeapon.y))
                    : CreateInstance(data.equippedWeapon, database);

                if (EquippedWeapon != null && EquippedWeapon.Definition.Id != data.equippedWeapon.itemId)
                {
                    Debug.LogWarning($"[Inventory] Saved equipped weapon '{data.equippedWeapon.itemId}' not found at its grid position.");
                    EquippedWeapon = null;
                }
            }

            StorageChanged?.Invoke(HasStorage);
            EquippedChanged?.Invoke(EquippedWeapon);
        }

        private void OnGridChanged()
        {
            // Dropping or moving the equipped weapon to the locker unequips it.
            if (HasStorage && EquippedWeapon != null && !Grid.Contains(EquippedWeapon)) Unequip();
        }

        private static void WriteGrid(InventoryGrid grid, List<ItemSaveData> target)
        {
            foreach (GridPlacement placement in grid.Placements)
                target.Add(ToSaveData(placement.Item, placement.Position.x, placement.Position.y, placement.Rotated));
        }

        private static void ReadGrid(InventoryGrid grid, List<ItemSaveData> source, ItemDatabase database)
        {
            foreach (ItemSaveData saved in source)
            {
                ItemInstance item = CreateInstance(saved, database);
                if (item == null) continue;

                if (grid.TryPlace(item, new Vector2Int(saved.x, saved.y), saved.rotated)) continue;
                if (grid.TryAutoAdd(item)) continue;

                Debug.LogWarning($"[Inventory] Saved item '{saved.itemId}' no longer fits and was discarded.");
            }
        }

        private static ItemSaveData ToSaveData(ItemInstance item, int x, int y, bool rotated) => new ItemSaveData
        {
            itemId = item.Definition.Id,
            x = x,
            y = y,
            rotated = rotated,
            quantity = item.Quantity,
            durability = item.Durability,
            loadedAmmo = item.LoadedAmmo,
        };

        private static ItemInstance CreateInstance(ItemSaveData saved, ItemDatabase database)
        {
            if (saved == null || !database.TryGet(saved.itemId, out ItemDefinition definition))
            {
                Debug.LogWarning($"[Inventory] Unknown item id '{saved?.itemId}' in save data.");
                return null;
            }

            ItemInstance item = new ItemInstance(definition, saved.quantity);
            item.SetDurability(saved.durability);
            item.SetLoadedAmmo(saved.loadedAmmo);
            return item;
        }
    }
}
