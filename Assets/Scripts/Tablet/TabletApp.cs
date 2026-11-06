using System;
using UnityEngine;

namespace NoOneCameBack.Tablet
{
    public abstract class TabletApp : MonoBehaviour
    {
        [SerializeField] private string appId = "app";
        [SerializeField] private string displayName = "App";
        [SerializeField] private Sprite icon;
        [Tooltip("Usable while the tablet is still broken (not found yet). Only the System app should have this.")]
        [SerializeField] private bool availableWhenBroken;
        [Tooltip("Unlocked automatically when the tablet is found. Turn off for apps with their own unlock (e.g. the map, via blueprints).")]
        [SerializeField] private bool unlockOnTabletFound = true;

        public event Action<TabletApp> UnlockChanged;

        public string AppId => appId;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public bool AvailableWhenBroken => availableWhenBroken;
        public bool UnlockOnTabletFound => unlockOnTabletFound;

        public bool IsUnlocked { get; private set; }
        public bool IsActive { get; private set; }

        public virtual bool PausesGame => false;

        protected TabletController Tablet { get; private set; }
        protected TabletInput Input => Tablet.Input;

        public void SetUnlocked(bool unlocked)
        {
            if (IsUnlocked == unlocked) return;

            IsUnlocked = unlocked;
            UnlockChanged?.Invoke(this);
        }

        // Called every frame while this app is focused and the tablet accepts input. Uses unscaled time.
        public virtual void Tick(float unscaledDeltaTime) { }

        // Back/cancel pressed: return true if the app consumed it (closed a submenu, dropped a held item...).
        public virtual bool TryCancel() => false;

        protected abstract void OnOpen();

        protected abstract void OnClose();

        internal void Bind(TabletController tablet)
        {
            Tablet = tablet;
            gameObject.SetActive(false);
        }

        internal void Activate()
        {
            if (IsActive) return;

            IsActive = true;
            gameObject.SetActive(true);
            OnOpen();
        }

        internal void Deactivate()
        {
            if (!IsActive) return;

            IsActive = false;
            OnClose();
            gameObject.SetActive(false);
        }
    }
}
