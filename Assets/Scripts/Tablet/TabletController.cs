using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace NoOneCameBack.Tablet
{
    // Can live anywhere inside the player hierarchy (e.g. on the tablet object); interactables find it with FindFor.
    [DisallowMultipleComponent]
    public class TabletController : MonoBehaviour, IPlayerActionBlocker
    {
        [Header("Input")]
        [Tooltip("Must be the same asset PlayerInputReader uses, so disabling the Player map actually stops the character.")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string playerMapName = "Player";
        [SerializeField] private string tabletMapName = "Tablet";
        [SerializeField] private string openInventoryActionName = "OpenInventory";
        [SerializeField] private string openSystemActionName = "OpenSystem";
        [SerializeField, Min(0f)] private float navigationRepeatDelay = 0.35f;
        [SerializeField, Min(0.01f)] private float navigationRepeatInterval = 0.1f;

        [Header("Apps")]
        [Tooltip("Order used by Next/Previous app (Q/E, L1/R1).")]
        [SerializeField] private List<TabletApp> apps = new List<TabletApp>();
        [SerializeField] private TabletApp inventoryApp;
        [SerializeField] private TabletApp systemApp;

        [Header("Presentation")]
        [SerializeField] private TabletView view;
        // TODO(diseño): open/close animation timing.
        [SerializeField, Min(0f)] private float openDuration = 0.25f;
        [SerializeField, Min(0f)] private float closeDuration = 0.2f;

        [Tooltip("Hides every renderer and canvas under this object (tablet mesh + screen) while the tablet is put away.")]
        [SerializeField] private bool hideWhenClosed = true;

        [Header("Debug")]
        [SerializeField] private bool startFound;

        [Header("Hooks")]
        [Tooltip("Character starts taking the tablet out (play the animation here).")]
        [SerializeField] private UnityEvent onTakeOut = new UnityEvent();
        [Tooltip("Character starts putting the tablet away.")]
        [SerializeField] private UnityEvent onPutAway = new UnityEvent();
        [SerializeField] private UnityEvent onFound = new UnityEvent();
        [Tooltip("True while the System app freezes the world. Drive the desaturate/darken post-process from here.")]
        [SerializeField] private UnityEvent<bool> onWorldFrozenChanged = new UnityEvent<bool>();

        private InputActionMap playerMap;
        private InputAction openInventory;
        private InputAction openSystem;
        private float transition;
        private int lastToggleFrame = -1;
        private Renderer[] physicalRenderers;
        private Canvas[] physicalCanvases;

        public event Action<TabletState> StateChanged;
        public event Action<TabletApp> AppChanged;
        public event Action TakeOutStarted;
        public event Action PutAwayStarted;
        public event Action Found;
        public event Action<bool> WorldFrozenChanged;

        public TabletState State { get; private set; } = TabletState.Closed;
        public TabletApp CurrentApp { get; private set; }
        public TabletInput Input { get; private set; }
        public bool IsFound { get; private set; }
        public bool IsWorldFrozen { get; private set; }
        public IReadOnlyList<TabletApp> Apps => apps;

        public bool BlocksCombat => State != TabletState.Closed;
        public bool BlocksInteraction => State != TabletState.Closed;

        private void Awake()
        {
            playerMap = inputActions.FindActionMap(playerMapName, throwIfNotFound: true);
            openInventory = playerMap.FindAction(openInventoryActionName, throwIfNotFound: true);
            openSystem = playerMap.FindAction(openSystemActionName, throwIfNotFound: true);
            Input = new TabletInput(inputActions.FindActionMap(tabletMapName, throwIfNotFound: true), navigationRepeatDelay, navigationRepeatInterval);

            foreach (TabletApp app in apps)
            {
                app.Bind(this);
                app.UnlockChanged += OnAppUnlockChanged;
            }

            IsFound = startFound;
            foreach (TabletApp app in apps)
                app.SetUnlocked(app.AvailableWhenBroken || (IsFound && app.UnlockOnTabletFound));

            if (view != null)
            {
                view.SetBroken(!IsFound);
                view.SetProgress(0f);
                view.SetVisible(false);
            }

            physicalRenderers = GetComponentsInChildren<Renderer>(true);
            physicalCanvases = GetComponentsInChildren<Canvas>(true);
            SetPhysicalVisible(false);
        }

        private void OnDisable()
        {
            if (State != TabletState.Closed) FinishClose();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            switch (State)
            {
                case TabletState.Closed:
                    if (Time.frameCount == lastToggleFrame) return;
                    if (openInventory.WasPressedThisFrame()) Open(inventoryApp);
                    else if (openSystem.WasPressedThisFrame()) Open(systemApp);
                    break;

                case TabletState.Opening:
                    transition = openDuration > 0f ? Mathf.Min(1f, transition + dt / openDuration) : 1f;
                    view?.SetProgress(transition);
                    if (transition >= 1f) SetState(TabletState.Open);
                    HandleInput(dt);
                    break;

                case TabletState.Open:
                    HandleInput(dt);
                    break;

                case TabletState.Closing:
                    transition = closeDuration > 0f ? Mathf.Max(0f, transition - dt / closeDuration) : 0f;
                    view?.SetProgress(transition);
                    if (transition <= 0f) FinishClose();
                    break;
            }
        }

        public static TabletController FindFor(GameObject interactor)
        {
            TabletController tablet = interactor.GetComponentInParent<TabletController>();
            if (tablet != null) return tablet;

            PlayerInputReader player = interactor.GetComponentInParent<PlayerInputReader>();
            Transform root = player != null ? player.transform : interactor.transform.root;
            return root.GetComponentInChildren<TabletController>(true);
        }

        public bool IsUsable(TabletApp app) => app != null && app.IsUnlocked && (IsFound || app.AvailableWhenBroken);

        public void Open(TabletApp app)
        {
            TabletApp target = IsUsable(app) ? app : FirstUsableApp();
            if (target == null) return;

            if (State == TabletState.Open || State == TabletState.Opening)
            {
                SwitchTo(target);
                return;
            }

            if (State == TabletState.Closed)
            {
                SetPhysicalVisible(true);
                playerMap.Disable();
                Input.Enable();
                view?.SetVisible(true);
                TakeOutStarted?.Invoke();
                onTakeOut.Invoke();
            }

            lastToggleFrame = Time.frameCount;
            SetState(TabletState.Opening);
            SwitchTo(target);
        }

        public void Close()
        {
            if (State == TabletState.Closed || State == TabletState.Closing) return;

            lastToggleFrame = Time.frameCount;
            SetWorldFrozen(false);
            SetState(TabletState.Closing);
            PutAwayStarted?.Invoke();
            onPutAway.Invoke();
        }

        public bool SwitchTo(TabletApp app)
        {
            if (!IsUsable(app) || State == TabletState.Closed) return false;

            if (app != CurrentApp)
            {
                CurrentApp?.Deactivate();
                CurrentApp = app;
                app.Activate();
                AppChanged?.Invoke(app);
            }

            SetWorldFrozen(app.PausesGame);
            view?.Refresh(this);
            return true;
        }

        public void CycleApp(int direction)
        {
            if (apps.Count == 0) return;

            int start = Mathf.Max(0, apps.IndexOf(CurrentApp));
            for (int i = 1; i <= apps.Count; i++)
            {
                TabletApp candidate = apps[((start + direction * i) % apps.Count + apps.Count) % apps.Count];
                if (IsUsable(candidate))
                {
                    SwitchTo(candidate);
                    return;
                }
            }
        }

        public void MarkFound()
        {
            if (IsFound) return;

            IsFound = true;
            foreach (TabletApp app in apps)
            {
                if (app.UnlockOnTabletFound) app.SetUnlocked(true);
            }

            view?.SetBroken(false);
            view?.Refresh(this);
            Found?.Invoke();
            onFound.Invoke();
        }

        public bool UnlockApp(string appId)
        {
            TabletApp app = GetApp(appId);
            if (app == null) return false;

            app.SetUnlocked(true);
            return true;
        }

        public TabletApp GetApp(string appId) => apps.Find(app => app != null && app.AppId == appId);

        public T GetApp<T>() where T : TabletApp
        {
            foreach (TabletApp app in apps)
            {
                if (app is T typed) return typed;
            }
            return null;
        }

        public List<string> GetUnlockedAppIds()
        {
            List<string> ids = new List<string>();
            foreach (TabletApp app in apps)
            {
                if (app.IsUnlocked) ids.Add(app.AppId);
            }
            return ids;
        }

        public void LoadUnlocks(bool found, IEnumerable<string> unlockedAppIds)
        {
            IsFound = found;
            foreach (TabletApp app in apps)
            {
                bool saved = unlockedAppIds != null && unlockedAppIds.Contains(app.AppId);
                app.SetUnlocked(app.AvailableWhenBroken || saved);
            }

            view?.SetBroken(!found);
            view?.Refresh(this);
        }

        private void HandleInput(float dt)
        {
            if (Time.frameCount == lastToggleFrame || CurrentApp == null) return;

            Input.Update(Time.unscaledTime);

            if (Input.ClosePressed)
            {
                Close();
                return;
            }

            if (Input.SystemPressed)
            {
                if (CurrentApp.TryCancel()) return;
                if (CurrentApp == systemApp) Close();
                else SwitchTo(systemApp);
                return;
            }

            if (Input.CancelPressed)
            {
                if (!CurrentApp.TryCancel()) Close();
                return;
            }

            if (Input.NextAppPressed) CycleApp(1);
            else if (Input.PreviousAppPressed) CycleApp(-1);

            CurrentApp.Tick(dt);
        }

        private void FinishClose()
        {
            CurrentApp?.Deactivate();
            CurrentApp = null;
            transition = 0f;

            SetWorldFrozen(false);
            Input.Disable();
            playerMap.Enable();
            view?.SetProgress(0f);
            view?.SetVisible(false);
            SetPhysicalVisible(false);

            lastToggleFrame = Time.frameCount;
            SetState(TabletState.Closed);
        }

        // Renderers/canvases are toggled instead of the GameObject so this controller keeps running while hidden.
        private void SetPhysicalVisible(bool visible)
        {
            if (!hideWhenClosed || physicalRenderers == null) return;

            foreach (Renderer physicalRenderer in physicalRenderers)
            {
                if (physicalRenderer != null) physicalRenderer.enabled = visible;
            }
            foreach (Canvas canvas in physicalCanvases)
            {
                if (canvas != null) canvas.enabled = visible;
            }
        }

        private TabletApp FirstUsableApp() => apps.Find(IsUsable);

        private void SetState(TabletState state)
        {
            if (State == state) return;

            State = state;
            StateChanged?.Invoke(state);
        }

        private void SetWorldFrozen(bool frozen)
        {
            if (IsWorldFrozen == frozen) return;

            IsWorldFrozen = frozen;
            Time.timeScale = frozen ? 0f : 1f;
            WorldFrozenChanged?.Invoke(frozen);
            onWorldFrozenChanged.Invoke(frozen);
        }

        private void OnAppUnlockChanged(TabletApp app) => view?.Refresh(this);
    }
}
