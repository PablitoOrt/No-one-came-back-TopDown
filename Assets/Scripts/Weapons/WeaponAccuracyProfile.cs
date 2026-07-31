using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Weapon Accuracy Profile", fileName = "NewWeaponAccuracyProfile")]
public class WeaponAccuracyProfile : ScriptableObject
{
    [Header("Base Spread (hip-fire, freshly aimed)")]
    [Tooltip("Cone half-angle, in degrees, applied to a shot the instant aiming starts.")]
    [SerializeField] private float baseSpreadAngle = 8f;

    [Tooltip("Best possible cone half-angle once aim is fully steadied, uninjured and calm.")]
    [SerializeField] private float minSpreadAngle = 0.5f;

    [Header("Steady Aim")]
    [Tooltip("Seconds of holding aim needed to reach minSpreadAngle. Climbs purely over time while aiming - moving around does not slow or reset it.")]
    [SerializeField] private float steadyAimDuration = 1.5f;

    [Header("Injury Penalty")]
    [Tooltip("Extra spread multiplier applied at maximum injury (0 health). 1 = spread doubles.")]
    [SerializeField] private float maxInjurySpreadMultiplier = 1.5f;

    [Header("Psychosis")]
    [Tooltip("Extra hip-fire spread multiplier applied at maximum psychosis.")]
    [SerializeField] private float maxPsychosisSpreadMultiplier = 1f;
    [Tooltip("At maximum psychosis, how much of the steady-aim benefit is lost (1 = steady aim gives no benefit at all).")]
    [SerializeField, Range(0f, 1f)] private float maxPsychosisSteadyLock = 1f;

    [Header("Aim Assist Bonus")]
    [Tooltip("While the aim-assist system has a target locked, steady-aim progress climbs directly at this rate (relative to steadyAimDuration) instead of being measured from aim-direction deltas - so tracking a moving target/moving yourself while locked on still reaches full accuracy. 1 = same pace as holding a perfectly still manual aim, higher = faster.")]
    [SerializeField] private float assistedSteadyRateMultiplier = 2f;

    public float BaseSpreadAngle => baseSpreadAngle;
    public float MinSpreadAngle => minSpreadAngle;
    public float SteadyAimDuration => steadyAimDuration;
    public float MaxInjurySpreadMultiplier => maxInjurySpreadMultiplier;
    public float MaxPsychosisSpreadMultiplier => maxPsychosisSpreadMultiplier;
    public float MaxPsychosisSteadyLock => maxPsychosisSteadyLock;
    public float AssistedSteadyRateMultiplier => assistedSteadyRateMultiplier;
}
