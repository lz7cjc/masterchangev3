using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Video Setup Tool - A utility to quickly add, configure, and check video components
/// </summary>
public class VideoSetupTool : EditorWindow
{
    private bool showAddComponents = true;
    private bool showBulkOperations = true;
    private bool showTroubleshooting = true;
    private bool showSystemInfo = true;

    private Vector2 scrollPosition;

    private SimplifiedVideoPlacementManager placementManager;
    private VideoDatabaseManager databaseManager;
    private int videosInScene = 0;
    private int positionsCached = 0;

    [MenuItem("Tools/Video Tools/Video Setup")]
    public static void ShowWindow()
    {
        VideoSetupTool window = GetWindow<VideoSetupTool>("Video Setup Tool");
        window.minSize = new Vector2(350, 500);
    }

    private void OnEnable()
    {
        FindManagers();
        CountVideos();
    }

    private void OnFocus()
    {
        // Refresh data when the window gains focus
        FindManagers();
        CountVideos();
    }

    private void FindManagers()
    {
        placementManager = FindObjectOfType<SimplifiedVideoPlacementManager>();
        databaseManager = FindObjectOfType<VideoDatabaseManager>();
    }

    private void CountVideos()
    {
        // Count videos in scene
        videosInScene = FindObjectsOfType<SimplifiedVideoHandler>().Length;

        // Count legacy components
        videosInScene += FindObjectsOfType<EnhancedVideoPlayer>().Length;

        // Try to count ToggleShowHideVideo components
        MonoBehaviour[] allMono = FindObjectsOfType<MonoBehaviour>();
        foreach (MonoBehaviour mono in allMono)
        {
            if (mono.GetType().Name == "ToggleShowHideVideo")
            {
                videosInScene++;
            }
        }

        // Count cached positions if cache file exists
        if (placementManager != null)
        {
            // Use reflection to access private field
            var cacheField = typeof(SimplifiedVideoPlacementManager).GetField("placementCache",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            if (cacheField != null)
            {
                var cache = cacheField.GetValue(placementManager);
                var placementsField = cache.GetType().GetField("placements");

                if (placementsField != null)
                {
                    var placements = placementsField.GetValue(cache) as System.Collections.ICollection;
                    if (placements != null)
                    {
                        positionsCached = placements.Count;
                    }
                }
            }
        }
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("Video Setup Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        // System status
        DrawSystemStatus();
        EditorGUILayout.Space(10);

        // Add Components section
        showAddComponents = EditorGUILayout.Foldout(showAddComponents, "Add Components", true);
        if (showAddComponents)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (GUILayout.Button("Add Placement Manager"))
            {
                CreatePlacementManager();
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Upgrade Legacy Components"))
            {
                UpgradeLegacyComponents();
            }

            EditorGUILayout.HelpBox(
                "This will convert all old video components (EnhancedVideoPlayer, ToggleShowHideVideo) " +
                "to SimplifiedVideoHandler.", MessageType.Info);

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space(10);

        // Bulk Operations section
        showBulkOperations = EditorGUILayout.Foldout(showBulkOperations, "Bulk Operations", true);
        if (showBulkOperations)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (GUILayout.Button("Place Videos in Zones"))
            {
                PlaceVideos();
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Save Video Positions"))
            {
                SavePositions();
            }

            EditorGUILayout.Space(5);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Clear Videos"))
            {
                ClearVideos(false);
            }

            if (GUILayout.Button("Delete ALL Video Objects"))
            {
                DeleteAllVideos();
            }

            GUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space(10);

        // Troubleshooting section
        showTroubleshooting = EditorGUILayout.Foldout(showTroubleshooting, "Troubleshooting", true);
        if (showTroubleshooting)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (GUILayout.Button("Initialize Database"))
            {
                InitializeDatabase();
            }

            EditorGUILayout.HelpBox(
                "If videos aren't showing up, try initializing the database.\n" +
                "This will load video data from the database file.", MessageType.Info);

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Fix Missing References"))
            {
                FixMissingReferences();
            }

            EditorGUILayout.HelpBox(
                "This will try to fix any missing references between components.", MessageType.Info);

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space(10);

        // System Info section
        showSystemInfo = EditorGUILayout.Foldout(showSystemInfo, "System Information", true);
        if (showSystemInfo)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Detected Components:", EditorStyles.boldLabel);

            string placementStatus = placementManager != null ? "Found" : "Missing";
            EditorGUILayout.LabelField($"Placement Manager: {placementStatus}");

            string databaseStatus = databaseManager != null ? "Found" : "Missing";
            EditorGUILayout.LabelField($"Database Manager: {databaseStatus}");

            string zoneManagerStatus = "N/A";
            if (placementManager != null)
            {
                var zoneField = typeof(SimplifiedVideoPlacementManager).GetField("zoneManager",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                if (zoneField != null)
                {
                    var zoneManager = zoneField.GetValue(placementManager);
                    zoneManagerStatus = zoneManager != null ? "Found" : "Missing";
                }
            }
            EditorGUILayout.LabelField($"Zone Manager: {zoneManagerStatus}");

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Video Objects:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Videos in Scene: {videosInScene}");
            EditorGUILayout.LabelField($"Cached Positions: {positionsCached}");

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Refresh Status"))
            {
                FindManagers();
                CountVideos();
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "This tool helps set up and manage videos in your scene.\n\n" +
            "1. Add the Placement Manager if not present\n" +
            "2. Upgrade legacy components if needed\n" +
            "3. Place videos in zones\n" +
            "4. Save positions after adjusting", MessageType.Info);

        EditorGUILayout.EndScrollView();
    }

    private void DrawSystemStatus()
    {
        GUIStyle statusStyle = new GUIStyle(EditorStyles.helpBox);
        statusStyle.richText = true;

        string statusMessage = "<b>System Status:</b> ";
        if (placementManager == null || databaseManager == null)
        {
            statusMessage += "<color=yellow>Components Missing</color>";
        }
        else if (videosInScene == 0)
        {
            statusMessage += "<color=yellow>No Videos</color>";
        }
        else
        {
            statusMessage += "<color=green>Ready</color>";
        }

        EditorGUILayout.LabelField(statusMessage, statusStyle);
    }

    #region Actions

    private void CreatePlacementManager()
    {
        // Check if already exists
        if (placementManager != null)
        {
            if (EditorUtility.DisplayDialog("Manager Already Exists",
                "A SimplifiedVideoPlacementManager already exists in the scene. Create another one?",
                "Yes", "Cancel"))
            {
                // Continue if user wants to create another one
            }
            else
            {
                // Select the existing one
                Selection.activeGameObject = placementManager.gameObject;
                return;
            }
        }

        // Create a new GameObject for the manager
        GameObject managerObj = new GameObject("SimplifiedVideoPlacementManager");
        placementManager = managerObj.AddComponent<SimplifiedVideoPlacementManager>();

        // Try to find required components
        databaseManager = FindObjectOfType<VideoDatabaseManager>();
        PolygonZoneManager zoneManager = FindObjectOfType<PolygonZoneManager>();

        // Set references via reflection
        var dbField = typeof(SimplifiedVideoPlacementManager).GetField("databaseManager",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        var zoneField = typeof(SimplifiedVideoPlacementManager).GetField("zoneManager",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

        if (dbField != null && databaseManager != null)
        {
            dbField.SetValue(placementManager, databaseManager);
        }

        if (zoneField != null && zoneManager != null)
        {
            zoneField.SetValue(placementManager, zoneManager);
        }

        // Select the new manager
        Selection.activeGameObject = managerObj;

        // Log and refresh
        Debug.Log("Created SimplifiedVideoPlacementManager");
        FindManagers();
        CountVideos();
    }

    private void UpgradeLegacyComponents()
    {
        if (placementManager == null)
        {
            if (EditorUtility.DisplayDialog("Manager Missing",
                "No SimplifiedVideoPlacementManager found. Create one first?",
                "Create Manager", "Cancel"))
            {
                CreatePlacementManager();
            }
            else
            {
                return;
            }
        }

        if (EditorUtility.DisplayDialog("Upgrade Legacy Components",
            "This will convert all old video components (EnhancedVideoPlayer, ToggleShowHideVideo) to the new SimplifiedVideoHandler.\n\n" +
            "The old components will be disabled but not removed.\n\nContinue?",
            "Yes, Upgrade Components", "Cancel"))
        {
            // Call the upgrade method
            placementManager.UpgradeLegacyComponents();

            // Refresh
            CountVideos();
        }
    }

    private void PlaceVideos()
    {
        if (placementManager == null)
        {
            if (EditorUtility.DisplayDialog("Manager Missing",
                "No SimplifiedVideoPlacementManager found. Create one first?",
                "Create Manager", "Cancel"))
            {
                CreatePlacementManager();
            }
            else
            {
                return;
            }
        }

        if (databaseManager == null || !databaseManager.IsInitialized)
        {
            if (EditorUtility.DisplayDialog("Database Not Ready",
                "VideoDatabaseManager is not ready. Would you like to initialize it first?",
                "Initialize Database", "Cancel"))
            {
                InitializeDatabase();
            }
            else
            {
                return;
            }
        }

        // Confirm if there are already videos in the scene
        if (videosInScene > 0)
        {
            if (!EditorUtility.DisplayDialog("Videos Already in Scene",
                $"There are already {videosInScene} videos in the scene. Placing new videos will clear existing ones.\n\nContinue?",
                "Yes, Replace Videos", "Cancel"))
            {
                return;
            }
        }

        // Call the place method
        placementManager.PlaceAllVideos();

        // Refresh
        CountVideos();
    }

    private void SavePositions()
    {
        if (placementManager == null)
        {
            if (EditorUtility.DisplayDialog("Manager Missing",
                "No SimplifiedVideoPlacementManager found. Create one first?",
                "Create Manager", "Cancel"))
            {
                CreatePlacementManager();
            }
            else
            {
                return;
            }
        }

        if (videosInScene == 0)
        {
            EditorUtility.DisplayDialog("No Videos Found",
                "No videos found in the scene. Place videos first.", "OK");
            return;
        }

        // Call the save method
        placementManager.SavePlacementCache();

        // Refresh
        CountVideos();

        EditorUtility.DisplayDialog("Save Complete",
            $"Saved positions for videos in the scene.", "OK");
    }

    private void ClearVideos(bool removeAllVideoObjects)
    {
        if (placementManager == null)
        {
            if (EditorUtility.DisplayDialog("Manager Missing",
                "No SimplifiedVideoPlacementManager found. Create one first?",
                "Create Manager", "Cancel"))
            {
                CreatePlacementManager();
            }
            else
            {
                return;
            }
        }

        if (videosInScene == 0)
        {
            EditorUtility.DisplayDialog("No Videos Found",
                "No videos found in the scene to clear.", "OK");
            return;
        }

        string message = removeAllVideoObjects ?
            "This will remove ALL video objects from the scene, including untracked ones." :
            "This will remove videos that are tracked by the placement manager.";

        if (EditorUtility.DisplayDialog("Clear Videos",
            $"{message}\n\nContinue?",
            "Yes, Clear Videos", "Cancel"))
        {
            // Call the clear method
            placementManager.ClearAllVideos(removeAllVideoObjects);

            // Refresh
            CountVideos();
        }
    }

    private void DeleteAllVideos()
    {
        if (videosInScene == 0)
        {
            EditorUtility.DisplayDialog("No Videos Found",
                "No videos found in the scene to delete.", "OK");
            return;
        }

        if (EditorUtility.DisplayDialog("Delete ALL Videos",
            "Are you sure you want to delete ALL video objects from the scene?\n\n" +
            "This will remove both tracked and untracked video objects and cannot be undone.",
            "Yes, Delete All", "Cancel"))
        {
            if (placementManager != null)
            {
                placementManager.ClearAllVideos(true);
            }
            else
            {
                // If no placement manager, manually delete all video objects
                SimplifiedVideoHandler[] handlers = FindObjectsOfType<SimplifiedVideoHandler>();
                foreach (SimplifiedVideoHandler handler in handlers)
                {
                    if (handler != null && handler.gameObject != null)
                    {
                        DestroyImmediate(handler.gameObject);
                    }
                }

                // Legacy components
                EnhancedVideoPlayer[] videoPlayers = FindObjectsOfType<EnhancedVideoPlayer>();
                foreach (EnhancedVideoPlayer player in videoPlayers)
                {
                    if (player != null && player.gameObject != null)
                    {
                        DestroyImmediate(player.gameObject);
                    }
                }

                // Find any ToggleShowHideVideo components
                MonoBehaviour[] allMono = FindObjectsOfType<MonoBehaviour>();
                foreach (MonoBehaviour mono in allMono)
                {
                    if (mono != null && mono.GetType().Name == "ToggleShowHideVideo")
                    {
                        DestroyImmediate(mono.gameObject);
                    }
                }
            }

            // Refresh
            CountVideos();
        }
    }

    private void InitializeDatabase()
    {
        if (databaseManager == null)
        {
            // Try to find it first
            databaseManager = FindObjectOfType<VideoDatabaseManager>();

            // If still null, create it
            if (databaseManager == null)
            {
                GameObject dbObj = new GameObject("VideoDatabaseManager");
                databaseManager = dbObj.AddComponent<VideoDatabaseManager>();
                Debug.Log("Created VideoDatabaseManager");
            }
        }

        // Try to initialize the database
        if (databaseManager != null)
        {
            // Call the load method using reflection
            System.Reflection.MethodInfo loadMethod = typeof(VideoDatabaseManager).GetMethod("LoadDatabaseFromJson",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

            if (loadMethod != null)
            {
                loadMethod.Invoke(databaseManager, null);
                Debug.Log("Initialized VideoDatabaseManager");
            }
            else
            {
                Debug.LogError("Could not find LoadDatabaseFromJson method");
            }

            // Alternatively, try the parse method if load failed
            if (!databaseManager.IsInitialized)
            {
                System.Reflection.MethodInfo parseMethod = typeof(VideoDatabaseManager).GetMethod("ParseCloudDatabase",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

                if (parseMethod != null)
                {
                    parseMethod.Invoke(databaseManager, null);
                    Debug.Log("Parsed cloud database");
                }
            }

            // Check if initialization worked
            if (databaseManager.IsInitialized)
            {
                EditorUtility.DisplayDialog("Database Initialized",
                    $"Database initialized with {databaseManager.EntryCount} video entries.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Database Initialization Failed",
                    "Could not initialize database. Check console for errors.", "OK");
            }
        }

        // Refresh
        FindManagers();
        CountVideos();
    }

    private void FixMissingReferences()
    {
        int fixedCount = 0;

        // First ensure managers are found
        if (databaseManager == null)
        {
            databaseManager = FindObjectOfType<VideoDatabaseManager>();
            if (databaseManager == null)
            {
                GameObject dbObj = new GameObject("VideoDatabaseManager");
                databaseManager = dbObj.AddComponent<VideoDatabaseManager>();
                Debug.Log("Created VideoDatabaseManager");
                fixedCount++;
            }
        }

        if (placementManager == null)
        {
            placementManager = FindObjectOfType<SimplifiedVideoPlacementManager>();
            if (placementManager == null)
            {
                GameObject managerObj = new GameObject("SimplifiedVideoPlacementManager");
                placementManager = managerObj.AddComponent<SimplifiedVideoPlacementManager>();
                Debug.Log("Created SimplifiedVideoPlacementManager");
                fixedCount++;
            }
        }

        // Fix database manager initialization if needed
        if (databaseManager != null && !databaseManager.IsInitialized)
        {
            // Try to call initialize methods
            System.Reflection.MethodInfo loadMethod = typeof(VideoDatabaseManager).GetMethod("LoadDatabaseFromJson",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

            if (loadMethod != null)
            {
                loadMethod.Invoke(databaseManager, null);
                Debug.Log("Reinitialized VideoDatabaseManager");
                fixedCount++;
            }
        }

        // Try to set references between managers
        if (placementManager != null && databaseManager != null)
        {
            var dbField = typeof(SimplifiedVideoPlacementManager).GetField("databaseManager",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

            if (dbField != null)
            {
                object currentValue = dbField.GetValue(placementManager);
                if (currentValue == null)
                {
                    dbField.SetValue(placementManager, databaseManager);
                    Debug.Log("Fixed database manager reference");
                    fixedCount++;
                }
            }
        }

        // Find any video handlers with missing data and try to fix them
        SimplifiedVideoHandler[] handlers = FindObjectsOfType<SimplifiedVideoHandler>();
        foreach (SimplifiedVideoHandler handler in handlers)
        {
            if (handler == null) continue;

            // Try to check if videoUrl field is empty using reflection
            var urlField = typeof(SimplifiedVideoHandler).GetField("videoUrl",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            if (urlField != null)
            {
                string url = urlField.GetValue(handler) as string;
                if (string.IsNullOrEmpty(url))
                {
                    // Try to get URL from a legacy component on the same object
                    EnhancedVideoPlayer legacyPlayer = handler.GetComponent<EnhancedVideoPlayer>();
                    if (legacyPlayer != null && !string.IsNullOrEmpty(legacyPlayer.VideoUrlLink))
                    {
                        urlField.SetValue(handler, legacyPlayer.VideoUrlLink);
                        Debug.Log($"Fixed missing URL for {handler.gameObject.name}");
                        fixedCount++;
                    }

                    // Try ToggleShowHideVideo
                    MonoBehaviour[] monos = handler.GetComponents<MonoBehaviour>();
                    foreach (MonoBehaviour mono in monos)
                    {
                        if (mono.GetType().Name == "ToggleShowHideVideo")
                        {
                            var legacyUrlField = mono.GetType().GetField("VideoUrlLink");
                            if (legacyUrlField != null)
                            {
                                string legacyUrl = legacyUrlField.GetValue(mono) as string;
                                if (!string.IsNullOrEmpty(legacyUrl))
                                {
                                    urlField.SetValue(handler, legacyUrl);
                                    Debug.Log($"Fixed missing URL for {handler.gameObject.name} from ToggleShowHideVideo");
                                    fixedCount++;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        // Refresh
        FindManagers();
        CountVideos();

        EditorUtility.DisplayDialog("Fix Complete",
            $"Fixed {fixedCount} issues with video components.", "OK");
    }

    #endregion
}