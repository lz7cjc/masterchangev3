#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor tools for video placement management
/// </summary>
public class VideoPlacementTools : EditorWindow
{
    private AdvancedVideoPlacementManager placementManager;
    private VideoPlacementCache cacheManager;
    private VideoDatabaseManager databaseManager;
    private PolygonZoneManager zoneManager;

    private bool showSettings = false;
    private bool showControls = true;

    [MenuItem("Tools/Video Management/Video Placement Tools")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(VideoPlacementTools), false, "Video Placement Tools");
    }

    private void OnEnable()
    {
        FindRequiredComponents();
    }

    private void OnGUI()
    {
        GUILayout.Label("Video Placement Tools", EditorStyles.boldLabel);

        EditorGUILayout.Space(10);

        // Status
        EditorGUILayout.LabelField("Status:", GetStatusText());

        if (GUILayout.Button("Find Components"))
        {
            FindRequiredComponents();
        }

        EditorGUILayout.Space(5);

        // Settings
        showSettings = EditorGUILayout.Foldout(showSettings, "Required Components", true);
        if (showSettings)
        {
            EditorGUI.indentLevel++;

            placementManager = EditorGUILayout.ObjectField("Placement Manager", placementManager, typeof(AdvancedVideoPlacementManager), true) as AdvancedVideoPlacementManager;
            cacheManager = EditorGUILayout.ObjectField("Cache Manager", cacheManager, typeof(VideoPlacementCache), true) as VideoPlacementCache;
            databaseManager = EditorGUILayout.ObjectField("Database Manager", databaseManager, typeof(VideoDatabaseManager), true) as VideoDatabaseManager;
            zoneManager = EditorGUILayout.ObjectField("Zone Manager", zoneManager, typeof(PolygonZoneManager), true) as PolygonZoneManager;

            EditorGUI.indentLevel--;

            if (GUILayout.Button("Create Missing Components"))
            {
                CreateMissingComponents();
            }
        }

        EditorGUILayout.Space(10);

        // Controls
        showControls = EditorGUILayout.Foldout(showControls, "Video Controls", true);
        if (showControls)
        {
            EditorGUI.indentLevel++;

            // Database Controls
            EditorGUILayout.LabelField("Database", EditorStyles.boldLabel);

            if (GUILayout.Button("Load Database"))
            {
                if (databaseManager != null)
                {
                    var loadMethod = databaseManager.GetType().GetMethod("LoadDatabaseFromJson");
                    if (loadMethod != null)
                    {
                        loadMethod.Invoke(databaseManager, null);
                        Debug.Log("Database loaded");
                    }
                }
            }

            EditorGUILayout.Space(5);

            // Placement Controls
            EditorGUILayout.LabelField("Placement", EditorStyles.boldLabel);

            if (GUILayout.Button("Place Videos"))
            {
                PlaceAllVideos();
            }

            if (GUILayout.Button("Clear Videos"))
            {
                ClearVideos();
            }

            EditorGUILayout.Space(5);

            // Cache Controls
            EditorGUILayout.LabelField("Cache", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Save Placements"))
            {
                SaveCache();
            }

            if (GUILayout.Button("Load Placements"))
            {
                LoadCache();
            }

            GUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Video Components
            EditorGUILayout.LabelField("Components", EditorStyles.boldLabel);

            if (GUILayout.Button("Add Components To All Videos"))
            {
                AddComponentsToAllVideos();
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(10);

        // One-click workflow
        if (GUILayout.Button("Run Complete Workflow", GUILayout.Height(30)))
        {
            RunCompleteWorkflow();
        }

        EditorGUILayout.HelpBox(
            "This tool helps you manage video placement in your scene.\n\n" +
            "1. Use 'Find Components' to locate required components\n" +
            "2. Use 'Place Videos' to place videos from your database\n" +
            "3. Use 'Save Placements' to save positions\n" +
            "4. Use 'Add Components To All Videos' to add required components\n\n" +
            "Or simply click 'Run Complete Workflow' to do everything at once.",
            MessageType.Info);
    }

    private string GetStatusText()
    {
        List<string> missing = new List<string>();

        if (placementManager == null) missing.Add("Placement Manager");
        if (cacheManager == null) missing.Add("Cache Manager");
        if (databaseManager == null) missing.Add("Database Manager");
        if (zoneManager == null) missing.Add("Zone Manager");

        if (missing.Count == 0)
        {
            return "All components found";
        }
        else
        {
            return "Missing: " + string.Join(", ", missing);
        }
    }

    private void FindRequiredComponents()
    {
        placementManager = FindObjectOfType<AdvancedVideoPlacementManager>();
        cacheManager = FindObjectOfType<VideoPlacementCache>();
        databaseManager = FindObjectOfType<VideoDatabaseManager>();
        zoneManager = FindObjectOfType<PolygonZoneManager>();
    }

    private void CreateMissingComponents()
    {
        // Find or create VideoSystem GameObject
        GameObject videoSystem = GameObject.Find("VideoSystem");
        if (videoSystem == null)
        {
            videoSystem = new GameObject("VideoSystem");
            Debug.Log("Created VideoSystem GameObject");
        }

        // Create Database Manager if needed
        if (databaseManager == null)
        {
            databaseManager = videoSystem.GetComponent<VideoDatabaseManager>();
            if (databaseManager == null)
            {
                databaseManager = videoSystem.AddComponent<VideoDatabaseManager>();
                Debug.Log("Added VideoDatabaseManager to VideoSystem");
            }
        }

        // Create Zone Manager if needed
        if (zoneManager == null)
        {
            zoneManager = videoSystem.GetComponent<PolygonZoneManager>();
            if (zoneManager == null)
            {
                zoneManager = videoSystem.AddComponent<PolygonZoneManager>();
                Debug.Log("Added PolygonZoneManager to VideoSystem");
            }
        }

        // Create Placement Manager if needed
        if (placementManager == null)
        {
            placementManager = videoSystem.GetComponent<AdvancedVideoPlacementManager>();
            if (placementManager == null)
            {
                placementManager = videoSystem.AddComponent<AdvancedVideoPlacementManager>();
                Debug.Log("Added AdvancedVideoPlacementManager to VideoSystem");
            }
        }

        // Create Cache Manager if needed
        if (cacheManager == null)
        {
            cacheManager = videoSystem.GetComponent<VideoPlacementCache>();
            if (cacheManager == null)
            {
                cacheManager = videoSystem.AddComponent<VideoPlacementCache>();
                Debug.Log("Added VideoPlacementCache to VideoSystem");
            }
        }

        Debug.Log("Created all missing components");
    }

    private void PlaceAllVideos()
    {
        if (databaseManager != null && !databaseManager.IsInitialized)
        {
            // Try to load the database
            var loadMethod = databaseManager.GetType().GetMethod("LoadDatabaseFromJson");
            if (loadMethod != null)
            {
                loadMethod.Invoke(databaseManager, null);
            }
        }

        // Call the PlaceAllVideos method
        if (placementManager != null)
        {
            placementManager.PlaceAllVideos();
            Debug.Log("Videos placed");
        }
        else
        {
            Debug.LogError("Placement Manager not found");
        }
    }

    private void ClearVideos()
    {
        // Call the ClearAllVideos method
        if (placementManager != null)
        {
            placementManager.ClearAllVideos(false);
            Debug.Log("Videos cleared");
        }
        else
        {
            Debug.LogError("Placement Manager not found");
        }
    }

    private void SaveCache()
    {
        if (cacheManager != null)
        {
            cacheManager.SaveCache();
            Debug.Log("Cache saved");
        }
        else
        {
            Debug.LogError("Cache Manager not found");
        }
    }

    private void LoadCache()
    {
        if (cacheManager != null)
        {
            cacheManager.LoadCache();
            Debug.Log("Cache loaded");
        }
        else
        {
            Debug.LogError("Cache Manager not found");
        }
    }

    private void AddComponentsToAllVideos()
    {
        EnhancedVideoPlayer[] videoPlayers = FindObjectsOfType<EnhancedVideoPlayer>();

        int adjustmentHandlersAdded = 0;
        int collidersAdded = 0;

        foreach (var player in videoPlayers)
        {
            if (player == null) continue;

            GameObject videoObj = player.gameObject;

            // Add VideoAdjustmentHandler if missing
            if (videoObj.GetComponent<VideoAdjustmentHandler>() == null)
            {
                videoObj.AddComponent<VideoAdjustmentHandler>();
                adjustmentHandlersAdded++;
            }

            // Add BoxCollider if missing
            if (videoObj.GetComponent<BoxCollider>() == null)
            {
                BoxCollider boxCollider = videoObj.AddComponent<BoxCollider>();
                boxCollider.size = new Vector3(2, 2, 0.2f);
                boxCollider.isTrigger = true;
                collidersAdded++;
            }
        }

        Debug.Log($"Added {adjustmentHandlersAdded} adjustment handlers and {collidersAdded} colliders to videos");
    }

    private void RunCompleteWorkflow()
    {
        FindRequiredComponents();

        if (databaseManager == null || placementManager == null || cacheManager == null)
        {
            CreateMissingComponents();
            FindRequiredComponents(); // Refresh references
        }

        PlaceAllVideos();
        AddComponentsToAllVideos();
        SaveCache();

        Debug.Log("Complete workflow executed");
    }
}
#endif