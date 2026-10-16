using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(RectTransform))]
public class TargetIndicatorUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerAimController aimController;
    [FormerlySerializedAs("weapon")]
    [SerializeField] private PlayerCombat combat;
    [Tooltip("Left empty, defaults to Camera.main.")]
    [SerializeField] private Camera worldCamera;

    [Header("Size")]
    [Tooltip("Indicator size (pixels) once the aim is fully steady.")]
    [SerializeField] private float minSize = 10f;
    [Tooltip("Indicator size (pixels) the instant aiming starts.")]
    [SerializeField] private float maxSize = 48f;

    private RectTransform rectTransform;
    private Canvas canvas;

    private void Awake()
    {
        rectTransform = (RectTransform)transform;
        canvas = GetComponentInParent<Canvas>();
        if (worldCamera == null) worldCamera = Camera.main;
    }

    private void LateUpdate()
    {
        ITargetable target = aimController != null ? aimController.LockedTargetable : null;
        if (target == null || combat == null || worldCamera == null)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);
        UpdatePosition(target.AimAnchor.position);
        rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(maxSize, minSize, combat.AimPrecision01);
    }

    private void SetVisible(bool visible)
    {
        rectTransform.localScale = visible ? Vector3.one : Vector3.zero;
    }

    private void UpdatePosition(Vector3 worldPoint)
    {
        Vector3 screenPoint = worldCamera.WorldToScreenPoint(worldPoint);
        if (screenPoint.z <= 0f) return;

        ScreenSpaceUI.PlaceAtScreenPoint(rectTransform, canvas, screenPoint, worldCamera);
    }
}
