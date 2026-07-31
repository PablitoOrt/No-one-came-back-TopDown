using UnityEngine;

// Shown only while aim-assist has a target locked; shrinks as CurrentSpreadAngle improves.
[RequireComponent(typeof(RectTransform))]
public class TargetIndicatorUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerAimController aimController;
    [SerializeField] private Weapon weapon;
    [Tooltip("Left empty, defaults to Camera.main.")]
    [SerializeField] private Camera worldCamera;

    [Header("Size")]
    [Tooltip("Indicator size (pixels) at the best possible precision (smallest spread).")]
    [SerializeField] private float minSize = 10f;
    [Tooltip("Indicator size (pixels) at the worst precision (base hip-fire spread or wider).")]
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
        if (aimController == null || weapon == null || worldCamera == null
            || !aimController.IsTargetLocked
            || !aimController.LockedTarget.TryGetComponent(out ITargetable targetable))
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);
        UpdatePosition(targetable.AimAnchor.position);
        UpdateSize();
    }

    private void SetVisible(bool visible)
    {
        rectTransform.localScale = visible ? Vector3.one : Vector3.zero;
    }

    private void UpdatePosition(Vector3 worldPoint)
    {
        Vector3 screenPoint = worldCamera.WorldToScreenPoint(worldPoint);
        if (screenPoint.z <= 0f) return; // behind the camera - leave it where it was

        ScreenSpaceUI.PlaceAtScreenPoint(rectTransform, canvas, screenPoint, worldCamera);
    }

    private void UpdateSize()
    {
        WeaponAccuracyProfile profile = weapon.Definition != null ? weapon.Definition.AccuracyProfile : null;
        if (profile == null) return;

        float precision01 = Mathf.InverseLerp(profile.MinSpreadAngle, profile.BaseSpreadAngle, weapon.CurrentSpreadAngle);
        rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(minSize, maxSize, precision01);
    }
}
