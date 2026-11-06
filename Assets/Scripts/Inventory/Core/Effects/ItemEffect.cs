using UnityEngine;

namespace NoOneCameBack.Inventory
{
    public abstract class ItemEffect : ScriptableObject
    {
        public abstract bool CanApply(ItemEffectContext context);

        public abstract void Apply(ItemEffectContext context);
    }
}
