using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Hitscan firearm that shoots toward PlayerAimController's AimDirection with
// a spread cone driven by WeaponAccuracyCalculator. Whether a shot actually
// lands is decided entirely by that spread vs. the target's size/distance -
// there is no separate hit-chance roll.
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

    [Header("Accuracy")]
    [SerializeField] private WeaponAccuracyProfile accuracyProfile;

    [Header("Firing")]
    [SerializeField] private float fireRate = 2f;
    [SerializeField] private float maxRange = 100f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("Debug")]
    [SerializeField] private bool debugDraw = true;
    [Tooltip("How many rays are used to trace the visible spread cone.")]
    [SerializeField] private int debugConeSegments = 16;
    [SerializeField] private float debugRayLength = 15f;
    [SerializeField] private float debugShotTraceDuration = 1f;

    public event Action<RaycastHit> ShotHit;
    public event Action<Vector3, Vector3> ShotMissed;

    private InputAction attackAction;
    private WeaponAccuracyCalculator accuracyCalculator;
    private float nextFireTime;

    public float CurrentSpreadAngle => accuracyCalculator.GetCurrentSpreadAngle(
        wielderStats != null ? wielderStats.InjuryFactor : 0f,
        wielderStats != null ? wielderStats.PsychosisFactor : 0f);

    public float SteadyProgress01 => accuracyCalculator.SteadyProgress01;

    private void Awake()
    {
        accuracyCalculator = new WeaponAccuracyCalculator(accuracyProfile);

        InputActionMap map = inputActions.FindActionMap(actionMapName, throwIfNotFound: true);
        attackAction = map.FindAction(attackActionName, throwIfNotFound: true);
    }

    private void OnEnable() => attackAction.Enable();
    private void OnDisable() => attackAction.Disable();

    private void Update()
    {
        accuracyCalculator.Tick(aimController.AimDirection, Time.deltaTime);

        if (attackAction.IsPressed() && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + 1f / fireRate;
            Fire();
        }

        if (debugDraw) DrawDebugCone();
    }

    private void Fire()
    {
        Vector3 origin = muzzle.position;
        Vector3 shotDirection = BallisticsUtility.ApplyConeSpread(aimController.AimDirection, CurrentSpreadAngle);

        if (Physics.Raycast(origin, shotDirection, out RaycastHit hit, maxRange, hitMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.TryGetComponent(out IDamageable damageable))
            {
                damageable.ApplyDamage(damage, hit.point, hit.normal);
            }

            if (debugDraw) Debug.DrawLine(origin, hit.point, Color.green, debugShotTraceDuration);
            ShotHit?.Invoke(hit);
        }
        else
        {
            if (debugDraw) Debug.DrawRay(origin, shotDirection * maxRange, Color.red, debugShotTraceDuration);
            ShotMissed?.Invoke(origin, shotDirection);
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

        GUI.Label(new Rect(10, 10, 320, 90),
            $"Spread: {CurrentSpreadAngle:F1}°\nSteady aim: {SteadyProgress01:P0}\nInjury: {injury}\nPsychosis: {psychosis}");
    }
}
