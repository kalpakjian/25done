using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WallGenerator))]
public class WallGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();

        WallGenerator generator = (WallGenerator)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Generate Room", GUILayout.Height(30)))
        {
            // Record undo for the changes made by generating the room
            Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "Generate Room");

            generator.GenerateRoom();

            // Mark the scene as dirty so changes are saved
            EditorUtility.SetDirty(generator);
        }
    }
}