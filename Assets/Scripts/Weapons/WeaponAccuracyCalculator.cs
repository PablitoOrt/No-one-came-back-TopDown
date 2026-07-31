using UnityEngine;

// Plain C# (not a MonoBehaviour) so it can be unit tested and reused freely.
public class WeaponAccuracyCalculator
{
    private readonly WeaponAccuracyProfile profile;

    private float steadyTimer;

    public WeaponAccuracyCalculator(WeaponAccuracyProfile profile)
    {
        this.profile = profile;
    }

    public float SteadyProgress01 => profile.SteadyAimDuration > 0f
        ? Mathf.Clamp01(steadyTimer / profile.SteadyAimDuration)
        : 1f;

    // Call once per frame while actively aiming - progress only ever climbs,
    // never drops from movement; injury/psychosis widen spread separately below.
    public void Tick(float deltaTime, bool isAssistedLock = false)
    {
        float rate = isAssistedLock ? profile.AssistedSteadyRateMultiplier : 1f;
        steadyTimer = Mathf.Min(profile.SteadyAimDuration, steadyTimer + deltaTime * rate);
    }

    public void Reset()
    {
        steadyTimer = 0f;
    }

    // Current cone half-angle (degrees) a shot should be spread within.
    public float GetCurrentSpreadAngle(float injuryFactor01, float psychosisFactor01)
    {
        injuryFactor01 = Mathf.Clamp01(injuryFactor01);
        psychosisFactor01 = Mathf.Clamp01(psychosisFactor01);

        float injuryMultiplier = 1f + injuryFactor01 * profile.MaxInjurySpreadMultiplier;

        float hipFireSpread = profile.BaseSpreadAngle
            * injuryMultiplier
            * (1f + psychosisFactor01 * profile.MaxPsychosisSpreadMultiplier);

        // Injury still shakes the wielder's hands even while fully steadied.
        float steadiedSpread = profile.MinSpreadAngle * injuryMultiplier;

        // The more psychotic the wielder, the closer the best achievable spread
        // sits to the unsteadied hip-fire spread - i.e. steady aim stops helping.
        float psychosisLockedFloor = Mathf.Lerp(
            steadiedSpread,
            hipFireSpread,
            psychosisFactor01 * profile.MaxPsychosisSteadyLock);

        return Mathf.Lerp(hipFireSpread, psychosisLockedFloor, SteadyProgress01);
    }
}
