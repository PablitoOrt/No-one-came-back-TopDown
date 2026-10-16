using UnityEngine;

public enum DamageType
{
    Generic,
    Melee,
    Ballistic,
}

public readonly struct DamageInfo
{
    public readonly float Amount;
    public readonly Vector3 Point;
    public readonly Vector3 Normal;
    public readonly DamageType Type;
    public readonly GameObject Source;

    public DamageInfo(float amount, Vector3 point, Vector3 normal, DamageType type = DamageType.Generic, GameObject source = null)
    {
        Amount = amount;
        Point = point;
        Normal = normal;
        Type = type;
        Source = source;
    }
}
