using UnityEngine;

// Tracks how steadily a weapon is being aimed and derives the current
// spread cone (in degrees) from a WeaponAccuracyProfile plus the wielder's
// injury and psychosis levels. Plain C# (not a MonoBehaviour) so it can be
// unit tested and reused by any weapon without extra GameObject overhead.
public class WeaponAccuracyCalculator
{
    private readonly WeaponAccuracyProfile profile;

    private float steadyTimer;
    private Vector3 lastAimDirection;
    private bool hasLastAimDirection;

    public WeaponAccuracyCalculator(WeaponAccuracyProfile profile)
    {
        this.profile = profile;
    }

    public float SteadyProgress01 => profile.SteadyAimDuration > 0f
        ? Mathf.Clamp01(steadyTimer / profile.SteadyAimDuration)
        : 1f;

    // Call once per frame with the current aim direction to update steadiness.
    // steadyToleranceMultiplier scales how forgiving the steadiness check is -
    // used by the aim-assist system so tracking a moving locked-on target keeps
    // counting as steady instead of resetting every time the target shifts.
    public void Tick(Vector3 aimDirection, float deltaTime, float steadyToleranceMultiplier = 1f)
    {
        if (hasLastAimDirection)
        {
            float angleDelta = Vector3.Angle(lastAimDirection, aimDirection);
            float toleratedDelta = profile.SteadyTolerancePerSecond * deltaTime * steadyToleranceMultiplier;
            float breakAngle = profile.SteadyBreakAngle * steadyToleranceMultiplier;

            if (angleDelta > breakAngle)
            {
                steadyTimer = 0f;
            }
            else if (angleDelta > toleratedDelta)
            {
                // A deliberate correction slows progress instead of resetting it outright.
                steadyTimer = Mathf.Max(0f, steadyTimer - deltaTime);
            }
            else
            {
                steadyTimer += deltaTime;
            }
        }

        lastAimDirection = aimDirection;
        hasLastAimDirection = true;
    }

    public void Reset()
    {
        steadyTimer = 0f;
        hasLastAimDirection = false;
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
