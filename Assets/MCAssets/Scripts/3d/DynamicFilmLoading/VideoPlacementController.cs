using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using UnityEngine.UI;
using UnityEditor;

// This component manages the runtime placement of video links using polygon zones
public class VideoPlacementController : MonoBehaviour
{
    [Header("Placement Settings")]
    [SerializeField] private float placementHeight = 0.1f; // Height above terrain
    // Note: We're no longer using a fixed offset, but instead calculating it per prefab
    // [SerializeField] private float yOffsetAdjustment = -4.07f; // Removed fixed Y offset
    [SerializeField] private float avoidObstacleRadius = 1.0f;
    [SerializeField] private int maxPlacementAttempts = 50;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private bool placeOnStart = true;
    [SerializeField] private float startDelay = 1.0f;

    [Header("Video Prefabs")]
    [SerializeField] private GameObject defaultPrefab;

    [System.Serializable]
    public class PrefabMapping
    {
        public string PrefabType;
        public GameObject Prefab;
    }

    [SerializeField] private List<PrefabMapping> prefabMappings = new List<PrefabMapping>();

    [System.Serializable]
    public class ZonePrefabMapping
    {
        public string ZoneName;
        public GameObject DefaultPrefab;
    }

    [Header("Zone Default Prefabs")]
    [SerializeField] private List<ZonePrefabMapping> zonePrefabMappings = new List<ZonePrefabMapping>();

    [Header("Debug Options")]
    [SerializeField] private bool verboseLogging = true;

    private VideoDatabaseManager databaseManager;
    private PolygonZoneManager zoneManager;
    private Terrain terrain;
    private TerrainCollider terrainCollider;

    private Dictionary<string, GameObject> prefabMap = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> zonePrefabMap = new Dictionary<string, GameObject>();

    private void Awake()
    {
        // Find the database manager
        databaseManager = FindObjectOfType<VideoDatabaseManager>();
        if (databaseManager == null)
        {
            Debug.LogError("No VideoDatabaseManager found in the scene!");
        }

        // Find the zone manager
        zoneManager = FindObjectOfType<PolygonZoneManager>();
        if (zoneManager == null)
        {
            Debug.LogWarning("No PolygonZoneManager found in the scene. Some features may not work correctly.");
        }

        // Find terrain components
        terrain = FindObjectOfType<Terrain>();
        terrainCollider = FindObjectOfType<TerrainCollider>();

        if (terrain == null && terrainCollider == null)
        {
            Debug.LogWarning("No terrain or terrain collider found in the scene. Will use flat ground.");
        }
        else if (verboseLogging)
        {
            if (terrain != null) Debug.Log("Found Terrain component for height sampling");
            if (terrainCollider != null) Debug.Log("Found TerrainCollider component for height sampling");
        }

        // Initialize prefab mapping from the inspector-configured mappings
        if (defaultPrefab == null)
        {
            Debug.LogError("Default prefab is not assigned! Videos may not appear correctly.");
        }
        else if (!prefabMap.ContainsKey("Default"))
        {
            prefabMap["Default"] = defaultPrefab;
            if (verboseLogging) Debug.Log("Added Default prefab to mapping");
        }

        // Initialize type-based prefab mappings
        foreach (PrefabMapping mapping in prefabMappings)
        {
            if (!string.IsNullOrEmpty(mapping.PrefabType) && mapping.Prefab != null)
            {
                prefabMap[mapping.PrefabType] = mapping.Prefab;
                if (verboseLogging) Debug.Log($"Added prefab mapping: {mapping.PrefabType} -> {mapping.Prefab.name}");
            }
            else
            {
                Debug.LogWarning($"Invalid prefab mapping: {(mapping.PrefabType ?? "null")} -> {(mapping.Prefab ? mapping.Prefab.name : "null")}");
            }
        }

        // Initialize zone-based prefab mappings
        foreach (ZonePrefabMapping mapping in zonePrefabMappings)
        {
            if (!string.IsNullOrEmpty(mapping.ZoneName) && mapping.DefaultPrefab != null)
            {
                zonePrefabMap[mapping.ZoneName] = mapping.DefaultPrefab;
                if (verboseLogging) Debug.Log($"Added zone prefab mapping: {mapping.ZoneName} -> {mapping.DefaultPrefab.name}");
            }
            else
            {
                Debug.LogWarning($"Invalid zone prefab mapping: {(mapping.ZoneName ?? "null")} -> {(mapping.DefaultPrefab ? mapping.DefaultPrefab.name : "null")}");
            }
        }
    }

