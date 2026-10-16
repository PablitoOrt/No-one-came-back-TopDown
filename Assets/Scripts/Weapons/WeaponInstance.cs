using UnityEngine;

public interface IAmmoSource
{
    int GetAmmo(AmmoType type);
    int TakeAmmo(AmmoType type, int requested);
}

public class WeaponUser
{
    public readonly GameObject Root;
    public readonly Transform Origin;
    public readonly PlayerAimController Aim;
    public readonly AimSteadiness Steadiness;
    public readonly IAmmoSource AmmoSource;
    public readonly LayerMask HitMask;

    public WeaponUser(GameObject root, Transform origin, PlayerAimController aim, AimSteadiness steadiness, IAmmoSource ammoSource, LayerMask hitMask)
    {
        Root = root;
        Origin = origin;
        Aim = aim;
        Steadiness = steadiness;
        AmmoSource = ammoSource;
        HitMask = hitMask;
    }

    public bool IsOwnCollider(Collider collider) => collider.transform.IsChildOf(Root.transform);
}

public abstract class WeaponInstance
{
    protected WeaponInstance(WeaponDefinition definition, WeaponUser user)
    {
        Definition = definition;
        User = user;
    }

    public WeaponDefinition Definition { get; }
    protected WeaponUser User { get; }

    public virtual bool IsBusy => false;

    public virtual void Tick(float deltaTime) { }

    public abstract bool TryAttack(bool pressedThisFrame, bool held);

    public virtual bool TryReload() => false;

    public virtual void OnEquipped() { }

    public virtual void OnUnequipped() { }
}
