using UnityEngine;

namespace NoOneCameBack.Inventory
{
    public sealed class ItemEffectContext
    {
        public ItemEffectContext(ItemInstance item, GameObject user, IHealth health, IPsychosis psychosis, IBleeding bleeding, IEquipHandler equipment)
        {
            Item = item;
            User = user;
            Health = health;
            Psychosis = psychosis;
            Bleeding = bleeding;
            Equipment = equipment;
        }

        public ItemInstance Item { get; }
        public GameObject User { get; }
        public IHealth Health { get; }
        public IPsychosis Psychosis { get; }
        public IBleeding Bleeding { get; }
        public IEquipHandler Equipment { get; }
    }
}
