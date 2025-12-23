using UnityEngine;
using UnityEngine.EventSystems;
using UnityEditor;
using System.Collections.Generic;

#if UNITY_EDITOR
/// <summary>
/// Selective Migration Tool - Test on specific objects before full migration
/// Right-click on GameObjects in Hierarchy to migrate them individually
/// </summary>
public class SelectiveMigrationTool : Editor
{
    [MenuItem("GameObject/Gaze System/Migrate This Object", false, 0)]
    private static void MigrateSelectedObject()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        
        if (selectedObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("No Selection", "Please select one or more GameObjects to migrate.", "OK");
            return;
        }

        int migrated = 0;
        List<string> results = new List<string>();

        foreach (GameObject obj in selectedObjects)
        {
            string result = MigrateSingleObject(obj);
            if (result != null)
            {
                results.Add($"✅ {obj.name}: {result}");
                migrated++;
            }
        }

        // Show results
        string message = $"Migrated {migrated} of {selectedObjects.Length} objects:\n\n";
        message += string.Join("\n", results);

        if (migrated > 0)
        {
            EditorUtility.DisplayDialog("Migration Complete", message, "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Nothing to Migrate", "Selected objects don't have Event Triggers.", "OK");
        }
    }

    /// <summary>
    /// Migrate a single GameObject
    /// </summary>
    private static string MigrateSingleObject(GameObject obj)
    {
        EventTrigger eventTrigger = obj.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            return null; // No Event Trigger, nothing to migrate
        }

        // Check what type of interaction this is
        bool hasZoneManager = FindObjectOfType<ZoneManager>() != null;
        bool hasVideoPlayer = obj.GetComponent<EnhancedVideoPlayer>() != null;

        // Add GazeHoverTrigger
        GazeHoverTrigger gazeTrigger = obj.GetComponent<GazeHoverTrigger>();
        if (gazeTrigger == null)
        {
            gazeTrigger = obj.AddComponent<GazeHoverTrigger>();
        }

        // Configure based on type
        if (hasVideoPlayer)
        {
            // Video interaction
            SerializedObject so = new SerializedObject(gazeTrigger);
            so.FindProperty("autoDetectVideoPlayer").boolValue = true;
            so.FindProperty("autoDetectZoneManager").boolValue = false;
            so.FindProperty("actionName").stringValue = "Play Video";
            so.ApplyModifiedProperties();
        }
        else if (hasZoneManager)
        {
            // Teleport interaction
            SerializedObject so = new SerializedObject(gazeTrigger);
            so.FindProperty("autoDetectZoneManager").boolValue = true;
            so.FindProperty("autoDetectVideoPlayer").boolValue = false;
            so.FindProperty("actionName").stringValue = "Teleport";
            so.ApplyModifiedProperties();
        }

        // Fix collider if needed
        Collider col = obj.GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
        }

        // Check if it's a MeshCollider
        MeshCollider meshCol = obj.GetComponent<MeshCollider>();
        if (meshCol != null)
        {
            // Replace with BoxCollider
            Bounds bounds = meshCol.bounds;
            DestroyImmediate(meshCol);
            
            BoxCollider boxCol = obj.AddComponent<BoxCollider>();
            boxCol.isTrigger = true;
            boxCol.center = Vector3.zero;
            boxCol.size = bounds.size;
        }

        // Remove Event Trigger (keep it for now to compare)
        // User can remove manually after testing
        eventTrigger.enabled = false;

        string result = "Added GazeHoverTrigger";
        if (hasVideoPlayer) result += " (Video)";
        else if (hasZoneManager) result += " (Teleport)";
        
        return result;
    }

    [MenuItem("GameObject/Gaze System/Remove Event Trigger from This", false, 1)]
    private static void RemoveEventTriggerFromSelected()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        
        if (selectedObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("No Selection", "Please select one or more GameObjects.", "OK");
            return;
        }

        int removed = 0;
        foreach (GameObject obj in selectedObjects)
        {
            EventTrigger et = obj.GetComponent<EventTrigger>();
            if (et != null)
            {
                DestroyImmediate(et);
                removed++;
            }
        }

        EditorUtility.DisplayDialog("Complete", $"Removed Event Trigger from {removed} objects.", "OK");
    }

    [MenuItem("GameObject/Gaze System/Test Selected Objects", false, 2)]
    private static void TestSelectedObjects()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        
        if (selectedObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("No Selection", "Please select one or more GameObjects to test.", "OK");
            return;
        }

        List<string> report = new List<string>();
        
        foreach (GameObject obj in selectedObjects)
        {
            string status = TestSingleObject(obj);
            report.Add($"{obj.name}:\n{status}\n");
        }

        string message = string.Join("\n", report);
        EditorUtility.DisplayDialog("Migration Test Results", message, "OK");
    }

    private static string TestSingleObject(GameObject obj)
    {
        List<string> status = new List<string>();

        // Check for GazeHoverTrigger
        GazeHoverTrigger gazeTrigger = obj.GetComponent<GazeHoverTrigger>();
        if (gazeTrigger != null)
        {
            status.Add("✅ Has GazeHoverTrigger");
        }
        else
        {
            status.Add("❌ Missing GazeHoverTrigger");
        }

        // Check for collider
        Collider col = obj.GetComponent<Collider>();
        if (col != null)
        {
            if (col.isTrigger)
            {
                status.Add("✅ Collider is Trigger");
            }
            else
            {
                status.Add("⚠️ Collider is NOT Trigger");
            }

            if (col is BoxCollider)
            {
                status.Add("✅ Using BoxCollider");
            }
            else if (col is MeshCollider)
            {
                status.Add("⚠️ Using MeshCollider (should be Box)");
            }
        }
        else
        {
            status.Add("❌ No Collider");
        }

        // Check for old Event Trigger
        EventTrigger et = obj.GetComponent<EventTrigger>();
        if (et != null)
        {
            if (et.enabled)
            {
                status.Add("⚠️ Event Trigger still enabled");
            }
            else
            {
                status.Add("ℹ️ Event Trigger disabled (safe to delete)");
            }
        }

        return string.Join("\n  ", status);
    }
}
#endif
