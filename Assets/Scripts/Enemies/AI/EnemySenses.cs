using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]
public class EnemySenses : MonoBehaviour
{
    private static readonly Collider[] OverlapBuffer = new Collider[8];

    [Header("Vision")]
    [SerializeField] private float viewRadius = 10f;
    [SerializeField, Range(0f, 360f)] private float viewAngle = 120f;
    [Tooltip("Targets this close are noticed even outside the view cone (still needs line of sight).")]
    [SerializeField] private float proximityRadius = 2.5f;
    [Tooltip("Origin of line-of-sight rays. Left empty, uses the Health aim anchor.")]
    [SerializeField] private Transform eyes;
    [Tooltip("Layer(s) potential targets live on. Left empty, defaults to Player.")]
    [SerializeField] private LayerMask targetMask;
    [Tooltip("Layer(s) that block sight. Left empty, defaults to Walls + Default.")]
    [SerializeField] private LayerMask obstacleMask;

    [Header("Memory")]
    [Tooltip("Seconds the target is still tracked (at its last known position) after losing sight.")]
    [SerializeField] private float memoryDuration = 3f;
    [SerializeField] private float scanInterval = 0.15f;

    [Header("Debug")]
    [SerializeField] private bool debugDraw = true;

    private Health ownHealth;
    private float nextScanTime;
    private float lastSeenTime = float.NegativeInfinity;

    public Health Target { get; private set; }
    public bool CanSeeTarget { get; private set; }
    public Vector3 LastKnownPosition { get; private set; }

    public bool HasTarget =>
        Target != null
        && !Target.IsDead
        && (CanSeeTarget || Time.time - lastSeenTime <= memoryDuration);

    public float DistanceToTarget =>
        Target != null ? EnemyMotor.FlatDistance(transform.position, Target.transform.position) : float.PositiveInfinity;

    private Vector3 EyePosition => eyes != null ? eyes.position : ownHealth.AimAnchor.position;

    private void Reset()
    {
        targetMask = LayerMask.GetMask("Player");
        obstacleMask = LayerMask.GetMask("Walls", "Default");
    }

    private void Awake()
    {
        ownHealth = GetComponent<Health>();
        if (targetMask == 0) targetMask = LayerMask.GetMask("Player");
        if (obstacleMask == 0) obstacleMask = LayerMask.GetMask("Walls", "Default");
    }

    private void OnEnable()
    {
        ownHealth.Damaged += HandleDamaged;
    }

    private void OnDisable()
    {
        ownHealth.Damaged -= HandleDamaged;
    }

    private void Update()
    {
        if (ownHealth.IsDead) return;

        if (Target != null && Target.IsDead)
        {
            Target = null;
            CanSeeTarget = false;
        }

        if (Time.time < nextScanTime) return;
        nextScanTime = Time.time + scanInterval;

        Scan();
    }

    public void Alert(Health target, Vector3 position)
    {
        if (target == null || target == ownHealth || target.IsDead) return;

        Target = target;
        Remember(position);
    }

    private void Scan()
    {
        Health best = null;
        float bestDistance = float.PositiveInfinity;

        int count = Physics.OverlapSphereNonAlloc(transform.position, viewRadius, OverlapBuffer, targetMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Health candidate = OverlapBuffer[i].GetComponentInParent<Health>();
            if (candidate == null || candidate == ownHealth || candidate.IsDead) continue;
            if (!IsVisible(candidate)) continue;

            float distance = EnemyMotor.FlatDistance(transform.position, candidate.transform.position);
            if (distance >= bestDistance) continue;

            best = candidate;
            bestDistance = distance;
        }

        CanSeeTarget = best != null;
        if (best == null) return;

        Target = best;
        Remember(best.transform.position);
    }

    private bool IsVisible(Health candidate)
    {
        Vector3 eye = EyePosition;
        Vector3 toTarget = candidate.AimAnchor.position - eye;
        Vector3 flat = new Vector3(toTarget.x, 0f, toTarget.z);

        float distance = flat.magnitude;
        if (distance > viewRadius) return false;
        if (distance > proximityRadius && Vector3.Angle(transform.forward, flat) > viewAngle * 0.5f) return false;

        return !Physics.Raycast(eye, toTarget.normalized, toTarget.magnitude, obstacleMask, QueryTriggerInteraction.Ignore);
    }

    private void Remember(Vector3 position)
    {
        LastKnownPosition = position;
        lastSeenTime = Time.time;
    }

    private void HandleDamaged(DamageInfo damage)
    {
        if (damage.Source == null) return;
        if ((targetMask.value & (1 << damage.Source.layer)) == 0) return;

        Alert(damage.Source.GetComponentInParent<Health>(), damage.Source.transform.position);
    }

    private void OnDrawGizmosSelected()
    {
        if (!debugDraw) return;

        Vector3 origin = transform.position;
        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(origin, viewRadius);

        Vector3 left = Quaternion.Euler(0f, -viewAngle * 0.5f, 0f) * transform.forward;
        Vector3 right = Quaternion.Euler(0f, viewAngle * 0.5f, 0f) * transform.forward;
        Gizmos.DrawLine(origin, origin + left * viewRadius);
        Gizmos.DrawLine(origin, origin + right * viewRadius);

        Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.8f);
        Gizmos.DrawWireSphere(origin, proximityRadius);

        if (Application.isPlaying && HasTarget)
        {
            Gizmos.color = CanSeeTarget ? Color.red : Color.gray;
            Gizmos.DrawLine(EyePosition, LastKnownPosition);
        }
    }
}
