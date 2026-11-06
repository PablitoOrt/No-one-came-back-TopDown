using UnityEngine;
using UnityEngine.Rendering;

namespace NoOneCameBack.Tablet
{
    // Fades a global Volume (e.g. softer CRT so the text is readable) while the tablet is opening/open, with any app.
    public class TabletFocusVolume : MonoBehaviour
    {
        [SerializeField] private TabletController tablet;
        [SerializeField] private Volume volume;
        // TODO(diseño): fade speed of the tablet look.
        [SerializeField, Min(0.01f)] private float fadeDuration = 0.2f;

        private float target;

        private void OnEnable()
        {
            if (tablet != null) tablet.StateChanged += OnStateChanged;
            target = tablet != null && IsActive(tablet.State) ? 1f : 0f;
            if (volume != null) volume.weight = target;
        }

        private void OnDisable()
        {
            if (tablet != null) tablet.StateChanged -= OnStateChanged;
        }

        private void Update()
        {
            if (volume == null || Mathf.Approximately(volume.weight, target)) return;

            volume.weight = Mathf.MoveTowards(volume.weight, target, Time.unscaledDeltaTime / fadeDuration);
        }

        private void OnStateChanged(TabletState state) => target = IsActive(state) ? 1f : 0f;

        private static bool IsActive(TabletState state) => state == TabletState.Opening || state == TabletState.Open;
    }
}
