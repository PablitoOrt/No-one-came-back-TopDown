using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Hitscan firearm. Firing data lives in a WeaponDefinition asset rather than
// on this component, so weapon "types" are assets, not classes - any
// runtime-mutable state (e.g. future ammo) must live here instead, since
// WeaponDefinition assets are shared.
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

    // Fired once per trigger pull (hook for muzzle flash/sound); ShotHit/ShotMissed fire once per pellet.
    public event Action WeaponFired;
    public event Action<RaycastHit> ShotHit;
    public event Action<Vector3, Vector3> ShotMissed;

    private InputAction attackAction;
    private WeaponAccuracyCalculator accuracyCalculator;
    private float nextFireTime;
    private bool wasAiming;

    public WeaponDefinition Definition => definition;

    // Guards accuracyCalculator too: definition is a SerializeField, so it's
    // already non-null from the Inspector before Awake() builds the calculator,
    // e.g. while this GameObject starts disabled and something else polls this.
    public float CurrentSpreadAngle => definition != null && accuracyCalculator != null
        ? accuracyCalculator.GetCurrentSpreadAngle(
            wielderStats != null ? wielderStats.InjuryFactor : 0f,
            wielderStats != null ? wielderStats.PsychosisFactor : 0f)
        : 0f;

    public float SteadyProgress01 => definition != null && accuracyCalculator != null ? accuracyCalculator.SteadyProgress01 : 0f;

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

        bool isAiming = aimController.IsAiming;

        // Every fresh press of Aim restarts precision from zero.
        if (isAiming && !wasAiming)
        {
            accuracyCalculator.Reset();
        }
        wasAiming = isAiming;

        if (isAiming)
        {
            accuracyCalculator.Tick(Time.deltaTime, aimController.IsTargetLocked);
        }

        bool wantsToFire = isAiming && (definition.FireMode == WeaponFireMode.Automatic
            ? attackAction.IsPressed()
            : attackAction.WasPressedThisFrame());

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
        accuracyCalculator.Reset();

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

    // Uses the same math as Fire(), so the drawn cone matches what you'd actually hit.
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
        string aimingState = aimController != null && aimController.IsAiming ? "AIMING" : "-";
        string lockState = aimController != null && aimController.IsTargetLocked ? "LOCKED" : "-";

        GUI.Label(new Rect(10, 10, 320, 125),
            $"Weapon: {weaponName}\nSpread: {CurrentSpreadAngle:F1}°\nSteady aim: {SteadyProgress01:P0}\nInjury: {injury}\nPsychosis: {psychosis}\nAim: {aimingState}\nAim assist: {lockState}");
    }
}
