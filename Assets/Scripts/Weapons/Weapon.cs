using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Hitscan firearm that shoots toward PlayerAimController's AimDirection with
// a spread cone driven by WeaponAccuracyCalculator. Whether a shot actually
// lands is decided entirely by that spread vs. the target's size/distance -
// there is no separate hit-chance roll.
//
// All firing data (fire rate, damage, pellet count, ...) lives in a
// WeaponDefinition asset rather than on this component, so different weapon
// "types" (pistol, shotgun, rifle) are different assets, not different
// classes. WeaponDefinition assets are shared, so any runtime-mutable state
// a future weapon feature needs (e.g. current ammo) must live here on the
// component instance, never added to WeaponDefinition itself.
public class Weapon : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string actionMapName = "Player";
    [SerializeField] private string attackActionName = "Attack";

    [Header("References")]
    [SerializeField] private Transform muzzle;
    [SerializeField] private PlayerAimController aimController;
    [SerializeField] private PlayerStats wielderStats;
    [Tooltip("Left empty, auto-resolved via GetComponent.")]
    [SerializeField] private PlayerWeaponInventory inventory;

    [Header("Loadout")]
    [SerializeField] private WeaponDefinition definition;

    [Header("Hit Detection")]
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("Debug")]
    [SerializeField] private bool debugDraw = true;
    [Tooltip("How many rays are used to trace the visible spread cone.")]
    [SerializeField] private int debugConeSegments = 16;
    [SerializeField] private float debugRayLength = 15f;
    [SerializeField] private float debugShotTraceDuration = 1f;

    // Fired once per trigger pull, before any pellets are resolved - the hook
    // for muzzle flash/fire sound. ShotHit/ShotMissed fire once per pellet
    // instead, for per-projectile impact effects.
    public event Action WeaponFired;
    public event Action<RaycastHit> ShotHit;
    public event Action<Vector3, Vector3> ShotMissed;

    private InputAction attackAction;
    private WeaponAccuracyCalculator accuracyCalculator;
    private float nextFireTime;

    public WeaponDefinition Definition => definition;

    public float CurrentSpreadAngle => definition != null
        ? accuracyCalculator.GetCurrentSpreadAngle(
            wielderStats != null ? wielderStats.InjuryFactor : 0f,
            wielderStats != null ? wielderStats.PsychosisFactor : 0f)
        : 0f;

    public float SteadyProgress01 => definition != null ? accuracyCalculator.SteadyProgress01 : 0f;

    private void Awake()
    {
        if (inventory == null) inventory = GetComponent<PlayerWeaponInventory>();

        InputActionMap map = inputActions.FindActionMap(actionMapName, throwIfNotFound: true);
        attackAction = map.FindAction(attackActionName, throwIfNotFound: true);

        WeaponDefinition startingDefinition = definition;
        definition = null;
        if (startingDefinition != null) SetDefinition(startingDefinition);
    }

    private void OnEnable() => attackAction.Enable();
    private void OnDisable() => attackAction.Disable();

    // Swaps in a new weapon: drops the currently equipped one (if any) as a
    // world pickup, rebuilds the accuracy calculator for the new profile, and
    // registers the new weapon as collected. Used both for the weapon
    // assigned in the Inspector at startup and for pickups equipped in play.
    public void EquipDefinition(WeaponDefinition newDefinition) => SetDefinition(newDefinition);

    private void SetDefinition(WeaponDefinition newDefinition)
    {
        if (newDefinition == null || newDefinition == definition) return;

        if (definition != null)
        {
            WeaponPickup.SpawnDropped(definition, transform.position);
        }

        definition = newDefinition;
        accuracyCalculator = new WeaponAccuracyCalculator(definition.AccuracyProfile);
        nextFireTime = 0f;

        inventory?.MarkCollected(definition);
    }

    private void Update()
    {
        if (definition == null) return;

        float toleranceMultiplier = aimController.IsTargetLocked
            ? definition.AccuracyProfile.AssistedSteadyToleranceMultiplier
            : 1f;
        accuracyCalculator.Tick(aimController.RawAimDirection, Time.deltaTime, toleranceMultiplier);

        bool wantsToFire = definition.FireMode == WeaponFireMode.Automatic
            ? attackAction.IsPressed()
            : attackAction.WasPressedThisFrame();

        if (wantsToFire && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + 1f / definition.FireRate;
            Fire();
        }

        if (debugDraw) DrawDebugCone();
    }

    private void Fire()
    {
        Vector3 origin = muzzle.position;
        Vector3 centerDirection = BallisticsUtility.ApplyConeSpread(aimController.AimDirection, CurrentSpreadAngle);

        WeaponFired?.Invoke();

        for (int i = 0; i < definition.PelletsPerShot; i++)
        {
            Vector3 pelletDirection = BallisticsUtility.ApplyConeSpread(centerDirection, definition.PelletSpreadAngle);
            FirePellet(origin, pelletDirection);
        }
    }

    private void FirePellet(Vector3 origin, Vector3 direction)
    {
        if (Physics.Raycast(origin, direction, out RaycastHit hit, definition.MaxRange, hitMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.TryGetComponent(out IDamageable damageable))
            {
                damageable.ApplyDamage(definition.Damage, hit.point, hit.normal);
            }

            if (debugDraw) Debug.DrawLine(origin, hit.point, Color.green, debugShotTraceDuration);
            ShotHit?.Invoke(hit);
        }
        else
        {
            if (debugDraw) Debug.DrawRay(origin, direction * definition.MaxRange, Color.red, debugShotTraceDuration);
            ShotMissed?.Invoke(origin, direction);
        }
    }

    // Traces the aimed direction (yellow) and the real spread cone edges (cyan) every
    // frame using the exact same math Fire() uses, so what you see is what you'd hit.
    private void DrawDebugCone()
    {
        if (muzzle == null || aimController == null) return;

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

    private void OnGUI()
    {
        if (!debugDraw) return;

        string injury = wielderStats != null ? wielderStats.InjuryFactor.ToString("P0") : "n/a";
        string psychosis = wielderStats != null ? wielderStats.PsychosisFactor.ToString("P0") : "n/a";
        string weaponName = definition != null ? definition.WeaponName : "none";
        string lockState = aimController != null && aimController.IsTargetLocked ? "LOCKED" : "-";

        GUI.Label(new Rect(10, 10, 320, 110),
            $"Weapon: {weaponName}\nSpread: {CurrentSpreadAngle:F1}°\nSteady aim: {SteadyProgress01:P0}\nInjury: {injury}\nPsychosis: {psychosis}\nAim assist: {lockState}");
    }
}