    private void Start()
    {
        // Start placement after the configured delay to ensure everything is loaded
        if (placeOnStart)
        {
            Invoke("PlaceAllVideoLinks", startDelay);
        }
    }

    // Place video links in all configured zones
    public void PlaceAllVideoLinks()
    {
        if (verboseLogging) Debug.Log("Starting video link placement...");

        if (databaseManager == null)
        {
            Debug.LogError("Cannot place video links: No database manager available");
            return;
        }

        if (!databaseManager.IsInitialized)
        {
            Debug.LogError("Database manager is not initialized. Make sure database is loaded before placing videos.");

            // Try to load the database
            databaseManager.LoadDatabaseFromJson();

            if (!databaseManager.IsInitialized)
            {
                Debug.LogError("Failed to initialize database. Check if your database file exists.");
                return;
            }
        }

        int entryCount = databaseManager.EntryCount;
        if (entryCount == 0)
        {
            Debug.LogError("No video entries in database. Check your database file.");
            return;
        }
        else
        {
            if (verboseLogging) Debug.Log($"Found {entryCount} videos in database");
        }

        // Clear existing video links
        ClearExistingVideoLinks();

        // Check if we have a polygon zone manager
        if (zoneManager != null && zoneManager.zones.Count > 0)
        {
            if (verboseLogging) Debug.Log($"Found {zoneManager.zones.Count} zones for placement");

            // Process each zone from the polygon zone manager
            foreach (PolygonZone zone in zoneManager.zones)
            {
                if (zone.Points.Count < 3)
                {
                    Debug.LogWarning($"Zone '{zone.Name}' has fewer than 3 points. Skipping this zone.");
                    continue;
                }

                PlaceVideosInPolygonZone(zone);
            }
        }
        else
        {
            Debug.LogWarning("No PolygonZoneManager or no zones defined. No videos will be placed.");
        }
    }

    // Clear all existing video links
    private void ClearExistingVideoLinks()
    {
        // Clear both old and new video link types
        EnhancedVideoPlayer[] enhancedLinks = FindObjectsOfType<EnhancedVideoPlayer>();
        int enhancedCount = enhancedLinks.Length;

        foreach (EnhancedVideoPlayer link in enhancedLinks)
        {
            Destroy(link.gameObject);
        }

        // Also clear legacy links if they exist
        var legacyLinks = FindObjectsOfType<MonoBehaviour>().Where(mb => mb.GetType().Name == "ToggleShowHideVideo");
        int legacyCount = legacyLinks.Count();

        foreach (MonoBehaviour link in legacyLinks)
        {
            Destroy(link.gameObject);
        }

        if (verboseLogging) Debug.Log($"Cleared {enhancedCount} enhanced video links and {legacyCount} legacy links");
    }

