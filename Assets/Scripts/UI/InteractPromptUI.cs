using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InteractPromptUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Left empty, found in the scene.")]
    [SerializeField] private PlayerInteractor interactor;
    [Tooltip("Left empty, taken from the interactor's parents.")]
    [SerializeField] private PlayerInputReader input;
    [Tooltip("Left empty, defaults to Camera.main.")]
    [SerializeField] private Camera worldCamera;

    [Header("UI")]
    [SerializeField] private RectTransform prompt;
    [SerializeField] private Image ringFill;
    [SerializeField] private Image keyIcon;
    [SerializeField] private TMP_Text keyLabel;
    [SerializeField] private InputIconSet icons;

    [Header("Placement")]
    [Tooltip("World height above the top of the object's collider.")]
    [SerializeField] private float heightAboveObject = 0.3f;

    [Header("Bindings")]
    [SerializeField] private string keyboardGroup = "Keyboard&Mouse";
    [SerializeField] private string gamepadGroup = "Gamepad";

    private Canvas canvas;
    private Component currentTarget;
    private Collider currentCollider;
    private InputDevice lastDevice;
    private bool showingGamepad;
    private bool keyDirty = true;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        if (interactor == null) interactor = FindFirstObjectByType<PlayerInteractor>();
        if (input == null && interactor != null) input = interactor.GetComponentInParent<PlayerInputReader>();
        if (worldCamera == null) worldCamera = Camera.main;
        SetVisible(false);
    }

    private void LateUpdate()
    {
        if (interactor == null || input == null || worldCamera == null) return;

        TrackDevice();

        Component target = interactor.NearestInteractable as Component;
        if (target == null)
        {
            currentTarget = null;
            SetVisible(false);
            return;
        }

        if (target != currentTarget)
        {
            currentTarget = target;
            currentCollider = target.GetComponentInChildren<Collider>();
        }

        Vector3 screenPoint = worldCamera.WorldToScreenPoint(AnchorPosition());
        if (screenPoint.z <= 0f)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);
        ScreenSpaceUI.PlaceAtScreenPoint(prompt, canvas, screenPoint, worldCamera);
        if (ringFill != null) ringFill.fillAmount = input.InteractHoldProgress;
        if (keyDirty) RefreshKey();
    }

    private Vector3 AnchorPosition()
    {
        if (currentCollider == null) return currentTarget.transform.position + Vector3.up * heightAboveObject;

        Bounds bounds = currentCollider.bounds;
        return new Vector3(bounds.center.x, bounds.max.y + heightAboveObject, bounds.center.z);
    }

    // Swaps the key icon between keyboard and gamepad when the player changes device.
    private void TrackDevice()
    {
        InputDevice device = input.InteractAction.activeControl?.device ?? input.LookDevice;
        if (device == null || device == lastDevice) return;

        // Also refreshes between gamepad brands, e.g. Xbox -> DualSense.
        lastDevice = device;
        showingGamepad = device is Gamepad;
        keyDirty = true;
    }

    private void RefreshKey()
    {
        keyDirty = false;

        InputAction action = input.InteractAction;
        int index = action.GetBindingIndex(InputBinding.MaskByGroup(showingGamepad ? gamepadGroup : keyboardGroup));
        if (index < 0) index = 0;

        string display = action.GetBindingDisplayString(index, out string bindingLayout, out string controlPath);
        // The binding only says "Gamepad"; the real device tells Xbox from PlayStation.
        string deviceLayout = lastDevice != null ? lastDevice.layout : bindingLayout;
        Sprite sprite = icons != null ? icons.Get(deviceLayout, controlPath) : null;

        if (keyIcon != null)
        {
            keyIcon.sprite = sprite;
            keyIcon.enabled = sprite != null;
        }
        if (keyLabel != null)
        {
            keyLabel.text = display;
            keyLabel.enabled = sprite == null;
        }
    }

    private void SetVisible(bool visible)
    {
        if (prompt != null) prompt.localScale = visible ? Vector3.one : Vector3.zero;
    }
}
