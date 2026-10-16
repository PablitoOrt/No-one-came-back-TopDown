using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

public static class PlayerSetupTool
{
    private const string DefaultInputAssetPath = "Assets/InputSystem_Actions.inputactions";
    private const float DefaultPlayerMaxHealth = 100f;

    [MenuItem("Tools/Player/Setup Player Components")]
    private static void SetupPlayer()
    {
        PlayerController player = Object.FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
        if (player == null)
        {
            EditorUtility.DisplayDialog("Setup Player", "No PlayerController found in the open scene.", "OK");
            return;
        }

        GameObject root = player.gameObject;
        PlayerAimController aim = root.GetComponentInChildren<PlayerAimController>(true);
        GameObject combatHost = aim != null ? aim.gameObject : root;
        var added = new List<string>();

        PlayerInputReader reader = Ensure<PlayerInputReader>(root, added);
        if (reader != null) AssignInputAsset(reader);

        bool healthWasMissing = root.GetComponent<Health>() == null;
        Health health = Ensure<Health>(root, added);
        if (healthWasMissing) SetFloat(health, "maxHealth", DefaultPlayerMaxHealth);

        Ensure<Sanity>(root, added);
        Ensure<PlayerInventory>(combatHost, added);
        Ensure<AimSteadiness>(combatHost, added);
        Ensure<PlayerCombat>(combatHost, added);

        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;

        Debug.Log(added.Count > 0
            ? $"[PlayerSetupTool] Added to '{root.name}': {string.Join(", ", added)}. Save the scene to keep them."
            : "[PlayerSetupTool] Player already has every component.", root);
    }

    private static T Ensure<T>(GameObject host, List<string> added) where T : Component
    {
        if (host.GetComponentInParent<T>(true) is T existing) return existing;

        added.Add($"{typeof(T).Name} ({host.name})");
        return Undo.AddComponent<T>(host);
    }

    private static void AssignInputAsset(PlayerInputReader reader)
    {
        var serialized = new SerializedObject(reader);
        SerializedProperty property = serialized.FindProperty("inputActions");
        if (property.objectReferenceValue != null) return;

        var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(DefaultInputAssetPath);
        if (asset == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:InputActionAsset");
            if (guids.Length > 0) asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        property.objectReferenceValue = asset;
        serialized.ApplyModifiedProperties();
    }

    private static void SetFloat(Object target, string propertyName, float value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(propertyName).floatValue = value;
        serialized.ApplyModifiedProperties();
    }
}
