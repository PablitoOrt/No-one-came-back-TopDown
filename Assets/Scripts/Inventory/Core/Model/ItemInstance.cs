using System;
using UnityEngine;

namespace NoOneCameBack.Inventory
{
    public sealed class ItemInstance
    {
        public ItemInstance(ItemDefinition definition, int quantity = 1)
        {
            Definition = definition != null ? definition : throw new ArgumentNullException(nameof(definition));
            Quantity = Mathf.Max(1, quantity);
            Durability = definition.MaxDurability;
        }

        public event Action<ItemInstance> Changed;

        public ItemDefinition Definition { get; }
        public int Quantity { get; private set; }
        public int Durability { get; private set; }
        public int LoadedAmmo { get; private set; }

        public int MaxStack => Definition.MaxStack;
        public int StackSpace => Mathf.Max(0, MaxStack - Quantity);
        public bool IsBroken => Definition.HasDurability && Durability <= 0;

        public bool CanStackWith(ItemInstance other) =>
            other != null && other != this && other.Definition == Definition && Definition.IsStackable;

        public void SetQuantity(int value)
        {
            value = Mathf.Max(0, value);
            if (value == Quantity) return;

            Quantity = value;
            Changed?.Invoke(this);
        }

        public void AddQuantity(int amount) => SetQuantity(Quantity + amount);

        public int TakeQuantity(int requested)
        {
            int taken = Mathf.Clamp(requested, 0, Quantity);
            if (taken > 0) SetQuantity(Quantity - taken);
            return taken;
        }

        public ItemInstance Split(int amount)
        {
            int taken = TakeQuantity(amount);
            return taken > 0 ? new ItemInstance(Definition, taken) : null;
        }

        public void SetDurability(int value)
        {
            value = Mathf.Clamp(value, 0, Definition.MaxDurability);
            if (value == Durability) return;

            Durability = value;
            Changed?.Invoke(this);
        }

        public void ReduceDurability(int amount = 1) => SetDurability(Durability - Mathf.Max(0, amount));

        public void SetLoadedAmmo(int value)
        {
            value = Mathf.Clamp(value, 0, Definition.MagazineCapacity);
            if (value == LoadedAmmo) return;

            LoadedAmmo = value;
            Changed?.Invoke(this);
        }
    }
}