    // Place videos within a polygon zone
    private void PlaceVideosInPolygonZone(PolygonZone zone)
    {
        if (verboseLogging) Debug.Log($"Processing zone: {zone.Name}");

        // Get videos assigned to this zone
        List<VideoEntry> zoneVideos = databaseManager.GetEntriesForZone(zone.Name);

        if (verboseLogging) Debug.Log($"Found {zoneVideos.Count} videos directly assigned to zone '{zone.Name}'");

        // If no direct zone assignments, fallback to category-based filtering
        if (zoneVideos.Count == 0 && zone.Categories != null && zone.Categories.Count > 0)
        {
            if (verboseLogging) Debug.Log($"No direct zone assignments, falling back to category filtering");

            foreach (string category in zone.Categories)
            {
                if (string.IsNullOrEmpty(category)) continue;

                string[] parts = category.Split('/');
                List<VideoEntry> categoryVideos;

                if (parts.Length == 1)
                {
                    // Main category only
                    categoryVideos = databaseManager.GetEntriesForCategory(parts[0]);
                    if (verboseLogging) Debug.Log($"Category '{parts[0]}' has {categoryVideos.Count} videos");
                }
                else if (parts.Length == 2)
                {
                    // Main category and subcategory
                    categoryVideos = databaseManager.GetEntriesForCategory(parts[0], parts[1]);
                    if (verboseLogging) Debug.Log($"Category '{parts[0]}/{parts[1]}' has {categoryVideos.Count} videos");
                }
                else
                {
                    if (verboseLogging) Debug.Log($"Invalid category format: {category}");
                    continue;
                }

                zoneVideos.AddRange(categoryVideos);
            }

            // Remove duplicates from category filtering
            int beforeCount = zoneVideos.Count;
            zoneVideos = zoneVideos.Distinct().ToList();
            if (verboseLogging && beforeCount != zoneVideos.Count)
                Debug.Log($"Removed {beforeCount - zoneVideos.Count} duplicate videos from category filtering");
        }

        if (zoneVideos.Count == 0)
        {
            Debug.LogWarning($"No videos found for zone '{zone.Name}'. Check zone assignments in your database.");
            return;
        }

        // Apply max videos limit
        if (zoneVideos.Count > zone.MaxVideos)
        {
            if (verboseLogging) Debug.Log($"Limiting videos in zone {zone.Name} to {zone.MaxVideos} (from {zoneVideos.Count})");
            zoneVideos = zoneVideos.Take(zone.MaxVideos).ToList();
        }

        Debug.Log($"Placing {zoneVideos.Count} videos in zone {zone.Name}");

        // Create a parent object for this zone
        GameObject zoneParent = new GameObject($"Zone_{zone.Name}");
        zoneParent.transform.parent = transform;

        // Keep track of placed positions
        List<Vector3> placedPositions = new List<Vector3>();

        // Place each video
        int placedCount = 0;
        foreach (VideoEntry video in zoneVideos)
        {
            // Use placement manager to find a valid position
            Vector3 position = zoneManager.FindValidPositionInZone(zone, placedPositions);

            if (position != Vector3.zero)
            {
                PlaceVideoLink(video, position, zone.Name, zoneParent.transform);
                placedPositions.Add(position);
                placedCount++;
            }
            else
            {
                Debug.LogWarning($"Could not find valid position for video '{video.Title}' in zone '{zone.Name}'");
            }
        }

        Debug.Log($"Successfully placed {placedCount} out of {zoneVideos.Count} videos in zone '{zone.Name}'");
    }

