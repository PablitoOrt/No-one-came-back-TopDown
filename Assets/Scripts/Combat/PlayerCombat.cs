using System;
using System.Collections.Generic;
using NoOneCameBack.Inventory;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Swing origin. Also the shot origin for firearms without a model.")]
    [SerializeField] private Transform muzzle;
    [Tooltip("Where equipped firearm models are spawned, e.g. the right hand bone. Left empty, this object is used.")]
    [SerializeField] private Transform weaponHolder;
    [Tooltip("Left empty, auto-resolved from this object or its parents.")]
    [SerializeField] private PlayerAimController aimController;
    [Tooltip("Left empty, auto-resolved from this object or its parents.")]
    [SerializeField] private AimSteadiness steadiness;
    [Tooltip("Source of the equipped weapon and reserve ammo. Left empty, auto-resolved from this object or its parents.")]
    [SerializeField] private PlayerInventoryHolder inventory;
    [Tooltip("Left empty, auto-resolved from this object or its parents.")]
    [SerializeField] private PlayerInputReader input;

    [Header("Loadout")]
    [Tooltip("Strike used while no melee weapon is equipped. Left empty, a default unarmed strike is created at runtime.")]
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
    private readonly Dictionary<FirearmDefinition, WeaponModel> models = new Dictionary<FirearmDefinition, WeaponModel>();
    private WeaponUser user;
    private IPlayerActionBlocker[] blockers;
    private ItemInstance equippedItem;

    public FirearmInstance CurrentFirearm { get; private set; }
    public MeleeInstance CurrentMelee { get; private set; }
    public WeaponModel CurrentModel { get; private set; }
    public Transform Muzzle => CurrentFirearm != null ? CurrentFirearm.ShotOrigin : muzzle;

    public event Action<FirearmInstance> FirearmEquipped;
    public event Action<MeleeInstance> MeleeEquipped;

    public float CurrentSpreadAngle => CurrentFirearm?.CurrentSpreadAngle ?? 0f;
    public float AimPrecision01 => steadiness != null ? steadiness.Progress01 : 0f;

    private void Awake()
    {
        if (aimController == null) aimController = GetComponentInParent<PlayerAimController>();
        if (steadiness == null) steadiness = GetComponentInParent<AimSteadiness>();
        if (inventory == null) inventory = GetComponentInParent<PlayerInventoryHolder>();
        if (input == null) input = GetComponentInParent<PlayerInputReader>();
        if (muzzle == null) muzzle = transform;
        if (weaponHolder == null) weaponHolder = transform;

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

    // Subscribed in Start: the holder builds its model in Awake, which may run after ours.
    private void Start()
    {
        if (inventory != null)
        {
            inventory.Model.EquippedChanged += ApplyEquippedItem;
            ApplyEquippedItem(inventory.Model.EquippedWeapon);
        }

        if (CurrentMelee == null) EquipMelee(unarmedMelee);
    }

    private void OnDestroy()
    {
        if (inventory != null && inventory.Model != null) inventory.Model.EquippedChanged -= ApplyEquippedItem;
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

    // The single equipped slot of the tablet inventory decides the loadout: a firearm keeps the unarmed strike,
    // a melee weapon replaces it, anything else (throwables, nothing) leaves the player unarmed.
    private void ApplyEquippedItem(ItemInstance item)
    {
        equippedItem = item;

        switch (item?.Definition.Weapon)
        {
            case FirearmDefinition firearm:
                EquipFirearm(firearm);
                CurrentFirearm.SetAmmoInMagazine(item.LoadedAmmo);
                EquipMelee(unarmedMelee);
                break;
            case MeleeDefinition melee:
                UnequipFirearm();
                EquipMelee(melee);
                break;
            default:
                UnequipFirearm();
                EquipMelee(unarmedMelee);
                break;
        }
    }

    private void EquipFirearm(FirearmDefinition definition)
    {
        if (CurrentFirearm?.Definition == definition) return;

        UnequipFirearm();

        CurrentModel = GetOrCreateModel(definition);
        if (CurrentModel != null) CurrentModel.gameObject.SetActive(true);

        CurrentFirearm = (FirearmInstance)GetOrCreateInstance(definition);
        CurrentFirearm.Muzzle = CurrentModel != null ? CurrentModel.Muzzle : null;
        CurrentFirearm.ShotHit += HandleShotHit;
        CurrentFirearm.ShotMissed += HandleShotMissed;
        CurrentFirearm.AmmoChanged += HandleAmmoChanged;
        CurrentFirearm.OnEquipped();

        steadiness?.SetProfile(definition.AccuracyProfile);
        FirearmEquipped?.Invoke(CurrentFirearm);
    }

    private void UnequipFirearm()
    {
        if (CurrentFirearm != null)
        {
            CurrentFirearm.OnUnequipped();
            CurrentFirearm.ShotHit -= HandleShotHit;
            CurrentFirearm.ShotMissed -= HandleShotMissed;
            CurrentFirearm.AmmoChanged -= HandleAmmoChanged;
            CurrentFirearm = null;
        }

        if (CurrentModel != null)
        {
            CurrentModel.gameObject.SetActive(false);
            CurrentModel = null;
        }
    }

    // Rounds live in the inventory item so they survive swapping weapons, dropping them and saving.
    private void HandleAmmoChanged(int rounds) => equippedItem?.SetLoadedAmmo(rounds);

    private void EquipMelee(MeleeDefinition definition)
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

    private WeaponModel GetOrCreateModel(FirearmDefinition definition)
    {
        if (definition.ModelPrefab == null) return null;

        if (!models.TryGetValue(definition, out WeaponModel model))
        {
            model = Instantiate(definition.ModelPrefab, weaponHolder, false);
            models.Add(definition, model);
        }
        return model;
    }

    private void HandleShotHit(RaycastHit hit)
    {
        if (debugDraw) Debug.DrawLine(Muzzle.position, hit.point, Color.green, debugShotTraceDuration);
    }

    private void HandleShotMissed(Vector3 origin, Vector3 direction)
    {
        if (debugDraw) Debug.DrawRay(origin, direction * CurrentFirearm.Firearm.MaxRange, Color.red, debugShotTraceDuration);
    }

    private void DrawDebugCone()
    {
        if (CurrentFirearm == null || !aimController.IsAiming) return;

        Vector3 origin = Muzzle.position;
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
