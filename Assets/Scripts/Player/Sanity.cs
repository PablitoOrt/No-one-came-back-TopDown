using System;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class Sanity : MonoBehaviour
{
    [FormerlySerializedAs("maxPsychosis")]
    [SerializeField] private float maxSanity = 100f;
    [SerializeField] private float currentSanity = 100f;

    public event Action<float, float> Changed;

    public float Current => currentSanity;
    public float Max => maxSanity;
    public float Sanity01 => maxSanity > 0f ? Mathf.Clamp01(currentSanity / maxSanity) : 1f;

    private void Awake()
    {
        currentSanity = Mathf.Clamp(currentSanity, 0f, maxSanity);
    }

    public void Drain(float amount) => Set(currentSanity - Mathf.Max(0f, amount));

    public void Restore(float amount) => Set(currentSanity + Mathf.Max(0f, amount));

    private void Set(float value)
    {
        value = Mathf.Clamp(value, 0f, maxSanity);
        if (Mathf.Approximately(value, currentSanity)) return;

        currentSanity = value;
        Changed?.Invoke(currentSanity, maxSanity);
    }

    [ContextMenu("Debug/Drain 25 Sanity")]
    private void DebugDrain() => Drain(25f);

    [ContextMenu("Debug/Restore All Sanity")]
    private void DebugRestore() => Restore(maxSanity);
}
