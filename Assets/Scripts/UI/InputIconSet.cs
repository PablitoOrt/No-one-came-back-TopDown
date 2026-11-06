using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(menuName = "UI/Input Icon Set")]
public class InputIconSet : ScriptableObject
{
    [System.Serializable]
    private struct Entry
    {
        [Tooltip("Control path as the Input System reports it, e.g. \"e\", \"buttonWest\", \"leftButton\".")]
        public string controlPath;
        [Tooltip("Optional. Empty matches any device; \"Gamepad\" matches every gamepad, \"DualShockGamepad\" only PlayStation pads.")]
        public string deviceLayout;
        public Sprite sprite;
    }

    [SerializeField] private Entry[] entries;

    public Sprite Get(string deviceLayout, string controlPath)
    {
        if (entries == null || string.IsNullOrEmpty(controlPath)) return null;

        // A layout-specific entry wins over a generic one.
        Sprite fallback = null;
        foreach (Entry entry in entries)
        {
            if (!string.Equals(entry.controlPath, controlPath, System.StringComparison.OrdinalIgnoreCase)) continue;

            if (string.IsNullOrEmpty(entry.deviceLayout))
            {
                if (fallback == null) fallback = entry.sprite;
            }
            else if (!string.IsNullOrEmpty(deviceLayout) && InputSystem.IsFirstLayoutBasedOnSecond(deviceLayout, entry.deviceLayout))
            {
                return entry.sprite;
            }
        }
        return fallback;
    }
}
