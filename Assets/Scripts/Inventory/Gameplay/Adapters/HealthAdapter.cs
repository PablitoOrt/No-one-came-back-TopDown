using System;
using UnityEngine;

namespace NoOneCameBack.Inventory
{
    // Exposes the existing Health component to item effects.
    [RequireComponent(typeof(Health))]
    public class HealthAdapter : MonoBehaviour, IHealth
    {
        private Health health;

        public event Action Changed;

        public float Current => Source.CurrentHealth;
        public float Max => Source.MaxHealth;

        private Health Source => health != null ? health : health = GetComponent<Health>();

        private void OnEnable() => Source.Changed += OnHealthChanged;

        private void OnDisable() => Source.Changed -= OnHealthChanged;

        public void Heal(float amount) => Source.Heal(amount);

        private void OnHealthChanged(float current, float max) => Changed?.Invoke();
    }
}
