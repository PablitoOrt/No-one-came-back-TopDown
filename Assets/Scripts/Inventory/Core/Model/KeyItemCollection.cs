using System;
using System.Collections.Generic;

namespace NoOneCameBack.Inventory
{
    public sealed class KeyItemCollection
    {
        private readonly List<ItemInstance> items = new List<ItemInstance>();

        public event Action Changed;

        public IReadOnlyList<ItemInstance> Items => items;
        public int Count => items.Count;

        public bool Add(ItemInstance item)
        {
            if (item == null || items.Contains(item)) return false;

            items.Add(item);
            Changed?.Invoke();
            return true;
        }

        public bool Remove(ItemInstance item)
        {
            if (!items.Remove(item)) return false;

            Changed?.Invoke();
            return true;
        }

        public bool Contains(ItemInstance item) => items.Contains(item);

        public bool Has(ItemDefinition definition) => Find(definition) != null;

        public bool HasId(string id) => items.Exists(item => item.Definition.Id == id);

        public ItemInstance Find(ItemDefinition definition) => items.Find(item => item.Definition == definition);

        public void Clear()
        {
            if (items.Count == 0) return;

            items.Clear();
            Changed?.Invoke();
        }
    }
}
