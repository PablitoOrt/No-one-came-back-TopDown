using System;
using System.Collections.Generic;
using UnityEngine;

// Tracks which weapons the player has ever collected, so a future inventory
// UI/selector has something to query. Deliberately does not do anything else
// (no UI, no currently-equipped list, no switching) - that's out of scope
// until the inventory itself gets built.
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
