using System;
using UnityEngine;

[Serializable]
public class LowHealthFleeRule
{
    [Tooltip("Flee is only considered at or below this many hit points.")]
    public float healthThreshold = 25f;
    [Tooltip("Chance (0-1), rolled on every hit taken at or below the threshold, to start fleeing instead of fighting on.")]
    [Range(0f, 1f)] public float fleeChance = 0.5f;

    public bool RollOnHit(Health health) =>
        !health.IsDead
        && health.CurrentHealth <= healthThreshold
        && UnityEngine.Random.value < fleeChance;
}
