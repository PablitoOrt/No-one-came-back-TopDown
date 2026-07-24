using UnityEngine;

// Per-weapon tuning data for the accuracy/spread system. Designers can create
// one asset per weapon (pistol, shotgun, ...) without touching any code.
[CreateAssetMenu(menuName = "Weapons/Weapon Accuracy Profile", fileName = "NewWeaponAccuracyProfile")]
public class WeaponAccuracyProfile : ScriptableObject
{
    [Header("Base Spread (hip-fire, freshly aimed)")]
    [Tooltip("Cone half-angle, in degrees, applied to a shot the instant aiming starts.")]
    [SerializeField] private float baseSpreadAngle = 8f;

    [Tooltip("Best possible cone half-angle once aim is fully steadied, uninjured and calm.")]
    [SerializeField] private float minSpreadAngle = 0.5f;

    [Header("Steady Aim")]
    [Tooltip("Seconds of holding a steady aim needed to reach minSpreadAngle.")]
    [SerializeField] private float steadyAimDuration = 1.5f;
    [Tooltip("Aim movement (degrees/second) still tolerated while counting as 'steady'.")]
    [SerializeField] private float steadyTolerancePerSecond = 25f;
    [Tooltip("A single-frame aim jump larger than this instantly breaks steadiness.")]
    [SerializeField] private float steadyBreakAngle = 20f;

    [Header("Injury Penalty")]
    [Tooltip("Extra spread multiplier applied at maximum injury (0 health). 1 = spread doubles.")]
    [SerializeField] private float maxInjurySpreadMultiplier = 1.5f;

    [Header("Psychosis")]
    [Tooltip("Extra hip-fire spread multiplier applied at maximum psychosis.")]
    [SerializeField] private float maxPsychosisSpreadMultiplier = 1f;
    [Tooltip("At maximum psychosis, how much of the steady-aim benefit is lost (1 = steady aim gives no benefit at all).")]
    [SerializeField, Range(0f, 1f)] private float maxPsychosisSteadyLock = 1f;

    public float BaseSpreadAngle => baseSpreadAngle;
    public float MinSpreadAngle => minSpreadAngle;
    public float SteadyAimDuration => steadyAimDuration;
    public float SteadyTolerancePerSecond => steadyTolerancePerSecond;
    public float SteadyBreakAngle => steadyBreakAngle;
    public float MaxInjurySpreadMultiplier => maxInjurySpreadMultiplier;
    public float MaxPsychosisSpreadMultiplier => maxPsychosisSpreadMultiplier;
    public float MaxPsychosisSteadyLock => maxPsychosisSteadyLock;
}
