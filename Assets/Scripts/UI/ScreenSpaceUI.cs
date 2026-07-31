using UnityEngine;

public static class ScreenSpaceUI
{
    public static void PlaceAtScreenPoint(RectTransform rectTransform, Canvas canvas, Vector2 screenPoint, Camera fallbackCamera)
    {
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            rectTransform.position = screenPoint;
            return;
        }

        Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceCamera ? canvas.worldCamera : fallbackCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform.parent as RectTransform, screenPoint, eventCamera, out Vector2 localPoint))
        {
            rectTransform.anchoredPosition = localPoint;
        }
    }
}
