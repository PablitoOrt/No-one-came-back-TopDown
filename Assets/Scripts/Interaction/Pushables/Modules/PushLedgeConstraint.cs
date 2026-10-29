using UnityEngine;

[RequireComponent(typeof(PushableObject))]
public class PushLedgeConstraint : MonoBehaviour, IPushConstraint
{
    public enum SupportRule
    {
        Center,
        AllCorners
    }

    [Tooltip("Center: stops when the object's center would leave the ground. AllCorners: stops at the very edge.")]
    [SerializeField] private SupportRule rule = SupportRule.Center;
    [SerializeField] private LayerMask groundMask = ~0;
    [Tooltip("Max drop (m) below the object's base still counted as ground.")]
    [SerializeField, Min(0f)] private float maxDrop = 0.2f;
    [Tooltip("Inset (m) applied to the sampled corners.")]
    [SerializeField, Min(0f)] private float cornerInset = 0.05f;

    private const float RayLift = 0.1f;

    private static readonly RaycastHit[] HitBuffer = new RaycastHit[8];

    public Vector3 Constrain(in PushContext context, Vector3 delta)
    {
        if (delta.sqrMagnitude < 0.00000001f) return delta;

        Bounds bounds = context.Pushable.WorldBounds;
        bounds.center += delta;
        return IsSupported(bounds, context.Pushable) ? delta : Vector3.zero;
    }

    private bool IsSupported(Bounds bounds, PushableObject pushable)
    {
        Vector3 bottom = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        if (rule == SupportRule.Center) return HasGround(bottom, pushable);

        float x = Mathf.Max(0f, bounds.extents.x - cornerInset);
        float z = Mathf.Max(0f, bounds.extents.z - cornerInset);

        return HasGround(bottom + new Vector3(x, 0f, z), pushable)
            && HasGround(bottom + new Vector3(-x, 0f, z), pushable)
            && HasGround(bottom + new Vector3(x, 0f, -z), pushable)
            && HasGround(bottom + new Vector3(-x, 0f, -z), pushable);
    }

    private bool HasGround(Vector3 point, PushableObject pushable)
    {
        int count = Physics.RaycastNonAlloc(point + Vector3.up * RayLift, Vector3.down, HitBuffer, RayLift + maxDrop, groundMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider hit = HitBuffer[i].collider;
            if (pushable.Owns(hit)) continue;
            if (pushable.Pusher != null && pushable.Pusher.Owns(hit)) continue;
            return true;
        }

        return false;
    }
}
