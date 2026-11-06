using System.Collections.Generic;
using NoOneCameBack.Tablet;
using UnityEngine;

namespace NoOneCameBack.Inventory
{
    public class ContextMenuView : MonoBehaviour
    {
        [Tooltip("Pivot at the top-left so the menu opens to the right of the item.")]
        [SerializeField] private RectTransform root;
        [SerializeField] private MenuListView list;

        public MenuListView List => list;
        public bool IsOpen => root.gameObject.activeSelf;

        public void Open(IReadOnlyList<string> labels, Vector3 worldPosition)
        {
            root.gameObject.SetActive(true);
            root.position = worldPosition;
            root.SetAsLastSibling();
            list.SetOptions(labels);
        }

        public void Close() => root.gameObject.SetActive(false);
    }
}
