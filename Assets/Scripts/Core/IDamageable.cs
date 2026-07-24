using UnityEngine;

// Implemented by anything a weapon shot can hit and damage
// (enemies, breakables, ...) so Weapon never needs to know their concrete type.
public interface IDamageable
{
    void ApplyDamage(float amount, Vector3 hitPoint, Vector3 hitNormal);
}
