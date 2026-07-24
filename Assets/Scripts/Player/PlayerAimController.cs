using UnityEngine;
using UnityEngine.InputSystem;

// Resolves where the player is aiming, in world space, from either the mouse
// (raycast against a ground plane at the aim origin's height) or a gamepad
// stick (treated as a direct camera-relative direction). Movement and
// weapons both read AimDirection/AimPoint instead of touching input directly.
public class PlayerAimController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string actionMapName = "Player";
    [SerializeField] private string aimPositionActionName = "AimPosition";
    [SerializeField] private string lookActionName = "Look";

    [Header("Aiming")]
    [Tooltip("Origin used for the aim plane/ray, usually the weapon muzzle or chest height.")]
    [SerializeField] private Transform aimOrigin;
    [SerializeField] private Camera aimCamera;
    [Tooltip("Minimum stick magnitude before the gamepad aim direction overrides the mouse.")]
    [SerializeField] private float gamepadLookDeadzone = 0.2f;
    [Tooltip("How far along the aim ray to place AimPoint when using the gamepad (no real hit point).")]
    [SerializeField] private float gamepadAimPointDistance = 50f;

    [Header("Debug")]
    [SerializeField] private bool debugDraw = true;

    private InputAction aimPositionAction;
    private InputAction lookAction;

    public Vector3 AimDirection { get; private set; } = Vector3.forward;
    public Vector3 AimPoint { get; private set; }

    private void Awake()
    {
        if (aimCamera == null) aimCamera = Camera.main;

        InputActionMap map = inputActions.FindActionMap(actionMapName, throwIfNotFound: true);
        aimPositionAction = map.FindAction(aimPositionActionName, throwIfNotFound: true);
        lookAction = map.FindAction(lookActionName, throwIfNotFound: true);
    }

    private void OnEnable()
    {
        aimPositionAction.Enable();
        lookAction.Enable();
    }

    private void OnDisable()
    {
        aimPositionAction.Disable();
        lookAction.Disable();
    }

    private void Update()
    {
        Vector2 look = lookAction.ReadValue<Vector2>();

        if (look.sqrMagnitude >= gamepadLookDeadzone * gamepadLookDeadzone)
        {
            AimDirection = GetGamepadAimDirection(look);
            AimPoint = aimOrigin.position + AimDirection * gamepadAimPointDistance;
        }
        else
        {
            AimDirection = GetPointerAimDirection(out Vector3 hitPoint);
            AimPoint = hitPoint;
        }

        if (debugDraw)
        {
            Debug.DrawLine(aimOrigin.position, AimPoint, Color.yellow);
        }
    }

    private void OnDrawGizmos()
    {
        if (!debugDraw || aimOrigin == null || !Application.isPlaying) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(aimOrigin.position, AimPoint);
        Gizmos.DrawWireSphere(AimPoint, 0.15f);
    }

    private Vector3 GetGamepadAimDirection(Vector2 stickInput)
    {
        Vector3 camForward = Vector3.ProjectOnPlane(aimCamera.transform.forward, Vector3.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(aimCamera.transform.right, Vector3.up).normalized;
        Vector3 direction = camRight * stickInput.x + camForward * stickInput.y;

        return direction.sqrMagnitude > 0.0001f ? direction.normalized : AimDirection;
    }

    private Vector3 GetPointerAimDirection(out Vector3 hitPoint)
    {
        Vector2 screenPosition = aimPositionAction.ReadValue<Vector2>();
        Ray ray = aimCamera.ScreenPointToRay(screenPosition);
        Plane aimPlane = new Plane(Vector3.up, aimOrigin.position);

        if (aimPlane.Raycast(ray, out float distance))
        {
            Vector3 point = ray.GetPoint(distance);
            Vector3 direction = point - aimOrigin.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.0001f)
            {
                hitPoint = point;
                return direction.normalized;
            }
        }

        hitPoint = aimOrigin.position + AimDirection * gamepadAimPointDistance;
        return AimDirection;
    }
}
