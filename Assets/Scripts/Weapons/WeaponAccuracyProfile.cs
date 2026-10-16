using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Weapon Accuracy Profile", fileName = "NewWeaponAccuracyProfile")]
public class WeaponAccuracyProfile : ScriptableObject
{
    [Header("Spread")]
    [Tooltip("Cone half-angle, in degrees, the instant aiming starts.")]
    [SerializeField, Min(0f)] private float baseSpreadAngle = 8f;
    [Tooltip("Cone half-angle once aim is fully steadied. 0 = perfectly exact.")]
    [SerializeField, Min(0f)] private float minSpreadAngle = 0f;
    [Tooltip("Shape of the transition. X: steadiness (0..1). Y: how far from base (0) to min (1) spread. Full steadiness always gives exactly minSpreadAngle.")]
    [SerializeField] private AnimationCurve spreadOverSteadiness = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Steady Aim")]
    [Tooltip("Seconds of holding aim (at full sanity) needed to reach minSpreadAngle. Moving does not slow it.")]
    [SerializeField, Min(0f)] private float steadyAimDuration = 1.5f;
    [Tooltip("Fraction of steadiness kept after each shot. 0 = every shot restarts from base spread, 1 = no recoil.")]
    [SerializeField, Range(0f, 1f)] private float steadinessKeptAfterShot = 0f;

    [Header("Aim Assist Bonus")]
    [Tooltip("Steadiness climbs this many times faster while aim assist has a target locked.")]
    [SerializeField, Min(0f)] private float assistedSteadyRateMultiplier = 2f;

    public float BaseSpreadAngle => baseSpreadAngle;
    public float MinSpreadAngle => minSpreadAngle;
    public float SteadyAimDuration => steadyAimDuration;
    public float SteadinessKeptAfterShot => steadinessKeptAfterShot;
    public float AssistedSteadyRateMultiplier => assistedSteadyRateMultiplier;

    public float EvaluateSpread(float steadiness01)
    {
        if (steadiness01 >= 1f) return minSpreadAngle;
        return Mathf.Lerp(baseSpreadAngle, minSpreadAngle, spreadOverSteadiness.Evaluate(Mathf.Clamp01(steadiness01)));
    }
}