    // Place a single video link
    private void PlaceVideoLink(VideoEntry video, Vector3 position, string zoneName, Transform parent)
    {
        if (verboseLogging)
        {
            Debug.Log($"===== PLACING VIDEO =====");
            Debug.Log($"Video Title: {video.Title}");
            Debug.Log($"Video URL: {video.PublicUrl}");
            Debug.Log($"Video Prefab Type: '{video.Prefab}'");
            Debug.Log($"In Zone: '{zoneName}'");
        }

        // Get prefab for this video - prioritize the video's prefab setting
        GameObject prefab = GetPrefabForVideo(video, zoneName);

        if (prefab == null)
        {
            Debug.LogError($"No prefab available for video {video.Title}. Prefab type: {video.Prefab}, Zone: {zoneName}");
            return;
        }

        if (verboseLogging)
        {
            Debug.Log($"Selected prefab: {prefab.name}");
            Debug.Log($"Prefab active state: {prefab.activeSelf}");
        }

        // Apply terrain height adjustment
        float terrainHeight = position.y; // Use the height already determined by PolygonZoneManager
        if (verboseLogging) Debug.Log($"Initial terrain height at position: {terrainHeight}");

        // Double-check terrain height using raycast for more accuracy
        if (terrainCollider != null)
        {
            RaycastHit hit;
            if (Physics.Raycast(new Vector3(position.x, 1000f, position.z), Vector3.down, out hit, 2000f, LayerMask.GetMask("Default", "Terrain")))
            {
                terrainHeight = hit.point.y;
                if (verboseLogging) Debug.Log($"Refined terrain height via raycast: {terrainHeight}");
            }
        }

        // Create a temporary instance to measure the prefab's dimensions
        GameObject tempInstance = Instantiate(prefab, new Vector3(0, -1000, 0), Quaternion.identity);
        // Force the temp instance to be active
        tempInstance.SetActive(true);

        // Activate all child objects to ensure accurate bounds calculation
        foreach (Transform child in tempInstance.transform)
        {
            child.gameObject.SetActive(true);
        }

        // Get the prefab's bounds to determine its height offset
        Bounds prefabBounds = CalculatePrefabBounds(tempInstance);
        float prefabHeight = prefabBounds.size.y;
        float prefabBottomOffset = prefabBounds.min.y - tempInstance.transform.position.y;

        // We won't need this temporary instance anymore
        Destroy(tempInstance);

        if (verboseLogging) Debug.Log($"Prefab height: {prefabHeight}, Bottom offset: {prefabBottomOffset}");

        // Calculate the position so the bottom of the prefab rests on the terrain
        Vector3 finalPosition = new Vector3(
            position.x,
            terrainHeight - prefabBottomOffset, // Position so bottom of prefab is at terrain height
            position.z
        );

        if (verboseLogging) Debug.Log($"Original position: {position}, Final adjusted position: {finalPosition}");

        // Create the actual game object
        GameObject videoLink = Instantiate(prefab, finalPosition, Quaternion.identity, parent);
        videoLink.name = $"VideoLink_{video.Title}";

        // IMPORTANT: Make sure the object is active
        videoLink.SetActive(true);

        // Ensure all child objects are active
        ActivateAllChildren(videoLink.transform);

        if (verboseLogging) Debug.Log($"Created video link GameObject: {videoLink.name}, Active: {videoLink.activeSelf}");

        // Add the EnhancedVideoPlayer component
        EnhancedVideoPlayer linkComponent = videoLink.GetComponent<EnhancedVideoPlayer>();
        if (linkComponent == null)
        {
            linkComponent = videoLink.AddComponent<EnhancedVideoPlayer>();
            if (verboseLogging) Debug.Log($"Added EnhancedVideoPlayer component to {videoLink.name}");
        }

        // Configure the component
        linkComponent.VideoUrlLink = video.PublicUrl;
        linkComponent.returntoscene = SceneHelper.GetActiveSceneName(); // Default to current scene
        linkComponent.behaviour = zoneName;
        linkComponent.title = video.Title;
        linkComponent.description = video.Description;
        linkComponent.prefabType = video.Prefab;
        linkComponent.zoneName = zoneName;
        linkComponent.debugMode = verboseLogging;

        // Add collider if missing
        Collider existingCollider = videoLink.GetComponent<Collider>();
        if (existingCollider == null)
        {
            BoxCollider collider = videoLink.AddComponent<BoxCollider>();
            collider.center = Vector3.up * 0.5f;
            collider.size = new Vector3(1f, 1f, 1f);
            if (verboseLogging) Debug.Log($"Added collider to {videoLink.name}");
        }

        // Add interaction component if missing
        if (videoLink.GetComponent<VideoLinkInteraction>() == null)
        {
            videoLink.AddComponent<VideoLinkInteraction>();
            if (verboseLogging) Debug.Log($"Added VideoLinkInteraction to {videoLink.name}");
        }

        // Add or update text component
        if (!string.IsNullOrEmpty(video.Title))
        {
            // Find existing TextMeshPro component
            TMP_Text textComponent = videoLink.GetComponentInChildren<TMP_Text>();

            if (textComponent == null)
            {
                // Create a new text object
                GameObject textObj = new GameObject("Title");
                textObj.transform.SetParent(videoLink.transform);
                textObj.transform.localPosition = new Vector3(0, 1.2f, 0);
                textObj.SetActive(true); // Ensure text object is active

                // Create canvas if needed
                Canvas canvas = videoLink.GetComponentInChildren<Canvas>();
                if (canvas == null)
                {
                    GameObject canvasObj = new GameObject("Canvas");
                    canvasObj.transform.SetParent(videoLink.transform);
                    canvasObj.SetActive(true); // Ensure canvas is active

                    canvas = canvasObj.AddComponent<Canvas>();
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
                }

                // Add TextMeshProUGUI component
                textComponent = textObj.AddComponent<TextMeshProUGUI>();
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
                background.SetActive(true); // Ensure background is active

                RectTransform bgRect = background.AddComponent<RectTransform>();
                bgRect.sizeDelta = new Vector2(180, 40);
                bgRect.anchoredPosition = Vector2.zero;
                Image bgImage = background.AddComponent<Image>();
                bgImage.color = new Color(0, 0, 0, 0.5f);

                if (verboseLogging) Debug.Log($"Created text component for {videoLink.name}");
            }

            // Set text
            textComponent.text = video.Title;

            // Link the text component to the video component
            linkComponent.TMP_title = textComponent;
            linkComponent.hasText = true;
        }

        // Find center of the zone for orientation
        Vector3 zoneCenter = CalculateZoneCenter(zoneName);
        if (zoneCenter != Vector3.zero)
        {
            // Make the object face toward the center of the zone
            Vector3 lookDirection = new Vector3(zoneCenter.x, position.y, zoneCenter.z) - position;
            if (lookDirection != Vector3.zero)
            {
                videoLink.transform.rotation = Quaternion.LookRotation(-lookDirection); // Look away from center
            }
        }

        // Final check to ensure everything is active
        if (!videoLink.activeSelf)
        {
            Debug.LogWarning($"Video link {videoLink.name} is still inactive after placement! Forcing activation.");
            videoLink.SetActive(true);
            ActivateAllChildren(videoLink.transform);
        }

        if (verboseLogging)
        {
            Debug.Log($"Successfully placed {video.Title} at {finalPosition}");
            Debug.Log($"Final active state: {videoLink.activeSelf}");
            Debug.Log($"===== PLACEMENT COMPLETE =====");
        }
    }

