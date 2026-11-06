using System;
using System.Collections.Generic;
using UnityEngine;

namespace NoOneCameBack.Inventory
{
    // Cells are addressed as (column, row) with (0,0) at the top-left; rows grow downward.
    public sealed class InventoryGrid
    {
        private readonly ItemInstance[,] cells;
        private readonly Dictionary<ItemInstance, GridPlacement> placements = new Dictionary<ItemInstance, GridPlacement>();
        private readonly List<ItemInstance> items = new List<ItemInstance>();

        private int batchDepth;
        private bool dirty;

        public InventoryGrid(int width, int height)
        {
            if (width < 1) throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 1) throw new ArgumentOutOfRangeException(nameof(height));

            Width = width;
            Height = height;
            cells = new ItemInstance[width, height];
        }

        public event Action Changed;

        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<ItemInstance> Items => items;

        public IEnumerable<GridPlacement> Placements
        {
            get
            {
                foreach (ItemInstance item in items) yield return placements[item];
            }
        }

        public bool IsInside(Vector2Int cell) => cell.x >= 0 && cell.y >= 0 && cell.x < Width && cell.y < Height;

        public bool Contains(ItemInstance item) => item != null && placements.ContainsKey(item);

        public bool TryGetPlacement(ItemInstance item, out GridPlacement placement)
        {
            if (item != null) return placements.TryGetValue(item, out placement);

            placement = default;
            return false;
        }

        public ItemInstance GetItemAt(Vector2Int cell) => IsInside(cell) ? cells[cell.x, cell.y] : null;

        public bool CanPlace(ItemDefinition definition, Vector2Int position, bool rotated) =>
            CanPlaceInternal(definition, position, rotated, null);

        // The item itself is ignored when testing for overlap, so this also validates moves inside this grid.
        public bool CanPlace(ItemInstance item, Vector2Int position, bool rotated) =>
            item != null && CanPlaceInternal(item.Definition, position, rotated, item);

        public bool TryPlace(ItemInstance item, Vector2Int position, bool rotated)
        {
            if (item == null || Contains(item)) return false;
            if (item.Quantity <= 0 || item.Quantity > item.MaxStack) return false;
            if (!CanPlaceInternal(item.Definition, position, rotated, null)) return false;

            Insert(item, position, rotated);
            return true;
        }

        public bool TryAutoAdd(ItemInstance item) => TryAutoAdd(item, out _);

        // Stacks into existing stacks first, then places what's left in the first free spot (unrotated, then rotated).
        // Returns true only when everything was stored; on false, item.Quantity holds the remainder that didn't fit.
        public bool TryAutoAdd(ItemInstance item, out int addedAmount)
        {
            addedAmount = 0;
            if (item == null || Contains(item) || item.Quantity <= 0 || !item.Definition.UsesGrid) return false;

            BeginBatch();
            try
            {
                addedAmount += StackIntoExisting(item);

                while (item.Quantity > item.MaxStack)
                {
                    if (!TryFindFreeSpot(item.Definition, out Vector2Int splitPosition, out bool splitRotated)) return false;

                    ItemInstance split = item.Split(item.MaxStack);
                    Insert(split, splitPosition, splitRotated);
                    addedAmount += split.Quantity;
                }

                if (item.Quantity == 0) return true;
                if (!TryFindFreeSpot(item.Definition, out Vector2Int position, out bool rotated)) return false;

                addedAmount += item.Quantity;
                Insert(item, position, rotated);
                return true;
            }
            finally
            {
                EndBatch();
            }
        }

        public bool TryFindFreeSpot(ItemDefinition definition, out Vector2Int position, out bool rotated)
        {
            rotated = false;
            if (TryFindFreeSpot(definition, false, out position)) return true;

            rotated = true;
            return definition != null && definition.CanRotate && TryFindFreeSpot(definition, true, out position);
        }

        public bool Remove(ItemInstance item)
        {
            if (!Contains(item)) return false;

            ClearCells(placements[item]);
            placements.Remove(item);
            items.Remove(item);
            item.Changed -= OnItemChanged;

            Notify();
            return true;
        }

        public bool TryMove(ItemInstance item, Vector2Int position, bool rotated)
        {
            if (!Contains(item) || !CanPlaceInternal(item.Definition, position, rotated, item)) return false;

            ClearCells(placements[item]);
            GridPlacement placement = new GridPlacement(item, position, rotated);
            placements[item] = placement;
            FillCells(placement);

            Notify();
            return true;
        }

