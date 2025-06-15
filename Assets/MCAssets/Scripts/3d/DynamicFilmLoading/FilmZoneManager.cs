using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;
using static UnityEditor.ShaderData;
using static UnityEditorInternal.ReorderableList;
using UnityEngine.InputSystem;

/// <summary>
/// Main Purpose: Manages video placement in 3D zones with a smart prefab selection system.
//Key Features:

//Zone Management: Defines polygonal areas in 3D space where videos can be placed
//3-Tier Prefab System: Automatically selects the right prefab for each video:

//Film - specific prefab(from JSON data)
//Zone - specific prefab(themed for each zone)
//    Default prefab(fallback)


//JSON Data Handling: Parses video information from JSON files
//Spatial Calculations: Determines if points are inside zones, calculates heights, finds random positions
//Video Components: Manages VideoZonePrefab components that handle individual video interactions

//What it does: Acts as the central brain that decides which prefab to use for each video and where to place it in the 3D world.
/// </summary>

// Data structures for JSON parsing
[Serializable]
public class FilmDataEntry
{
    public string FileName;          // IGNORED
    public string PublicUrl;         // MANDATORY - video URL
    public string BucketPath;        // IGNORED
    public string Title;             // MANDATORY - video title
    public string Description;       // OPTIONAL - video description
    public string Prefab;           // OPTIONAL - specific prefab name (uses default if empty)
    public string Zone;             // IGNORED - legacy field
    public string[] Zones;          // MANDATORY - array of zones to place this video in

    // Helper method to get the video URL
    public string GetVideoUrl()
    {
        return PublicUrl;
    }

    // Helper method to check if custom prefab is specified
    public bool HasCustomPrefab()
    {
        return !string.IsNullOrEmpty(Prefab);
    }

    // Helper method to get zones for placement (handles both single Zone and Zones array)
    public string[] GetPlacementZones()
    {
        // Primary: use Zones array if it exists and has content
        if (Zones != null && Zones.Length > 0)
        {
            // Filter out empty strings
            var validZones = new System.Collections.Generic.List<string>();
            foreach (var zone in Zones)
            {
                if (!string.IsNullOrEmpty(zone.Trim()))
                {
                    validZones.Add(zone.Trim());
                }
            }
            return validZones.ToArray();
        }

        // Fallback: if Zones array is empty but Zone field exists, use that
        if (!string.IsNullOrEmpty(Zone))
        {
            return new string[] { Zone.Trim() };
        }

        // No zones specified
        return new string[0];
    }
}

[Serializable]
public class FilmDataCollection
{
    public FilmDataEntry[] Entries;
}

// Film zone definition for polygonal areas
[Serializable]
public class FilmZone
{
    public string zoneName;
    public List<Vector3> polygonPoints = new List<Vector3>();
    public Color gizmoColor = Color.blue;
    public bool showGizmos = true;

    // Check if a point is inside this polygonal zone
    public bool IsPointInZone(Vector3 point)
    {
        if (polygonPoints.Count < 3) return false;

        // Convert 3D points to 2D for polygon calculation (using X,Z coordinates)
        Vector2 testPoint = new Vector2(point.x, point.z);
        Vector2[] polygon = new Vector2[polygonPoints.Count];

        for (int i = 0; i < polygonPoints.Count; i++)
        {
            polygon[i] = new Vector2(polygonPoints[i].x, polygonPoints[i].z);
        }

        return IsPointInPolygon(testPoint, polygon);
    }

    // Check if a 2D point is inside this zone (ignoring Y coordinate)
    public bool IsPointInZone2D(Vector2 point)
    {
        if (polygonPoints.Count < 3) return false;

        Vector2[] polygon = new Vector2[polygonPoints.Count];
        for (int i = 0; i < polygonPoints.Count; i++)
        {
            polygon[i] = new Vector2(polygonPoints[i].x, polygonPoints[i].z);
        }

        return IsPointInPolygon(point, polygon);
    }

