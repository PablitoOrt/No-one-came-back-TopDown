using System;
using System.Collections;
using UnityEngine;

// Minimal test target: reacts to hits with a flash/punch/knockback, no AI.
[DisallowMultipleComponent]
public class BasicEnemy : MonoBehaviour, IDamageable, ITargetable
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 30f;
    private float currentHealth;

    [Header("Targeting")]
    [Tooltip("Point the aim-assist system pulls toward. Left empty, falls back to this transform (usually the root/feet).")]
    [SerializeField] private Transform aimAnchor;

    [Header("Hit Flash")]
    [Tooltip("Left empty, all renderers in children are used automatically.")]
    [SerializeField] private Renderer[] flashRenderers;
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.12f;
    [Tooltip("Shader property holding the base/tint color (URP Lit/Simple Lit use _BaseColor).")]
    [SerializeField] private string colorPropertyName = "_BaseColor";

    [Header("Hit Scale Punch")]
    [SerializeField] private float punchDuration = 0.25f;
    [Tooltip("Scale multiplier over time (1 = base scale). Default overshoots then settles back to 1.")]
    [SerializeField] private AnimationCurve punchScaleCurve = new AnimationCurve(
        new Keyframe(0f, 1f),
        new Keyframe(0.15f, 1.3f),
        new Keyframe(0.45f, 0.9f),
        new Keyframe(1f, 1f));

    [Header("Knockback")]
    [SerializeField] private float knockbackDistance = 0.25f;
    [SerializeField] private float knockbackDuration = 0.15f;

    [Header("Death")]
    [SerializeField] private float deathShrinkDuration = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool debugDraw = true;

    public event Action<float, float> Damaged;
    public event Action Died;

    public bool IsDead { get; private set; }
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public Transform AimAnchor => aimAnchor != null ? aimAnchor : transform;

    private Vector3 baseScale;
    private Color[] baseColors;
    private MaterialPropertyBlock propertyBlock;

    private Coroutine flashCoroutine;
    private Coroutine punchCoroutine;
    private Coroutine knockbackCoroutine;

    private void Awake()
    {
        currentHealth = maxHealth;
        baseScale = transform.localScale;
        propertyBlock = new MaterialPropertyBlock();

        if (flashRenderers == null || flashRenderers.Length == 0)
        {
            flashRenderers = GetComponentsInChildren<Renderer>();
        }

        baseColors = new Color[flashRenderers.Length];
        for (int i = 0; i < flashRenderers.Length; i++)
        {
            Material material = flashRenderers[i] != null ? flashRenderers[i].sharedMaterial : null;
            baseColors[i] = material != null && material.HasProperty(colorPropertyName)
                ? material.GetColor(colorPropertyName)
                : Color.white;
        }
    }

    public void ApplyDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (IsDead || amount <= 0f) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        Damaged?.Invoke(currentHealth, maxHealth);

        RestartCoroutine(ref flashCoroutine, FlashRoutine());
        RestartCoroutine(ref punchCoroutine, PunchRoutine());
        RestartCoroutine(ref knockbackCoroutine, KnockbackRoutine(hitPoint));

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void RestartCoroutine(ref Coroutine handle, IEnumerator routine)
    {
        if (handle != null) StopCoroutine(handle);
        handle = StartCoroutine(routine);
    }

    private IEnumerator FlashRoutine()
    {
        SetColor(flashColor);

        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            SetColorLerp(Mathf.Clamp01(elapsed / flashDuration));
            yield return null;
        }

        RestoreBaseColors();
    }

    private IEnumerator PunchRoutine()
    {
        float elapsed = 0f;
        while (elapsed < punchDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / punchDuration;
            transform.localScale = baseScale * punchScaleCurve.Evaluate(t);
            yield return null;
        }

        transform.localScale = baseScale;
    }

    private IEnumerator KnockbackRoutine(Vector3 hitPoint)
    {
        Vector3 direction = transform.position - hitPoint;
        direction.y = 0f;
        direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : -transform.forward;

        Vector3 start = transform.position;
        Vector3 target = start + direction * knockbackDistance;

        float elapsed = 0f;
        while (elapsed < knockbackDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / knockbackDuration);
            transform.position = Vector3.Lerp(start, target, t);
            yield return null;
        }
    }

    private void Die()
    {
        IsDead = true;
        Died?.Invoke();

        if (TryGetComponent(out Collider col)) col.enabled = false;

        StopAllCoroutines();
        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        SetColor(flashColor);
        Vector3 startScale = transform.localScale;

        float elapsed = 0f;
        while (elapsed < deathShrinkDuration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsed / deathShrinkDuration);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void SetColor(Color color)
    {
        for (int i = 0; i < flashRenderers.Length; i++)
        {
            if (flashRenderers[i] == null) continue;
            flashRenderers[i].GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(colorPropertyName, color);
            flashRenderers[i].SetPropertyBlock(propertyBlock);
        }
    }

    private void SetColorLerp(float t)
    {
        for (int i = 0; i < flashRenderers.Length; i++)
        {
            if (flashRenderers[i] == null) continue;
            flashRenderers[i].GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(colorPropertyName, Color.Lerp(flashColor, baseColors[i], t));
            flashRenderers[i].SetPropertyBlock(propertyBlock);
        }
    }

    private void RestoreBaseColors()
    {
        for (int i = 0; i < flashRenderers.Length; i++)
        {
            if (flashRenderers[i] == null) continue;
            flashRenderers[i].GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(colorPropertyName, baseColors[i]);
            flashRenderers[i].SetPropertyBlock(propertyBlock);
        }
    }

    private void OnGUI()
    {
        if (!debugDraw || IsDead || Camera.main == null) return;

        Vector3 screenPoint = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);
        if (screenPoint.z <= 0f) return;

        GUI.Label(new Rect(screenPoint.x - 40f, Screen.height - screenPoint.y, 80f, 20f),
            $"HP {currentHealth:F0}/{maxHealth:F0}");
    }
}
