using System;
using UnityEngine;

public class AimSteadiness : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Left empty, auto-resolved from this object or its parents.")]
    [SerializeField] private PlayerAimController aimController;
    [Tooltip("Left empty, auto-resolved from parents. Taking damage resets the aim.")]
    [SerializeField] private Health health;
    [Tooltip("Left empty, auto-resolved from parents. Optional.")]
    [SerializeField] private Sanity sanity;

    [Header("Sanity")]
    [Tooltip("How many times longer it takes to fully steady the aim at 0 sanity. 1 = sanity has no effect.")]
    [SerializeField, Min(1f)] private float aimTimeMultiplierAtZeroSanity = 2.5f;
    [Tooltip("How the penalty ramps as sanity is lost. X: sanity lost (0 = lucid, 1 = none left). Y: fraction of the penalty applied.")]
    [SerializeField] private AnimationCurve sanityPenaltyCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    private WeaponAccuracyProfile profile;

    public float Progress01 { get; private set; }
    public bool IsFullySteady => Progress01 >= 1f;
    public WeaponAccuracyProfile Profile => profile;

    public float AimTimeMultiplier => sanity == null
        ? 1f
        : Mathf.Lerp(1f, aimTimeMultiplierAtZeroSanity, sanityPenaltyCurve.Evaluate(1f - sanity.Sanity01));

    public float CurrentSpreadAngle => profile != null ? profile.EvaluateSpread(Progress01) : 0f;

    public event Action Interrupted;

    private void Awake()
    {
        if (aimController == null) aimController = GetComponentInParent<PlayerAimController>();
        if (health == null) health = GetComponentInParent<Health>();
        if (sanity == null) sanity = GetComponentInParent<Sanity>();
    }

    private void OnEnable()
    {
        if (health != null) health.Damaged += HandleDamaged;
    }

    private void OnDisable()
    {
        if (health != null) health.Damaged -= HandleDamaged;
    }

    private void Update()
    {
        if (profile == null || aimController == null || !aimController.IsAiming)
        {
            Progress01 = 0f;
            return;
        }

        float duration = profile.SteadyAimDuration * AimTimeMultiplier;
        float rate = aimController.IsTargetLocked ? profile.AssistedSteadyRateMultiplier : 1f;

        Progress01 = duration > 0f
            ? Mathf.Min(1f, Progress01 + Time.deltaTime * rate / duration)
            : 1f;
    }

    public void SetProfile(WeaponAccuracyProfile newProfile)
    {
        profile = newProfile;
        Progress01 = 0f;
    }

    public void NotifyShotFired()
    {
        Progress01 *= profile != null ? profile.SteadinessKeptAfterShot : 0f;
    }

    private void HandleDamaged(DamageInfo damage)
    {
        Progress01 = 0f;
        Interrupted?.Invoke();
    }
}
