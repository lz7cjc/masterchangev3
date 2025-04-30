using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

/// <summary>
/// Helper component for the video placement workflow
/// Contains instructions and provides utility methods
/// </summary>
public class VideoPlacementWorkflow : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] private VideoDatabaseManager databaseManager;
    [SerializeField] private PolygonZoneManager zoneManager;
    [SerializeField] private AdvancedVideoPlacementManager placementManager;
    [SerializeField] private VideoPlacementCache cacheManager;

    [Header("Workflow Settings")]
    [SerializeField] private bool autoSetup = true;
    [SerializeField] private bool debugMode = true;

    // Status tracking
    private bool isSetup = false;
    private bool hasLoadedCache = false;

    private void Awake()
    {
        if (autoSetup)
        {
            SetupWorkflow();
        }
    }

    private void Start()
    {
        if (!isSetup && autoSetup)
        {
            SetupWorkflow();
        }
    }

    /// <summary>
    /// Find and setup all required components
    /// </summary>
    public void SetupWorkflow()
    {
        // Find database manager if not assigned
        if (databaseManager == null)
        {
            databaseManager = FindObjectOfType<VideoDatabaseManager>();
            if (databaseManager == null)
            {
                Debug.LogError("VideoDatabaseManager not found in scene! Please add one.");
                return;
            }
        }

        // Find zone manager if not assigned
        if (zoneManager == null)
        {
            zoneManager = FindObjectOfType<PolygonZoneManager>();
            if (zoneManager == null)
            {
                Debug.LogWarning("PolygonZoneManager not found in scene! Zone-based placement unavailable.");
            }
        }

        // Find placement manager if not assigned
        if (placementManager == null)
        {
            placementManager = FindObjectOfType<AdvancedVideoPlacementManager>();
            if (placementManager == null)
            {
                // Create one
                GameObject pmObj = new GameObject("AdvancedVideoPlacementManager");
                placementManager = pmObj.AddComponent<AdvancedVideoPlacementManager>();
                Debug.Log("Created AdvancedVideoPlacementManager");
            }
        }

        // Find cache manager if not assigned
        if (cacheManager == null)
        {
            cacheManager = FindObjectOfType<VideoPlacementCache>();
            if (cacheManager == null)
            {
                // Create one
                GameObject cacheObj = new GameObject("VideoPlacementCache");
                cacheManager = cacheObj.AddComponent<VideoPlacementCache>();
                Debug.Log("Created VideoPlacementCache");
            }
        }

        // Set debug mode
        if (placementManager != null)
        {
            // Set the debug mode field if it's accessible via reflection
            System.Reflection.FieldInfo debugField = placementManager.GetType().GetField("debugMode",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

            if (debugField != null)
            {
                debugField.SetValue(placementManager, debugMode);
            }
        }

        isSetup = true;

        // Log setup status
        if (debugMode)
        {
            Debug.Log("Video Placement Workflow setup complete!");
            Debug.Log($"Database Manager: {(databaseManager != null ? "Found" : "Missing")}");
            Debug.Log($"Zone Manager: {(zoneManager != null ? "Found" : "Missing")}");
            Debug.Log($"Placement Manager: {(placementManager != null ? "Found" : "Missing")}");
            Debug.Log($"Cache Manager: {(cacheManager != null ? "Found" : "Missing")}");
        }
    }

    /// <summary>
    /// Execute the full video placement workflow
    /// </summary>
    public void ExecuteWorkflow()
    {
        if (!isSetup)
        {
            SetupWorkflow();
        }

        if (!databaseManager.IsInitialized)
        {
            Debug.LogWarning("Database not initialized. Attempting to load...");
            databaseManager.LoadDatabaseFromJson();

            if (!databaseManager.IsInitialized)
            {
                Debug.LogError("Failed to initialize database. Workflow cannot continue.");
                return;
            }
        }

        // Step 1: Load cache if not loaded
        if (!hasLoadedCache && cacheManager != null)
        {
            cacheManager.LoadCache();
            hasLoadedCache = true;

            if (debugMode) Debug.Log("Cache loaded");
        }

        // Step 2: Place videos using placement manager
        if (placementManager != null)
        {
            placementManager.PlaceAllVideos();

            if (debugMode) Debug.Log("Videos placed");
        }
        else
        {
            Debug.LogError("Cannot place videos: Placement manager not found");
        }
    }

    /// <summary>
    /// Save the current video placements
    /// </summary>
    public void SavePlacements()
    {
        if (cacheManager != null)
        {
            cacheManager.SaveCache();

            if (debugMode) Debug.Log("Placements saved to cache");
        }
        else
        {
            Debug.LogError("Cannot save placements: Cache manager not found");
        }
    }

    /// <summary>
    /// Clear all videos from the scene
    /// </summary>
    public void ClearAllVideos()
    {
        if (placementManager != null)
        {
            placementManager.ClearAllVideos();

            if (debugMode) Debug.Log("Cleared all videos from scene");
        }
        else
        {
            Debug.LogError("Cannot clear videos: Placement manager not found");
        }
    }

    /// <summary>
    /// Add VideoAdjustmentHandler components to all video objects
    /// </summary>
    public void AddAdjustmentHandlersToVideos()
    {
        // Find all EnhancedVideoPlayer components in the scene
        EnhancedVideoPlayer[] videoPlayers = FindObjectsOfType<EnhancedVideoPlayer>();

        int count = 0;
        foreach (EnhancedVideoPlayer player in videoPlayers)
        {
            if (player.GetComponent<VideoAdjustmentHandler>() == null)
            {
                player.gameObject.AddComponent<VideoAdjustmentHandler>();
                count++;
            }
        }

        if (debugMode) Debug.Log($"Added VideoAdjustmentHandler to {count} video objects");
    }

    /// <summary>
    /// Basic workflow - place videos, add adjustment handlers, save on quit
    /// </summary>
    [ContextMenu("Run Complete Workflow")]
    public void RunCompleteWorkflow()
    {
        SetupWorkflow();
        ExecuteWorkflow();
        AddAdjustmentHandlersToVideos();
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(VideoPlacementWorkflow))]
public class VideoPlacementWorkflowEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        VideoPlacementWorkflow workflow = (VideoPlacementWorkflow)target;

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("Workflow Controls", EditorStyles.boldLabel);

        if (GUILayout.Button("Setup Workflow", GUILayout.Height(30)))
        {
            workflow.SetupWorkflow();
        }

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Place Videos"))
        {
            workflow.ExecuteWorkflow();
        }

        if (GUILayout.Button("Clear Videos"))
        {
            workflow.ClearAllVideos();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Save Placements"))
        {
            workflow.SavePlacements();
        }

        if (GUILayout.Button("Add Adjustment Handlers"))
        {
            workflow.AddAdjustmentHandlersToVideos();
        }

        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Run Complete Workflow", GUILayout.Height(30)))
        {
            workflow.RunCompleteWorkflow();
        }

        EditorGUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "WORKFLOW INSTRUCTIONS:\n\n" +
            "1. Make sure you have a VideoDatabaseManager in your scene\n" +
            "2. Set up your zones in the PolygonZoneManager\n" +
            "3. Click 'Run Complete Workflow' to place videos\n" +
            "4. Manually adjust video positions in the scene\n" +
            "5. Save placements when satisfied\n\n" +
            "TIP: You can use Zone Assignment Editor (Tools > Video Management) to manage videos and zones",
            MessageType.Info);
    }
}
#endif