using UnityEngine;
using UnityEngine.InputSystem;

// Follows the mouse cursor 1:1; hidden while a gamepad is driving aim.
[RequireComponent(typeof(RectTransform))]
public class MouseCrosshairUI : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string actionMapName = "Player";
    [SerializeField] private string aimPositionActionName = "AimPosition";

    [Header("References")]
    [Tooltip("Used only to know whether a gamepad is currently driving aim, so the crosshair can hide itself.")]
    [SerializeField] private PlayerAimController aimController;
    [Tooltip("Left empty, defaults to Camera.main. Only used if the Canvas isn't Screen Space - Overlay.")]
    [SerializeField] private Camera worldCamera;

    private RectTransform rectTransform;
    private Canvas canvas;
    private InputAction aimPositionAction;

    private void Awake()
    {
        rectTransform = (RectTransform)transform;
        canvas = GetComponentInParent<Canvas>();
        if (worldCamera == null) worldCamera = Camera.main;

        InputActionMap map = inputActions.FindActionMap(actionMapName, throwIfNotFound: true);
        aimPositionAction = map.FindAction(aimPositionActionName, throwIfNotFound: true);
    }

    private void OnEnable() => aimPositionAction.Enable();
    private void OnDisable() => aimPositionAction.Disable();

    private void Update()
    {
        bool usingGamepad = aimController != null && aimController.IsUsingGamepad;
        rectTransform.localScale = usingGamepad ? Vector3.zero : Vector3.one;
        if (usingGamepad) return;

        ScreenSpaceUI.PlaceAtScreenPoint(rectTransform, canvas, aimPositionAction.ReadValue<Vector2>(), worldCamera);
    }
}