    // Recursively activate all children
    private void ActivateAllChildren(Transform parent)
    {
        foreach (Transform child in parent)
        {
            child.gameObject.SetActive(true);

            // Recursively activate children of this child
            if (child.childCount > 0)
            {
                ActivateAllChildren(child);
            }
        }
    }

    // Calculate the bounds of a prefab including all child renderers and colliders
    private Bounds CalculatePrefabBounds(GameObject prefab)
    {
        // Get all renderers and colliders in the prefab, including inactive ones
        Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
        Collider[] colliders = prefab.GetComponentsInChildren<Collider>(true);

        if (verboseLogging)
        {
            Debug.Log($"Calculating bounds for prefab: {prefab.name}");
            Debug.Log($"Found {renderers.Length} renderers and {colliders.Length} colliders");
        }

        // Initialize bounds
        Bounds bounds = new Bounds();
        bool boundsInitialized = false;

        // Include renderer bounds
        foreach (Renderer renderer in renderers)
        {
            // Temporarily activate the renderer's GameObject if it's inactive
            bool wasActive = renderer.gameObject.activeSelf;
            if (!wasActive)
            {
                renderer.gameObject.SetActive(true);
            }

            if (!boundsInitialized)
            {
                bounds = renderer.bounds;
                boundsInitialized = true;

                if (verboseLogging)
                {
                    Debug.Log($"Initial bounds from renderer {renderer.name}: Center={bounds.center}, Size={bounds.size}, Min={bounds.min}, Max={bounds.max}");
                }
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }

            // Restore original state if we changed it
            if (!wasActive)
            {
                renderer.gameObject.SetActive(wasActive);
            }
        }

        // Include collider bounds
        foreach (Collider collider in colliders)
        {
            // Temporarily activate the collider's GameObject if it's inactive
            bool wasActive = collider.gameObject.activeSelf;
            if (!wasActive)
            {
                collider.gameObject.SetActive(true);
            }

            if (!boundsInitialized)
            {
                bounds = collider.bounds;
                boundsInitialized = true;

                if (verboseLogging)
                {
                    Debug.Log($"Initial bounds from collider {collider.name}: Center={bounds.center}, Size={bounds.size}, Min={bounds.min}, Max={bounds.max}");
                }
            }
            else
            {
                bounds.Encapsulate(collider.bounds);
            }

            // Restore original state if we changed it
            if (!wasActive)
            {
                collider.gameObject.SetActive(wasActive);
            }
        }

        // If no renderers or colliders found, use a default size
        if (!boundsInitialized)
        {
            bounds.center = prefab.transform.position;
            bounds.size = Vector3.one;
            if (verboseLogging) Debug.LogWarning($"No renderers or colliders found on prefab. Using default bounds.");
        }

        if (verboseLogging)
        {
            Debug.Log($"Final calculated bounds: Center={bounds.center}, Size={bounds.size}, Min={bounds.min}, Max={bounds.max}");
        }

        return bounds;
    }

