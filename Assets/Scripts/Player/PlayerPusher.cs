using System;
using UnityEngine;

public class PlayerPusher : MonoBehaviour, IPlayerMovementModifier, IPlayerActionBlocker
{
    [Header("Input")]
    [Tooltip("Left empty, auto-resolved from this object or its parents.")]
    [SerializeField] private PlayerInputReader input;

    [Header("Debug")]
    [SerializeField] private bool debugDraw = true;

    private Collider[] ownColliders;
    private Vector3 lastResolvedDelta;
    private float grabDistance;
    private int grabFrame = -1;
    private int releaseFrame = -1;

    public PushableObject Current { get; private set; }
    public bool IsPushing => Current != null;

    public event Action<PushableObject> PushStarted;
    public event Action<PushableObject> PushEnded;

    public bool IsActive => IsPushing;
    public bool AllowsSprint => !IsPushing || Current.Profile.AllowSprint;
    public float SpeedMultiplier => IsPushing ? Current.Profile.SpeedMultiplier : 1f;
    public bool BlocksCombat => IsPushing && Current.Profile.BlocksCombat;
    public bool BlocksInteraction => IsPushing || Time.frameCount == releaseFrame;

    private void Awake()
    {
        if (input == null) input = GetComponentInParent<PlayerInputReader>();
        ownColliders = GetComponentsInChildren<Collider>(true);
    }

    private void OnDisable() => Release();

    private void Update()
    {
        if (!IsPushing) return;

        if (Time.frameCount > grabFrame && input.InteractPerformed)
        {
            Release();
            return;
        }

        if (HorizontalDistanceTo(Current) - grabDistance > Current.Profile.ReleaseDistanceTolerance) Release();
    }

    public bool TryGrab(PushableObject pushable)
    {
        if (IsPushing || pushable == null || pushable.IsGrabbed) return false;

        Current = pushable;
        grabFrame = Time.frameCount;
        grabDistance = HorizontalDistanceTo(pushable);
        lastResolvedDelta = Vector3.zero;

        SetCollisionIgnored(pushable, true);
        pushable.NotifyGrabbed(this);
        PushStarted?.Invoke(pushable);
        return true;
    }

    public void Release()
    {
        if (!IsPushing) return;

        PushableObject released = Current;
        Current = null;
        releaseFrame = Time.frameCount;

        SetCollisionIgnored(released, false);
        released.NotifyReleased();
        PushEnded?.Invoke(released);
    }

    public bool Owns(Collider collider) => Array.IndexOf(ownColliders, collider) >= 0;

    public Vector3 ModifyMove(Vector3 horizontalDelta)
    {
        lastResolvedDelta = IsPushing ? Current.ResolveMove(horizontalDelta) : horizontalDelta;
        return lastResolvedDelta;
    }

    public void OnMoved(Vector3 actualDelta)
    {
        if (!IsPushing) return;

        actualDelta.y = 0f;
        float maxDistance = lastResolvedDelta.magnitude;
        if (maxDistance < 0.0001f) return;

        Vector3 direction = lastResolvedDelta / maxDistance;
        float along = Mathf.Clamp(Vector3.Dot(actualDelta, direction), 0f, maxDistance);
        Current.ApplyMove(direction * along);
    }

    public bool TryGetFacing(out Vector3 direction)
    {
        bool facing = IsPushing && Current.Profile.FacePusher;
        direction = facing ? Current.Context.PushAxis : Vector3.zero;
        return facing;
    }

    private float HorizontalDistanceTo(PushableObject pushable)
    {
        Vector3 offset = pushable.transform.position - transform.position;
        offset.y = 0f;
        return offset.magnitude;
    }

    private void SetCollisionIgnored(PushableObject pushable, bool ignored)
    {
        foreach (Collider own in ownColliders)
        {
            if (own == null) continue;
            foreach (Collider other in pushable.Colliders)
            {
                if (other != null) Physics.IgnoreCollision(own, other, ignored);
            }
        }
    }

    // UI debug
    private void OnGUI()
    {
        if (!debugDraw || !IsPushing) return;

        GUI.Label(new Rect(10, Screen.height - 40, 400, 30), $"Empujando {Current.name} - Interactuar para soltar");
    }
}
