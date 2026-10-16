using System;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyMeleeAttack : MonoBehaviour, IEnemyAttack
{
    private enum Phase
    {
        Ready,
        Windup,
        Recovery,
    }

    [Header("Damage")]
    [SerializeField] private float damage = 10f;

    [Header("Reach")]
    [Tooltip("Distance (feet to feet) at which the enemy starts a swing.")]
    [SerializeField] private float range = 1.6f;
    [Tooltip("Extra reach allowed when the swing lands, so tiny steps back don't always dodge it.")]
    [SerializeField] private float hitRangeTolerance = 0.3f;
    [SerializeField, Range(0f, 360f)] private float hitArc = 100f;

    [Header("Timing")]
    [SerializeField] private float windupDuration = 0.45f;
    [SerializeField] private float recoveryDuration = 0.5f;
    [Tooltip("Seconds after recovery before the next swing can start.")]
    [SerializeField] private float cooldown = 0.8f;

    private Health ownHealth;
    private Health currentTarget;
    private Phase phase;
    private float phaseRemaining;
    private float readyTime;

    public event Action WindupStarted;
    public event Action<bool> AttackResolved;

    public bool IsBusy => phase != Phase.Ready;

    private void Awake()
    {
        TryGetComponent(out ownHealth);
    }

    public bool IsInRange(Health target) =>
        target != null && !target.IsDead && FlatDistanceTo(target) <= range;

    public bool CanStartAttack(Health target) =>
        !IsBusy && Time.time >= readyTime && IsInRange(target);

    public void StartAttack(Health target)
    {
        if (!CanStartAttack(target)) return;

        currentTarget = target;
        EnterPhase(Phase.Windup, windupDuration);
        WindupStarted?.Invoke();
    }

    public void Cancel()
    {
        if (phase == Phase.Ready) return;

        phase = Phase.Ready;
        currentTarget = null;
        readyTime = Time.time + cooldown;
    }

    private void Update()
    {
        if (phase == Phase.Ready) return;
        if (ownHealth != null && ownHealth.IsDead)
        {
            Cancel();
            return;
        }

        phaseRemaining -= Time.deltaTime;
        if (phaseRemaining > 0f) return;

        if (phase == Phase.Windup)
        {
            bool hit = TryHit();
            AttackResolved?.Invoke(hit);
            EnterPhase(Phase.Recovery, recoveryDuration);
        }
        else
        {
            phase = Phase.Ready;
            currentTarget = null;
            readyTime = Time.time + cooldown;
        }
    }

    private void EnterPhase(Phase newPhase, float duration)
    {
        phase = newPhase;
        phaseRemaining = duration;
    }

    private bool TryHit()
    {
        if (currentTarget == null || currentTarget.IsDead) return false;
        if (FlatDistanceTo(currentTarget) > range + hitRangeTolerance) return false;

        Vector3 toTarget = currentTarget.transform.position - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude > 0.0001f && Vector3.Angle(transform.forward, toTarget) > hitArc * 0.5f) return false;

        Vector3 point = currentTarget.AimAnchor.position;
        Vector3 normal = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : transform.forward;
        currentTarget.ApplyDamage(new DamageInfo(damage, point, normal, DamageType.Melee, gameObject));
        return true;
    }

    private float FlatDistanceTo(Health target) =>
        EnemyMotor.FlatDistance(transform.position, target.transform.position);

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = phase == Phase.Windup ? Color.red : new Color(1f, 0.3f, 0.3f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
