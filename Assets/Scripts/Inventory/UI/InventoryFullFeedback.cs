using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NoOneCameBack.Inventory
{
    // Shakes and tints the interaction prompt when a pickup fails (inventory full, or no tablet yet).
    public class InventoryFullFeedback : MonoBehaviour
    {
        [SerializeField] private PlayerInventoryHolder holder;
        [SerializeField] private RectTransform target;
        [SerializeField] private Graphic tintTarget;
        [SerializeField] private TMP_Text messageLabel;

        [Header("Feel")]
        [SerializeField] private Color failColor = new Color(1f, 0.25f, 0.2f);
        [SerializeField, Min(0.01f)] private float duration = 0.4f;
        [SerializeField, Min(0f)] private float amplitude = 12f;
        [SerializeField, Min(0f)] private float frequency = 40f;

        [Header("Text")]
        [SerializeField] private string noSpaceText = "I have no room for this.";
        [SerializeField] private string noStorageText = "I have nowhere to put this.";
        [SerializeField, Min(0f)] private float messageDuration = 2f;

        private Vector2 restPosition;
        private Color restColor;
        private float shakeTimer;
        private float messageTimer;

        private void Awake()
        {
            if (target != null) restPosition = target.anchoredPosition;
            if (tintTarget != null) restColor = tintTarget.color;
            if (messageLabel != null) messageLabel.text = string.Empty;
        }

        private void OnEnable()
        {
            if (holder != null) holder.PickupFailed += OnPickupFailed;
        }

        private void OnDisable()
        {
            if (holder != null) holder.PickupFailed -= OnPickupFailed;
            shakeTimer = 0f;
            ApplyShake(0f);
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            if (shakeTimer > 0f)
            {
                shakeTimer = Mathf.Max(0f, shakeTimer - dt);
                ApplyShake(shakeTimer / duration);
            }

            if (messageTimer > 0f)
            {
                messageTimer -= dt;
                if (messageTimer <= 0f && messageLabel != null) messageLabel.text = string.Empty;
            }
        }

        private void OnPickupFailed(ItemInstance item, PickupResult result)
        {
            shakeTimer = duration;

            if (messageLabel == null) return;
            messageLabel.text = result == PickupResult.NoStorage ? noStorageText : noSpaceText;
            messageTimer = messageDuration;
        }

        private void ApplyShake(float strength)
        {
            if (target != null)
            {
                float offset = Mathf.Sin(Time.unscaledTime * frequency) * amplitude * strength;
                target.anchoredPosition = restPosition + new Vector2(offset, 0f);
            }

            if (tintTarget != null) tintTarget.color = Color.Lerp(restColor, failColor, strength);
        }
    }
}
