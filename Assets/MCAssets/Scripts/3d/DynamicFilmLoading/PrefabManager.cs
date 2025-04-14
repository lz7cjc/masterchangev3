using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PrefabManager : MonoBehaviour
{
    // Path to the bespoke video prefabs folder
    public const string PREFABS_FOLDER = "Assets/MCAssets/Prefabs/BespokeVideoPrefabs";

    // Local export path - relative to your project
    public string exportPath = "Assets/WebExport/prefabs.json";

    // Optional absolute path for easier access
    public string absoluteExportPath = "";

    // Singleton instance
    private static PrefabManager _instance;
    public static PrefabManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PrefabManager>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("PrefabManager");
                    _instance = obj.AddComponent<PrefabManager>();
                }
            }
            return _instance;
        }
    }

    // Get all available prefab names from the prefabs folder
    public List<string> GetAvailablePrefabNames()
    {
        List<string> prefabNames = new List<string>();

#if UNITY_EDITOR
        // Ensure the folder path exists
        if (!AssetDatabase.IsValidFolder(PREFABS_FOLDER))
        {
            Debug.LogWarning($"Prefabs folder not found: {PREFABS_FOLDER}");
            return prefabNames;
        }

        // Get all prefab files in the folder
        string[] prefabGuids = AssetDatabase.FindAssets("t:prefab", new[] { PREFABS_FOLDER });

        // Add "Default" as the first option
        prefabNames.Add("Default");

        foreach (string guid in prefabGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            string prefabName = Path.GetFileNameWithoutExtension(assetPath);
            prefabNames.Add(prefabName);
        }
#endif

        return prefabNames;
    }

#if UNITY_EDITOR
    [ContextMenu("Export Prefab Names to JSON")]
    public void ExportPrefabNamesToJson()
    {
        List<string> prefabNames = GetAvailablePrefabNames();

        // Create a simple object to serialize
        var prefabData = new { prefabs = prefabNames };
        string json = JsonUtility.ToJson(new PrefabListWrapper(prefabNames), true);

        // First save to the Unity project path
        string fullProjectPath = Path.Combine(Application.dataPath, "..", exportPath);
        string directory = Path.GetDirectoryName(fullProjectPath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Save the JSON to the project path
        File.WriteAllText(fullProjectPath, json);

        // Also save to the absolute path if provided
        if (!string.IsNullOrEmpty(absoluteExportPath))
        {
            string absDirectory = Path.GetDirectoryName(absoluteExportPath);
            if (!Directory.Exists(absDirectory))
            {
                Directory.CreateDirectory(absDirectory);
            }

            File.WriteAllText(absoluteExportPath, json);
            Debug.Log($"Exported prefab names to absolute path: {absoluteExportPath}");
        }

        Debug.Log($"Exported {prefabNames.Count} prefab names to {fullProjectPath}");
        AssetDatabase.Refresh();

        // Show the file in explorer for easier manual uploading
        EditorUtility.RevealInFinder(fullProjectPath);
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
[CustomEditor(typeof(PrefabManager))]
public class PrefabManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PrefabManager manager = (PrefabManager)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Prefab Management", EditorStyles.boldLabel);

        // Display all available prefabs
        List<string> prefabNames = manager.GetAvailablePrefabNames();
        EditorGUILayout.LabelField($"Available Prefabs ({prefabNames.Count}):", EditorStyles.boldLabel);

        EditorGUI.indentLevel++;
        foreach (string prefabName in prefabNames)
        {
            EditorGUILayout.LabelField(prefabName);
        }
        EditorGUI.indentLevel--;

        EditorGUILayout.Space(10);

        // Export button
        if (GUILayout.Button("Export Prefab Names to JSON"))
        {
            manager.ExportPrefabNamesToJson();
        }

        // Help box explaining the workflow
        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "Workflow:\n" +
            "1. Click 'Export Prefab Names to JSON' to generate the JSON file\n" +
            "2. The file will be saved to the specified path and opened in Explorer\n" +
            "3. Upload this file to your remote server in the WebExport folder\n" +
            "4. Repeat this process whenever you add or change prefabs",
            MessageType.Info);
    }
}
#endif