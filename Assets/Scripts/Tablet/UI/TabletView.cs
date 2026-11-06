using TMPro;
using UnityEngine;

namespace NoOneCameBack.Tablet
{
    public class TabletView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [Tooltip("Tablet frame (team art). Slides in from shownPosition + hiddenOffset.")]
        [SerializeField] private RectTransform frame;
        [SerializeField] private Vector2 hiddenOffset = new Vector2(0f, -250f);
        [Tooltip("Cracked screen overlay shown until the tablet is found.")]
        [SerializeField] private GameObject brokenOverlay;
        [SerializeField] private TabletTabBar tabBar;
        [SerializeField] private TMP_Text appTitle;

        private Vector2 shownPosition;
        private bool initialized;

        private void Awake() => Initialize();

        public void SetVisible(bool visible)
        {
            Initialize();
            canvasGroup.gameObject.SetActive(visible);
        }

        public void SetProgress(float t)
        {
            Initialize();
            float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);

            canvasGroup.alpha = eased;
            if (frame != null) frame.anchoredPosition = shownPosition + hiddenOffset * (1f - eased);
        }

        public void SetBroken(bool broken)
        {
            if (brokenOverlay != null) brokenOverlay.SetActive(broken);
        }

        public void Refresh(TabletController tablet)
        {
            if (appTitle != null) appTitle.text = tablet.CurrentApp != null ? tablet.CurrentApp.DisplayName : string.Empty;
            if (tabBar != null) tabBar.Refresh(tablet);
        }

        private void Initialize()
        {
            if (initialized) return;

            initialized = true;
            if (frame != null) shownPosition = frame.anchoredPosition;
        }
    }
}
