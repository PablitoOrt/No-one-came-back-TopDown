using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NoOneCameBack.Tablet
{
    public class TabletTabView : MonoBehaviour
    {
        public enum TabState
        {
            Available,
            Selected,
            Locked,
        }

        [SerializeField] private TMP_Text label;
        [SerializeField] private Image icon;
        [SerializeField] private Image background;
        [SerializeField] private Color availableColor = new Color(1f, 1f, 1f, 0.15f);
        [SerializeField] private Color selectedColor = new Color(1f, 1f, 1f, 0.6f);
        [SerializeField] private Color lockedColor = new Color(1f, 1f, 1f, 0.03f);
        [SerializeField] private string lockedLabel = "???";

        public void Show(TabletApp app, TabState state)
        {
            bool locked = state == TabState.Locked;

            if (label != null) label.text = locked ? lockedLabel : app.DisplayName;
            if (icon != null)
            {
                icon.sprite = app.Icon;
                icon.enabled = app.Icon != null && !locked;
            }
            if (background != null)
            {
                background.color = state switch
                {
                    TabState.Selected => selectedColor,
                    TabState.Locked => lockedColor,
                    _ => availableColor,
                };
            }
        }
    }
}
