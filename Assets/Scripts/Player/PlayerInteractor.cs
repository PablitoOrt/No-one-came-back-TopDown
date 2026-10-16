using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Left empty, auto-resolved from this object or its parents.")]
    [SerializeField] private PlayerInputReader input;

    [Header("Detection")]
    [SerializeField] private Transform interactOrigin;
    [SerializeField] private float interactRadius = 1.5f;
    [SerializeField] private LayerMask interactableMask;
    [Tooltip("Seconds between scans for nearby interactables.")]
    [SerializeField] private float scanInterval = 0.1f;

    [Header("Debug")]
    [SerializeField] private bool debugDraw = true;

    private static readonly Collider[] OverlapBuffer = new Collider[16];

    private float nextScanTime;

    public IInteractable NearestInteractable { get; private set; }

    private void Awake()
    {
        if (input == null) input = GetComponentInParent<PlayerInputReader>();
    }

    private void Update()
    {
        if (Time.time >= nextScanTime)
        {
            nextScanTime = Time.time + scanInterval;
            NearestInteractable = FindNearestInteractable();
        }

        if (NearestInteractable != null && input.InteractPerformed && NearestInteractable.CanInteract(gameObject))
        {
            NearestInteractable.Interact(gameObject);
            NearestInteractable = null;
        }
    }

    private IInteractable FindNearestInteractable()
    {
        Vector3 origin = interactOrigin.position;
        int count = Physics.OverlapSphereNonAlloc(origin, interactRadius, OverlapBuffer, interactableMask, QueryTriggerInteraction.Collide);

        IInteractable nearest = null;
        float nearestSqrDistance = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            if (!OverlapBuffer[i].TryGetComponent(out IInteractable interactable)) continue;
            if (!interactable.CanInteract(gameObject)) continue;

            float sqrDistance = (OverlapBuffer[i].transform.position - origin).sqrMagnitude;
            if (sqrDistance < nearestSqrDistance)
            {
                nearestSqrDistance = sqrDistance;
                nearest = interactable;
            }
        }

        return nearest;
    }

    private void OnDrawGizmosSelected()
    {
        if (!debugDraw || interactOrigin == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(interactOrigin.position, interactRadius);
    }

    // UI debug
    private void OnGUI()
    {
        if (!debugDraw || NearestInteractable == null) return;

        GUI.Label(new Rect(10, Screen.height - 40, 400, 30), NearestInteractable.InteractPrompt);
    }
}
