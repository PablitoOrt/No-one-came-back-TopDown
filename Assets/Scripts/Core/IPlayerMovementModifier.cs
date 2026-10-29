using UnityEngine;

public interface IPlayerMovementModifier
{
    bool IsActive { get; }

    bool AllowsSprint { get; }

    float SpeedMultiplier { get; }

    Vector3 ModifyMove(Vector3 horizontalDelta);

    void OnMoved(Vector3 actualDelta);

    bool TryGetFacing(out Vector3 direction);
}
