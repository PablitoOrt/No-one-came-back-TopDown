namespace NoOneCameBack.Inventory
{
    // Implemented by the combat weapon assets (WeaponDefinition) so the inventory can read ammo/magazine
    // data without depending on the combat assembly.
    public interface IWeaponData
    {
        ItemDefinition AmmoItem { get; }

        int MagazineCapacity { get; }
    }
}
