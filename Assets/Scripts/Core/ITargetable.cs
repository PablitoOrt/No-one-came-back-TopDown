using UnityEngine;

public interface ITargetable
{
    bool IsDead { get; }

    Transform AimAnchor { get; }
}
