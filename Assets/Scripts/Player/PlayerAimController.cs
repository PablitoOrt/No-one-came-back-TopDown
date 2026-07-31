using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Resolves where the player is aiming (mouse raycast against a ground plane,
// or gamepad stick as a camera-relative direction), plus an aim-assist
// lock-on that pulls AimDirection toward a nearby visible enemy within a
// cone. RawAimDirection stays untouched by the assist - Weapon uses it to
// measure genuine steadiness, separate from the assisted shot direction.
public class PlayerAimController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string actionMapName = "Player";
    [SerializeField] private string aimPositionActionName = "AimPosition";
    [SerializeField] private string lookActionName = "Look";
    [Tooltip("Held to actually aim. Steady-aim accuracy and aim-assist lock-on only run while this is held.")]
    [SerializeField] private string aimActionName = "Aim";

    [Header("Aiming")]
    [Tooltip("Origin used for the aim plane/ray, usually the weapon muzzle or chest height.")]
    [SerializeField] private Transform aimOrigin;
    [SerializeField] private Camera aimCamera;
    [Tooltip("Minimum stick magnitude before the gamepad aim direction overrides the mouse.")]
    [SerializeField] private float gamepadLookDeadzone = 0.2f;
    [Tooltip("How far along the aim ray to place AimPoint when using the gamepad (no real hit point). Also caps how far the mouse ground-plane intersection is allowed to land, so aiming near the top of the screen (where the camera ray goes near-parallel to the ground) can't produce a wildly distant/unstable point.")]
    [SerializeField] private float maxAimPointDistance = 50f;

    [Header("Aim Assist")]
    [Tooltip("Layer(s) enemies live on.")]
    [SerializeField] private LayerMask enemyMask;
    [Tooltip("Layer(s) that block line of sight, used so aim assist never locks onto an enemy hidden behind geometry.")]
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float assistRange = 12f;
    [Tooltip("Cone half-angle (degrees) from the raw aim direction within which a new target can be acquired.")]
    [SerializeField] private float acquireConeAngle = 6f;
    [Tooltip("Cone half-angle (degrees) from the raw aim direction beyond which an active lock releases. Kept wider than acquireConeAngle on purpose so the lock doesn't flicker at the boundary.")]
    [SerializeField] private float breakConeAngle = 18f;
    [Tooltip("Max degrees the assisted direction may bend away from the raw mouse direction. Mouse aim is already pixel-precise, so this defaults to 0.")]
    [SerializeField] private float mouseAssistBendAngle = 0f;
    [Tooltip("Max degrees the assisted direction may bend away from the raw gamepad stick direction.")]
    [SerializeField] private float gamepadAssistBendAngle = 10f;
    [SerializeField] private float targetReacquireInterval = 0.1f;

    [Header("Debug")]
    [SerializeField] private bool debugDraw = true;

    private static readonly Collider[] OverlapBuffer = new Collider[16];

    private InputAction aimPositionAction;
    private InputAction lookAction;
    private InputAction aimAction;
    private ITargetable lockedTargetable;
    private float nextReacquireTime;
    private bool usingGamepadThisFrame;

    public Vector3 AimDirection { get; private set; } = Vector3.forward;
    public Vector3 RawAimDirection { get; private set; } = Vector3.forward;
    public Vector3 AimPoint { get; private set; }
    public Transform LockedTarget { get; private set; }
    public bool IsTargetLocked => LockedTarget != null;
    public bool IsAiming => aimAction.IsPressed();
    public bool IsUsingGamepad => usingGamepadThisFrame;

    public event Action<Transform> TargetLocked;
    public event Action TargetUnlocked;

    private void Awake()
    {
        if (aimCamera == null) aimCamera = Camera.main;

        InputActionMap map = inputActions.FindActionMap(actionMapName, throwIfNotFound: true);
        aimPositionAction = map.FindAction(aimPositionActionName, throwIfNotFound: true);
        lookAction = map.FindAction(lookActionName, throwIfNotFound: true);
        aimAction = map.FindAction(aimActionName, throwIfNotFound: true);
    }

    private void OnEnable()
    {
        aimPositionAction.Enable();
        lookAction.Enable();
        aimAction.Enable();
    }

    private void OnDisable()
    {
        aimPositionAction.Disable();
        lookAction.Disable();
        aimAction.Disable();
    }

    private void Update()
    {
        Vector2 look = lookAction.ReadValue<Vector2>();

        // Look is bound to both gamepad stick and mouse delta, so magnitude
        // alone can't tell them apart - check the actual source device.
        usingGamepadThisFrame = lookAction.activeControl?.device is Gamepad
            && look.sqrMagnitude >= gamepadLookDeadzone * gamepadLookDeadzone;

        Vector3 rawDirection;
        Vector3 rawPoint;

        if (usingGamepadThisFrame)
        {
            rawDirection = GetGamepadAimDirection(look);
            rawPoint = aimOrigin.position + rawDirection * maxAimPointDistance;
        }
        else
        {
            rawDirection = GetPointerAimDirection(out rawPoint);
        }

        RawAimDirection = rawDirection;
        float aimDistance = Vector3.Distance(aimOrigin.position, rawPoint);

        UpdateAimAssist(rawDirection);
        AimPoint = aimOrigin.position + AimDirection * aimDistance;

        if (debugDraw)
        {
            Debug.DrawLine(aimOrigin.position, AimPoint, Color.yellow);
        }
    }

    private void OnDrawGizmos()
    {
        if (!debugDraw || aimOrigin == null || !Application.isPlaying) return;

        Gizmos.color = IsTargetLocked ? Color.red : Color.yellow;
        Gizmos.DrawLine(aimOrigin.position, AimPoint);
        Gizmos.DrawWireSphere(AimPoint, 0.15f);
    }

    private void UpdateAimAssist(Vector3 rawDirection)
    {
        if (LockedTarget != null && !IsLockStillValid(rawDirection))
        {
            ReleaseLock();
        }

        if (LockedTarget == null && Time.time >= nextReacquireTime)
        {
            nextReacquireTime = Time.time + targetReacquireInterval;
            TryAcquireTarget(rawDirection);
        }

        if (LockedTarget != null)
        {
            Vector3 toTarget = lockedTargetable.AimAnchor.position - aimOrigin.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude > 0.0001f)
            {
                float bendAngle = usingGamepadThisFrame ? gamepadAssistBendAngle : mouseAssistBendAngle;
                AimDirection = Vector3.RotateTowards(rawDirection, toTarget.normalized, bendAngle * Mathf.Deg2Rad, 0f);
                return;
            }
        }

        AimDirection = rawDirection;
    }

    private bool IsLockStillValid(Vector3 rawDirection)
    {
        if (lockedTargetable == null || lockedTargetable.IsDead) return false;

        Vector3 anchor = lockedTargetable.AimAnchor.position;
        if (Vector3.Distance(aimOrigin.position, anchor) > assistRange) return false;

        Vector3 toTarget = anchor - aimOrigin.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.0001f) return false;

        return Vector3.Angle(rawDirection, toTarget.normalized) <= breakConeAngle;
    }

    private void TryAcquireTarget(Vector3 rawDirection)
    {
        int count = Physics.OverlapSphereNonAlloc(aimOrigin.position, assistRange, OverlapBuffer, enemyMask, QueryTriggerInteraction.Ignore);

        Transform bestTransform = null;
        ITargetable bestTargetable = null;
        float bestAngle = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            if (!OverlapBuffer[i].TryGetComponent(out ITargetable targetable) || targetable.IsDead) continue;

            Vector3 anchor = targetable.AimAnchor.position;
            Vector3 toCandidate = anchor - aimOrigin.position;
            toCandidate.y = 0f;
            if (toCandidate.sqrMagnitude < 0.0001f) continue;

            float angle = Vector3.Angle(rawDirection, toCandidate.normalized);
            if (angle > acquireConeAngle || angle >= bestAngle) continue;

            if (Physics.Linecast(aimOrigin.position, anchor, obstacleMask, QueryTriggerInteraction.Ignore)) continue;

            bestAngle = angle;
            bestTransform = OverlapBuffer[i].transform;
            bestTargetable = targetable;
        }

        if (bestTransform != null)
        {
            LockedTarget = bestTransform;
            lockedTargetable = bestTargetable;
            TargetLocked?.Invoke(bestTransform);
        }
    }

    private void ReleaseLock()
    {
        LockedTarget = null;
        lockedTargetable = null;
        TargetUnlocked?.Invoke();
    }

    private Vector3 GetGamepadAimDirection(Vector2 stickInput)
    {
        Vector3 camForward = Vector3.ProjectOnPlane(aimCamera.transform.forward, Vector3.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(aimCamera.transform.right, Vector3.up).normalized;
        Vector3 direction = camRight * stickInput.x + camForward * stickInput.y;

        return direction.sqrMagnitude > 0.0001f ? direction.normalized : RawAimDirection;
    }

    // Distance is clamped: near the top of the screen the ray can go nearly
    // parallel to the plane, making the raw intersection distance blow up
    // and swing wildly for tiny mouse moves. When the ray misses the plane
    // entirely, extrapolate along its own horizontal direction instead of
    // falling back to a stale direction, which would jump abruptly.
    private Vector3 GetPointerAimDirection(out Vector3 hitPoint)
    {
        Vector2 screenPosition = aimPositionAction.ReadValue<Vector2>();
        Ray ray = aimCamera.ScreenPointToRay(screenPosition);
        Plane aimPlane = new(Vector3.up, aimOrigin.position);

        Vector3 point;
        if (aimPlane.Raycast(ray, out float distance) && distance <= maxAimPointDistance)
        {
            point = ray.GetPoint(distance);
        }
        else
        {
            Vector3 horizontalRayDirection = ray.direction;
            horizontalRayDirection.y = 0f;
            if (horizontalRayDirection.sqrMagnitude < 0.0001f) horizontalRayDirection = RawAimDirection;

            point = ray.origin + horizontalRayDirection.normalized * maxAimPointDistance;
        }

        Vector3 direction = point - aimOrigin.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
        {
            hitPoint = point;
            return direction.normalized;
        }

        hitPoint = aimOrigin.position + RawAimDirection * maxAimPointDistance;
        return RawAimDirection;
    }
}
