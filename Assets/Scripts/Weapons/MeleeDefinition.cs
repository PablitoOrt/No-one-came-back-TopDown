using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Melee Definition", fileName = "NewMelee")]
public class MeleeDefinition : WeaponDefinition
{
    [Header("Reach")]
    [SerializeField] private float range = 1.4f;
    [Tooltip("Full width (degrees) of the arc in front of the attacker that the strike covers.")]
    [SerializeField, Range(1f, 360f)] private float arcAngle = 100f;
    [Tooltip("Max enemies one swing can hit.")]
    [SerializeField, Min(1)] private int maxTargets = 3;

    [Header("Timing")]
    [Tooltip("Seconds between pressing attack and the hit landing.")]
    [SerializeField, Min(0f)] private float windupDuration = 0.15f;
    [Tooltip("Seconds after the hit before another action is allowed.")]
    [SerializeField, Min(0f)] private float recoveryDuration = 0.35f;

    public float Range => range;
    public float ArcAngle => arcAngle;
    public int MaxTargets => maxTargets;
    public float WindupDuration => windupDuration;
    public float RecoveryDuration => recoveryDuration;

    public override WeaponInstance CreateInstance(WeaponUser user) => new MeleeInstance(this, user);
}
