using NoOneCameBack.Tablet;
using UnityEngine;
using UnityEngine.UI;

namespace NoOneCameBack.Inventory
{
    public class InventoryCellView : MonoBehaviour
    {
        [SerializeField] private Image background;

        public void Place(Vector2 position, Vector2 size)
        {
            RectTransform rect = (RectTransform)transform;
            TabletUIUtility.ForceTopLeftAnchors(rect);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        public void SetColor(Color color)
        {
            if (background != null) background.color = color;
        }
    }
}
