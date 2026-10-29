using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Shot / swing origin.")]
    [SerializeField] private Transform muzzle;
    [Tooltip("Left empty, auto-resolved from this object or its parents.")]
    [SerializeField] private PlayerAimController aimController;
    [Tooltip("Left empty, auto-resolved from this object or its parents.")]
    [SerializeField] private AimSteadiness steadiness;
    [Tooltip("Left empty, auto-resolved from this object or its parents.")]
    [SerializeField] private PlayerInventory inventory;
    [Tooltip("Left empty, auto-resolved from this object or its parents.")]
    [SerializeField] private PlayerInputReader input;

    [Header("Loadout")]
    [Tooltip("Added to the inventory and equipped at start.")]
    [FormerlySerializedAs("definition")]
    [SerializeField] private FirearmDefinition startingFirearm;
    [Tooltip("Strike used until a melee weapon is picked up. Left empty, a default unarmed strike is created at runtime.")]
    [SerializeField] private MeleeDefinition unarmedMelee;

    [Header("Hit Detection")]
    [Tooltip("Layers shots and swings can hit. Include walls so bullets don't pass through them.")]
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("Debug")]
    [SerializeField] private bool debugDraw = true;
    [Tooltip("How many rays are used to trace the visible spread cone.")]
    [SerializeField] private int debugConeSegments = 16;
    [SerializeField] private float debugRayLength = 15f;
    [SerializeField] private float debugShotTraceDuration = 1f;

    private readonly Dictionary<WeaponDefinition, WeaponInstance> instances = new Dictionary<WeaponDefinition, WeaponInstance>();
    private WeaponUser user;
    private IPlayerActionBlocker[] blockers;

    public FirearmInstance CurrentFirearm { get; private set; }
    public MeleeInstance CurrentMelee { get; private set; }

    public event Action<FirearmInstance> FirearmEquipped;
    public event Action<MeleeInstance> MeleeEquipped;

    public float CurrentSpreadAngle => CurrentFirearm?.CurrentSpreadAngle ?? 0f;
    public float AimPrecision01 => steadiness != null ? steadiness.Progress01 : 0f;

    private void Awake()
    {
        if (aimController == null) aimController = GetComponentInParent<PlayerAimController>();
        if (steadiness == null) steadiness = GetComponentInParent<AimSteadiness>();
        if (inventory == null) inventory = GetComponentInParent<PlayerInventory>();
        if (input == null) input = GetComponentInParent<PlayerInputReader>();
        if (muzzle == null) muzzle = transform;

        Health ownerHealth = GetComponentInParent<Health>();
        GameObject root = ownerHealth != null ? ownerHealth.gameObject : transform.root.gameObject;
        user = new WeaponUser(root, muzzle, aimController, steadiness, inventory, hitMask);
        blockers = root.GetComponentsInChildren<IPlayerActionBlocker>(true);

        if (unarmedMelee == null)
        {
            unarmedMelee = ScriptableObject.CreateInstance<MeleeDefinition>();
            unarmedMelee.name = "Unarmed";
        }
    }

    private void OnEnable()
    {
        if (inventory != null) inventory.WeaponAdded += HandleWeaponAdded;
    }

    private void OnDisable()
    {
        if (inventory != null) inventory.WeaponAdded -= HandleWeaponAdded;
    }

    private void Start()
    {
        if (startingFirearm != null && inventory != null) inventory.AddWeapon(startingFirearm);

        if (inventory != null)
        {
            foreach (WeaponDefinition owned in inventory.Weapons) HandleWeaponAdded(owned);
        }

        if (CurrentFirearm == null && startingFirearm != null && inventory == null) EquipFirearm(startingFirearm);
        if (CurrentMelee == null) EquipMelee(unarmedMelee);
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        CurrentFirearm?.Tick(deltaTime);
        CurrentMelee?.Tick(deltaTime);

        bool meleeBusy = CurrentMelee != null && CurrentMelee.IsBusy;
        bool blocked = IsCombatBlocked();
        aimController.CanAim = CurrentFirearm != null && !meleeBusy && !blocked;

        if (blocked) return;

        if (input.NextWeaponPressed) CycleFirearm(1);
        if (input.PreviousWeaponPressed) CycleFirearm(-1);
        if (input.ReloadPressed) CurrentFirearm?.TryReload();

        if (aimController.IsAiming)
        {
            CurrentFirearm.TryAttack(input.AttackPressed, input.AttackHeld);
        }
        else if (input.AttackPressed && CurrentMelee != null && (CurrentFirearm == null || !CurrentFirearm.IsBusy))
        {
            CurrentMelee.TryAttack(true, true);
        }

        if (debugDraw) DrawDebugCone();
    }

    private bool IsCombatBlocked()
    {
        foreach (IPlayerActionBlocker blocker in blockers)
        {
            if (blocker.BlocksCombat) return true;
        }
        return false;
    }

    public void EquipFirearm(FirearmDefinition definition)
    {
        if (definition == null || CurrentFirearm?.Definition == definition) return;

        if (CurrentFirearm != null)
        {
            CurrentFirearm.OnUnequipped();
            CurrentFirearm.ShotHit -= HandleShotHit;
            CurrentFirearm.ShotMissed -= HandleShotMissed;
        }

        CurrentFirearm = (FirearmInstance)GetOrCreateInstance(definition);
        CurrentFirearm.ShotHit += HandleShotHit;
        CurrentFirearm.ShotMissed += HandleShotMissed;
        CurrentFirearm.OnEquipped();

        steadiness?.SetProfile(definition.AccuracyProfile);
        FirearmEquipped?.Invoke(CurrentFirearm);
    }

    public void EquipMelee(MeleeDefinition definition)
    {
        if (definition == null || CurrentMelee?.Definition == definition) return;

        CurrentMelee?.OnUnequipped();
        CurrentMelee = (MeleeInstance)GetOrCreateInstance(definition);
        CurrentMelee.OnEquipped();

        MeleeEquipped?.Invoke(CurrentMelee);
    }

    private WeaponInstance GetOrCreateInstance(WeaponDefinition definition)
    {
        if (!instances.TryGetValue(definition, out WeaponInstance instance))
        {
            instance = definition.CreateInstance(user);
            instances.Add(definition, instance);
        }
        return instance;
    }

    private void HandleWeaponAdded(WeaponDefinition definition)
    {
        switch (definition)
        {
            case FirearmDefinition firearm when CurrentFirearm == null:
                EquipFirearm(firearm);
                break;
            case MeleeDefinition melee when CurrentMelee == null || CurrentMelee.Definition == unarmedMelee:
                EquipMelee(melee);
                break;
        }
    }

    private void CycleFirearm(int step)
    {
        if (inventory == null || (CurrentFirearm != null && CurrentFirearm.IsBusy)) return;

        IReadOnlyList<WeaponDefinition> owned = inventory.Weapons;
        int count = owned.Count;
        int start = CurrentFirearm != null ? IndexOf(owned, CurrentFirearm.Definition) : -1;

        for (int offset = 1; offset <= count; offset++)
        {
            int index = ((start + step * offset) % count + count) % count;
            if (owned[index] is FirearmDefinition firearm)
            {
                EquipFirearm(firearm);
                return;
            }
        }
    }

    private static int IndexOf(IReadOnlyList<WeaponDefinition> list, WeaponDefinition item)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == item) return i;
        }
        return -1;
    }

    private void HandleShotHit(RaycastHit hit)
    {
        if (debugDraw) Debug.DrawLine(muzzle.position, hit.point, Color.green, debugShotTraceDuration);
    }

    private void HandleShotMissed(Vector3 origin, Vector3 direction)
    {
        if (debugDraw) Debug.DrawRay(origin, direction * CurrentFirearm.Firearm.MaxRange, Color.red, debugShotTraceDuration);
    }

    private void DrawDebugCone()
    {
        if (CurrentFirearm == null || !aimController.IsAiming) return;

        Vector3 origin = muzzle.position;
        Vector3 aimDirection = aimController.AimDirection;
        float spreadAngle = CurrentSpreadAngle;

        Debug.DrawRay(origin, aimDirection * debugRayLength, Color.yellow);

        for (int i = 0; i < debugConeSegments; i++)
        {
            float spin = i * (360f / debugConeSegments);
            Vector3 edgeDirection = BallisticsUtility.TiltDirection(aimDirection, spreadAngle, spin);
            Debug.DrawRay(origin, edgeDirection * debugRayLength, Color.cyan);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!debugDraw || CurrentMelee == null || muzzle == null) return;

        Gizmos.color = CurrentMelee.IsBusy ? Color.red : new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(muzzle.position, CurrentMelee.Melee.Range);
    }

    // UI debug
    private void OnGUI()
    {
        if (!debugDraw) return;

        string weaponName = CurrentFirearm != null ? CurrentFirearm.Definition.WeaponName : "none";
        string ammo = "-";
        if (CurrentFirearm != null)
        {
            int reserve = CurrentFirearm.ReserveAmmo;
            ammo = CurrentFirearm.Firearm.UsesMagazine
                ? $"{CurrentFirearm.AmmoInMagazine}/{CurrentFirearm.Firearm.MagazineSize} (+{(reserve == int.MaxValue ? "∞" : reserve.ToString())})"
                : "∞";
            if (CurrentFirearm.IsReloading) ammo += $" RELOADING {CurrentFirearm.ReloadProgress01:P0}";
        }

        string meleeName = CurrentMelee != null ? CurrentMelee.Definition.name : "none";
        string meleeState = CurrentMelee != null && CurrentMelee.IsBusy ? " (swinging)" : "";
        string aimState = aimController.IsAiming ? "AIMING" : "-";
        string lockState = aimController.IsTargetLocked ? "LOCKED" : "-";
        string timeMultiplier = steadiness != null ? $"x{steadiness.AimTimeMultiplier:F2}" : "n/a";

        GUI.Label(new Rect(10, 10, 360, 150),
            $"Firearm: {weaponName}\nAmmo: {ammo}\nMelee: {meleeName}{meleeState}\n" +
            $"Aim: {aimState}   Assist: {lockState}\nSteady aim: {AimPrecision01:P0}   Spread: {CurrentSpreadAngle:F1}°\n" +
            $"Aim time (sanity): {timeMultiplier}");
    }
}
