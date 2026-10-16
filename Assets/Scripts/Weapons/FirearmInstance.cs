using System;
using UnityEngine;

public class FirearmInstance : WeaponInstance
{
    private static readonly RaycastHit[] HitBuffer = new RaycastHit[16];

    private readonly FirearmDefinition firearm;
    private float nextFireTime;
    private float reloadRemaining;

    public event Action Fired;
    public event Action<RaycastHit> ShotHit;
    public event Action<Vector3, Vector3> ShotMissed;
    public event Action DryFired;
    public event Action ReloadStarted;
    public event Action ReloadFinished;
    public event Action<int> AmmoChanged;

    public FirearmInstance(FirearmDefinition definition, WeaponUser user) : base(definition, user)
    {
        firearm = definition;
        AmmoInMagazine = definition.MagazineSize;
    }

    public FirearmDefinition Firearm => firearm;
    public int AmmoInMagazine { get; private set; }
    public bool IsReloading => reloadRemaining > 0f;
    public float ReloadProgress01 => IsReloading && firearm.ReloadDuration > 0f ? 1f - reloadRemaining / firearm.ReloadDuration : 0f;
    public override bool IsBusy => IsReloading;

    public int ReserveAmmo => firearm.AmmoType == null || User.AmmoSource == null
        ? int.MaxValue
        : User.AmmoSource.GetAmmo(firearm.AmmoType);

    public float CurrentSpreadAngle => firearm.AccuracyProfile != null && User.Steadiness != null
        ? firearm.AccuracyProfile.EvaluateSpread(User.Steadiness.Progress01)
        : 0f;

    public override void Tick(float deltaTime)
    {
        if (!IsReloading) return;

        reloadRemaining -= deltaTime;
        if (reloadRemaining <= 0f) FinishReload();
    }

    public override bool TryAttack(bool pressedThisFrame, bool held)
    {
        if (!User.Aim.IsAiming || IsReloading) return false;

        bool triggerPulled = firearm.FireMode == WeaponFireMode.Automatic ? held : pressedThisFrame;
        if (!triggerPulled || Time.time < nextFireTime) return false;

        if (firearm.UsesMagazine && AmmoInMagazine <= 0)
        {
            if (pressedThisFrame) DryFired?.Invoke();
            if (firearm.AutoReloadWhenEmpty) TryReload();
            return false;
        }

        nextFireTime = Time.time + 1f / Mathf.Max(0.01f, firearm.FireRate);

        if (firearm.UsesMagazine)
        {
            AmmoInMagazine--;
            AmmoChanged?.Invoke(AmmoInMagazine);
        }

        Fire();
        return true;
    }

    public override bool TryReload()
    {
        if (!firearm.UsesMagazine || IsReloading || AmmoInMagazine >= firearm.MagazineSize) return false;
        if (ReserveAmmo <= 0) return false;

        reloadRemaining = Mathf.Max(0.0001f, firearm.ReloadDuration);
        ReloadStarted?.Invoke();
        return true;
    }

    public override void OnUnequipped()
    {
        reloadRemaining = 0f;
    }

    private void FinishReload()
    {
        reloadRemaining = 0f;

        int needed = firearm.MagazineSize - AmmoInMagazine;
        int taken = firearm.AmmoType == null || User.AmmoSource == null
            ? needed
            : User.AmmoSource.TakeAmmo(firearm.AmmoType, needed);

        AmmoInMagazine += taken;
        AmmoChanged?.Invoke(AmmoInMagazine);
        ReloadFinished?.Invoke();
    }

    private void Fire()
    {
        Vector3 origin = User.Origin.position;
        Vector3 centerDirection = BallisticsUtility.ApplyConeSpread(User.Aim.AimDirection, CurrentSpreadAngle);

        Fired?.Invoke();
        User.Steadiness?.NotifyShotFired();

        for (int i = 0; i < firearm.PelletsPerShot; i++)
        {
            Vector3 pelletDirection = BallisticsUtility.ApplyConeSpread(centerDirection, firearm.PelletSpreadAngle);
            FirePellet(origin, pelletDirection);
        }
    }

    private void FirePellet(Vector3 origin, Vector3 direction)
    {
        if (TryRaycastIgnoringSelf(origin, direction, out RaycastHit hit))
        {
            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
            damageable?.ApplyDamage(new DamageInfo(firearm.Damage, hit.point, hit.normal, DamageType.Ballistic, User.Root));

            ShotHit?.Invoke(hit);
        }
        else
        {
            ShotMissed?.Invoke(origin, direction);
        }
    }

    private bool TryRaycastIgnoringSelf(Vector3 origin, Vector3 direction, out RaycastHit nearest)
    {
        int count = Physics.RaycastNonAlloc(origin, direction, HitBuffer, firearm.MaxRange, User.HitMask, QueryTriggerInteraction.Ignore);

        nearest = default;
        float nearestDistance = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            if (User.IsOwnCollider(HitBuffer[i].collider) || HitBuffer[i].distance >= nearestDistance) continue;

            nearest = HitBuffer[i];
            nearestDistance = nearest.distance;
        }

        return nearestDistance < float.MaxValue;
    }
}
