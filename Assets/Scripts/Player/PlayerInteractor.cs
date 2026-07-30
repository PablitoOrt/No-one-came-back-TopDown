using UnityEngine;
using UnityEngine.InputSystem;

// Finds the nearest IInteractable in range and triggers it on the existing
// "Interact" input action (configured as a Hold interaction in the Input
// System asset, so WasPerformedThisFrame fires once the hold completes).
public class PlayerInteractor : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string actionMapName = "Player";
    [SerializeField] private string interactActionName = "Interact";

    [Header("Detection")]
    [SerializeField] private Transform interactOrigin;
    [SerializeField] private float interactRadius = 1.5f;
    [SerializeField] private LayerMask interactableMask;
    [Tooltip("Seconds between scans for nearby interactables.")]
    [SerializeField] private float scanInterval = 0.1f;

    [Header("Debug")]
    [SerializeField] private bool debugDraw = true;

    private static readonly Collider[] OverlapBuffer = new Collider[16];

    private InputAction interactAction;
    private float nextScanTime;

    public IInteractable NearestInteractable { get; private set; }

    private void Awake()
    {
        InputActionMap map = inputActions.FindActionMap(actionMapName, throwIfNotFound: true);
        interactAction = map.FindAction(interactActionName, throwIfNotFound: true);
    }

    private void OnEnable() => interactAction.Enable();
    private void OnDisable() => interactAction.Disable();

    private void Update()
    {
        if (Time.time >= nextScanTime)
        {
            nextScanTime = Time.time + scanInterval;
            NearestInteractable = FindNearestInteractable();
        }

        if (NearestInteractable != null && interactAction.WasPerformedThisFrame() && NearestInteractable.CanInteract(gameObject))
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

    private void OnGUI()
    {
        if (!debugDraw || NearestInteractable == null) return;

        GUI.Label(new Rect(10, Screen.height - 40, 400, 30), NearestInteractable.InteractPrompt);
    }
}
