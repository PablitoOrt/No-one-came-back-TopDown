using UnityEngine;

[RequireComponent(typeof(PushableObject))]
public class PushDirectionConstraint : MonoBehaviour, IPushConstraint
{
    [SerializeField] private bool allowPush = true;
    [SerializeField] private bool allowPull = false;

    public Vector3 Constrain(in PushContext context, Vector3 delta)
    {
        float along = Vector3.Dot(delta, context.PushAxis);
        bool allowed = along >= 0f ? allowPush : allowPull;
        return allowed ? delta : delta - context.PushAxis * along;
    }
}
