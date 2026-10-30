using System.Collections.Generic;
using UnityEngine;

public class FirearmEffects : MonoBehaviour
{
    [Tooltip("Left empty, auto-resolved from this object or its parents.")]
    [SerializeField] private PlayerCombat combat;
    [Tooltip("Left empty, auto-resolved from this object or its parents.")]
    [SerializeField] private PlayerAimController aimController;

    private readonly Dictionary<FirearmDefinition, EffectSet> sets = new Dictionary<FirearmDefinition, EffectSet>();
    private Transform container;
    private FirearmInstance current;
    private EffectSet currentSet;
    private Light muzzleLight;
    private float lightRemaining;
    private float lightDuration;
    private float lightIntensity;

    private void Awake()
    {
        if (combat == null) combat = GetComponentInParent<PlayerCombat>();
        if (aimController == null) aimController = GetComponentInParent<PlayerAimController>();
        container = new GameObject($"{name} Firearm Effects").transform;

        muzzleLight = new GameObject("Muzzle Light").AddComponent<Light>();
        muzzleLight.transform.SetParent(container, false);
        muzzleLight.type = LightType.Point;
        muzzleLight.shadows = LightShadows.None;
        muzzleLight.enabled = false;
    }

    private void Update()
    {
        if (lightRemaining <= 0f) return;

        lightRemaining -= Time.deltaTime;
        if (lightRemaining <= 0f)
        {
            muzzleLight.enabled = false;
            return;
        }

        muzzleLight.transform.position = combat.Muzzle.position;
        muzzleLight.intensity = lightIntensity * (lightRemaining / lightDuration);
    }

    private void OnEnable()
    {
        if (combat == null) return;

        combat.FirearmEquipped += HandleFirearmEquipped;
        if (combat.CurrentFirearm != null) HandleFirearmEquipped(combat.CurrentFirearm);
    }

    private void OnDisable()
    {
        if (combat != null) combat.FirearmEquipped -= HandleFirearmEquipped;
        Unbind();
    }

    private void OnDestroy()
    {
        if (container != null) Destroy(container.gameObject);
    }

    private void HandleFirearmEquipped(FirearmInstance firearm)
    {
        Unbind();

        currentSet = GetOrCreateSet(firearm.Firearm);
        if (currentSet == null) return;

        current = firearm;
        current.Fired += HandleFired;
        current.ShotHit += HandleShotHit;
        current.ShotMissed += HandleShotMissed;
    }

    private void Unbind()
    {
        if (current != null)
        {
            current.Fired -= HandleFired;
            current.ShotHit -= HandleShotHit;
            current.ShotMissed -= HandleShotMissed;
        }

        current = null;
        currentSet = null;
        lightRemaining = 0f;
        if (muzzleLight != null) muzzleLight.enabled = false;
    }

    private EffectSet GetOrCreateSet(FirearmDefinition definition)
    {
        if (definition.EffectsProfile == null) return null;

        if (!sets.TryGetValue(definition, out EffectSet set))
        {
            set = new EffectSet(definition, container);
            sets.Add(definition, set);
        }
        return set;
    }

    private void HandleFired()
    {
        Transform muzzle = combat.Muzzle;
        Quaternion aimRotation = AimRotation(muzzle);

        currentSet.MuzzleFlash?.Play(muzzle.position, aimRotation);
        Transform shellEject = combat.CurrentModel != null ? combat.CurrentModel.ShellEject : null;
        Vector3 shellPosition = shellEject != null
            ? shellEject.position
            : muzzle.position + aimRotation * currentSet.Profile.ShellEjectOffset;
        currentSet.Shells?.Play(shellPosition, aimRotation);

        FlashLight(muzzle.position);
    }

    private void FlashLight(Vector3 position)
    {
        FirearmEffectsProfile profile = currentSet.Profile;
        if (!profile.UseMuzzleLight) return;

        lightDuration = profile.MuzzleLightDuration;
        lightIntensity = profile.MuzzleLightIntensity;
        lightRemaining = lightDuration;

        muzzleLight.color = profile.MuzzleLightColor;
        muzzleLight.range = profile.MuzzleLightRange;
        muzzleLight.intensity = lightIntensity;
        muzzleLight.transform.position = position;
        muzzleLight.enabled = true;
    }

    private void HandleShotHit(RaycastHit hit)
    {
        EmitTracer(combat.Muzzle.position, hit.point);
        currentSet.Impacts?.Play(hit.point, Quaternion.LookRotation(hit.normal));
    }

    private void HandleShotMissed(Vector3 origin, Vector3 direction)
    {
        EmitTracer(origin, origin + direction * current.Firearm.MaxRange);
    }

    private void EmitTracer(Vector3 from, Vector3 to)
    {
        FirearmEffectsProfile profile = currentSet.Profile;
        if (currentSet.Tracer == null || Random.value > profile.TracerChance) return;

        Vector3 delta = to - from;
        float distance = delta.magnitude;
        if (distance < 0.01f) return;

        var emitParams = new ParticleSystem.EmitParams
        {
            position = from,
            velocity = delta / distance * profile.TracerSpeed,
            startLifetime = distance / profile.TracerSpeed,
            applyShapeToPosition = false,
        };
        currentSet.Tracer.Emit(emitParams, 1);
    }

    private Quaternion AimRotation(Transform muzzle)
    {
        Vector3 direction = aimController != null ? aimController.AimDirection : muzzle.forward;
        return direction.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(direction, Vector3.up) : muzzle.rotation;
    }

    private class EffectSet
    {
        public readonly FirearmEffectsProfile Profile;
        public readonly EffectPool MuzzleFlash;
        public readonly EffectPool Shells;
        public readonly EffectPool Impacts;
        public readonly ParticleSystem Tracer;

        public EffectSet(FirearmDefinition definition, Transform container)
        {
            Profile = definition.EffectsProfile;
            Transform root = new GameObject(definition.WeaponName).transform;
            root.SetParent(container, false);

            MuzzleFlash = EffectPool.Create(Profile.MuzzleFlash, Profile.PoolSize, root);
            Shells = EffectPool.Create(Profile.ShellEject, Profile.PoolSize, root);
            Impacts = EffectPool.Create(Profile.Impact, Profile.PoolSize * definition.PelletsPerShot, root);

            if (Profile.Tracer != null)
            {
                Tracer = Object.Instantiate(Profile.Tracer, root);
                ParticleSystem.MainModule main = Tracer.main;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                Tracer.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }

    private class EffectPool
    {
        private readonly ParticleSystem[] instances;
        private int next;

        private EffectPool(ParticleSystem prefab, int size, Transform parent)
        {
            instances = new ParticleSystem[size];
            for (int i = 0; i < size; i++)
            {
                instances[i] = Object.Instantiate(prefab, parent);
                instances[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        public static EffectPool Create(ParticleSystem prefab, int size, Transform parent)
        {
            return prefab != null ? new EffectPool(prefab, Mathf.Max(1, size), parent) : null;
        }

        public void Play(Vector3 position, Quaternion rotation)
        {
            ParticleSystem instance = instances[next];
            next = (next + 1) % instances.Length;

            instance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.Play(true);
        }
    }
}
