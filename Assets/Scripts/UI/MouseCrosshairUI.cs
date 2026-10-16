using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class MouseCrosshairUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Used to know whether a gamepad is currently driving aim, so the crosshair can hide itself.")]
    [SerializeField] private PlayerAimController aimController;
    [Tooltip("Left empty, resolved from the aim controller's parents.")]
    [SerializeField] private PlayerInputReader input;
    [Tooltip("Left empty, defaults to Camera.main. Only used if the Canvas isn't Screen Space - Overlay.")]
    [SerializeField] private Camera worldCamera;

    private RectTransform rectTransform;
    private Canvas canvas;

    private void Awake()
    {
        rectTransform = (RectTransform)transform;
        canvas = GetComponentInParent<Canvas>();
        if (worldCamera == null) worldCamera = Camera.main;
        if (input == null && aimController != null) input = aimController.GetComponentInParent<PlayerInputReader>();
    }

    private void Update()
    {
        bool usingGamepad = aimController != null && aimController.IsUsingGamepad;
        bool visible = input != null && !usingGamepad;
        rectTransform.localScale = visible ? Vector3.one : Vector3.zero;
        if (!visible) return;

        ScreenSpaceUI.PlaceAtScreenPoint(rectTransform, canvas, input.AimPosition, worldCamera);
    }
}
