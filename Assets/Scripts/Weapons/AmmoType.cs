using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Ammo Type", fileName = "NewAmmoType")]
public class AmmoType : ScriptableObject
{
    [SerializeField] private string displayName = "Ammo";
    [SerializeField] private Sprite icon;

    public string DisplayName => displayName;
    public Sprite Icon => icon;
}
