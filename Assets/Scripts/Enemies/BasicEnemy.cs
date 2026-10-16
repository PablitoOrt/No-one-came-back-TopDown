using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(HitReactionEffects))]
public class BasicEnemy : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool debugDraw = true;

    private Health health;
    private Camera debugCamera;

    private void Awake()
    {
        health = GetComponent<Health>();
        debugCamera = Camera.main;
    }

    // UI debug
    private void OnGUI()
    {
        if (!debugDraw || health.IsDead || debugCamera == null) return;

        Vector3 screenPoint = debugCamera.WorldToScreenPoint(transform.position + Vector3.up * 2f);
        if (screenPoint.z <= 0f) return;

        GUI.Label(new Rect(screenPoint.x - 40f, Screen.height - screenPoint.y, 80f, 20f),
            $"HP {health.CurrentHealth:F0}/{health.MaxHealth:F0}");
    }
}
