using UnityEngine;

public abstract class WeaponDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string weaponName = "Weapon";
    [Tooltip("Stable key for the save system. Kept in sync with the asset's GUID automatically, so duplicated assets never share an id.")]
    [SerializeField] private string weaponId;
    [SerializeField] private Sprite icon;

    [Header("Damage")]
    [Tooltip("Damage dealt per hit (per pellet for firearms, per target for melee).")]
    [SerializeField] private float damage = 10f;

    public string WeaponName => weaponName;
    public string WeaponId => weaponId;
    public Sprite Icon => icon;
    public float Damage => damage;

    public abstract WeaponInstance CreateInstance(WeaponUser user);

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        string guid = UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(this));
        if (!string.IsNullOrEmpty(guid) && weaponId != guid)
        {
            weaponId = guid;
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif
}
