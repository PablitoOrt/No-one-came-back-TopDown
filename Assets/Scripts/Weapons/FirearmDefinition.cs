using UnityEngine;

public enum WeaponFireMode
{
    Semiautomatic,
    Automatic,
}

[CreateAssetMenu(menuName = "Weapons/Firearm Definition", fileName = "NewFirearm")]
public class FirearmDefinition : WeaponDefinition
{
    [Header("Firing")]
    [SerializeField] private WeaponFireMode fireMode = WeaponFireMode.Semiautomatic;
    [Tooltip("Shots per second.")]
    [SerializeField] private float fireRate = 2f;
    [SerializeField] private float maxRange = 100f;

    [Header("Pellets")]
    [SerializeField, Min(1)] private int pelletsPerShot = 1;
    [Tooltip("Cone half-angle (degrees) pellets spread within around the aimed shot. Fixed per weapon, independent of how steady the aim is.")]
    [SerializeField] private float pelletSpreadAngle = 0f;

    [Header("Accuracy")]
    [SerializeField] private WeaponAccuracyProfile accuracyProfile;

    [Header("Ammo")]
    [Tooltip("Ammo consumed from the inventory when reloading. Empty = unlimited reserve.")]
    [SerializeField] private AmmoType ammoType;
    [Tooltip("Rounds per magazine. 0 = no magazine: unlimited shots, never reloads.")]
    [SerializeField, Min(0)] private int magazineSize = 0;
    [SerializeField, Min(0f)] private float reloadDuration = 1.5f;
    [Tooltip("Start reloading automatically when trying to fire with an empty magazine.")]
    [SerializeField] private bool autoReloadWhenEmpty = true;

    [Header("Visuals")]
    [Tooltip("Prefab with the weapon mesh and a WeaponModel component marking the barrel tip. Spawned on the player's weapon holder when equipped.")]
    [SerializeField] private WeaponModel modelPrefab;

    [Header("Effects")]
    [SerializeField] private FirearmEffectsProfile effectsProfile;

    public WeaponFireMode FireMode => fireMode;
    public float FireRate => fireRate;
    public float MaxRange => maxRange;
    public int PelletsPerShot => pelletsPerShot;
    public float PelletSpreadAngle => pelletSpreadAngle;
    public WeaponAccuracyProfile AccuracyProfile => accuracyProfile;
    public AmmoType AmmoType => ammoType;
    public int MagazineSize => magazineSize;
    public bool UsesMagazine => magazineSize > 0;
    public float ReloadDuration => reloadDuration;
    public bool AutoReloadWhenEmpty => autoReloadWhenEmpty;
    public WeaponModel ModelPrefab => modelPrefab;
    public FirearmEffectsProfile EffectsProfile => effectsProfile;

    public override WeaponInstance CreateInstance(WeaponUser user) => new FirearmInstance(this, user);
}
