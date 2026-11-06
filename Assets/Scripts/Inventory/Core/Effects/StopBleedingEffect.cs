using UnityEngine;

namespace NoOneCameBack.Inventory
{
    [CreateAssetMenu(menuName = "Inventory/Effects/Stop Bleeding", fileName = "StopBleedingEffect")]
    public class StopBleedingEffect : ItemEffect
    {
        public override bool CanApply(ItemEffectContext context) => context.Bleeding != null && context.Bleeding.IsBleeding;

        public override void Apply(ItemEffectContext context) => context.Bleeding.StopBleeding();
    }
}
