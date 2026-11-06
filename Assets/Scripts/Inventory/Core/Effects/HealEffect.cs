using UnityEngine;

namespace NoOneCameBack.Inventory
{
    [CreateAssetMenu(menuName = "Inventory/Effects/Heal", fileName = "HealEffect")]
    public class HealEffect : ItemEffect
    {
        // TODO(diseño): heal amount per item.
        [SerializeField, Min(0f)] private float amount = 25f;

        public float Amount => amount;

        public override bool CanApply(ItemEffectContext context) =>
            amount > 0f && context.Health != null && context.Health.Current < context.Health.Max;

        public override void Apply(ItemEffectContext context) => context.Health.Heal(amount);
    }
}
