using System;
using System.Collections.Generic;
using UnityEngine;

public class MeleeInstance : WeaponInstance
{
    private enum Phase
    {
        Idle,
        Windup,
        Recovery,
    }

    private static readonly Collider[] OverlapBuffer = new Collider[32];
    private static readonly HashSet<IDamageable> HitThisSwing = new HashSet<IDamageable>();

    private readonly MeleeDefinition melee;
    private Phase phase;
    private float phaseRemaining;

    public event Action SwingStarted;
    public event Action<int> SwingResolved;

    public MeleeInstance(MeleeDefinition definition, WeaponUser user) : base(definition, user)
    {
        melee = definition;
    }

    public MeleeDefinition Melee => melee;
    public override bool IsBusy => phase != Phase.Idle;

    public override bool TryAttack(bool pressedThisFrame, bool held)
    {
        if (!pressedThisFrame || phase != Phase.Idle) return false;

        EnterPhase(Phase.Windup, melee.WindupDuration);
        SwingStarted?.Invoke();
        return true;
    }

    public override void Tick(float deltaTime)
    {
        if (phase == Phase.Idle) return;

        phaseRemaining -= deltaTime;
        if (phaseRemaining > 0f) return;

        if (phase == Phase.Windup)
        {
            int hits = PerformHit();
            SwingResolved?.Invoke(hits);
            EnterPhase(Phase.Recovery, melee.RecoveryDuration);
        }
        else
        {
            phase = Phase.Idle;
        }
    }

    public override void OnUnequipped()
    {
        phase = Phase.Idle;
    }

    private void EnterPhase(Phase newPhase, float duration)
    {
        phase = newPhase;
        phaseRemaining = duration;
    }

    private int PerformHit()
    {
        Vector3 center = User.Origin.position;
        Vector3 forward = User.Aim.AimDirection;
        forward.y = 0f;
        forward.Normalize();

        int count = Physics.OverlapSphereNonAlloc(center, melee.Range, OverlapBuffer, User.HitMask, QueryTriggerInteraction.Ignore);
        HitThisSwing.Clear();

        for (int i = 0; i < count && HitThisSwing.Count < melee.MaxTargets; i++)
        {
            Collider target = OverlapBuffer[i];
            if (User.IsOwnCollider(target)) continue;

            IDamageable damageable = target.GetComponentInParent<IDamageable>();
            if (damageable == null || HitThisSwing.Contains(damageable)) continue;

            Vector3 point = target.ClosestPoint(center);
            Vector3 toTarget = point - center;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.0001f && Vector3.Angle(forward, toTarget) > melee.ArcAngle * 0.5f) continue;

            HitThisSwing.Add(damageable);
            Vector3 normal = toTarget.sqrMagnitude > 0.0001f ? -toTarget.normalized : -forward;
            damageable.ApplyDamage(new DamageInfo(melee.Damage, point, normal, DamageType.Melee, User.Root));
        }

        return HitThisSwing.Count;
    }
}
