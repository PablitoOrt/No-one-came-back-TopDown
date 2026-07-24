using System.Collections.Generic;
using UnityEngine;

// Fades out walls (on wallMask) that sit between this camera and target, so the
// player stays visible in a fixed/top-down camera setup (Signalis-style rooms).
// Detection is a throttled SphereCast rather than a per-frame Raycast: walls are
// static, so checking ~12x/sec instead of 60x/sec costs nothing visually.
// Fade is applied per-renderer via MaterialPropertyBlock so every wall piece can
// still share the same WallMaterial asset (no per-object material instances).
[RequireComponent(typeof(Camera))]
public class WallOcclusionFader : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private LayerMask wallMask;

    [Header("Detection")]
    [Tooltip("Sphere radius used for the cast toward the target; roughly the player's visual half-width so corners of walls near the player still register as occluders.")]
    [SerializeField] private float castRadius = 0.35f;
    [Tooltip("How many times per second to re-check for occluders. Walls are static, so this doesn't need to run every frame.")]
    [SerializeField] private float checksPerSecond = 12f;
    [SerializeField] private int maxHits = 8;

    [Header("Fade")]
    [SerializeField, Range(0f, 1f)] private float fadedAlpha = 0.25f;
    [SerializeField] private float fadeSpeed = 8f;

    [Header("Debug")]
    [SerializeField] private bool debugDraw = false;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private Camera cam;
    private RaycastHit[] hitBuffer;
    private float checkTimer;

    private readonly Dictionary<Renderer, FadeState> fadeStates = new();
    private readonly HashSet<Renderer> occludersThisCheck = new();
    private readonly List<Renderer> settledRenderers = new();

    private class FadeState
    {
        public MaterialPropertyBlock Block;
        public Color BaseColor;
        public float CurrentAlpha;
        public float TargetAlpha;
    }

    private void Awake()
    {
        cam = GetComponent<Camera>();
        hitBuffer = new RaycastHit[Mathf.Max(1, maxHits)];
    }

    private void Update()
    {
        checkTimer -= Time.deltaTime;
        if (checkTimer <= 0f)
        {
            checkTimer = 1f / Mathf.Max(1f, checksPerSecond);
            DetectOccluders();
        }

        TickFades();
    }

    private void DetectOccluders()
    {
        occludersThisCheck.Clear();

        if (target == null) return;

        Vector3 origin = cam.transform.position;
        Vector3 toTarget = target.position - origin;
        float distance = toTarget.magnitude;
        if (distance <= 0.001f) return;

        Vector3 direction = toTarget / distance;

        int hitCount = Physics.SphereCastNonAlloc(
            origin, castRadius, direction, hitBuffer, distance, wallMask, QueryTriggerInteraction.Ignore);

        if (debugDraw) Debug.DrawRay(origin, direction * distance, Color.yellow);

        for (int i = 0; i < hitCount; i++)
        {
            Renderer hitRenderer = hitBuffer[i].collider.GetComponent<Renderer>();
            if (hitRenderer == null) continue;

            occludersThisCheck.Add(hitRenderer);

            if (!fadeStates.TryGetValue(hitRenderer, out FadeState state))
            {
                state = new FadeState
                {
                    Block = new MaterialPropertyBlock(),
                    BaseColor = hitRenderer.sharedMaterial.GetColor(BaseColorId),
                    CurrentAlpha = 1f
                };
                fadeStates.Add(hitRenderer, state);
            }

            state.TargetAlpha = fadedAlpha;
        }

        foreach (KeyValuePair<Renderer, FadeState> entry in fadeStates)
        {
            if (!occludersThisCheck.Contains(entry.Key))
                entry.Value.TargetAlpha = 1f;
        }
    }

    private void TickFades()
    {
        settledRenderers.Clear();

        foreach (KeyValuePair<Renderer, FadeState> entry in fadeStates)
        {
            Renderer rend = entry.Key;
            FadeState state = entry.Value;

            if (rend == null)
            {
                settledRenderers.Add(rend);
                continue;
            }

            state.CurrentAlpha = Mathf.MoveTowards(state.CurrentAlpha, state.TargetAlpha, fadeSpeed * Time.deltaTime);

            if (Mathf.Approximately(state.TargetAlpha, 1f) && Mathf.Approximately(state.CurrentAlpha, 1f))
            {
                rend.SetPropertyBlock(null);
                settledRenderers.Add(rend);
                continue;
            }

            Color color = state.BaseColor;
            color.a = state.CurrentAlpha;
            state.Block.SetColor(BaseColorId, color);
            rend.SetPropertyBlock(state.Block);
        }

        for (int i = 0; i < settledRenderers.Count; i++)
            fadeStates.Remove(settledRenderers[i]);
    }
}
