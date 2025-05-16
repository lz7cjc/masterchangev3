using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

#if UNITY_EDITOR
/// <summary>
/// Helper component for the video placement workflow
/// This is an editor window that coordinates the video placement process
/// </summary>
public class VideoPlacementWorkflow : EditorWindow
{
    // Required component references
    private VideoDatabaseManager databaseManager;
    private PolygonZoneManager zoneManager;
    private AdvancedVideoPlacementManager placementManager;
    private VideoPlacementCache cacheManager;

    // Workflow settings
    private bool debugMode = true;
    private Vector3 defaultColliderSize = new Vector3(2, 2, 0.2f);
    private float selectionTimeThreshold = 2.0f;
    private string playerPrefsKey = "360VideoURL";
    private string videoAppScene = "360VideoApp";

    // Foldout states
    private bool showRequiredComponents = true;
    private bool showSettings = false;

    // Status tracking
    private bool isSetup = false;
    private bool hasLoadedCache = false;

    [MenuItem("Tools/Video Management/Video Placement Workflow")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(VideoPlacementWorkflow), false, "Video Placement Workflow");
    }

    private void OnEnable()
    {
        // Try to find components
        FindRequiredComponents();
    }

    private void OnGUI()
    {
        GUILayout.Label("Video Placement Workflow", EditorStyles.boldLabel);

        EditorGUILayout.Space(10);

        // Required Components Section
        showRequiredComponents = EditorGUILayout.Foldout(showRequiredComponents, "Required Components", true);
        if (showRequiredComponents)
        {
            EditorGUI.indentLevel++;

            // Database Manager
            EditorGUILayout.BeginHorizontal();
            databaseManager = (VideoDatabaseManager)EditorGUILayout.ObjectField("Database Manager", databaseManager, typeof(VideoDatabaseManager), true);
            if (GUILayout.Button("Find", GUILayout.Width(50)))
            {
                databaseManager = FindObjectOfType<VideoDatabaseManager>();
            }
            EditorGUILayout.EndHorizontal();

            // Zone Manager
            EditorGUILayout.BeginHorizontal();
            zoneManager = (PolygonZoneManager)EditorGUILayout.ObjectField("Zone Manager", zoneManager, typeof(PolygonZoneManager), true);
            if (GUILayout.Button("Find", GUILayout.Width(50)))
            {
                zoneManager = FindObjectOfType<PolygonZoneManager>();
            }
            EditorGUILayout.EndHorizontal();

            // Placement Manager
            EditorGUILayout.BeginHorizontal();
            placementManager = (AdvancedVideoPlacementManager)EditorGUILayout.ObjectField("Placement Manager", placementManager, typeof(AdvancedVideoPlacementManager), true);
            if (GUILayout.Button("Find", GUILayout.Width(50)))
            {
                placementManager = FindObjectOfType<AdvancedVideoPlacementManager>();
            }
            EditorGUILayout.EndHorizontal();

            // Cache Manager
            EditorGUILayout.BeginHorizontal();
            cacheManager = (VideoPlacementCache)EditorGUILayout.ObjectField("Cache Manager", cacheManager, typeof(VideoPlacementCache), true);
            if (GUILayout.Button("Find", GUILayout.Width(50)))
            {
                cacheManager = FindObjectOfType<VideoPlacementCache>();
            }
            EditorGUILayout.EndHorizontal();

            // Create missing components button
            if (GUILayout.Button("Create Missing Components"))
            {
                CreateMissingComponents();
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(5);

        // Settings Section
        showSettings = EditorGUILayout.Foldout(showSettings, "Workflow Settings", true);
        if (showSettings)
        {
            EditorGUI.indentLevel++;

            debugMode = EditorGUILayout.Toggle("Debug Mode", debugMode);
            defaultColliderSize = EditorGUILayout.Vector3Field("Default Collider Size", defaultColliderSize);
            selectionTimeThreshold = EditorGUILayout.FloatField("Selection Time Threshold", selectionTimeThreshold);
            playerPrefsKey = EditorGUILayout.TextField("PlayerPrefs Key", playerPrefsKey);
            videoAppScene = EditorGUILayout.TextField("Video App Scene", videoAppScene);

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(10);

        // Status Section
        EditorGUILayout.LabelField("Status:", isSetup ? "Ready" : "Not Setup");

        // Need to check if we have setup properly
        isSetup = databaseManager != null && placementManager != null;

        // Workflow Controls
        EditorGUILayout.LabelField("Workflow Controls", EditorStyles.boldLabel);

        EditorGUI.BeginDisabledGroup(!isSetup);

        if (GUILayout.Button("Setup Workflow", GUILayout.Height(30)))
        {
            SetupWorkflow();
        }

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Place Videos"))
        {
            ExecuteWorkflow();
        }

        if (GUILayout.Button("Clear Videos"))
        {
            if (EditorUtility.DisplayDialog("Clear Videos",
                "Are you sure you want to clear all videos from the scene? This operation can't be undone.",
                "Clear Videos", "Cancel"))
            {
                ClearVideos();
            }
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Save Placements"))
        {
            SavePlacements();
        }

        if (GUILayout.Button("Load Placements"))
        {
            LoadPlacements();
        }

        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Add Missing Components"))
        {
            AddMissingComponents();
        }

        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space(10);

        // Run Complete Workflow Button
        if (GUILayout.Button("Run Complete Workflow", GUILayout.Height(30)))
        {
            RunCompleteWorkflow();
        }

        EditorGUILayout.Space(5);

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

    // Find required components in the scene
    private void FindRequiredComponents()
    {
        databaseManager = FindObjectOfType<VideoDatabaseManager>();
        zoneManager = FindObjectOfType<PolygonZoneManager>();
        placementManager = FindObjectOfType<AdvancedVideoPlacementManager>();
        cacheManager = FindObjectOfType<VideoPlacementCache>();

        isSetup = databaseManager != null && placementManager != null;
    }

    // Create any missing components
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

        // Now try to set up references between components
        SetupComponentReferences();

        // Mark the system as set up
        isSetup = true;
        Debug.Log("Created all missing components");
    }

    // Setup references between components
    private void SetupComponentReferences()
    {
        // Set up references in placement manager
        if (placementManager != null)
        {
            SerializedObject serializedObject = new SerializedObject(placementManager);

            // Set database manager reference
            SerializedProperty dbManagerProp = serializedObject.FindProperty("databaseManager");
            if (dbManagerProp != null)
            {
                dbManagerProp.objectReferenceValue = databaseManager;
            }

            // Set zone manager reference
            SerializedProperty zoneManagerProp = serializedObject.FindProperty("zoneManager");
            if (zoneManagerProp != null)
            {
                zoneManagerProp.objectReferenceValue = zoneManager;
            }

            // Apply changes
            serializedObject.ApplyModifiedProperties();
        }
    }

    /// <summary>
    /// Set up workflow connections
    /// </summary>
    private void SetupWorkflow()
    {
        FindRequiredComponents();

        if (!isSetup)
        {
            CreateMissingComponents();
        }

        // Configure components if needed
        ConfigureComponents();

        Debug.Log("Workflow setup complete!");
    }

    // Configure components with default settings
    private void ConfigureComponents()
    {
        // Configure database manager if needed
        if (databaseManager != null)
        {
            SerializedObject serializedObject = new SerializedObject(databaseManager);

            // Set default database file path if not set
            SerializedProperty filePathProp = serializedObject.FindProperty("databaseFilePath");
            if (filePathProp != null && string.IsNullOrEmpty(filePathProp.stringValue))
            {
                filePathProp.stringValue = "Assets/Resources/film_data.json";
            }

            // Apply changes
            serializedObject.ApplyModifiedProperties();
        }

        // Configure placement manager if needed
        if (placementManager != null)
        {
            SerializedObject serializedObject = new SerializedObject(placementManager);

            // Set debug mode
            SerializedProperty debugProp = serializedObject.FindProperty("debugMode");
            if (debugProp != null)
            {
                debugProp.boolValue = debugMode;
            }

            // Set selection time threshold
            SerializedProperty thresholdProp = serializedObject.FindProperty("selectionTimeThreshold");
            if (thresholdProp != null)
            {
                thresholdProp.floatValue = selectionTimeThreshold;
            }

            // Set playerPrefs key
            SerializedProperty keyProp = serializedObject.FindProperty("playerPrefsKey");
            if (keyProp != null)
            {
                keyProp.stringValue = playerPrefsKey;
            }

            // Set video app scene
            SerializedProperty sceneProp = serializedObject.FindProperty("videoAppScene");
            if (sceneProp != null)
            {
                sceneProp.stringValue = videoAppScene;
            }

            // Apply changes
            serializedObject.ApplyModifiedProperties();
        }
    }

    /// <summary>
    /// Execute the main workflow: place videos
    /// </summary>
    private void ExecuteWorkflow()
    {
        if (!isSetup)
        {
            SetupWorkflow();
        }

        if (databaseManager != null && !databaseManager.IsInitialized)
        {
            Debug.Log("Loading database...");

            // Try to invoke the LoadDatabaseFromJson method
            var loadMethod = databaseManager.GetType().GetMethod("LoadDatabaseFromJson");
            if (loadMethod != null)
            {
                loadMethod.Invoke(databaseManager, null);
            }
        }

        // Check if database initialized after loading
        if (databaseManager != null && !databaseManager.IsInitialized)
        {
            Debug.LogError("Failed to initialize database. Workflow cannot continue.");
            return;
        }

        // Execute the placement
        if (placementManager != null)
        {
            // Try to invoke the PlaceAllVideos method
            var placeMethod = placementManager.GetType().GetMethod("PlaceAllVideos");
            if (placeMethod != null)
            {
                Debug.Log("Placing videos...");
                placeMethod.Invoke(placementManager, null);
            }
            else
            {
                Debug.LogError("PlaceAllVideos method not found on AdvancedVideoPlacementManager");
            }
        }
        else
        {
            Debug.LogError("Cannot place videos: Placement manager not found");
        }
    }

    /// <summary>
    /// Clear all videos from the scene
    /// </summary>
    private void ClearVideos()
    {
        if (placementManager != null)
        {
            // Try to invoke the ClearAllVideos method with false parameter
            var clearMethod = placementManager.GetType().GetMethod("ClearAllVideos");
            if (clearMethod != null)
            {
                Debug.Log("Clearing videos...");
                clearMethod.Invoke(placementManager, new object[] { false });
            }
            else
            {
                Debug.LogError("ClearAllVideos method not found on AdvancedVideoPlacementManager");
            }
        }
        else
        {
            Debug.LogError("Cannot clear videos: Placement manager not found");
        }
    }

    /// <summary>
    /// Save placements to cache
    /// </summary>
    private void SavePlacements()
    {
        if (cacheManager != null)
        {
            // Try to invoke the SaveCache method
            var saveMethod = cacheManager.GetType().GetMethod("SaveCache");
            if (saveMethod != null)
            {
                Debug.Log("Saving placements...");
                saveMethod.Invoke(cacheManager, null);
            }
            else
            {
                Debug.LogError("SaveCache method not found on VideoPlacementCache");
            }
        }
        else
        {
            Debug.LogError("Cannot save placements: Cache manager not found");
        }
    }

    /// <summary>
    /// Load placements from cache
    /// </summary>
    private void LoadPlacements()
    {
        if (cacheManager != null)
        {
            // Try to invoke the LoadCache method
            var loadMethod = cacheManager.GetType().GetMethod("LoadCache");
            if (loadMethod != null)
            {
                Debug.Log("Loading placements...");
                loadMethod.Invoke(cacheManager, null);
                hasLoadedCache = true;
            }
            else
            {
                Debug.LogError("LoadCache method not found on VideoPlacementCache");
            }
        }
        else
        {
            Debug.LogError("Cannot load placements: Cache manager not found");
        }
    }

    /// <summary>
    /// Add missing components to videos
    /// </summary>
    private void AddMissingComponents()
    {
        // Open the VideoSelectionSetup window
        VideoSelectionSetup selectionSetup = EditorWindow.GetWindow<VideoSelectionSetup>("Video Selection Setup");
        if (selectionSetup != null)
        {
            // Bring window to front
            selectionSetup.Focus();

            // Maybe we can trigger the function using reflection
            var addComponentsMethod = selectionSetup.GetType().GetMethod("AddComponentsToAllVideos",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (addComponentsMethod != null)
            {
                Debug.Log("Adding missing components to videos...");
                addComponentsMethod.Invoke(selectionSetup, null);
            }
            else
            {
                // Just show a message to the user
                Debug.Log("Please click 'Add Components To All Videos' in the Video Selection Setup window");
            }
        }
        else
        {
            // If we can't get the window, just create it
            VideoSelectionSetup.ShowWindow();
            Debug.Log("Please click 'Add Components To All Videos' in the Video Selection Setup window");
        }
    }

    /// <summary>
    /// Run the complete workflow
    /// </summary>
    private void RunCompleteWorkflow()
    {
        SetupWorkflow();
        ExecuteWorkflow();
        AddMissingComponents();
        SavePlacements();

        Debug.Log("Complete workflow executed successfully!");
    }
}
#endif