    // Ray casting algorithm for point-in-polygon test
    private bool IsPointInPolygon(Vector2 point, Vector2[] polygon)
    {
        int count = 0;
        for (int i = 0; i < polygon.Length; i++)
        {
            Vector2 p1 = polygon[i];
            Vector2 p2 = polygon[(i + 1) % polygon.Length];

            if (((p1.y > point.y) != (p2.y > point.y)) &&
                (point.x < (p2.x - p1.x) * (point.y - p1.y) / (p2.y - p1.y) + p1.x))
            {
                count++;
            }
        }
        return (count % 2) == 1;
    }

    // Get the average height of the zone
    public float GetZoneHeight()
    {
        if (polygonPoints.Count == 0) return 0f;

        float totalHeight = 0f;
        foreach (var point in polygonPoints)
        {
            totalHeight += point.y;
        }
        return totalHeight / polygonPoints.Count;
    }

    // Get a random position within the zone bounds at zone height
    public Vector3 GetRandomPointInZoneAtZoneHeight()
    {
        if (polygonPoints.Count < 3) return Vector3.zero;

        // Get bounding box
        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;

        foreach (var point in polygonPoints)
        {
            minX = Mathf.Min(minX, point.x);
            maxX = Mathf.Max(maxX, point.x);
            minZ = Mathf.Min(minZ, point.z);
            maxZ = Mathf.Max(maxZ, point.z);
        }

        // Get the zone's average height
        float zoneHeight = GetZoneHeight();

        // Try to find a valid point within the polygon
        for (int attempts = 0; attempts < 100; attempts++)
        {
            float x = UnityEngine.Random.Range(minX, maxX);
            float z = UnityEngine.Random.Range(minZ, maxZ);
            Vector2 testPoint2D = new Vector2(x, z);

            if (IsPointInZone2D(testPoint2D))
            {
                // Return position at zone height
                return new Vector3(x, zoneHeight, z);
            }
        }

        // Fallback to center of bounding box at zone height
        Vector3 center = new Vector3((minX + maxX) / 2, zoneHeight, (minZ + maxZ) / 2);
        return center;
    }

    // Get a random point within the zone bounds (legacy method for terrain sampling)
    public Vector3 GetRandomPointInZone()
    {
        if (polygonPoints.Count < 3) return Vector3.zero;

        // Get bounding box
        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;

        foreach (var point in polygonPoints)
        {
            minX = Mathf.Min(minX, point.x);
            maxX = Mathf.Max(maxX, point.x);
            minZ = Mathf.Min(minZ, point.z);
            maxZ = Mathf.Max(maxZ, point.z);
        }

        // Try to find a valid point within the polygon
        for (int attempts = 0; attempts < 100; attempts++)
        {
            float x = UnityEngine.Random.Range(minX, maxX);
            float z = UnityEngine.Random.Range(minZ, maxZ);
            Vector3 testPoint = new Vector3(x, 0, z);

            if (IsPointInZone(testPoint))
            {
                // Sample terrain height at this position
                Terrain terrain = Terrain.activeTerrain;
                if (terrain != null)
                {
                    float height = terrain.SampleHeight(testPoint);
                    testPoint.y = height;
                }

                return testPoint;
            }
        }

        // Fallback to center of bounding box
        Vector3 center = new Vector3((minX + maxX) / 2, 0, (minZ + maxZ) / 2);
        Terrain activeTerrain = Terrain.activeTerrain;
        if (activeTerrain != null)
        {
            center.y = activeTerrain.SampleHeight(center);
        }

        return center;
    }
}

// Main film zone manager component
public class FilmZoneManager : MonoBehaviour
{
    [Header("Film Zone Configuration")]
    public List<FilmZone> zones = new List<FilmZone>();

    [Header("Prefab Settings")]
    public GameObject defaultPrefab;
    public List<PrefabMapping> prefabMappings = new List<PrefabMapping>();
    public List<ZonePrefabMapping> zonePrefabMappings = new List<ZonePrefabMapping>();

