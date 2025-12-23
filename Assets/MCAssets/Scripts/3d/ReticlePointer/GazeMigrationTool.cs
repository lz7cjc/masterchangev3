using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Migration Tool - Converts Event Trigger system to GazeHoverTrigger system
/// One-time use to upgrade existing objects
/// Removes redundancy and creates clean VR-native architecture
/// </summary>
public class GazeMigrationTool : MonoBehaviour
{
    [Header("Migration Settings")]
    [Tooltip("Remove Event Triggers after migration")]
    public bool removeEventTriggers = true;
    
    [Tooltip("Fix MeshCollider issues automatically")]
    public bool fixMeshColliders = true;
    
    [Tooltip("Default hover delay for migrated objects")]
    public float defaultHoverDelay = 3f;

    [Header("Preview Mode")]
    [Tooltip("Show what will be changed without making changes")]
    public bool previewMode = false;

    // Statistics
    private int eventTriggersFound = 0;
    private int zoneManagerCallsFound = 0;
    private int videoPlayerCallsFound = 0;
    private int customEventsFound = 0;
    private int gazeTriggersAdded = 0;
    private int eventTriggersRemoved = 0;
    private int collidersFixed = 0;

#if UNITY_EDITOR
    [ContextMenu("Run Migration")]
    public void RunMigration()
    {
        Debug.Log("=== GAZE MIGRATION TOOL START ===");
        Debug.Log($"Preview Mode: {previewMode}");
        
        ResetStatistics();
        
        // Find all Event Triggers in scene
        EventTrigger[] eventTriggers = FindObjectsByType<EventTrigger>(FindObjectsSortMode.None);
        eventTriggersFound = eventTriggers.Length;
        
        Debug.Log($"Found {eventTriggersFound} Event Triggers to migrate");
        
        foreach (EventTrigger eventTrigger in eventTriggers)
        {
            MigrateEventTrigger(eventTrigger);
        }
        
        PrintStatistics();
        
        if (!previewMode)
        {
            EditorUtility.SetDirty(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            Debug.Log("✓ Migration complete! Save your scene.");
        }
        else
        {
            Debug.Log("ℹ️ Preview mode - no changes made");
        }
        
        Debug.Log("=== GAZE MIGRATION TOOL COMPLETE ===");
    }

    private void MigrateEventTrigger(EventTrigger eventTrigger)
    {
        GameObject obj = eventTrigger.gameObject;
        
        if (previewMode)
        {
            Debug.Log($"[PREVIEW] Would migrate: {obj.name}");
            AnalyzeEventTrigger(eventTrigger);
            return;
        }
        
        Debug.Log($"--- Migrating: {obj.name} ---");
        
        // Check if already has GazeHoverTrigger
        if (obj.GetComponent<GazeHoverTrigger>() != null)
        {
            Debug.Log($"  ⚠️ Already has GazeHoverTrigger, skipping");
            return;
        }
        
        // Fix collider if needed
        if (fixMeshColliders)
        {
            FixCollider(obj);
        }
        
        // Analyze what the Event Trigger does
        InteractionType interactionType = AnalyzeEventTrigger(eventTrigger);
        
        // Add GazeHoverTrigger
        GazeHoverTrigger gazeHoverTrigger = obj.AddComponent<GazeHoverTrigger>();
        
        // Configure based on analysis
        ConfigureGazeHoverTrigger(gazeHoverTrigger, obj, interactionType);
        
        gazeTriggersAdded++;
        
        // Remove Event Trigger if requested
        if (removeEventTriggers)
        {
            DestroyImmediate(eventTrigger);
            eventTriggersRemoved++;
            Debug.Log($"  ✓ Removed Event Trigger");
        }
        
        Debug.Log($"  ✓ Migration complete for {obj.name}");
    }

    private enum InteractionType
    {
        ZoneManager,
        VideoPlayer,
        CustomEvent,
        Unknown
    }

    private InteractionType AnalyzeEventTrigger(EventTrigger eventTrigger)
    {
        InteractionType type = InteractionType.Unknown;
        
        // Check all triggers
        foreach (EventTrigger.Entry entry in eventTrigger.triggers)
        {
            if (entry.eventID == EventTriggerType.PointerEnter || 
                entry.eventID == EventTriggerType.PointerExit)
            {
                // Check what methods are called
                foreach (var call in entry.callback.GetPersistentEventCount() > 0 ? 
                    Enumerable.Range(0, entry.callback.GetPersistentEventCount()) : 
                    new int[0])
                {
                    string methodName = entry.callback.GetPersistentMethodName(call);
                    Object target = entry.callback.GetPersistentTarget(call);
                    
                    if (target is ZoneManager)
                    {
                        type = InteractionType.ZoneManager;
                        zoneManagerCallsFound++;
                        Debug.Log($"  → Detected ZoneManager call: {methodName}");
                    }
                    else if (target is EnhancedVideoPlayer)
                    {
                        type = InteractionType.VideoPlayer;
                        videoPlayerCallsFound++;
                        Debug.Log($"  → Detected EnhancedVideoPlayer call: {methodName}");
                    }
                    else
                    {
                        type = InteractionType.CustomEvent;
                        customEventsFound++;
                        Debug.Log($"  → Detected custom event: {methodName}");
                    }
                }
            }
        }
        
        return type;
    }

    private void ConfigureGazeHoverTrigger(GazeHoverTrigger trigger, GameObject obj, InteractionType type)
    {
        SerializedObject serializedTrigger = new SerializedObject(trigger);
        
        // Basic settings
        serializedTrigger.FindProperty("actionName").stringValue = obj.name;
        serializedTrigger.FindProperty("hoverDelay").floatValue = defaultHoverDelay;
        serializedTrigger.FindProperty("showCountdown").boolValue = true;
        serializedTrigger.FindProperty("debugMode").boolValue = false;
        
        // Detect if HUD element
        bool isHUD = IsHUDElement(obj);
        serializedTrigger.FindProperty("isHUDElement").boolValue = isHUD;
        
        // Configure based on interaction type
        switch (type)
        {
            case InteractionType.ZoneManager:
                serializedTrigger.FindProperty("autoDetectZoneManager").boolValue = true;
                serializedTrigger.FindProperty("autoDetectVideoPlayer").boolValue = false;
                Debug.Log($"  → Configured for ZoneManager (HUD: {isHUD})");
                break;
                
            case InteractionType.VideoPlayer:
                serializedTrigger.FindProperty("autoDetectZoneManager").boolValue = false;
                serializedTrigger.FindProperty("autoDetectVideoPlayer").boolValue = true;
                
                // Check hover delay from EnhancedVideoPlayer
                EnhancedVideoPlayer videoPlayer = obj.GetComponent<EnhancedVideoPlayer>();
                if (videoPlayer != null)
                {
                    SerializedObject serializedVideo = new SerializedObject(videoPlayer);
                    float videoHoverDelay = serializedVideo.FindProperty("hoverTimeRequired").floatValue;
                    if (videoHoverDelay > 0)
                    {
                        serializedTrigger.FindProperty("hoverDelay").floatValue = videoHoverDelay;
                        Debug.Log($"  → Matched hover delay from EnhancedVideoPlayer: {videoHoverDelay}s");
                    }
                }
                Debug.Log($"  → Configured for EnhancedVideoPlayer (HUD: {isHUD})");
                break;
                
            case InteractionType.CustomEvent:
                serializedTrigger.FindProperty("autoDetectZoneManager").boolValue = false;
                serializedTrigger.FindProperty("autoDetectVideoPlayer").boolValue = false;
                Debug.Log($"  → Configured for Custom Events (HUD: {isHUD})");
                Debug.LogWarning($"  ⚠️ Custom events need manual wiring in Inspector!");
                break;
                
            case InteractionType.Unknown:
                Debug.LogWarning($"  ⚠️ Unknown interaction type - using defaults");
                break;
        }
        
        serializedTrigger.ApplyModifiedProperties();
    }

    private void FixCollider(GameObject obj)
    {
        Collider collider = obj.GetComponent<Collider>();
        
        if (collider == null)
        {
            // No collider - add BoxCollider
            BoxCollider boxCol = obj.AddComponent<BoxCollider>();
            boxCol.isTrigger = true;
            
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                boxCol.center = renderer.bounds.center - obj.transform.position;
                boxCol.size = renderer.bounds.size;
            }
            
            collidersFixed++;
            Debug.Log($"  + Added BoxCollider");
            return;
        }
        
        // Check if MeshCollider
        MeshCollider meshCol = collider as MeshCollider;
        if (meshCol != null && !meshCol.convex)
        {
            // Concave MeshCollider - replace with BoxCollider
            Debug.Log($"  ⚠️ Replacing concave MeshCollider with BoxCollider");
            
            Bounds bounds = meshCol.bounds;
            Vector3 center = bounds.center - obj.transform.position;
            Vector3 size = bounds.size;
            
            DestroyImmediate(meshCol);
            
            BoxCollider boxCol = obj.AddComponent<BoxCollider>();
            boxCol.center = center;
            boxCol.size = size;
            boxCol.isTrigger = true;
            
            collidersFixed++;
            return;
        }
        
        // Ensure isTrigger is set
        if (!collider.isTrigger)
        {
            collider.isTrigger = true;
            Debug.Log($"  → Set collider.isTrigger = true");
        }
    }

