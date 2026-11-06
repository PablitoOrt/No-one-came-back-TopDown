using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NoOneCameBack.Inventory
{
    public class ItemInfoPanel : MonoBehaviour
    {
        [Header("Summary")]
        [SerializeField] private GameObject summaryRoot;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text descriptionLabel;
        [Tooltip("Quantity, magazine, durability and equipped state.")]
        [SerializeField] private TMP_Text detailsLabel;
        [SerializeField] private TMP_Text messageLabel;

        [Header("Examine")]
        [SerializeField] private GameObject examineRoot;
        [SerializeField] private Image examineIcon;
        [SerializeField] private TMP_Text examineName;
        [SerializeField] private TMP_Text examineDescription;

        private readonly StringBuilder details = new StringBuilder();

        public void Show(ItemInstance item, bool equipped)
        {
            if (summaryRoot != null) summaryRoot.SetActive(item != null);
            if (item == null) return;

            ItemDefinition definition = item.Definition;
            if (icon != null)
            {
                icon.sprite = definition.Icon;
                icon.enabled = definition.Icon != null;
            }
            if (nameLabel != null) nameLabel.text = definition.DisplayName;
            if (descriptionLabel != null) descriptionLabel.text = definition.Description;

            details.Clear();
            if (definition.IsStackable) details.AppendLine($"Cantidad: {item.Quantity}/{definition.MaxStack}");
            if (definition.HasMagazine) details.AppendLine($"Cargador: {item.LoadedAmmo}/{definition.MagazineCapacity}");
            if (definition.HasDurability) details.AppendLine($"Durabilidad: {item.Durability}/{definition.MaxDurability}");
            if (equipped) details.AppendLine("Equipado");
            if (detailsLabel != null) detailsLabel.text = details.ToString();
        }

        public void ShowMessage(string message)
        {
            if (messageLabel != null) messageLabel.text = message;
        }

        public void ClearMessage() => ShowMessage(string.Empty);

        public void ShowExamine(ItemInstance item)
        {
            if (examineRoot == null || item == null) return;

            examineRoot.SetActive(true);
            if (examineIcon != null)
            {
                examineIcon.sprite = item.Definition.Icon;
                examineIcon.enabled = item.Definition.Icon != null;
            }
            if (examineName != null) examineName.text = item.Definition.DisplayName;
            if (examineDescription != null) examineDescription.text = item.Definition.Description;
        }

        public void HideExamine()
        {
            if (examineRoot != null) examineRoot.SetActive(false);
        }
    }
}