    [Header("Placement Settings")]
    public float prefabSpacing = 2f;
    public bool placeOnTerrain = false; // Changed default to false to use zone height
    public LayerMask terrainLayer = 1;

    [Header("Debug")]
    public bool showZoneGizmos = true;

    private Dictionary<string, GameObject> prefabDict;

    [Serializable]
    public class PrefabMapping
    {
        public string prefabName;
        public GameObject prefab;
    }

    [Serializable]
    public class ZonePrefabMapping
    {
        public string zoneName;
        public GameObject prefab;
    }

    private void Awake()
    {
        // Build prefab dictionary
        prefabDict = new Dictionary<string, GameObject>();

        if (prefabMappings != null)
        {
            foreach (var mapping in prefabMappings)
            {
                if (!string.IsNullOrEmpty(mapping.prefabName) && mapping.prefab != null)
                {
                    prefabDict[mapping.prefabName] = mapping.prefab;
                }
            }
        }
    }

    public FilmZone GetZoneByName(string zoneName)
    {
        if (zones == null || string.IsNullOrEmpty(zoneName)) return null;
        return zones.Find(z => z.zoneName.Equals(zoneName, StringComparison.OrdinalIgnoreCase));
    }

    public GameObject GetPrefabForEntry(FilmDataEntry entry, string zoneName = null)
    {
        // Ensure prefabDict is initialized
        if (prefabDict == null)
        {
            Debug.Log("Initializing prefab dictionary...");
            prefabDict = new Dictionary<string, GameObject>();
            if (prefabMappings != null)
            {
                foreach (var mapping in prefabMappings)
                {
                    if (!string.IsNullOrEmpty(mapping.prefabName) && mapping.prefab != null)
                    {
                        prefabDict[mapping.prefabName] = mapping.prefab;
                        Debug.Log($"Added prefab mapping: '{mapping.prefabName}' -> {mapping.prefab.name}");
                    }
                }
            }
        }

        // PRIORITY 1: Check if entry specifies a custom prefab (Film-level)
        if (entry != null && !string.IsNullOrEmpty(entry.Prefab))
        {
            Debug.Log($"Looking for custom film prefab '{entry.Prefab}' for entry '{entry.Title}'");

            // Try exact match first
            if (prefabDict.ContainsKey(entry.Prefab))
            {
                Debug.Log($"✅ Found exact film prefab match: Using custom prefab '{entry.Prefab}' for entry '{entry.Title}'");
                return prefabDict[entry.Prefab];
            }

            // Try case-insensitive match
            foreach (var kvp in prefabDict)
            {
                if (string.Equals(kvp.Key, entry.Prefab, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.Log($"✅ Found case-insensitive film prefab match: Using custom prefab '{kvp.Key}' for entry '{entry.Title}'");
                    return kvp.Value;
                }
            }

            // Try partial match (contains)
            foreach (var kvp in prefabDict)
            {
                if (kvp.Key.ToLower().Contains(entry.Prefab.ToLower()) ||
                    entry.Prefab.ToLower().Contains(kvp.Key.ToLower()))
                {
                    Debug.Log($"✅ Found partial film prefab match: Using custom prefab '{kvp.Key}' for entry '{entry.Title}' (searched for '{entry.Prefab}')");
                    return kvp.Value;
                }
            }

            Debug.LogWarning($"❌ Custom film prefab '{entry.Prefab}' not found in prefab mappings for entry '{entry.Title}'. Checking zone prefabs...");
        }

        // PRIORITY 2: Check for zone-specific prefab (Zone-level)
        if (!string.IsNullOrEmpty(zoneName) && zonePrefabMappings != null)
        {
            Debug.Log($"Looking for zone prefab for zone '{zoneName}'");

            var zonePrefabMapping = zonePrefabMappings.FirstOrDefault(z =>
                string.Equals(z.zoneName, zoneName, StringComparison.OrdinalIgnoreCase));

            if (zonePrefabMapping != null && zonePrefabMapping.prefab != null)
            {
                Debug.Log($"✅ Found zone prefab: Using zone prefab '{zonePrefabMapping.prefab.name}' for zone '{zoneName}' and entry '{entry?.Title ?? "Unknown"}'");
                return zonePrefabMapping.prefab;
            }
            else
            {
                Debug.Log($"No zone prefab found for zone '{zoneName}'. Falling back to default prefab.");
            }
        }

        // PRIORITY 3: Return default prefab (Default-level)
        if (defaultPrefab != null)
        {
            Debug.Log($"Using default prefab '{defaultPrefab.name}' for entry '{entry?.Title ?? "Unknown"}' in zone '{zoneName ?? "Unknown"}'");
            return defaultPrefab;
        }
        else
        {
            Debug.LogError($"❌ No default prefab set! Cannot place entry '{entry?.Title ?? "Unknown"}'. Please assign a default prefab in the FilmZoneManager.");
            return null;
        }
    }

    // Helper method to get zone prefab (for UI display purposes)
    public GameObject GetZonePrefab(string zoneName)
    {
        if (string.IsNullOrEmpty(zoneName) || zonePrefabMappings == null)
            return null;

        var zonePrefabMapping = zonePrefabMappings.FirstOrDefault(z =>
            string.Equals(z.zoneName, zoneName, StringComparison.OrdinalIgnoreCase));

        return zonePrefabMapping?.prefab;
    }

    public Vector3 GetTerrainPosition(Vector3 worldPosition)
    {
        if (!placeOnTerrain) return worldPosition;

        Terrain terrain = Terrain.activeTerrain;
        if (terrain != null)
        {
            float height = terrain.SampleHeight(worldPosition);
            return new Vector3(worldPosition.x, height, worldPosition.z);
        }

        // Fallback: raycast downward
        RaycastHit hit;
        if (Physics.Raycast(worldPosition + Vector3.up * 100f, Vector3.down, out hit, 200f, terrainLayer))
        {
            return hit.point;
        }

        return worldPosition;
    }

    private void OnDrawGizmos()
    {
        if (!showZoneGizmos || zones == null) return;

        foreach (var zone in zones)
        {
            if (zone == null || !zone.showGizmos || zone.polygonPoints.Count < 3) continue;

            Gizmos.color = zone.gizmoColor;

            // Draw polygon outline
            for (int i = 0; i < zone.polygonPoints.Count; i++)
            {
                Vector3 current = zone.polygonPoints[i];
                Vector3 next = zone.polygonPoints[(i + 1) % zone.polygonPoints.Count];
                Gizmos.DrawLine(current, next);

                // Draw vertical lines to show height
                Gizmos.color = Color.red;
                Gizmos.DrawLine(current, current + Vector3.down * 2f);
                Gizmos.color = zone.gizmoColor;
            }

            // Draw zone center with height indicator
            if (zone.polygonPoints.Count > 0)
            {
                Vector3 center = Vector3.zero;
                foreach (var point in zone.polygonPoints)
                {
                    center += point;
                }
                center /= zone.polygonPoints.Count;

                // Draw center sphere
                Gizmos.color = zone.gizmoColor * 0.8f;
                Gizmos.DrawSphere(center, 0.5f);

                // Draw height indicator line
                Gizmos.color = Color.red;
                Gizmos.DrawLine(center, center + Vector3.down * 5f);
            }
        }
    }
}

// Component for individual video prefabs that works with your existing Event Trigger system
public class VideoZonePrefab : MonoBehaviour
{
    [Header("Video Data")]
    public string videoUrl;
    public string videoTitle;
    public string videoDescription;
    public string zoneName;

