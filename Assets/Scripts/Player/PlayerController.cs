using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3.5f;
    [SerializeField] private float sprintSpeed = 6f;
    [Tooltip("Degrees/second the body turns to face the aim direction.")]
    [SerializeField] private float rotationSpeed = 720f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -20f;
    [Tooltip("Small downward speed kept while grounded so the controller stays snapped to slopes/steps.")]
    [SerializeField] private float groundedStickSpeed = -2f;

    [Header("References")]
    [Tooltip("Left empty, auto-resolved from this object or its parents.")]
    [SerializeField] private PlayerInputReader input;
    [SerializeField] private Camera movementCamera;
    [SerializeField] private PlayerAimController aimController;

    [Header("Debug")]
    [SerializeField] private bool debugDraw = true;

    private CharacterController controller;
    private IPlayerMovementModifier[] modifiers;
    private Vector3 moveDirection;
    private float verticalVelocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        modifiers = GetComponentsInChildren<IPlayerMovementModifier>(true);

        if (input == null) input = GetComponentInParent<PlayerInputReader>();
        if (movementCamera == null) movementCamera = Camera.main;
    }

    private void Update()
    {
        moveDirection = CameraRelativeDirection(input.Move);

        Move();
        Rotate();

        if (debugDraw)
        {
            Debug.DrawRay(transform.position, moveDirection * 2f, Color.blue);
            Debug.DrawRay(transform.position, transform.forward * 2f, Color.magenta);
        }
    }

    private void Move()
    {
        bool canSprint = true;
        float speedMultiplier = 1f;
        foreach (IPlayerMovementModifier modifier in modifiers)
        {
            if (!modifier.IsActive) continue;
            canSprint &= modifier.AllowsSprint;
            speedMultiplier *= modifier.SpeedMultiplier;
        }

        float speed = (input.SprintHeld && canSprint ? sprintSpeed : walkSpeed) * speedMultiplier;

        verticalVelocity = controller.isGrounded
            ? groundedStickSpeed
            : verticalVelocity + gravity * Time.deltaTime;

        Vector3 horizontalDelta = moveDirection * speed * Time.deltaTime;
        foreach (IPlayerMovementModifier modifier in modifiers)
        {
            if (modifier.IsActive) horizontalDelta = modifier.ModifyMove(horizontalDelta);
        }

        Vector3 delta = horizontalDelta;
        delta.y = verticalVelocity * Time.deltaTime;

        Vector3 previousPosition = transform.position;
        controller.Move(delta);
        Vector3 actualDelta = transform.position - previousPosition;

        foreach (IPlayerMovementModifier modifier in modifiers)
        {
            if (modifier.IsActive) modifier.OnMoved(actualDelta);
        }
    }

    private void Rotate()
    {
        Vector3 lookDirection;
        if (TryGetModifierFacing(out Vector3 facing)) lookDirection = facing;
        else if (aimController != null) lookDirection = aimController.AimDirection;
        else return;

        lookDirection.y = 0f;
        if (lookDirection.sqrMagnitude < 0.0001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private bool TryGetModifierFacing(out Vector3 direction)
    {
        foreach (IPlayerMovementModifier modifier in modifiers)
        {
            if (modifier.IsActive && modifier.TryGetFacing(out direction)) return true;
        }

        direction = Vector3.zero;
        return false;
    }

    private Vector3 CameraRelativeDirection(Vector2 input)
    {
        if (movementCamera == null) return new Vector3(input.x, 0f, input.y);

        Vector3 camForward = Vector3.ProjectOnPlane(movementCamera.transform.forward, Vector3.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(movementCamera.transform.right, Vector3.up).normalized;
        Vector3 direction = camRight * input.x + camForward * input.y;

        return direction.sqrMagnitude > 1f ? direction.normalized : direction;
    }
}
