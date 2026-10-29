using System;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
public class PushableObject : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    [Tooltip("Left empty, a default profile is created at runtime.")]
    [SerializeField] private PushableProfile profile;
    [SerializeField] private string interactPrompt = "Empujar";

    [Header("Collision")]
    [Tooltip("Layers that stop the object.")]
    [SerializeField] private LayerMask obstacleMask = ~0;
    [Tooltip("Gap kept between the object and obstacles.")]
    [SerializeField, Min(0f)] private float skinWidth = 0.02f;
    [Tooltip("Surfaces with a normal Y above this count as floor and never block.")]
    [SerializeField, Range(0f, 1f)] private float floorNormalThreshold = 0.7f;

    [Header("Events")]
    [SerializeField] private UnityEvent onGrabbed;
    [SerializeField] private UnityEvent onReleased;
    [SerializeField] private UnityEvent onStartedMoving;
    [SerializeField] private UnityEvent onStoppedMoving;

    [Header("Debug")]
    [SerializeField] private bool debugDraw = true;

    private const float MinMove = 0.0001f;

    private Rigidbody body;
    private Collider[] colliders;
    private IPushConstraint[] constraints;
    private float distanceThisFrame;

    public PushableProfile Profile => profile;
    public PlayerPusher Pusher { get; private set; }
    public PushContext Context { get; private set; }
    public bool IsGrabbed => Pusher != null;
    public bool IsMoving { get; private set; }
    public Collider[] Colliders => colliders;

    public event Action<PushableObject> Grabbed;
    public event Action<PushableObject> Released;
    public event Action<PushableObject> StartedMoving;
    public event Action<PushableObject> StoppedMoving;

    public string InteractPrompt => interactPrompt;

    public Bounds WorldBounds
    {
        get
        {
            Bounds bounds = new Bounds(transform.position, Vector3.zero);
            bool initialized = false;
            foreach (Collider collider in colliders)
            {
                if (collider == null || collider.isTrigger || !collider.enabled) continue;
                if (!initialized)
                {
                    bounds = collider.bounds;
                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(collider.bounds);
                }
            }
            return bounds;
        }
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.interpolation = RigidbodyInterpolation.None;

        colliders = GetComponentsInChildren<Collider>(true);
        constraints = GetComponents<IPushConstraint>();

        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<PushableProfile>();
            profile.name = "DefaultPushable";
        }
    }

    private void OnDisable()
    {
        if (IsGrabbed) Pusher.Release();
    }

    private void LateUpdate()
    {
        float speed = Time.deltaTime > 0f ? distanceThisFrame / Time.deltaTime : 0f;
        distanceThisFrame = 0f;
        SetMoving(IsGrabbed && speed > profile.MovingSpeedThreshold);
    }

    public bool CanInteract(GameObject interactor)
    {
        if (!isActiveAndEnabled || IsGrabbed) return false;

        PlayerPusher pusher = interactor.GetComponentInParent<PlayerPusher>();
        return pusher != null && !pusher.IsPushing;
    }

    public void Interact(GameObject interactor)
    {
        PlayerPusher pusher = interactor.GetComponentInParent<PlayerPusher>();
        if (pusher != null) pusher.TryGrab(this);
    }

    public void NotifyGrabbed(PlayerPusher pusher)
    {
        Pusher = pusher;
        Context = new PushContext(this, pusher, ComputeGrabNormal(pusher.transform.position));

        Grabbed?.Invoke(this);
        onGrabbed?.Invoke();
    }

    public void NotifyReleased()
    {
        Pusher = null;
        SetMoving(false);

        Released?.Invoke(this);
        onReleased?.Invoke();
    }

    public Vector3 ResolveMove(Vector3 desiredDelta)
    {
        desiredDelta.y = 0f;

        foreach (IPushConstraint constraint in constraints)
        {
            if (constraint is Behaviour behaviour && !behaviour.enabled) continue;

            desiredDelta = constraint.Constrain(Context, desiredDelta);
            desiredDelta.y = 0f;
            if (desiredDelta.sqrMagnitude < MinMove * MinMove) return Vector3.zero;
        }

        return Sweep(desiredDelta);
    }

    public void ApplyMove(Vector3 delta)
    {
        delta.y = 0f;
        if (delta.sqrMagnitude < MinMove * MinMove) return;

        transform.position += delta;
        body.position = transform.position;
        distanceThisFrame += delta.magnitude;
    }

    public bool Owns(Collider collider) => Array.IndexOf(colliders, collider) >= 0;

    private Vector3 Sweep(Vector3 delta)
    {
        float distance = delta.magnitude;
        if (distance < MinMove) return Vector3.zero;

        Vector3 direction = delta / distance;
        RaycastHit[] hits = body.SweepTestAll(direction, distance + skinWidth, QueryTriggerInteraction.Ignore);

        float allowed = distance;
        foreach (RaycastHit hit in hits)
        {
            if (!IsObstacle(hit, direction)) continue;
            allowed = Mathf.Min(allowed, Mathf.Max(0f, hit.distance - skinWidth));
        }

        return direction * allowed;
    }

    private bool IsObstacle(RaycastHit hit, Vector3 direction)
    {
        if ((obstacleMask.value & (1 << hit.collider.gameObject.layer)) == 0) return false;
        if (hit.normal.y > floorNormalThreshold) return false;
        if (Vector3.Dot(hit.normal, direction) >= 0f) return false;
        if (Owns(hit.collider)) return false;
        if (Pusher != null && Pusher.Owns(hit.collider)) return false;
        return true;
    }

    private Vector3 ComputeGrabNormal(Vector3 pusherPosition)
    {
        Vector3 toPusher = pusherPosition - transform.position;
        toPusher.y = 0f;
        if (toPusher.sqrMagnitude < MinMove) toPusher = -transform.forward;

        if (!profile.SnapGrabToFaces) return toPusher.normalized;

        Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        float rightDot = Vector3.Dot(toPusher, right);
        float forwardDot = Vector3.Dot(toPusher, forward);

        return Mathf.Abs(rightDot) > Mathf.Abs(forwardDot)
            ? right * Mathf.Sign(rightDot)
            : forward * Mathf.Sign(forwardDot);
    }

    private void SetMoving(bool moving)
    {
        if (IsMoving == moving) return;
        IsMoving = moving;

        if (moving)
        {
            StartedMoving?.Invoke(this);
            onStartedMoving?.Invoke();
        }
        else
        {
            StoppedMoving?.Invoke(this);
            onStoppedMoving?.Invoke();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!debugDraw || !Application.isPlaying || !IsGrabbed) return;

        Vector3 center = WorldBounds.center;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(center, center + Context.PushAxis * 1.5f);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(center, center + Context.GrabNormal * 1.5f);
    }
}
