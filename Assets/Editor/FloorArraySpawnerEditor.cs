using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(FloorArraySpawner))]
public class FloorArraySpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();

        FloorArraySpawner spawner = (FloorArraySpawner)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Rebuild Floor", GUILayout.Height(30)))
        {
            // Record undo for the changes made by rebuilding
            Undo.RegisterFullObjectHierarchyUndo(spawner.gameObject, "Rebuild Floor");
            
            spawner.RebuildFloor();
            
            // Mark the scene as dirty so changes are saved
            EditorUtility.SetDirty(spawner);
        }
    }
}
