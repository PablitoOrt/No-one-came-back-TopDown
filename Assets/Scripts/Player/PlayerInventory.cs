using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour, IAmmoSource
{
    [Serializable]
    private struct AmmoStack
    {
        public AmmoType type;
        public int amount;
    }

    [SerializeField] private List<WeaponDefinition> startingWeapons = new List<WeaponDefinition>();
    [SerializeField] private List<AmmoStack> startingAmmo = new List<AmmoStack>();

    private readonly List<WeaponDefinition> weapons = new List<WeaponDefinition>();
    private readonly Dictionary<AmmoType, int> ammo = new Dictionary<AmmoType, int>();

    public event Action<WeaponDefinition> WeaponAdded;
    public event Action<AmmoType, int> AmmoChanged;

    public IReadOnlyList<WeaponDefinition> Weapons => weapons;

    private void Awake()
    {
        foreach (WeaponDefinition weapon in startingWeapons) AddWeapon(weapon);
        foreach (AmmoStack stack in startingAmmo) AddAmmo(stack.type, stack.amount);
    }

    public bool HasWeapon(WeaponDefinition definition) => definition != null && weapons.Contains(definition);

    public bool AddWeapon(WeaponDefinition definition)
    {
        if (definition == null || weapons.Contains(definition)) return false;

        weapons.Add(definition);
        WeaponAdded?.Invoke(definition);
        return true;
    }

    public int GetAmmo(AmmoType type) => type != null && ammo.TryGetValue(type, out int amount) ? amount : 0;

    public void AddAmmo(AmmoType type, int amount)
    {
        if (type == null || amount <= 0) return;

        int total = GetAmmo(type) + amount;
        ammo[type] = total;
        AmmoChanged?.Invoke(type, total);
    }

    public int TakeAmmo(AmmoType type, int requested)
    {
        int available = GetAmmo(type);
        int taken = Mathf.Clamp(requested, 0, available);
        if (taken == 0) return 0;

        ammo[type] = available - taken;
        AmmoChanged?.Invoke(type, available - taken);
        return taken;
    }
}