    private bool IsHUDElement(GameObject obj)
    {
        // Check name
        string name = obj.name.ToLower();
        if (name.Contains("hud") || name.Contains("menu") || name.Contains("button") || 
            name.Contains("panel") || name.Contains("level"))
        {
            return true;
        }
        
        // Check parent hierarchy
        Transform parent = obj.transform.parent;
        while (parent != null)
        {
            string parentName = parent.name.ToLower();
            if (parentName.Contains("hud") || parentName.Contains("menu") || parentName.Contains("canvas"))
            {
                return true;
            }
            parent = parent.parent;
        }
        
        return false;
    }

    private void ResetStatistics()
    {
        eventTriggersFound = 0;
        zoneManagerCallsFound = 0;
        videoPlayerCallsFound = 0;
        customEventsFound = 0;
        gazeTriggersAdded = 0;
        eventTriggersRemoved = 0;
        collidersFixed = 0;
    }

    private void PrintStatistics()
    {
        Debug.Log("=== MIGRATION STATISTICS ===");
        Debug.Log($"Event Triggers found: {eventTriggersFound}");
        Debug.Log($"  - ZoneManager calls: {zoneManagerCallsFound}");
        Debug.Log($"  - VideoPlayer calls: {videoPlayerCallsFound}");
        Debug.Log($"  - Custom events: {customEventsFound}");
        Debug.Log($"---");
        Debug.Log($"GazeHoverTrigger components added: {gazeTriggersAdded}");
        Debug.Log($"Event Triggers removed: {eventTriggersRemoved}");
        Debug.Log($"Colliders fixed: {collidersFixed}");
        Debug.Log($"---");
        Debug.Log($"TOTAL MIGRATED: {gazeTriggersAdded} objects");
    }
#endif

