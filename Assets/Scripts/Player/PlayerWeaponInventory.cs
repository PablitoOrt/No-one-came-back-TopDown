using System;
using System.Collections.Generic;
using UnityEngine;

// Ownership tracking only - no UI/switching yet, that's a separate future system.
public class PlayerWeaponInventory : MonoBehaviour
{
    private readonly HashSet<WeaponDefinition> ownedWeapons = new HashSet<WeaponDefinition>();

    public event Action<WeaponDefinition> WeaponCollected;

    public bool HasCollected(WeaponDefinition definition) => definition != null && ownedWeapons.Contains(definition);

    public void MarkCollected(WeaponDefinition definition)
    {
        if (definition == null) return;

        if (ownedWeapons.Add(definition))
        {
            WeaponCollected?.Invoke(definition);
        }
    }
}
