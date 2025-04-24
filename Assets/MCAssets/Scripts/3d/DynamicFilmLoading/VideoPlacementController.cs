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
    [SerializeField] private float avoidObstacleRadius = 1.0f;
    [SerializeField] private int maxPlacementAttempts = 50;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private bool placeOnStart = true;
    [SerializeField] private float startDelay = 1.0f;

    [Header("Reticle Settings")]
    [SerializeField] private float reticleRange = 15.0f; // Range of the reticle pointer
    [SerializeField] private bool showReticleGizmo = true;
    [SerializeField] private Color reticleColor = new Color(1f, 0f, 0f, 0.3f);

    [Header("Orientation Settings")]
    [SerializeField] private bool faceCamera = true; // Make prefabs face camera
    [SerializeField] private bool dynamicFacing = true; // Make prefabs constantly face camera as it moves
    [SerializeField] private float updateInterval = 0.2f; // How often to update orientation (seconds)

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
    private Camera mainCamera;
    private List<GameObject> activePrefabs = new List<GameObject>();
    private float lastOrientationUpdateTime;

    private Dictionary<string, GameObject> prefabMap = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> zonePrefabMap = new Dictionary<string, GameObject>();

    private void Awake()
    {
        // Find the main camera
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("Main camera not found. Camera-facing orientation will not work.");
        }

        // Find the database manager
        databaseManager = FindObjectOfType<VideoDatabaseManager>();
        if (databaseManager == null)
        {
            Debug.LogError("No VideoDatabaseManager found in the scene!");
            // Create one to avoid null reference exceptions
            GameObject dbManagerObj = new GameObject("VideoDatabaseManager");
            databaseManager = dbManagerObj.AddComponent<VideoDatabaseManager>();
            Debug.LogWarning("Created a VideoDatabaseManager, but it needs configuration");
        }

        // Find the zone manager
        zoneManager = FindObjectOfType<PolygonZoneManager>();
        if (zoneManager == null)
        {
            Debug.LogWarning("No PolygonZoneManager found in the scene. Some features may not work correctly.");
        }
        else
        {
            // Set the reticle range in the zone manager
            zoneManager.reticleRange = reticleRange;
            if (verboseLogging) Debug.Log($"Set reticle range in PolygonZoneManager to {reticleRange}");
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

    private void Update()
    {
        // Update prefab orientation to face camera if enabled
        if (dynamicFacing && mainCamera != null && activePrefabs.Count > 0)
        {
            // Only update at the specified interval to avoid performance issues
            if (Time.time - lastOrientationUpdateTime >= updateInterval)
            {
                UpdatePrefabOrientation();
                lastOrientationUpdateTime = Time.time;
            }
        }
    }

    // Update orientation of all prefabs to face the camera
    private void UpdatePrefabOrientation()
    {
        Vector3 cameraPosition = mainCamera.transform.position;

        foreach (GameObject prefab in activePrefabs)
        {
            if (prefab != null)
            {
                // Calculate direction from prefab to camera (ignoring Y axis for a cleaner rotation)
                Vector3 lookDirection = new Vector3(
                    cameraPosition.x - prefab.transform.position.x,
                    0, // Keep y axis constant
                    cameraPosition.z - prefab.transform.position.z
                );

                if (lookDirection != Vector3.zero)
                {
                    // Make the prefab face the camera
                    prefab.transform.rotation = Quaternion.LookRotation(lookDirection);
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (showReticleGizmo)
        {
            // For simplicity, just show the reticle range at the camera position in editor
            Vector3 center = Camera.main != null ? Camera.main.transform.position : transform.position;

            // Draw circle at ground level
            DrawCircleGizmo(center, reticleRange, reticleColor);
        }
    }

    // Helper method to draw circle gizmos
    private void DrawCircleGizmo(Vector3 center, float radius, Color color)
    {
        Gizmos.color = color;

        const int segments = 32;
        float angleStep = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep * Mathf.Deg2Rad;
            float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;

            Vector3 point1 = new Vector3(
                center.x + Mathf.Cos(angle1) * radius,
                center.y, // Keep at same height
                center.z + Mathf.Sin(angle1) * radius
            );

            Vector3 point2 = new Vector3(
                center.x + Mathf.Cos(angle2) * radius,
                center.y, // Keep at same height
                center.z + Mathf.Sin(angle2) * radius
            );

            Gizmos.DrawLine(point1, point2);
        }
    }

    // Get the appropriate prefab for a video with the following priority:
    // 1. Video-specific prefab from the database
    // 2. Zone-specific default prefab
    // 3. Global default prefab
    public GameObject GetPrefabForVideo(VideoEntry video)
    {
        if (video == null)
        {
            Debug.LogWarning("Null video passed to GetPrefabForVideo");
            return defaultPrefab;
        }

        // First check if the video specifies a prefab type and we have that mapping
        if (!string.IsNullOrEmpty(video.Prefab) && prefabMap.ContainsKey(video.Prefab))
        {
            if (verboseLogging) Debug.Log($"Using video-specific prefab type: {video.Prefab} for {video.Title}");
            return prefabMap[video.Prefab];
        }

        // Check if the video is in a specific zone and we have a prefab for that zone
        if (zoneManager != null && video.Zones.Count > 0)
        {
            foreach (string zoneName in video.Zones)
            {
                if (zonePrefabMap.ContainsKey(zoneName))
                {
                    if (verboseLogging) Debug.Log($"Using zone-specific prefab for zone: {zoneName}, video: {video.Title}");
                    return zonePrefabMap[zoneName];
                }
            }
        }

        // Fall back to the global default prefab
        if (verboseLogging) Debug.Log($"Using default prefab for video: {video.Title}");
        return defaultPrefab;
    }

    // Main method to place all video links in the scene - make it public to be accessible via Invoke
    public void PlaceAllVideoLinks()
    {
        // Ensure we have a database manager
        if (databaseManager == null)
        {
            databaseManager = FindObjectOfType<VideoDatabaseManager>();
            if (databaseManager == null)
            {
                Debug.LogError("Cannot place video links: database manager is null");
                return;
            }
        }

        if (verboseLogging) Debug.Log("Starting placement of all video links...");

        // Clear any existing prefabs
        ClearAllActivePrefabs();

        // Ensure zone manager is available
        if (zoneManager == null)
        {
            zoneManager = FindObjectOfType<PolygonZoneManager>();
            if (zoneManager == null)
            {
                Debug.LogWarning("Zone manager not found. Some placement features may not work correctly.");
            }
        }

        // Process each zone if zone manager exists
        if (zoneManager != null && zoneManager.zones != null)
        {
            foreach (PolygonZone zone in zoneManager.zones)
            {
                if (zone != null && zone.Points.Count >= 3)
                {
                    PlaceVideosInZone(zone);
                }
            }
        }
        else
        {
            // Fallback to using the database directly
            List<VideoEntry> allVideos = databaseManager.GetAllEntries();

            if (allVideos != null && allVideos.Count > 0)
            {
                if (verboseLogging) Debug.Log($"Found {allVideos.Count} videos to place");

                foreach (VideoEntry video in allVideos)
                {
                    PlaceVideoByDefault(video);
                }
            }
            else
            {
                Debug.LogWarning("No videos found in database to place");
            }
        }

        // Update orientation initially if not using dynamic updates
        if (faceCamera && !dynamicFacing)
        {
            UpdatePrefabOrientation();
        }

        if (verboseLogging) Debug.Log($"Finished placing {activePrefabs.Count} video links");
    }

    // Helper method to place videos in a zone
    private void PlaceVideosInZone(PolygonZone zone)
    {
        if (databaseManager == null || zone == null) return;

        // Get videos for this zone
        List<VideoEntry> zoneVideos = databaseManager.GetEntriesForZone(zone.Name);

        if (zoneVideos == null || zoneVideos.Count == 0)
        {
            if (verboseLogging) Debug.Log($"No videos found for zone: {zone.Name}");
            return;
        }

        int videoCount = Mathf.Min(zoneVideos.Count, zone.MaxVideos);
        if (verboseLogging) Debug.Log($"Placing {videoCount} videos in zone: {zone.Name}");

        // Track positions for spacing
        List<Vector3> placedPositions = new List<Vector3>();

        // Place each video
        for (int i = 0; i < videoCount; i++)
        {
            VideoEntry video = zoneVideos[i];
            Vector3 position = zoneManager.FindValidPositionInZone(zone, placedPositions, video);

            if (position == Vector3.zero)
            {
                Debug.LogWarning($"Could not find valid position for video in zone: {zone.Name}");
                continue;
            }

            PlaceVideoAtPosition(video, position);
            placedPositions.Add(position);
        }
    }

    // Place a video using default logic (when zone placement isn't available)
    private void PlaceVideoByDefault(VideoEntry video)
    {
        if (video == null) return;

        Vector3 position;

        // Try to place at a random position
        if (TryRandomPlacement(out position))
        {
            PlaceVideoAtPosition(video, position);
        }
        else
        {
            Debug.LogWarning($"Could not find valid position for video: {video.Title}");
        }
    }

    // Helper method to place a video at a specific position
    private void PlaceVideoAtPosition(VideoEntry video, Vector3 position)
    {
        GameObject prefab = GetPrefabForVideo(video);
        if (prefab == null)
        {
            prefab = defaultPrefab;
            if (prefab == null)
            {
                Debug.LogError("No default prefab available for video placement");
                return;
            }
        }

        // Create the instance
        GameObject instance = Instantiate(prefab, position, Quaternion.identity);

        // Find or add a video link component
        VideoLinkComponent linkComponent = instance.GetComponent<VideoLinkComponent>();
        if (linkComponent == null)
        {
            linkComponent = instance.AddComponent<VideoLinkComponent>();
        }

        // Set up the component with video data
        if (linkComponent != null)
        {
            linkComponent.Initialize(video);
        }

        activePrefabs.Add(instance);
    }

    // Helper method to clear all active prefabs
    private void ClearAllActivePrefabs()
    {
        foreach (GameObject prefab in activePrefabs)
        {
            if (prefab != null)
            {
                Destroy(prefab);
            }
        }

        activePrefabs.Clear();
        if (verboseLogging) Debug.Log("Cleared all active prefabs");
    }

    // Try to place a video at a random position within range
    private bool TryRandomPlacement(out Vector3 position)
    {
        position = Vector3.zero;
        Vector3 center = mainCamera != null ? mainCamera.transform.position : transform.position;

        for (int i = 0; i < maxPlacementAttempts; i++)
        {
            // Get random position within reticle range
            float angle = Random.Range(0f, 2f * Mathf.PI);
            float distance = Random.Range(0f, reticleRange);

            position = new Vector3(
                center.x + Mathf.Cos(angle) * distance,
                0,
                center.z + Mathf.Sin(angle) * distance
            );

            if (IsPositionClear(position))
            {
                // Adjust height based on terrain
                position.y = GetHeightAtPosition(position) + placementHeight;
                return true;
            }
        }

        return false;
    }

    // Check if a position is clear of obstacles
    private bool IsPositionClear(Vector3 position)
    {
        Collider[] colliders = Physics.OverlapSphere(position, avoidObstacleRadius, obstacleLayer);
        return colliders.Length == 0;
    }

    // Get the terrain height at a specific position
    private float GetHeightAtPosition(Vector3 position)
    {
        // If we have a terrain, use its height
        if (terrain != null)
        {
            return terrain.SampleHeight(position);
        }
        // If we have a terrain collider, use raycast to find height
        else if (terrainCollider != null)
        {
            RaycastHit hit;
            if (Physics.Raycast(position + Vector3.up * 100f, Vector3.down, out hit, 200f))
            {
                return hit.point.y;
            }
        }

        // Default to zero if no terrain information available
        return 0f;
    }
}

// Simple video link component to avoid missing component errors
public class VideoLinkComponent : MonoBehaviour
{
    public string VideoUrl { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }

    public void Initialize(VideoEntry video)
    {
        if (video != null)
        {
            VideoUrl = video.PublicUrl;
            Title = video.Title;
            Description = video.Description;

            // Set up any UI elements with the video data
            SetupUI();
        }
    }

    private void SetupUI()
    {
        // Find title text component if it exists
        TextMeshProUGUI titleText = GetComponentInChildren<TextMeshProUGUI>();
        if (titleText != null)
        {
            titleText.text = Title;
        }
    }
}