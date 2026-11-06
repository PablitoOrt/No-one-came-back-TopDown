using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Left empty, auto-resolved from this object or its parents.")]
    [SerializeField] private PlayerInputReader input;

    [Header("Detection")]
    [Tooltip("Trigger BoxCollider on the interact origin; its shape defines the detection area.")]
    [SerializeField] private BoxCollider interactArea;
    [SerializeField] private LayerMask interactableMask;
    [Tooltip("Seconds between scans for nearby interactables.")]
    [SerializeField] private float scanInterval = 0.1f;

    [Header("Debug")]
    [SerializeField] private bool debugDraw = true;

    private static readonly Collider[] OverlapBuffer = new Collider[16];

    private IPlayerActionBlocker[] blockers;
    private float nextScanTime;

    public IInteractable NearestInteractable { get; private set; }

    private void Awake()
    {
        if (input == null) input = GetComponentInParent<PlayerInputReader>();
        blockers = GetComponentsInParent<IPlayerActionBlocker>(true);
    }

    private void Update()
    {
        if (IsBlocked())
        {
            NearestInteractable = null;
            return;
        }

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

    private bool IsBlocked()
    {
        foreach (IPlayerActionBlocker blocker in blockers)
        {
            if (blocker.BlocksInteraction) return true;
        }
        return false;
    }

    private IInteractable FindNearestInteractable()
    {
        if (interactArea == null || !interactArea.enabled) return null;

        Transform area = interactArea.transform;
        Vector3 origin = area.TransformPoint(interactArea.center);
        Vector3 halfExtents = Vector3.Scale(interactArea.size, Abs(area.lossyScale)) * 0.5f;
        int count = Physics.OverlapBoxNonAlloc(origin, halfExtents, OverlapBuffer, area.rotation, interactableMask, QueryTriggerInteraction.Collide);

        IInteractable nearest = null;
        float nearestSqrDistance = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            if (OverlapBuffer[i] == interactArea) continue;
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

    private static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

    private void OnDrawGizmosSelected()
    {
        if (!debugDraw || interactArea == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.matrix = interactArea.transform.localToWorldMatrix;
        Gizmos.DrawWireCube(interactArea.center, interactArea.size);
        Gizmos.matrix = Matrix4x4.identity;
    }

    // UI debug
    private void OnGUI()
    {
        if (!debugDraw || NearestInteractable == null) return;

        GUI.Label(new Rect(10, Screen.height - 40, 400, 30), NearestInteractable.InteractPrompt);
    }
}
