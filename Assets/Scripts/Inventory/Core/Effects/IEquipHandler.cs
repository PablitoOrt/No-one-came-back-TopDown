namespace NoOneCameBack.Inventory
{
    public interface IEquipHandler
    {
        bool CanEquip(ItemInstance item);

        bool Equip(ItemInstance item);

        bool IsEquipped(ItemInstance item);
    }
}