        public static bool TryTransfer(InventoryGrid from, ItemInstance item, InventoryGrid to, Vector2Int position, bool rotated)
        {
            if (from == null || to == null) return false;
            if (from == to) return from.TryMove(item, position, rotated);
            if (!from.Contains(item) || !to.CanPlace(item.Definition, position, rotated)) return false;

            from.Remove(item);
            to.Insert(item, position, rotated);
            return true;
        }

        // Moves as many units as fit from source into target (target must be in this grid).
        // Source is removed from its own grid automatically if it ends up empty.
        public int MergeInto(ItemInstance source, ItemInstance target)
        {
            if (!Contains(target) || source == null || !target.CanStackWith(source)) return 0;

            int amount = Mathf.Min(target.StackSpace, source.Quantity);
            if (amount <= 0) return 0;

            BeginBatch();
            try
            {
                source.TakeQuantity(amount);
                target.AddQuantity(amount);
            }
            finally
            {
                EndBatch();
            }
            return amount;
        }

        public int CountOf(ItemDefinition definition)
        {
            int total = 0;
            foreach (ItemInstance item in items)
            {
                if (item.Definition == definition) total += item.Quantity;
            }
            return total;
        }

        public int Consume(ItemDefinition definition, int amount)
        {
            if (definition == null || amount <= 0) return 0;

            int consumed = 0;
            BeginBatch();
            try
            {
                // Copy: emptied stacks remove themselves while iterating.
                foreach (ItemInstance item in items.ToArray())
                {
                    if (consumed >= amount) break;
                    if (item.Definition != definition) continue;

                    consumed += item.TakeQuantity(amount - consumed);
                }
            }
            finally
            {
                EndBatch();
            }
            return consumed;
        }

        public void Clear()
        {
            if (items.Count == 0) return;

            foreach (ItemInstance item in items) item.Changed -= OnItemChanged;
            items.Clear();
            placements.Clear();
            Array.Clear(cells, 0, cells.Length);

            Notify();
        }

        private bool CanPlaceInternal(ItemDefinition definition, Vector2Int position, bool rotated, ItemInstance ignore)
        {
            if (definition == null || !definition.UsesGrid) return false;
            if (rotated && !definition.CanRotate) return false;

            Vector2Int size = definition.GetSize(rotated);
            if (position.x < 0 || position.y < 0 || position.x + size.x > Width || position.y + size.y > Height) return false;

            for (int x = position.x; x < position.x + size.x; x++)
            {
                for (int y = position.y; y < position.y + size.y; y++)
                {
                    ItemInstance occupant = cells[x, y];
                    if (occupant != null && occupant != ignore) return false;
                }
            }
            return true;
        }

        private bool TryFindFreeSpot(ItemDefinition definition, bool rotated, out Vector2Int position)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    position = new Vector2Int(x, y);
                    if (CanPlaceInternal(definition, position, rotated, null)) return true;
                }
            }

            position = default;
            return false;
        }

        private int StackIntoExisting(ItemInstance incoming)
        {
            int stacked = 0;
            foreach (ItemInstance existing in items)
            {
                if (incoming.Quantity == 0) break;
                if (!existing.CanStackWith(incoming) || existing.StackSpace == 0) continue;

                int amount = incoming.TakeQuantity(existing.StackSpace);
                existing.AddQuantity(amount);
                stacked += amount;
            }
            return stacked;
        }

        private void Insert(ItemInstance item, Vector2Int position, bool rotated)
        {
            GridPlacement placement = new GridPlacement(item, position, rotated);
            placements.Add(item, placement);
            items.Add(item);
            FillCells(placement);
            item.Changed += OnItemChanged;
            Notify();
        }

        private void FillCells(GridPlacement placement) => SetCells(placement, placement.Item);

        private void ClearCells(GridPlacement placement) => SetCells(placement, null);

        private void SetCells(GridPlacement placement, ItemInstance value)
        {
            RectInt rect = placement.Rect;
            for (int x = rect.xMin; x < rect.xMax; x++)
            {
                for (int y = rect.yMin; y < rect.yMax; y++) cells[x, y] = value;
            }
        }

        private void OnItemChanged(ItemInstance item)
        {
            if (item.Quantity <= 0) Remove(item);
            else Notify();
        }

        private void BeginBatch() => batchDepth++;

        private void EndBatch()
        {
            batchDepth--;
            if (batchDepth > 0 || !dirty) return;

            dirty = false;
            Changed?.Invoke();
        }

        private void Notify()
        {
            if (batchDepth > 0)
            {
                dirty = true;
                return;
            }

            Changed?.Invoke();
        }
    }
}
