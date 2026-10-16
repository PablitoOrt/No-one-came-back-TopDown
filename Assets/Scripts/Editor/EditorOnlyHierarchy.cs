using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class EditorOnlyHierarchy
{
    private static readonly Color RowTint = new Color(1f, 0.2f, 0.2f, 0.18f);

    static EditorOnlyHierarchy()
    {
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
    }

    private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
    {
        GameObject gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

        if (gameObject == null || !gameObject.CompareTag("EditorOnly"))
            return;

        EditorGUI.DrawRect(selectionRect, RowTint);
    }
}
