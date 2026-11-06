using NoOneCameBack.Tablet;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NoOneCameBack.Inventory
{
    public class InventoryItemView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [Tooltip("Child of this object. Its anchors are forced to the center so it can rotate with the item.")]
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text quantityLabel;
        [SerializeField] private GameObject equippedBadge;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField, Min(0f)] private float iconPadding = 6f;
        [SerializeField, Range(0f, 1f)] private float dimmedAlpha = 0.35f;

        public void Show(ItemInstance item, Vector2 position, Vector2 size, bool rotated, bool equipped, bool dimmed)
        {
            RectTransform rect = (RectTransform)transform;
            TabletUIUtility.ForceTopLeftAnchors(rect);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            ItemDefinition definition = item.Definition;
            if (icon != null)
            {
                RectTransform iconRect = icon.rectTransform;
                iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(0.5f, 0.5f);
                iconRect.anchoredPosition = Vector2.zero;

                Vector2 padded = new Vector2(Mathf.Max(0f, size.x - iconPadding * 2f), Mathf.Max(0f, size.y - iconPadding * 2f));
                iconRect.sizeDelta = rotated ? new Vector2(padded.y, padded.x) : padded;
                iconRect.localRotation = Quaternion.Euler(0f, 0f, rotated ? -90f : 0f);

                icon.sprite = definition.Icon;
                icon.enabled = definition.Icon != null;
            }

            if (quantityLabel != null)
            {
                quantityLabel.gameObject.SetActive(definition.IsStackable);
                if (definition.IsStackable) quantityLabel.SetText("{0}", item.Quantity);
            }

            if (equippedBadge != null) equippedBadge.SetActive(equipped);
            if (canvasGroup != null) canvasGroup.alpha = dimmed ? dimmedAlpha : 1f;
        }

        public void SetTint(Color color)
        {
            if (background != null) background.color = color;
        }
    }
}
