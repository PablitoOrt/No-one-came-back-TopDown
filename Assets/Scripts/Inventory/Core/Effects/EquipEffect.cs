using UnityEngine;

namespace NoOneCameBack.Inventory
{
    [CreateAssetMenu(menuName = "Inventory/Effects/Equip", fileName = "EquipEffect")]
    public class EquipEffect : ItemEffect
    {
        public override bool CanApply(ItemEffectContext context) =>
            context.Equipment != null && !context.Equipment.IsEquipped(context.Item) && context.Equipment.CanEquip(context.Item);

        public override void Apply(ItemEffectContext context) => context.Equipment.Equip(context.Item);
    }
}
