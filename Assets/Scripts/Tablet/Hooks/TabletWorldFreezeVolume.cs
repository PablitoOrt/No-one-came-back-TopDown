using UnityEngine;
using UnityEngine.Rendering;

namespace NoOneCameBack.Tablet
{
    // Fades a global Volume (e.g. Color Adjustments with low saturation/exposure) while the System app has the world frozen.
    public class TabletWorldFreezeVolume : MonoBehaviour
    {
        [SerializeField] private TabletController tablet;
        [SerializeField] private Volume volume;
        // TODO(diseño): fade speed of the frozen-world look.
        [SerializeField, Min(0.01f)] private float fadeDuration = 0.2f;

        private float target;

        private void OnEnable()
        {
            if (tablet != null) tablet.WorldFrozenChanged += OnWorldFrozenChanged;
            target = tablet != null && tablet.IsWorldFrozen ? 1f : 0f;
            if (volume != null) volume.weight = target;
        }

        private void OnDisable()
        {
            if (tablet != null) tablet.WorldFrozenChanged -= OnWorldFrozenChanged;
        }

        private void Update()
        {
            if (volume == null || Mathf.Approximately(volume.weight, target)) return;

            volume.weight = Mathf.MoveTowards(volume.weight, target, Time.unscaledDeltaTime / fadeDuration);
        }

        private void OnWorldFrozenChanged(bool frozen) => target = frozen ? 1f : 0f;
    }
}
