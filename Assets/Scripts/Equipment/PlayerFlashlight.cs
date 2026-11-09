using System.Collections.Generic;
using NoOneCameBack.Inventory;
using UnityEngine;

public class PlayerFlashlight : MonoBehaviour
{
    [Tooltip("Where the model is spawned, e.g. the hand bone. Left empty, this object is used.")]
    [SerializeField] private Transform holder;
    [SerializeField] private PlayerInventoryHolder inventory;
    [SerializeField] private PlayerInputReader input;
    [SerializeField] private bool startOn = true;

    private readonly Dictionary<FlashlightDefinition, GameObject> models = new Dictionary<FlashlightDefinition, GameObject>();
    private GameObject currentModel;
    private Light currentLight;
    private bool isOn;

    public bool IsEquipped => currentLight != null;
    public bool IsOn => IsEquipped && isOn;

    private void Awake()
    {
        if (inventory == null) inventory = GetComponentInParent<PlayerInventoryHolder>();
        if (input == null) input = GetComponentInParent<PlayerInputReader>();
        if (holder == null) holder = transform;
        isOn = startOn;
    }

    // Subscribed in Start: the holder builds its model in Awake.
    private void Start()
    {
        if (inventory == null) return;

        inventory.Model.EquippedChanged += ApplyEquippedItem;
        ApplyEquippedItem(inventory.Model.EquippedWeapon);
    }

    private void OnDestroy()
    {
        if (inventory != null && inventory.Model != null) inventory.Model.EquippedChanged -= ApplyEquippedItem;
    }

    private void Update()
    {
        if (currentLight == null || input == null || !input.FlashlightPressed) return;

        isOn = !isOn;
        currentLight.enabled = isOn;
    }

    private void ApplyEquippedItem(ItemInstance item)
    {
        if (currentModel != null) currentModel.SetActive(false);
        currentModel = null;
        currentLight = null;

        if (!(item?.Definition.Equipment is FlashlightDefinition definition) || definition.ModelPrefab == null) return;

        if (!models.TryGetValue(definition, out currentModel))
        {
            currentModel = Instantiate(definition.ModelPrefab, holder, false);
            models.Add(definition, currentModel);
        }

        currentModel.SetActive(true);
        currentLight = currentModel.GetComponentInChildren<Light>(true);
        if (currentLight == null)
        {
            Debug.LogWarning($"[PlayerFlashlight] '{definition.ModelPrefab.name}' has no Light in its children.", definition);
            return;
        }

        currentLight.type = LightType.Spot;
        currentLight.intensity = definition.Intensity;
        currentLight.range = definition.Range;
        currentLight.spotAngle = definition.SpotAngle;
        currentLight.color = definition.Color;
        currentLight.enabled = isOn;
    }
}
