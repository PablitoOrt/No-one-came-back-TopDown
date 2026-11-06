using System.Collections.Generic;
using NoOneCameBack.Tablet;
using UnityEngine;

namespace NoOneCameBack.Inventory
{
    public class KeyItemsView : MonoBehaviour
    {
        [SerializeField] private MenuListView list;
        [SerializeField] private GameObject emptyLabel;

        private readonly List<string> labels = new List<string>();
        private KeyItemCollection items;

        public ItemInstance Selected =>
            items != null && list.SelectedIndex >= 0 && list.SelectedIndex < items.Count ? items.Items[list.SelectedIndex] : null;

        private void OnDestroy() => Unbind();

        public void Bind(KeyItemCollection collection)
        {
            Unbind();
            items = collection;
            if (items != null) items.Changed += Refresh;
            Refresh();
        }

        public void Unbind()
        {
            if (items != null) items.Changed -= Refresh;
            items = null;
        }

        // Returns the index confirmed this frame, or -1.
        public int HandleInput(TabletInput input) => list.HandleInput(input);

        private void Refresh()
        {
            int previous = list.SelectedIndex;

            labels.Clear();
            if (items != null)
            {
                foreach (ItemInstance item in items.Items) labels.Add(item.Definition.DisplayName);
            }

            list.SetOptions(labels);
            if (previous >= 0) list.Select(previous);
            if (emptyLabel != null) emptyLabel.SetActive(labels.Count == 0);
        }
    }
}
