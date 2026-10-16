using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
public class PlayerInputReader : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string actionMapName = "Player";

    [Header("Action Names")]
    [SerializeField] private string moveActionName = "Move";
    [SerializeField] private string lookActionName = "Look";
    [SerializeField] private string aimPositionActionName = "AimPosition";
    [SerializeField] private string aimActionName = "Aim";
    [SerializeField] private string attackActionName = "Attack";
    [SerializeField] private string sprintActionName = "Sprint";
    [SerializeField] private string interactActionName = "Interact";
    [Tooltip("Optional - reload is simply unavailable if the action doesn't exist.")]
    [SerializeField] private string reloadActionName = "Reload";
    [Tooltip("Optional.")]
    [SerializeField] private string nextWeaponActionName = "Next";
    [Tooltip("Optional.")]
    [SerializeField] private string previousWeaponActionName = "Previous";

    private InputActionMap map;
    private InputAction move;
    private InputAction look;
    private InputAction aimPosition;
    private InputAction aim;
    private InputAction attack;
    private InputAction sprint;
    private InputAction interact;
    private InputAction reload;
    private InputAction nextWeapon;
    private InputAction previousWeapon;

    public Vector2 Move => move.ReadValue<Vector2>();
    public Vector2 Look => look.ReadValue<Vector2>();
    public InputDevice LookDevice => look.activeControl?.device;
    public Vector2 AimPosition => aimPosition.ReadValue<Vector2>();

    public bool AimHeld => aim.IsPressed();
    public bool SprintHeld => sprint.IsPressed();
    public bool AttackHeld => attack.IsPressed();
    public bool AttackPressed => attack.WasPressedThisFrame();
    public bool InteractPerformed => interact.WasPerformedThisFrame();
    public bool ReloadPressed => reload != null && reload.WasPressedThisFrame();
    public bool NextWeaponPressed => nextWeapon != null && nextWeapon.WasPressedThisFrame();
    public bool PreviousWeaponPressed => previousWeapon != null && previousWeapon.WasPressedThisFrame();

    private void Awake()
    {
        map = inputActions.FindActionMap(actionMapName, throwIfNotFound: true);

        move = map.FindAction(moveActionName, throwIfNotFound: true);
        look = map.FindAction(lookActionName, throwIfNotFound: true);
        aimPosition = map.FindAction(aimPositionActionName, throwIfNotFound: true);
        aim = map.FindAction(aimActionName, throwIfNotFound: true);
        attack = map.FindAction(attackActionName, throwIfNotFound: true);
        sprint = map.FindAction(sprintActionName, throwIfNotFound: true);
        interact = map.FindAction(interactActionName, throwIfNotFound: true);
        reload = map.FindAction(reloadActionName);
        nextWeapon = map.FindAction(nextWeaponActionName);
        previousWeapon = map.FindAction(previousWeaponActionName);
    }

    private void OnEnable() => map.Enable();
    private void OnDisable() => map.Disable();
}
