using UnityEngine;

// Implemented by anything the aim-assist system can lock onto (enemies, ...).
// Kept separate from IDamageable so non-damageable objects could be targetable
// later, and so PlayerAimController never depends on a concrete enemy type.
public interface ITargetable
{
    bool IsDead { get; }

    // Point the aim assist should pull toward - usually a chest/center anchor
    // rather than the object's root, which is often at its feet.
    Transform AimAnchor { get; }
}
