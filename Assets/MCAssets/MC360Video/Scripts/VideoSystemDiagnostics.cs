using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// This diagnostic tool helps identify and fix issues with the video placement system.
/// Attach it to any GameObject in your scene to run diagnostics and place test videos.
/// </summary>
public class VideoSystemDiagnostics : MonoBehaviour
{
    [Header("Diagnostic Settings")]
    [SerializeField] private bool runOnStart = true;
    [SerializeField] private bool verboseLogging = true;
    [SerializeField] private bool attemptAutoFix = true;

    [Header("Manager References")]
    [SerializeField] private VideoDatabaseManager databaseManager;
    [SerializeField] private PolygonZoneManager zoneManager;
    [SerializeField] private AdvancedVideoPlacementManager placementManager;

    [Header("Manual Test")]
    [SerializeField] private string testZoneName = "Travel";
    [SerializeField] private bool placeTestVideoInZone = false;
    [SerializeField] private GameObject testPrefab;
    [SerializeField] private Vector3 testPosition = Vector3.zero;

    [Header("Status")]
    [SerializeField] private string databaseStatus = "Not checked";
    [SerializeField] private string zonesStatus = "Not checked";
    [SerializeField] private string prefabsStatus = "Not checked";
    [SerializeField] private List<string> issuesFound = new List<string>();

    private void Start()
    {
        if (runOnStart)
        {
            Invoke("RunDiagnostics", 0.5f); // Slight delay to ensure other components are initialized
        }
    }

    private void Update()
    {
        // Manual placement trigger
        if (placeTestVideoInZone)
        {
            placeTestVideoInZone = false;
            PlaceTestVideo();
        }
    }

    /// <summary>
    /// Run a complete diagnostic check on the video system
    /// </summary>
    public void RunDiagnostics()
    {
        Log("Starting video system diagnostics...");
        issuesFound.Clear();

        // Find components if not assigned
        FindRequiredComponents();

        // Check database
        CheckDatabase();

        // Check zones
        CheckZones();

        // Check prefabs
        CheckPrefabs();

        // Check paths
        CheckFilePaths();

        // Summary
        Log($"Diagnostics complete. Found {issuesFound.Count} issues.");
        foreach (string issue in issuesFound)
        {
            Debug.LogWarning(issue);
        }

        // Try to force placement if auto-fix is enabled
        if (attemptAutoFix && placementManager != null)
        {
            Log("Attempting to fix issues and place videos...");

            // Delay placement to allow fixes to apply
            Invoke("ForcePlacement", 1.0f);
        }
    }

    /// <summary>
    /// Find required components if not manually assigned
    /// </summary>
    private void FindRequiredComponents()
    {
        if (databaseManager == null)
        {
            databaseManager = FindObjectOfType<VideoDatabaseManager>();
            if (databaseManager == null)
            {
                AddIssue("VideoDatabaseManager not found in scene");
            }
            else
            {
                Log("Found VideoDatabaseManager");
            }
        }

        if (zoneManager == null)
        {
            zoneManager = FindObjectOfType<PolygonZoneManager>();
            if (zoneManager == null)
            {
                AddIssue("PolygonZoneManager not found in scene");
            }
            else
            {
                Log("Found PolygonZoneManager");
            }
        }

        if (placementManager == null)
        {
            placementManager = FindObjectOfType<AdvancedVideoPlacementManager>();
            if (placementManager == null)
            {
                AddIssue("VideoPlacementController not found in scene");
            }
            else
            {
                Log("Found VideoPlacementController");
            }
        }
    }

