using UnityEngine;

public static class BallisticsUtility
{
    // Deviates 'direction' by a random angle within a cone of the given half-angle
    // (degrees). Uses a center-weighted (triangular) distribution so most shots land
    // close to the aimed direction while a minority reach the full spread - this is
    // what lets some bullets miss even when the player is "aiming correctly".
    public static Vector3 ApplyConeSpread(Vector3 direction, float spreadAngleDegrees)
    {
        if (spreadAngleDegrees <= 0f || direction.sqrMagnitude < 0.0001f)
        {
            return direction.normalized;
        }

        float angle = (Random.value + Random.value) * 0.5f * spreadAngleDegrees;
        float spin = Random.Range(0f, 360f);
        return TiltDirection(direction, angle, spin);
    }

    // Deterministic version of the tilt used by ApplyConeSpread: rotates 'direction' by
    // exactly 'angleDegrees' around an axis that itself spins 'spinDegrees' around
    // 'direction'. Reused by debug drawers to trace the exact cone a weapon can fire into.
    public static Vector3 TiltDirection(Vector3 direction, float angleDegrees, float spinDegrees)
    {
        Vector3 forward = direction.normalized;
        Vector3 perpendicular = Vector3.Cross(forward, Vector3.up);
        if (perpendicular.sqrMagnitude < 0.0001f)
        {
            perpendicular = Vector3.Cross(forward, Vector3.right);
        }
        perpendicular.Normalize();

        Vector3 tiltAxis = Quaternion.AngleAxis(spinDegrees, forward) * perpendicular;
        return Quaternion.AngleAxis(angleDegrees, tiltAxis) * forward;
    }
}
