using UnityEngine;
using UnityEngine.InputSystem;

namespace NoOneCameBack.Tablet
{
    // Wraps the "Tablet" action map. Navigation is turned into discrete cell/option steps with key-repeat,
    // and Step.y grows downward to match grid rows and vertical menus.
    public sealed class TabletInput
    {
        private const float NavigationDeadZone = 0.5f;
        private const float PointerMoveThreshold = 0.5f;

        private readonly InputActionMap map;
        private readonly InputAction navigate;
        private readonly InputAction submit;
        private readonly InputAction cancel;
        private readonly InputAction close;
        private readonly InputAction system;
        private readonly InputAction nextApp;
        private readonly InputAction previousApp;
        private readonly InputAction point;
        private readonly InputAction click;
        private readonly InputAction rotate;
        private readonly InputAction itemMenu;
        private readonly InputAction switchPanel;

        private readonly float repeatDelay;
        private readonly float repeatInterval;

        private Vector2Int heldDirection;
        private float nextRepeatTime;
        private Vector2 lastPointerPosition;

        public TabletInput(InputActionMap map, float repeatDelay, float repeatInterval)
        {
            this.map = map;
            this.repeatDelay = repeatDelay;
            this.repeatInterval = repeatInterval;

            navigate = map.FindAction("Navigate", throwIfNotFound: true);
            submit = map.FindAction("Submit", throwIfNotFound: true);
            cancel = map.FindAction("Cancel", throwIfNotFound: true);
            close = map.FindAction("Close", throwIfNotFound: true);
            system = map.FindAction("System", throwIfNotFound: true);
            nextApp = map.FindAction("NextApp", throwIfNotFound: true);
            previousApp = map.FindAction("PreviousApp", throwIfNotFound: true);

            point = map.FindAction("Point");
            click = map.FindAction("Click");
            rotate = map.FindAction("Rotate");
            itemMenu = map.FindAction("ItemMenu");
            switchPanel = map.FindAction("SwitchPanel");
        }

        public bool Enabled => map.enabled;

        public Vector2Int NavigationStep { get; private set; }
        public bool PointerMoved { get; private set; }
        public Vector2 PointerPosition => point != null ? point.ReadValue<Vector2>() : Vector2.zero;

        public bool SubmitPressed => submit.WasPressedThisFrame();
        public bool CancelPressed => cancel.WasPressedThisFrame();
        public bool ClosePressed => close.WasPressedThisFrame();
        public bool SystemPressed => system.WasPressedThisFrame();
        public bool NextAppPressed => nextApp.WasPressedThisFrame();
        public bool PreviousAppPressed => previousApp.WasPressedThisFrame();
        public bool ClickPressed => click != null && click.WasPressedThisFrame();
        public bool ClickReleased => click != null && click.WasReleasedThisFrame();
        public bool RotatePressed => rotate != null && rotate.WasPressedThisFrame();
        public bool ItemMenuPressed => itemMenu != null && itemMenu.WasPressedThisFrame();
        public bool SwitchPanelPressed => switchPanel != null && switchPanel.WasPressedThisFrame();

        public void Enable()
        {
            map.Enable();
            heldDirection = Vector2Int.zero;
            NavigationStep = Vector2Int.zero;
            PointerMoved = false;
            lastPointerPosition = PointerPosition;
        }

        public void Disable() => map.Disable();

        public void Update(float unscaledTime)
        {
            UpdateNavigation(unscaledTime);

            Vector2 pointer = PointerPosition;
            PointerMoved = (pointer - lastPointerPosition).sqrMagnitude > PointerMoveThreshold * PointerMoveThreshold;
            lastPointerPosition = pointer;
        }

        private void UpdateNavigation(float now)
        {
            Vector2 value = navigate.ReadValue<Vector2>();
            Vector2Int direction = Vector2Int.zero;

            if (value.sqrMagnitude >= NavigationDeadZone * NavigationDeadZone)
            {
                if (Mathf.Abs(value.x) > Mathf.Abs(value.y)) direction.x = value.x > 0f ? 1 : -1;
                else direction.y = value.y > 0f ? -1 : 1;
            }

            NavigationStep = Vector2Int.zero;

            if (direction != heldDirection)
            {
                heldDirection = direction;
                if (direction == Vector2Int.zero) return;

                NavigationStep = direction;
                nextRepeatTime = now + repeatDelay;
            }
            else if (direction != Vector2Int.zero && now >= nextRepeatTime)
            {
                NavigationStep = direction;
                nextRepeatTime = now + repeatInterval;
            }
        }
    }
}
