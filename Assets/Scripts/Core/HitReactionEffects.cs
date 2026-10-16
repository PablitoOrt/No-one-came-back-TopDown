using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Health))]
public class HitReactionEffects : MonoBehaviour
{
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
    [SerializeField] private bool destroyOnDeath = true;

    private Health health;
    private NavMeshAgent agent;
    private CharacterController characterController;

    private Vector3 baseScale;
    private Color[] baseColors;
    private MaterialPropertyBlock propertyBlock;
    private int colorPropertyId;

    private Coroutine flashCoroutine;
    private Coroutine punchCoroutine;
    private Coroutine knockbackCoroutine;

    private void Awake()
    {
        health = GetComponent<Health>();
        TryGetComponent(out agent);
        TryGetComponent(out characterController);

        colorPropertyId = Shader.PropertyToID(colorPropertyName);
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
            baseColors[i] = material != null && material.HasProperty(colorPropertyId)
                ? material.GetColor(colorPropertyId)
                : Color.white;
        }
    }

    private void OnEnable()
    {
        health.Damaged += HandleDamaged;
        health.Died += HandleDied;
    }

    private void OnDisable()
    {
        health.Damaged -= HandleDamaged;
        health.Died -= HandleDied;
    }

    private void HandleDamaged(DamageInfo damage)
    {
        if (health.IsDead) return;

        RestartCoroutine(ref flashCoroutine, FlashRoutine());
        RestartCoroutine(ref punchCoroutine, PunchRoutine());
        RestartCoroutine(ref knockbackCoroutine, KnockbackRoutine(damage.Point));
    }

    private void HandleDied()
    {
        foreach (Collider col in GetComponentsInChildren<Collider>()) col.enabled = false;
        if (agent != null) agent.enabled = false;

        StopAllCoroutines();
        StartCoroutine(DeathRoutine());
    }

    private void RestartCoroutine(ref Coroutine handle, IEnumerator routine)
    {
        if (handle != null) StopCoroutine(handle);
        handle = StartCoroutine(routine);
    }

    private IEnumerator FlashRoutine()
    {
        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            SetColorLerp(Mathf.Clamp01(elapsed / flashDuration));
            elapsed += Time.deltaTime;
            yield return null;
        }

        SetColorLerp(1f);
    }

    private IEnumerator PunchRoutine()
    {
        float elapsed = 0f;
        while (elapsed < punchDuration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = baseScale * punchScaleCurve.Evaluate(elapsed / punchDuration);
            yield return null;
        }

        transform.localScale = baseScale;
    }

    private IEnumerator KnockbackRoutine(Vector3 hitPoint)
    {
        Vector3 direction = transform.position - hitPoint;
        direction.y = 0f;
        direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : -transform.forward;

        float elapsed = 0f;
        float travelled = 0f;
        while (elapsed < knockbackDuration)
        {
            elapsed += Time.deltaTime;
            float target = Mathf.SmoothStep(0f, 1f, elapsed / knockbackDuration) * knockbackDistance;
            MoveBody(direction * (target - travelled));
            travelled = target;
            yield return null;
        }
    }

    private void MoveBody(Vector3 delta)
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh) agent.Move(delta);
        else if (characterController != null && characterController.enabled) characterController.Move(delta);
        else transform.position += delta;
    }

    private IEnumerator DeathRoutine()
    {
        SetColorLerp(0f);
        Vector3 startScale = transform.localScale;

        float elapsed = 0f;
        while (elapsed < deathShrinkDuration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsed / deathShrinkDuration);
            yield return null;
        }

        if (destroyOnDeath) Destroy(gameObject);
    }

    private void SetColorLerp(float t)
    {
        for (int i = 0; i < flashRenderers.Length; i++)
        {
            if (flashRenderers[i] == null) continue;
            flashRenderers[i].GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(colorPropertyId, Color.Lerp(flashColor, baseColors[i], t));
            flashRenderers[i].SetPropertyBlock(propertyBlock);
        }
    }
}
