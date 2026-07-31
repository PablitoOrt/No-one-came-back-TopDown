using System;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;

    [Header("Psychosis")]
    [Tooltip("0 = perfectly calm, maxPsychosis = fully psychotic.")]
    [SerializeField] private float maxPsychosis = 100f;
    [SerializeField] private float currentPsychosis = 0f;

    public event Action<float, float> HealthChanged;
    public event Action<float, float> PsychosisChanged;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float CurrentPsychosis => currentPsychosis;
    public float MaxPsychosis => maxPsychosis;
    public bool IsDead => currentHealth <= 0f;

    // 0 = full health, 1 = near death. Widens weapon spread as the player gets hurt.
    public float InjuryFactor => maxHealth > 0f ? 1f - Mathf.Clamp01(currentHealth / maxHealth) : 0f;

    // 0 = calm, 1 = fully psychotic. Determines how much steady aim can still help.
    public float PsychosisFactor => maxPsychosis > 0f ? Mathf.Clamp01(currentPsychosis / maxPsychosis) : 0f;

    private void Awake()
    {
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        currentPsychosis = Mathf.Clamp(currentPsychosis, 0f, maxPsychosis);
    }

    public void ApplyDamage(float amount)
    {
        if (amount <= 0f) return;
        currentHealth = Mathf.Clamp(currentHealth - amount, 0f, maxHealth);
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void Heal(float amount)
    {
        if (amount <= 0f) return;
        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void AddPsychosis(float amount)
    {
        currentPsychosis = Mathf.Clamp(currentPsychosis + amount, 0f, maxPsychosis);
        PsychosisChanged?.Invoke(currentPsychosis, maxPsychosis);
    }

    public void ReducePsychosis(float amount) => AddPsychosis(-amount);
}