    // Calculate center of a zone by name
    private Vector3 CalculateZoneCenter(string zoneName)
    {
        if (zoneManager == null)
            return Vector3.zero;

        // Find the zone with the matching name
        PolygonZone zone = zoneManager.zones.FirstOrDefault(z => z.Name == zoneName);

        if (zone == null || zone.Points.Count == 0)
            return Vector3.zero;

        // Calculate the center of all points
        Vector3 center = Vector3.zero;
        foreach (Vector3 point in zone.Points)
        {
            center += point;
        }
        center /= zone.Points.Count;

        return center;
    }

    // Get the appropriate prefab for a video with the following priority:
    // 1. Video-specific prefab from the database
    // 2. Zone-specific default prefab
    // 3. Global default prefab
    private GameObject GetPrefabForVideo(VideoEntry video, string zoneName)
    {
        // First check if the video specifies a prefab type in its data
        if (!string.IsNullOrEmpty(video.Prefab) && prefabMap.ContainsKey(video.Prefab))
        {
            if (verboseLogging) Debug.Log($"Using video-specific prefab type: {video.Prefab}");
            return prefabMap[video.Prefab];
        }

        // Next check if the zone has a default prefab assigned
        if (!string.IsNullOrEmpty(zoneName) && zonePrefabMap.ContainsKey(zoneName))
        {
            if (verboseLogging) Debug.Log($"Using zone-specific default prefab for zone: {zoneName}");
            return zonePrefabMap[zoneName];
        }

        // Fallback to default prefab mapping
        if (prefabMap.ContainsKey("Default"))
        {
            if (verboseLogging) Debug.Log($"Using global default prefab for {video.Title}");
            return prefabMap["Default"];
        }

        // Last resort - use the default prefab from inspector
        if (verboseLogging) Debug.Log($"Using default prefab from inspector for {video.Title}");
        return defaultPrefab;
    }
}

// Helper class for scene management
public static class SceneHelper
{
    public static string GetActiveSceneName()
    {
        return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
    }
}

// Editor extension for the VideoPlacementController
#if UNITY_EDITOR
[CustomEditor(typeof(VideoPlacementController))]
public class VideoPlacementControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        VideoPlacementController controller = (VideoPlacementController)target;

        EditorGUILayout.Space();

        if (GUILayout.Button("Place All Video Links"))
        {
            if (Application.isPlaying)
            {
                controller.PlaceAllVideoLinks();
            }
            else
            {
                EditorUtility.DisplayDialog("Runtime Only",
                    "This operation can only be performed in Play mode.", "OK");
            }
        }

        // Display help box with troubleshooting info
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Troubleshooting:\n" +
            "1. Make sure VideoDatabaseManager has loaded films\n" +
            "2. Check that PolygonZoneManager has zones with valid points\n" +
            "3. Verify prefab mappings match your database entries\n" +
            "4. Enable verbose logging for detailed placement information",
            MessageType.Info);
    }
}
#endif