    /// <summary>
    /// Check database configuration and status
    /// </summary>
    private void CheckDatabase()
    {
        if (databaseManager == null) return;

        Log("Checking database...");

        // Check if database is initialized
        if (!databaseManager.IsInitialized)
        {
            AddIssue("Database is not initialized");

            // Try to load database
            if (attemptAutoFix)
            {
                Log("Attempting to load database...");
                databaseManager.LoadDatabaseFromJson();

                if (databaseManager.IsInitialized)
                {
                    Log("Successfully loaded database");
                }
                else
                {
                    AddIssue("Failed to load database");
                }
            }
        }

        // Check entry count
        int entryCount = databaseManager.EntryCount;
        if (entryCount == 0)
        {
            AddIssue("Database has no entries");
            databaseStatus = "Empty";
        }
        else
        {
            Log($"Database has {entryCount} entries");
            databaseStatus = $"OK ({entryCount} entries)";

            // Check if entries have URLs
            List<VideoEntry> entries = databaseManager.GetAllEntries();
            int emptyUrlCount = entries.Count(e => string.IsNullOrEmpty(e.PublicUrl));
            if (emptyUrlCount > 0)
            {
                AddIssue($"{emptyUrlCount} entries have empty URLs");
            }

            // Check zones
            List<string> zones = databaseManager.GetAllZones();
            Log($"Database contains {zones.Count} unique zones");

            if (zones.Count == 0)
            {
                AddIssue("No zones defined in database entries");
            }
            else
            {
                // Check if each zone has videos
                foreach (string zone in zones)
                {
                    List<VideoEntry> zoneEntries = databaseManager.GetEntriesForZone(zone);
                    Log($"Zone '{zone}' has {zoneEntries.Count} assigned videos");

                    if (zoneEntries.Count == 0)
                    {
                        AddIssue($"Zone '{zone}' has no assigned videos");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Check zone configuration
    /// </summary>
    private void CheckZones()
    {
        if (zoneManager == null) return;

        Log("Checking zones...");

        int zoneCount = zoneManager.zones.Count;
        if (zoneCount == 0)
        {
            AddIssue("No zones defined in PolygonZoneManager");
            zonesStatus = "No zones";
        }
        else
        {
            Log($"Found {zoneCount} zones");
            int invalidZones = 0;

            foreach (PolygonZone zone in zoneManager.zones)
            {
                // Check zone points
                if (zone.Points.Count < 3)
                {
                    AddIssue($"Zone '{zone.Name}' has fewer than 3 points ({zone.Points.Count})");
                    invalidZones++;
                }
                else
                {
                    Log($"Zone '{zone.Name}' has {zone.Points.Count} points");
                }

                // Check if database has this zone
                if (databaseManager != null && databaseManager.IsInitialized)
                {
                    List<VideoEntry> zoneEntries = databaseManager.GetEntriesForZone(zone.Name);

                    if (zoneEntries.Count == 0)
                    {
                        AddIssue($"Zone '{zone.Name}' has no matching videos in database. Check zone name spelling.");
                    }
                    else
                    {
                        Log($"Zone '{zone.Name}' has {zoneEntries.Count} videos in database");
                    }
                }
            }

            if (invalidZones > 0)
            {
                zonesStatus = $"Issues ({invalidZones}/{zoneCount})";
            }
            else
            {
                zonesStatus = $"OK ({zoneCount})";
            }

            // Check for database zones not in the polygon manager
            if (databaseManager != null && databaseManager.IsInitialized)
            {
                List<string> dbZones = databaseManager.GetAllZones();
                List<string> sceneZones = zoneManager.zones.Select(z => z.Name).ToList();

                List<string> missingZones = dbZones.Where(z => !sceneZones.Contains(z)).ToList();

                if (missingZones.Count > 0)
                {
                    AddIssue($"Database contains {missingZones.Count} zones not defined in PolygonZoneManager: {string.Join(", ", missingZones)}");
                }
            }
        }
    }

    /// <summary>
    /// Check prefab configuration
    /// </summary>
    private void CheckPrefabs()
    {
        if (placementManager == null) return;

        Log("Checking prefabs...");

        // Find prefab fields through reflection
        var defaultPrefabField = placementManager.GetType().GetField("defaultPrefab",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        var prefabMappingsField = placementManager.GetType().GetField("prefabMappings",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        if (defaultPrefabField != null && prefabMappingsField != null)
        {
            GameObject defaultPrefab = defaultPrefabField.GetValue(placementManager) as GameObject;
            var prefabMappings = prefabMappingsField.GetValue(placementManager) as IList;

            // Check default prefab
            if (defaultPrefab == null)
            {
                AddIssue("Default prefab is not assigned in VideoPlacementController");
                prefabsStatus = "Missing default";
            }
            else
            {
                Log($"Default prefab is assigned: {defaultPrefab.name}");
                testPrefab = defaultPrefab; // Use for testing

                // Check EnhancedVideoPlayer component
                if (defaultPrefab.GetComponent<EnhancedVideoPlayer>() == null)
                {
                    Log("Default prefab doesn't have EnhancedVideoPlayer component (will be added at runtime)");
                }

                // Check collider
                if (defaultPrefab.GetComponent<Collider>() == null)
                {
                    Log("Default prefab doesn't have a Collider component (will be added at runtime)");
                }
            }

            // Check mappings
            if (prefabMappings == null || prefabMappings.Count == 0)
            {
                Log("No custom prefab mappings defined (will use default for all)");
            }
            else
            {
                Log($"Found {prefabMappings.Count} custom prefab mappings");

                // Check for null mappings
                int nullMappings = 0;
                foreach (var mapping in prefabMappings)
                {
                    var prefabField = mapping.GetType().GetField("Prefab");
                    var typeField = mapping.GetType().GetField("PrefabType");

                    if (prefabField != null && typeField != null)
                    {
                        GameObject prefab = prefabField.GetValue(mapping) as GameObject;
                        string prefabType = typeField.GetValue(mapping) as string;

                        if (prefab == null)
                        {
                            AddIssue($"Prefab for type '{prefabType}' is not assigned");
                            nullMappings++;
                        }
                        else if (string.IsNullOrEmpty(prefabType))
                        {
                            AddIssue($"Prefab type for '{prefab.name}' is empty");
                            nullMappings++;
                        }
                        else
                        {
                            Log($"Prefab mapping: {prefabType} -> {prefab.name}");
                        }
                    }
                }

                if (nullMappings > 0)
                {
                    prefabsStatus = $"Issues ({nullMappings}/{prefabMappings.Count})";
                }
                else
                {
                    prefabsStatus = $"OK ({prefabMappings.Count})";
                }
            }
        }
        else
        {
            AddIssue("Could not access prefab fields in VideoPlacementController");
            prefabsStatus = "Access error";
        }
    }

    /// <summary>
    /// Check file paths
    /// </summary>
    private void CheckFilePaths()
    {
        if (databaseManager == null) return;

        Log("Checking file paths...");

        // Get database path field
        var pathField = databaseManager.GetType().GetField("databaseFilePath",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        if (pathField != null)
        {
            string path = pathField.GetValue(databaseManager) as string;

            if (string.IsNullOrEmpty(path))
            {
                AddIssue("Database file path is not set");
            }
            else
            {
                Log($"Database path: {path}");

                // Check if file exists
                string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
                if (!File.Exists(fullPath))
                {
                    AddIssue($"Database file not found at: {fullPath}");
                }
                else
                {
                    Log($"Database file exists: {fullPath}");
                }
            }
        }
    }

    /// <summary>
    /// Force placement of videos
    /// </summary>
    /// <summary>
    /// Force placement of videos
    /// </summary>
    private void ForcePlacement()
    {
        if (placementManager != null)
        {
            Log("Forcing video placement...");
            placementManager.PlaceAllVideos(); // Corrected method name
        }
    }


    /// <summary>
    /// Place a test video prefab in the scene
    /// </summary>
    private void PlaceTestVideo()
    {
        if (testPrefab == null)
        {
            Debug.LogError("No test prefab assigned!");
            return;
        }

        // Place at test position or find a position in the requested zone
        Vector3 position = testPosition;

        if (zoneManager != null && !string.IsNullOrEmpty(testZoneName))
        {
            PolygonZone zone = zoneManager.zones.FirstOrDefault(z => z.Name == testZoneName);
            if (zone != null && zone.Points.Count >= 3)
            {
                Vector3 zonePos = zoneManager.FindValidPositionInZone(zone, new List<Vector3>());
                if (zonePos != Vector3.zero)
                {
                    position = zonePos;
                    Log($"Found position in zone '{testZoneName}': {position}");
                }
                else
                {
                    Log($"Could not find position in zone '{testZoneName}', using default position: {position}");
                }
            }
            else
            {
                Log($"Zone '{testZoneName}' not found or doesn't have enough points");
            }
        }

        // Create a test video prefab
        GameObject testObj = Instantiate(testPrefab, position, Quaternion.identity);
        testObj.name = "TestVideoLink";

        // Add or configure the EnhancedVideoPlayer component
        EnhancedVideoPlayer player = testObj.GetComponent<EnhancedVideoPlayer>();
        if (player == null)
        {
            player = testObj.AddComponent<EnhancedVideoPlayer>();
        }

        // Set test values
        player.VideoUrlLink = "https://storage.googleapis.com/masterchange/test/test_video.mp4";
        player.title = "Test Video";
        player.description = "This is a test video link";
        player.debugMode = true;

        // Add collider if missing
        if (testObj.GetComponent<Collider>() == null)
        {
            BoxCollider collider = testObj.AddComponent<BoxCollider>();
            collider.center = Vector3.up * 0.5f;
            collider.size = new Vector3(1f, 1f, 1f);
        }

        // Add interaction component if missing
        if (testObj.GetComponent<VideoLinkInteraction>() == null)
        {
            testObj.AddComponent<VideoLinkInteraction>();
        }

        // Add text display
        GameObject textObj = new GameObject("Title");
        textObj.transform.SetParent(testObj.transform);
        textObj.transform.localPosition = new Vector3(0, 1.2f, 0);

        // Create canvas
        GameObject canvasObj = new GameObject("Canvas");
        canvasObj.transform.SetParent(testObj.transform);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        // Set canvas size
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(200, 100);
        canvasRect.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        // Add canvas scaler
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;

        // Add raycaster
        canvasObj.AddComponent<GraphicRaycaster>();

        // Move text under canvas
        textObj.transform.SetParent(canvasObj.transform);

        // Add TextMeshProUGUI component
        TMP_Text textComponent = textObj.AddComponent<TextMeshProUGUI>();
        textComponent.text = "Test Video";
        textComponent.fontSize = 12;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.color = Color.white;

        // Set rect transform
        RectTransform textRect = textComponent.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(150, 30);
        textRect.anchoredPosition = new Vector2(0, 0);

        // Add background
        GameObject background = new GameObject("Background");
        background.transform.SetParent(textObj.transform);
        background.transform.SetAsFirstSibling(); // Put background behind text
        RectTransform bgRect = background.AddComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(180, 40);
        bgRect.anchoredPosition = Vector2.zero;
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.5f);

        // Link the text component to the video component
        player.TMP_title = textComponent;
        player.hasText = true;

        Debug.Log($"Placed test video at {position}");
    }

    /// <summary>
    /// Add an issue to the list
    /// </summary>
    private void AddIssue(string issue)
    {
        Debug.LogWarning($"[VideoSystemDiagnostics] {issue}");
        issuesFound.Add(issue);
    }

    /// <summary>
    /// Log a diagnostic message
    /// </summary>
    private void Log(string message)
    {
        if (verboseLogging)
        {
            Debug.Log($"[VideoSystemDiagnostics] {message}");
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(VideoSystemDiagnostics))]
public class VideoSystemDiagnosticsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        VideoSystemDiagnostics diagnostics = (VideoSystemDiagnostics)target;

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Run Diagnostics"))
        {
            if (Application.isPlaying)
            {
                diagnostics.RunDiagnostics();
            }
            else
            {
                EditorUtility.DisplayDialog("Runtime Only",
                    "Diagnostics can only be run in Play mode.", "OK");
            }
        }

        if (GUILayout.Button("Place Test Video"))
        {
            if (Application.isPlaying)
            {
                var prop = serializedObject.FindProperty("placeTestVideoInZone");
                prop.boolValue = true;
                serializedObject.ApplyModifiedProperties();
            }
            else
            {
                EditorUtility.DisplayDialog("Runtime Only",
                    "Test placement can only be done in Play mode.", "OK");
            }
        }

        // Show troubleshooting tips
        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "Common Issues:\n" +
            "1. Database file not found or empty\n" +
            "2. Zones have no points defined\n" +
            "3. Zone names in database don't match PolygonZoneManager\n" +
            "4. Missing prefab assignments\n" +
            "5. Collider issues preventing mouse interaction\n" +
            "Try using the 'Place Test Video' to verify basic functionality.",
            MessageType.Info);
    }
}
#endif