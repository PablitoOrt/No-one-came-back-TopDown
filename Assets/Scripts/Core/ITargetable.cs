using UnityEngine;

public interface ITargetable
{
    bool IsDead { get; }

    // Chest/center point to aim at - not the object's root, often at its feet.
    Transform AimAnchor { get; }
}
