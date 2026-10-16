using System;
using UnityEngine;

[DisallowMultipleComponent]
public class Health : MonoBehaviour, IDamageable, ITargetable
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 30f;
    [Tooltip("Live value, shown for debugging. Change it through ApplyDamage/Heal, not here.")]
    [SerializeField, ReadOnly] private float currentHealth;

    [Header("Targeting")]
    [Tooltip("Point the aim-assist system pulls toward. Left empty, falls back to this transform (usually the root/feet).")]
    [SerializeField] private Transform aimAnchor;

    public event Action<DamageInfo> Damaged;
    public event Action<float, float> Changed;
    public event Action Died;

    public bool IsDead { get; private set; }
    public float CurrentHealth
    {
        get => currentHealth;
        private set => currentHealth = value;
    }
    public float MaxHealth => maxHealth;
    public float Health01 => maxHealth > 0f ? CurrentHealth / maxHealth : 0f;
    public Transform AimAnchor => aimAnchor != null ? aimAnchor : transform;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    private void OnValidate()
    {
        if (!Application.isPlaying) currentHealth = maxHealth;
    }

    public void ApplyDamage(in DamageInfo damage)
    {
        if (IsDead || damage.Amount <= 0f) return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage.Amount);
        Damaged?.Invoke(damage);
        Changed?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth <= 0f) Die();
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;

        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        Changed?.Invoke(CurrentHealth, maxHealth);
    }

    public void Kill()
    {
        if (IsDead) return;

        CurrentHealth = 0f;
        Changed?.Invoke(CurrentHealth, maxHealth);
        Die();
    }

    private void Die()
    {
        IsDead = true;
        Died?.Invoke();
    }

    [ContextMenu("Debug/Take 10 Damage")]
    private void DebugTakeDamage() => ApplyDamage(new DamageInfo(10f, transform.position, Vector3.up));
}
