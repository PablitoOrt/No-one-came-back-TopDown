using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NoOneCameBack.Tablet
{
    public class MenuOptionView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image highlight;
        [SerializeField] private Color normalTextColor = new Color(0.75f, 0.75f, 0.75f);
        [SerializeField] private Color selectedTextColor = Color.white;

        public RectTransform Rect => (RectTransform)transform;

        public void SetLabel(string text) => label.text = text;

        public void SetSelected(bool selected)
        {
            if (highlight != null) highlight.enabled = selected;
            label.color = selected ? selectedTextColor : normalTextColor;
        }
    }
}
