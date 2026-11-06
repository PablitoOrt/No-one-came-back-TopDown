using UnityEngine;

namespace NoOneCameBack.Inventory
{
    [CreateAssetMenu(menuName = "Inventory/Effects/Reduce Psychosis", fileName = "ReducePsychosisEffect")]
    public class ReducePsychosisEffect : ItemEffect
    {
        // TODO(diseño): amount, in the units of the IPsychosis implementation (the Sanity adapter uses 0-100).
        [SerializeField, Min(0f)] private float amount = 25f;

        public float Amount => amount;

        public override bool CanApply(ItemEffectContext context) =>
            amount > 0f && context.Psychosis != null && context.Psychosis.Level01 > 0f;

        public override void Apply(ItemEffectContext context) => context.Psychosis.Reduce(amount);
    }
}
