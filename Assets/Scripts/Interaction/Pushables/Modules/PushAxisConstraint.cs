using UnityEngine;

[RequireComponent(typeof(PushableObject))]
public class PushAxisConstraint : MonoBehaviour, IPushConstraint
{
    public enum AxisMode
    {
        GrabAxis,
        LocalCardinal,
        WorldCardinal
    }

    [Tooltip("GrabAxis: only along the face that was grabbed. LocalCardinal: object's own X/Z. WorldCardinal: world X/Z.")]
    [SerializeField] private AxisMode mode = AxisMode.GrabAxis;

    public Vector3 Constrain(in PushContext context, Vector3 delta)
    {
        switch (mode)
        {
            case AxisMode.GrabAxis:
                return Vector3.Project(delta, context.PushAxis);
            case AxisMode.LocalCardinal:
                return DominantAxis(delta, Flatten(transform.right), Flatten(transform.forward));
            default:
                return DominantAxis(delta, Vector3.right, Vector3.forward);
        }
    }

    private static Vector3 DominantAxis(Vector3 delta, Vector3 a, Vector3 b)
    {
        Vector3 alongA = Vector3.Project(delta, a);
        Vector3 alongB = Vector3.Project(delta, b);
        return alongA.sqrMagnitude >= alongB.sqrMagnitude ? alongA : alongB;
    }

    private static Vector3 Flatten(Vector3 vector) => Vector3.ProjectOnPlane(vector, Vector3.up).normalized;
}
