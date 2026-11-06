using System;
using UnityEngine;

namespace NoOneCameBack.Inventory
{
    // Psychosis is the inverse of the existing Sanity component: reducing psychosis restores sanity.
    [RequireComponent(typeof(Sanity))]
    public class SanityPsychosisAdapter : MonoBehaviour, IPsychosis
    {
        private Sanity sanity;

        public event Action Changed;

        public float Level01 => 1f - Source.Sanity01;

        private Sanity Source => sanity != null ? sanity : sanity = GetComponent<Sanity>();

        private void OnEnable() => Source.Changed += OnSanityChanged;

        private void OnDisable() => Source.Changed -= OnSanityChanged;

        public void Reduce(float amount) => Source.Restore(amount);

        private void OnSanityChanged(float current, float max) => Changed?.Invoke();
    }
}
