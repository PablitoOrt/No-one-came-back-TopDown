using UnityEngine;

public static class BallisticsUtility
{
    // Center-weighted (triangular) distribution, not uniform, so most shots
    // land near the aimed direction while a minority reach the full spread.
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

    // Deterministic tilt, reused by debug drawers to trace the exact cone.
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