    [Header("Hover Settings")]
    public float hoverTimeRequired = 3.0f;
    public bool mouseHover = false;
    private float hoverTimer = 0;

    [Header("Scene Navigation")]
    public string returnScene = "mainVR";
    public string nextScene = "360VideoApp";
    public string behaviour = "return";
    public int stage = 0;

    [Header("Components")]
    public EnhancedVideoPlayer videoPlayer;
    public BoxCollider boxCollider;
    public EventTrigger eventTrigger;

    // Progress indicator (optional)
    private GameObject progressIndicator;
    private Image progressBar;

    private void Awake()
    {
        SetupComponents();
    }

    private void SetupComponents()
    {
        // Ensure we have required components
        if (boxCollider == null)
        {
            boxCollider = GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                boxCollider = gameObject.AddComponent<BoxCollider>();
                boxCollider.isTrigger = true; // Make sure it's a trigger for event system
            }
        }

        // Set up Event Trigger component like your existing system
        if (eventTrigger == null)
        {
            eventTrigger = GetComponent<EventTrigger>();
            if (eventTrigger == null)
            {
                eventTrigger = gameObject.AddComponent<EventTrigger>();
            }
        }

        SetupEventTriggers();

        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<EnhancedVideoPlayer>();
            if (videoPlayer == null)
            {
                videoPlayer = gameObject.AddComponent<EnhancedVideoPlayer>();
            }
        }
    }

    private void SetupEventTriggers()
    {
        if (eventTrigger.triggers == null)
        {
            eventTrigger.triggers = new List<EventTrigger.Entry>();
        }

        // Clear existing entries to avoid duplicates
        eventTrigger.triggers.Clear();

        // Pointer Enter Event
        EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
        pointerEnter.eventID = EventTriggerType.PointerEnter;
        pointerEnter.callback.AddListener((data) => { MouseHoverChangeScene(); });
        eventTrigger.triggers.Add(pointerEnter);

        // Pointer Exit Event
        EventTrigger.Entry pointerExit = new EventTrigger.Entry();
        pointerExit.eventID = EventTriggerType.PointerExit;
        pointerExit.callback.AddListener((data) => { MouseExit(); });
        eventTrigger.triggers.Add(pointerExit);

        // Optional: Pointer Click Event for immediate trigger
        EventTrigger.Entry pointerClick = new EventTrigger.Entry();
        pointerClick.eventID = EventTriggerType.PointerClick;
        pointerClick.callback.AddListener((data) => { MouseHoverChangeScene(); });
        eventTrigger.triggers.Add(pointerClick);
    }

    private void Update()
    {
        // Handle hover timer logic (same as DynamicLoadVideo1)
        if (mouseHover)
        {
            hoverTimer += Time.deltaTime;

            // Update progress indicator if it exists
            UpdateProgressIndicator();

            if (hoverTimer >= hoverTimeRequired)
            {
                mouseHover = false;
                hoverTimer = 0;
                TriggerVideoLoad();
            }
        }
    }

    private void UpdateProgressIndicator()
    {
        if (progressBar != null)
        {
            float progress = Mathf.Clamp01(hoverTimer / hoverTimeRequired);
            progressBar.fillAmount = progress;
        }
    }

    private void Start()
    {
        ConfigureVideoPlayer();
        CreateProgressIndicator();
    }

    public void SetVideoData(FilmDataEntry entry, string placementZone)
    {
        if (entry == null) return;

        videoUrl = entry.GetVideoUrl();  // Use PublicUrl
        videoTitle = entry.Title;
        videoDescription = entry.Description ?? ""; // Handle null descriptions
        zoneName = placementZone;  // The specific zone this instance is placed in

        // Update the gameObject name for clarity
        gameObject.name = $"Video_{entry.Title}_{placementZone}";

        ConfigureVideoPlayer();
    }

    private void ConfigureVideoPlayer()
    {
        if (videoPlayer == null) return;

        // Configure the EnhancedVideoPlayer to match your existing setup
        videoPlayer.VideoUrlLink = videoUrl;
        videoPlayer.title = videoTitle;
        videoPlayer.description = videoDescription;
        videoPlayer.nextscene = nextScene;
        videoPlayer.zoneName = zoneName;
        videoPlayer.returntoscene = returnScene;
        videoPlayer.behaviour = behaviour;
        videoPlayer.returnstage = stage;

        // Set hover time to match this component
        videoPlayer.hoverTimeRequired = hoverTimeRequired;
    }

    private void CreateProgressIndicator()
    {
        // Create a simple progress indicator similar to your existing system
        GameObject canvas = new GameObject("ProgressCanvas");
        canvas.transform.SetParent(transform);
        canvas.transform.localPosition = Vector3.zero;

        Canvas canvasComponent = canvas.AddComponent<Canvas>();
        canvasComponent.renderMode = RenderMode.WorldSpace;
        canvasComponent.worldCamera = Camera.main;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(2, 0.5f);
        canvasRect.localPosition = new Vector3(0, 2, 0);
        canvasRect.localScale = Vector3.one * 0.01f;

        // Progress bar background
        GameObject progressBG = new GameObject("ProgressBackground");
        progressBG.transform.SetParent(canvas.transform);

        Image bgImage = progressBG.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.5f);

        RectTransform bgRect = progressBG.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;

        // Progress bar fill
        GameObject progressFill = new GameObject("ProgressFill");
        progressFill.transform.SetParent(progressBG.transform);

        progressBar = progressFill.AddComponent<Image>();
        progressBar.color = Color.green;
        progressBar.type = Image.Type.Filled;
        progressBar.fillMethod = Image.FillMethod.Horizontal;

        RectTransform fillRect = progressFill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;

        progressIndicator = canvas;
        progressIndicator.SetActive(false);
    }

    // Event Trigger Methods (matching your DynamicLoadVideo1 pattern)
    public void MouseHoverChangeScene()
    {
        mouseHover = true;
        hoverTimer = 0;

        if (progressIndicator != null)
        {
            progressIndicator.SetActive(true);
        }

        Debug.Log($"Mouse hover started on {videoTitle}");
    }

    public void MouseExit()
    {
        mouseHover = false;
        hoverTimer = 0;

        if (progressIndicator != null)
        {
            progressIndicator.SetActive(false);
        }

        Debug.Log($"Mouse exit on {videoTitle}");
    }

    private void TriggerVideoLoad()
    {
        // Set PlayerPrefs exactly like DynamicLoadVideo1
        PlayerPrefs.SetString("nextscene", nextScene);
        PlayerPrefs.SetString("returntoscene", returnScene);
        PlayerPrefs.SetInt("stage", stage);
        PlayerPrefs.SetString("behaviour", behaviour);
        PlayerPrefs.SetString("VideoUrl", videoUrl);
        PlayerPrefs.SetString("lastknownzone", zoneName);

        // Store video metadata
        if (!string.IsNullOrEmpty(videoTitle))
        {
            PlayerPrefs.SetString("videoTitle", videoTitle);
        }

        if (!string.IsNullOrEmpty(videoDescription))
        {
            PlayerPrefs.SetString("videoDescription", videoDescription);
        }

        Debug.Log($"Loading video: {videoTitle} from zone: {zoneName}");

        // Load the video scene
        UnityEngine.SceneManagement.SceneManager.LoadScene("360VideoApp", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    // Method to manually trigger video playback (for testing)
    [ContextMenu("Play Video")]
    public void PlayVideo()
    {
        TriggerVideoLoad();
    }
}

// Serializable data for saving/loading prefab positions
[Serializable]
public class PrefabPositionData
{
    public string prefabId;
    public Vector3 position;
    public Vector3 rotation;
    public Vector3 scale;
    public string zoneName;
    public string videoUrl;
    public string videoTitle;
    public string videoDescription;
}

[Serializable]
public class ZoneLayoutData
{
    public List<PrefabPositionData> prefabPositions = new List<PrefabPositionData>();
}