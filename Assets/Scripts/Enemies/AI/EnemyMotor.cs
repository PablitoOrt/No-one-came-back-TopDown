using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMotor : MonoBehaviour
{
    private const int FleeSamples = 8;
    private const float FleeSpreadAngle = 120f;

    [Tooltip("Degrees/second used when turning in place (attacking, idling toward a sound).")]
    [SerializeField] private float turnSpeed = 540f;
    [Tooltip("Distance at which a destination counts as reached, on top of the agent's stopping distance.")]
    [SerializeField] private float arriveDistance = 0.3f;

    private NavMeshAgent agent;
    private NavMeshPath scratchPath;

    public NavMeshAgent Agent => agent;
    public bool IsReady => agent != null && agent.enabled && agent.isOnNavMesh;

    public bool HasArrived =>
        IsReady
        && !agent.pathPending
        && agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, arriveDistance);

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        scratchPath = new NavMeshPath();

        if (TryGetComponent(out Rigidbody body)) body.isKinematic = true;
    }

    public bool MoveTo(Vector3 destination, float speed)
    {
        if (!IsReady) return false;

        agent.speed = speed;
        agent.isStopped = false;
        return agent.SetDestination(destination);
    }

    public void Stop()
    {
        if (!IsReady) return;

        agent.isStopped = true;
        agent.ResetPath();
    }

    public void FaceTowards(Vector3 point, float deltaTime)
    {
        Vector3 direction = point - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        Quaternion target = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * deltaTime);
    }

    public bool TryGetRandomPoint(Vector3 center, float radius, out Vector3 point)
    {
        for (int i = 0; i < 4; i++)
        {
            Vector2 offset = Random.insideUnitCircle * radius;
            Vector3 candidate = center + new Vector3(offset.x, 0f, offset.y);
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, radius, AreaMask))
            {
                point = hit.position;
                return true;
            }
        }

        point = transform.position;
        return false;
    }

    public bool TryGetPointAwayFrom(Vector3 threat, float distance, out Vector3 point)
    {
        Vector3 origin = transform.position;
        Vector3 away = origin - threat;
        away.y = 0f;
        away = away.sqrMagnitude > 0.0001f ? away.normalized : -transform.forward;

        bool found = false;
        float bestScore = float.MinValue;
        point = origin;

        for (int i = 0; i < FleeSamples; i++)
        {
            float angle = i == 0 ? 0f : Random.Range(-FleeSpreadAngle, FleeSpreadAngle);
            Vector3 candidate = origin + Quaternion.Euler(0f, angle, 0f) * away * distance;

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, distance * 0.5f, AreaMask)) continue;
            if (!NavMesh.CalculatePath(origin, hit.position, AreaMask, scratchPath)
                || scratchPath.status != NavMeshPathStatus.PathComplete) continue;

            float score = FlatDistance(hit.position, threat);
            if (score <= bestScore) continue;

            bestScore = score;
            point = hit.position;
            found = true;
        }

        return found;
    }

    private int AreaMask => agent != null ? agent.areaMask : NavMesh.AllAreas;

    public static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
