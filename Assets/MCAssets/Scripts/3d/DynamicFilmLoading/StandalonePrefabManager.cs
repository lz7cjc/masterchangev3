using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Standalone manager for exporting prefab information for the web interface.
/// This class doesn't depend on any other controllers or managers.
/// </summary>
public class StandalonePrefabManager : MonoBehaviour
{
    // Path to the bespoke video prefabs folder
    public string prefabsFolder = "Assets/MCAssets/Prefabs/BespokeVideoPrefabs";

    // Local export path inside Unity project
    public string projectExportPath = "Assets/WebExport/prefabs.json";

    // Desktop export path for convenience
    public bool saveToDesktop = true;

    // Store discovered prefabs
    [SerializeField, HideInInspector]
    private List<string> discoveredPrefabs = new List<string>();

#if UNITY_EDITOR
    /// <summary>
    /// Scans the prefab folder and updates the list of available prefabs
    /// </summary>
    [ContextMenu("Scan Prefab Folder")]
    public void ScanPrefabFolder()
    {
        discoveredPrefabs.Clear();

        // Add Default as the first option
        discoveredPrefabs.Add("Default");

        // Ensure the folder path exists
        if (!AssetDatabase.IsValidFolder(prefabsFolder))
        {
            Debug.LogWarning($"Prefabs folder not found: {prefabsFolder}");
            return;
        }

        // Get all prefab files in the folder
        string[] prefabGuids = AssetDatabase.FindAssets("t:prefab", new[] { prefabsFolder });

        foreach (string guid in prefabGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            string prefabName = Path.GetFileNameWithoutExtension(assetPath);
            discoveredPrefabs.Add(prefabName);
            Debug.Log($"Found prefab: {prefabName}");
        }

        Debug.Log($"Found {discoveredPrefabs.Count - 1} prefabs (plus Default)");

        // Mark the object as dirty so Unity saves the discovered prefabs list
        EditorUtility.SetDirty(this);
    }

