using System;
using System.Collections.Generic;
using UnityEngine;

namespace NoOneCameBack.Inventory
{
    [CreateAssetMenu(menuName = "Inventory/Item Database", fileName = "ItemDatabase")]
    public class ItemDatabase : ScriptableObject
    {
        [SerializeField] private List<ItemDefinition> items = new List<ItemDefinition>();

        private Dictionary<string, ItemDefinition> lookup;

        public IReadOnlyList<ItemDefinition> Items => items;

        public bool TryGet(string id, out ItemDefinition definition)
        {
            definition = null;
            if (string.IsNullOrEmpty(id)) return false;

            EnsureLookup();
            return lookup.TryGetValue(id, out definition);
        }

        public ItemDefinition Get(string id) => TryGet(id, out ItemDefinition definition) ? definition : null;

        private void OnEnable() => lookup = null;

        private void OnValidate() => lookup = null;

        private void EnsureLookup()
        {
            if (lookup != null) return;

            lookup = new Dictionary<string, ItemDefinition>(StringComparer.Ordinal);
            foreach (ItemDefinition item in items)
            {
                if (item == null) continue;
                if (!lookup.TryAdd(item.Id, item))
                    Debug.LogWarning($"[ItemDatabase] Duplicate item id '{item.Id}' ({item.name}). Only the first one is used.", this);
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Collect All Item Definitions")]
        private void CollectAll()
        {
            items.Clear();
            foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:" + nameof(ItemDefinition)))
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                ItemDefinition definition = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
                if (definition != null) items.Add(definition);
            }

            lookup = null;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
