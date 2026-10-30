using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Firearm Effects Profile", fileName = "NewFirearmEffects")]
public class FirearmEffectsProfile : ScriptableObject
{
    [Header("Muzzle")]
    [Tooltip("Played once per shot at the muzzle, facing the aim direction.")]
    [SerializeField] private ParticleSystem muzzleFlash;

    [Header("Muzzle Light")]
    [SerializeField] private bool useMuzzleLight = true;
    [SerializeField] private Color muzzleLightColor = new Color(1f, 0.7f, 0.35f, 1f);
    [SerializeField, Min(0f)] private float muzzleLightIntensity = 3f;
    [SerializeField, Min(0f)] private float muzzleLightRange = 4f;
    [Tooltip("Seconds the light takes to fade out after each shot.")]
    [SerializeField, Min(0.01f)] private float muzzleLightDuration = 0.05f;

    [Header("Shells")]
    [Tooltip("Played once per shot at the ejection point.")]
    [SerializeField] private ParticleSystem shellEject;
    [Tooltip("Ejection point relative to the muzzle, in aim space (x = right, y = up, z = forward).")]
    [SerializeField] private Vector3 shellEjectOffset = new Vector3(0.05f, 0.05f, -0.25f);

    [Header("Tracers")]
    [Tooltip("One particle is emitted per pellet and flies to the hit point. The system needs no emission of its own.")]
    [SerializeField] private ParticleSystem tracer;
    [SerializeField, Min(1f)] private float tracerSpeed = 150f;
    [Tooltip("Chance per pellet to draw a tracer.")]
    [SerializeField, Range(0f, 1f)] private float tracerChance = 1f;

    [Header("Impacts")]
    [Tooltip("Played per pellet that hits something, facing the surface normal.")]
    [SerializeField] private ParticleSystem impact;

    [Header("Pooling")]
    [Tooltip("Instances kept per effect (impacts are multiplied by pellets per shot). Raise it if effects get cut short on fast weapons.")]
    [SerializeField, Min(1)] private int poolSize = 6;

    public ParticleSystem MuzzleFlash => muzzleFlash;
    public bool UseMuzzleLight => useMuzzleLight;
    public Color MuzzleLightColor => muzzleLightColor;
    public float MuzzleLightIntensity => muzzleLightIntensity;
    public float MuzzleLightRange => muzzleLightRange;
    public float MuzzleLightDuration => muzzleLightDuration;
    public ParticleSystem ShellEject => shellEject;
    public Vector3 ShellEjectOffset => shellEjectOffset;
    public ParticleSystem Tracer => tracer;
    public float TracerSpeed => tracerSpeed;
    public float TracerChance => tracerChance;
    public ParticleSystem Impact => impact;
    public int PoolSize => poolSize;
}