    // Helper to make Enumerable.Range work
    private static class Enumerable
    {
        public static IEnumerable<int> Range(int start, int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return start + i;
            }
        }
    }
}

#if UNITY_EDITOR
public static class GazeMigrationMenu
{
    [MenuItem("Tools/Gaze System/Migrate From Event Triggers")]
    public static void MigrateFromEventTriggers()
    {
        GameObject migrationObj = new GameObject("_GazeMigrationTool");
        GazeMigrationTool tool = migrationObj.AddComponent<GazeMigrationTool>();
        
        tool.removeEventTriggers = true;
        tool.fixMeshColliders = true;
        tool.defaultHoverDelay = 3f;
        tool.previewMode = false;
        
        tool.RunMigration();
        
        Object.DestroyImmediate(migrationObj);
        
        Debug.Log("✓ Migration complete! Save your scene and test.");
    }

    [MenuItem("Tools/Gaze System/Preview Migration (No Changes)")]
    public static void PreviewMigration()
    {
        GameObject migrationObj = new GameObject("_GazeMigrationTool");
        GazeMigrationTool tool = migrationObj.AddComponent<GazeMigrationTool>();
        
        tool.removeEventTriggers = true;
        tool.fixMeshColliders = true;
        tool.defaultHoverDelay = 3f;
        tool.previewMode = true;
        
        tool.RunMigration();
        
        Object.DestroyImmediate(migrationObj);
        
        Debug.Log("ℹ️ Preview complete. Run 'Migrate From Event Triggers' to apply.");
    }
}
#endif
