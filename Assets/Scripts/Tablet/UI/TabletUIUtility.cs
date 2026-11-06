using UnityEngine;

namespace NoOneCameBack.Tablet
{
    public static class TabletUIUtility
    {
        public static Camera GetEventCamera(Component uiElement)
        {
            Canvas canvas = uiElement.GetComponentInParent<Canvas>();
            if (canvas == null) return null;

            Canvas root = canvas.rootCanvas;
            if (root.renderMode == RenderMode.ScreenSpaceOverlay) return null;
            return root.worldCamera != null ? root.worldCamera : Camera.main;
        }

        public static void ForceTopLeftAnchors(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
        }
    }
}
