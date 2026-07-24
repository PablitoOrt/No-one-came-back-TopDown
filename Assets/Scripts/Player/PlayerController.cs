using UnityEngine;
using UnityEngine.InputSystem;

// Free (non-tank) top-down movement: the player moves in any camera-relative
// direction while the body rotates independently to face PlayerAimController's
// AimDirection, matching Signalis-style "move one way, shoot another".
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string actionMapName = "Player";
    [SerializeField] private string moveActionName = "Move";
    [SerializeField] private string sprintActionName = "Sprint";

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
    [SerializeField] private Camera movementCamera;
    [SerializeField] private PlayerAimController aimController;

    [Header("Debug")]
    [SerializeField] private bool debugDraw = true;

    private CharacterController controller;
    private InputAction moveAction;
    private InputAction sprintAction;
    private Vector3 moveDirection;
    private float verticalVelocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (movementCamera == null) movementCamera = Camera.main;

        InputActionMap map = inputActions.FindActionMap(actionMapName, throwIfNotFound: true);
        moveAction = map.FindAction(moveActionName, throwIfNotFound: true);
        sprintAction = map.FindAction(sprintActionName, throwIfNotFound: true);
    }

    private void OnEnable()
    {
        moveAction.Enable();
        sprintAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        sprintAction.Disable();
    }

    private void Update()
    {
        moveDirection = CameraRelativeDirection(moveAction.ReadValue<Vector2>());

        Move();
        RotateTowardsAim();

        if (debugDraw)
        {
            Debug.DrawRay(transform.position, moveDirection * 2f, Color.blue);
            Debug.DrawRay(transform.position, transform.forward * 2f, Color.magenta);
        }
    }

    private void Move()
    {
        float speed = sprintAction.IsPressed() ? sprintSpeed : walkSpeed;

        verticalVelocity = controller.isGrounded
            ? groundedStickSpeed
            : verticalVelocity + gravity * Time.deltaTime;

        Vector3 velocity = moveDirection * speed;
        velocity.y = verticalVelocity;

        controller.Move(velocity * Time.deltaTime);
    }

    private void RotateTowardsAim()
    {
        if (aimController == null) return;

        Vector3 lookDirection = aimController.AimDirection;
        lookDirection.y = 0f;
        if (lookDirection.sqrMagnitude < 0.0001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
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
