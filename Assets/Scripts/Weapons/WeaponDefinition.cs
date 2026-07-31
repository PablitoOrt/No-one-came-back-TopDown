using UnityEngine;

public enum WeaponFireMode
{
    Semiautomatic,
    Automatic,
}

[CreateAssetMenu(menuName = "Weapons/Weapon Definition", fileName = "NewWeaponDefinition")]
public class WeaponDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string weaponName = "Weapon";
    [Tooltip("Stable key for a future save system. Generated once when the asset is created - do not regenerate.")]
    [SerializeField] private string weaponId = System.Guid.NewGuid().ToString("N");

    [Header("Firing")]
    [SerializeField] private WeaponFireMode fireMode = WeaponFireMode.Semiautomatic;
    [SerializeField] private float fireRate = 2f;
    [SerializeField] private float maxRange = 100f;
    [Tooltip("Damage dealt per pellet/projectile, not per trigger pull.")]
    [SerializeField] private float damage = 10f;

    [Header("Pellets")]
    [SerializeField, Min(1)] private int pelletsPerShot = 1;
    [Tooltip("Cone half-angle (degrees) pellets spread within around the aimed shot. Independent of the wobble/steady-aim cone in WeaponAccuracyProfile - this is fixed per weapon, not affected by how steady the aim is.")]
    [SerializeField] private float pelletSpreadAngle = 0f;

    [Header("Accuracy")]
    [SerializeField] private WeaponAccuracyProfile accuracyProfile;

    public string WeaponName => weaponName;
    public string WeaponId => weaponId;
    public WeaponFireMode FireMode => fireMode;
    public float FireRate => fireRate;
    public float MaxRange => maxRange;
    public float Damage => damage;
    public int PelletsPerShot => pelletsPerShot;
    public float PelletSpreadAngle => pelletSpreadAngle;
    public WeaponAccuracyProfile AccuracyProfile => accuracyProfile;
}
