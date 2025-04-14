using UnityEngine;
using UnityEditor;
using System.IO;

[CustomEditor(typeof(VideoDatabaseManager))]
public class VideoDatabaseManagerHelper : Editor
{
    private TextAsset databaseTextFile;

    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();

        // Get a reference to the target
        VideoDatabaseManager manager = (VideoDatabaseManager)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Database Import Tools", EditorStyles.boldLabel);

        // Create a field for dragging and dropping the text file
        databaseTextFile = (TextAsset)EditorGUILayout.ObjectField(
            "Database Text File",
            databaseTextFile,
            typeof(TextAsset),
            false);

        if (databaseTextFile != null)
        {
            if (GUILayout.Button("Import From Text File"))
            {
                // Set the cloudDatabaseFile property
                SerializedProperty cloudFileProperty = serializedObject.FindProperty("cloudDatabaseFile");
                cloudFileProperty.objectReferenceValue = databaseTextFile;
                serializedObject.ApplyModifiedProperties();

                // Parse the database
                manager.ParseCloudDatabase();

                EditorUtility.SetDirty(manager);
            }
        }

        EditorGUILayout.Space(5);

        if (GUILayout.Button("Load From JSON"))
        {
            manager.LoadDatabaseFromJson();
        }

        if (GUILayout.Button("Save To JSON"))
        {
            manager.SaveDatabaseToJson();
        }
    }
}