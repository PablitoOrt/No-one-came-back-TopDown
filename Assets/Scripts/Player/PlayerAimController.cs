using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Resolves where the player is aiming, in world space, from either the mouse
// (raycast against a ground plane at the aim origin's height) or a gamepad
// stick (treated as a direct camera-relative direction). Movement and
// weapons both read AimDirection/AimPoint instead of touching input directly.
//
// On top of that raw input direction, this also runs an aim-assist lock-on:
// while a nearby, visible enemy sits within a narrow cone of the raw aim
// direction, AimDirection gets pulled a limited amount toward it. The lock
// releases once the raw aim direction strays past a wider cone (hysteresis,
// so it doesn't flicker at the boundary) or the target dies/leaves range.
// RawAimDirection always stays the untouched input direction - Weapon uses
// it to measure genuine aim steadiness, separate from the assisted direction
// used to decide where a shot actually goes.
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
    private ITargetable lockedTargetable;
    private float nextReacquireTime;
    private bool usingGamepadThisFrame;

    public Vector3 AimDirection { get; private set; } = Vector3.forward;
    public Vector3 RawAimDirection { get; private set; } = Vector3.forward;
    public Vector3 AimPoint { get; private set; }
    public Transform LockedTarget { get; private set; }
    public bool IsTargetLocked => LockedTarget != null;

    public event Action<Transform> TargetLocked;
    public event Action TargetUnlocked;

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
        usingGamepadThisFrame = look.sqrMagnitude >= gamepadLookDeadzone * gamepadLookDeadzone;

        Vector3 rawDirection;
        Vector3 rawPoint;

        if (usingGamepadThisFrame)
        {
            rawDirection = GetGamepadAimDirection(look);
            rawPoint = aimOrigin.position + rawDirection * gamepadAimPointDistance;
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

    // Scans for the best lockable enemy: within acquireConeAngle of the raw
    // aim direction, in range, and with a clear line of sight (no obstacleMask
    // hit between aimOrigin and its AimAnchor).
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

        hitPoint = aimOrigin.position + RawAimDirection * gamepadAimPointDistance;
        return RawAimDirection;
    }
}
