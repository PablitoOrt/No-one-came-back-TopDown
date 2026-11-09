using UnityEngine;

[CreateAssetMenu(menuName = "Equipment/Flashlight Definition", fileName = "NewFlashlight")]
public class FlashlightDefinition : ScriptableObject
{
    [Tooltip("Model held in the hand. Needs a Spot Light somewhere in its children, pointing forward.")]
    [SerializeField] private GameObject modelPrefab;

    [Header("Light")]
    [SerializeField, Min(0f)] private float intensity = 3f;
    [SerializeField, Min(0f)] private float range = 15f;
    [SerializeField, Range(1f, 179f)] private float spotAngle = 45f;
    [SerializeField] private Color color = new Color(1f, 0.95f, 0.85f);

    public GameObject ModelPrefab => modelPrefab;
    public float Intensity => intensity;
    public float Range => range;
    public float SpotAngle => spotAngle;
    public Color Color => color;
}
