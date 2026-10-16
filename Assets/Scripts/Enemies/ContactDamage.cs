using UnityEngine;

public class ContactDamage : MonoBehaviour
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private float radius = 1f;
    [SerializeField] private float cooldown = 1f;
    [Tooltip("Layer(s) victims live on - usually just Player, so enemies don't hurt each other.")]
    [SerializeField] private LayerMask targetMask;

    private static readonly Collider[] OverlapBuffer = new Collider[8];

    private Health ownHealth;
    private float nextHitTime;

    private void Reset()
    {
        targetMask = LayerMask.GetMask("Player");
    }

    private void Awake()
    {
        TryGetComponent(out ownHealth);
    }

    private void Update()
    {
        if (Time.time < nextHitTime || (ownHealth != null && ownHealth.IsDead)) return;

        int count = Physics.OverlapSphereNonAlloc(transform.position, radius, OverlapBuffer, targetMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Collider other = OverlapBuffer[i];
            if (other.transform.IsChildOf(transform)) continue;

            IDamageable victim = other.GetComponentInParent<IDamageable>();
            if (victim == null) continue;

            Vector3 point = other.ClosestPoint(transform.position);
            victim.ApplyDamage(new DamageInfo(damage, point, (point - transform.position).normalized, DamageType.Melee, gameObject));
            nextHitTime = Time.time + cooldown;
            return;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
