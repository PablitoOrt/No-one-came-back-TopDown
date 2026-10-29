using UnityEngine;

public interface IPushConstraint
{
    Vector3 Constrain(in PushContext context, Vector3 delta);
}