    /// <summary>
    /// Exports the prefab list to a JSON file
    /// </summary>
    [ContextMenu("Export Prefab Names to JSON")]
    public void ExportPrefabNamesToJson()
    {
        // Make sure we have an up-to-date list of prefabs
        if (discoveredPrefabs.Count <= 1) // Only has Default
        {
            if (EditorUtility.DisplayDialog("Scan Prefabs First?",
                "The prefab list seems empty. Do you want to scan for prefabs first?",
                "Yes, scan first", "No, export anyway"))
            {
                ScanPrefabFolder();
            }
        }

        // Create a serializable wrapper
        var wrapper = new PrefabListWrapper(discoveredPrefabs);
        string json = JsonUtility.ToJson(wrapper, true);

        bool exportSuccess = false;
        string exportedPath = "";

        try
        {
            // First try to save inside the Unity project
            if (!string.IsNullOrEmpty(projectExportPath))
            {
                try
                {
                    // Get full path
                    string fullProjectPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", projectExportPath));

                    // Ensure the directory exists
                    string directory = Path.GetDirectoryName(fullProjectPath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    // Save the file
                    File.WriteAllText(fullProjectPath, json);
                    Debug.Log($"Exported prefab names to: {fullProjectPath}");
                    exportedPath = fullProjectPath;
                    exportSuccess = true;

                    // Refresh AssetDatabase
                    AssetDatabase.Refresh();
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Could not save to project path: {ex.Message}");
                }
            }

            // Always try to save to desktop as well if enabled
            if (saveToDesktop)
            {
                try
                {
                    // Get desktop path
                    string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
                    string desktopFilePath = Path.Combine(desktopPath, "prefabs.json");

                    // Save the file
                    File.WriteAllText(desktopFilePath, json);
                    Debug.Log($"Exported prefab names to Desktop: {desktopFilePath}");

                    // If we didn't succeed earlier, use this path
                    if (!exportSuccess)
                    {
                        exportedPath = desktopFilePath;
                        exportSuccess = true;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Could not save to desktop: {ex.Message}");
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error exporting prefabs: {ex.Message}");
            EditorUtility.DisplayDialog("Export Error",
                $"Failed to export prefab list: {ex.Message}", "OK");
            return;
        }

        // Show results
        if (exportSuccess)
        {
            // Show the file in explorer
            EditorUtility.RevealInFinder(exportedPath);

            EditorUtility.DisplayDialog("Export Complete",
                $"Exported {discoveredPrefabs.Count} prefab names to JSON.\n\n" +
                $"File saved to: {exportedPath}\n\n" +
                "Please upload this file to your remote server in the WebExport folder.",
                "OK");
        }
        else
        {
            string tempPath = Path.Combine(Path.GetTempPath(), "prefabs.json");
            File.WriteAllText(tempPath, json);

            EditorUtility.RevealInFinder(tempPath);

            EditorUtility.DisplayDialog("Export Complete (Alternative Location)",
                $"Couldn't write to the project or desktop, but saved the file to: {tempPath}\n\n" +
                "Please upload this file to your remote server in the WebExport folder.",
                "OK");
        }
    }
#endif

    // Wrapper class for serialization
    [System.Serializable]
    private class PrefabListWrapper
    {
        public List<string> prefabs;

        public PrefabListWrapper(List<string> prefabList)
        {
            prefabs = prefabList;
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(StandalonePrefabManager))]
public class StandalonePrefabManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        StandalonePrefabManager manager = (StandalonePrefabManager)target;

        // Draw the default fields
        EditorGUILayout.LabelField("Prefab Folder Settings", EditorStyles.boldLabel);

        // Show the prefab folder path field
        EditorGUI.BeginChangeCheck();
        string newPrefabsFolder = EditorGUILayout.TextField("Prefabs Folder", manager.prefabsFolder);
        if (EditorGUI.EndChangeCheck() && newPrefabsFolder != manager.prefabsFolder)
        {
            Undo.RecordObject(manager, "Change Prefabs Folder");
            manager.prefabsFolder = newPrefabsFolder;
            EditorUtility.SetDirty(manager);
        }

        // Add a button to browse for the folder
        if (GUILayout.Button("Browse Prefabs Folder"))
        {
            string folder = EditorUtility.OpenFolderPanel("Select Prefabs Folder", Application.dataPath, "");
            if (!string.IsNullOrEmpty(folder))
            {
                // Convert to a project-relative path if possible
                string projectPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                if (folder.StartsWith(projectPath))
                {
                    folder = "Assets" + folder.Substring(projectPath.Length).Replace('\\', '/');
                    Undo.RecordObject(manager, "Change Prefabs Folder");
                    manager.prefabsFolder = folder;
                    EditorUtility.SetDirty(manager);
                }
                else
                {
                    EditorUtility.DisplayDialog("Invalid Folder",
                        "Please select a folder within your Unity project.", "OK");
                }
            }
        }

        EditorGUILayout.Space(10);

        // Export paths
        EditorGUILayout.LabelField("Export Settings", EditorStyles.boldLabel);

        // Project-relative export path
        EditorGUI.BeginChangeCheck();
        string newExportPath = EditorGUILayout.TextField("Export Path (Project)", manager.projectExportPath);
        if (EditorGUI.EndChangeCheck() && newExportPath != manager.projectExportPath)
        {
            Undo.RecordObject(manager, "Change Export Path");
            manager.projectExportPath = newExportPath;
            EditorUtility.SetDirty(manager);
        }

        // Desktop option
        EditorGUI.BeginChangeCheck();
        bool newSaveToDesktop = EditorGUILayout.Toggle("Also Save to Desktop", manager.saveToDesktop);
        if (EditorGUI.EndChangeCheck() && newSaveToDesktop != manager.saveToDesktop)
        {
            Undo.RecordObject(manager, "Change Desktop Export Option");
            manager.saveToDesktop = newSaveToDesktop;
            EditorUtility.SetDirty(manager);
        }

        EditorGUILayout.Space(15);

        // Get discovered prefabs
        SerializedProperty prefabsProperty = serializedObject.FindProperty("discoveredPrefabs");
        EditorGUILayout.LabelField($"Discovered Prefabs ({prefabsProperty.arraySize}):", EditorStyles.boldLabel);

        // Show discovered prefabs (read-only)
        if (prefabsProperty.arraySize > 0)
        {
            EditorGUI.indentLevel++;
            for (int i = 0; i < prefabsProperty.arraySize; i++)
            {
                EditorGUILayout.LabelField(prefabsProperty.GetArrayElementAtIndex(i).stringValue);
            }
            EditorGUI.indentLevel--;
        }
        else
        {
            EditorGUILayout.HelpBox("No prefabs discovered yet. Click 'Scan Prefab Folder' to find prefabs.", MessageType.Info);
        }

        EditorGUILayout.Space(10);

        // Buttons
        if (GUILayout.Button("Scan Prefab Folder"))
        {
            manager.ScanPrefabFolder();
            serializedObject.Update();
        }

        if (GUILayout.Button("Export Prefab Names to JSON"))
        {
            manager.ExportPrefabNamesToJson();
        }

        // Help box explaining the workflow
        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "Workflow:\n" +
            "1. Set the correct path to your prefabs folder\n" +
            "2. Click 'Scan Prefab Folder' to find all prefabs\n" +
            "3. Click 'Export Prefab Names to JSON' to generate the file\n" +
            "4. Upload the generated file to your remote server\n" +
            "5. Repeat steps 2-4 whenever you add/change prefabs",
            MessageType.Info);
    }
}
#endif