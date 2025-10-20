using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;
using System.IO;

/// <summary>
/// COMPLETE FIXED FilmZoneManager with proper position persistence, consistent video handling, and HIERARCHY ORGANIZATION
/// UPDATED with EventSystem fix methods and enhanced debugging
/// </summary>

// Film data structures (unchanged - these get overwritten by DB exports)
[Serializable]
public class FilmDataEntry
{
    public string FileName;          // IGNORED
    public string PublicUrl;         // MANDATORY - video URL (used as unique key)
    public string BucketPath;        // IGNORED
    public string Title;             // MANDATORY - video title
    public string Description;       // OPTIONAL - video description
    public string Prefab;           // OPTIONAL - specific prefab name
    public string Zone;             // IGNORED - legacy field
    public string[] Zones;          // MANDATORY - array of zones to place this video in

    public string GetVideoUrl() => PublicUrl;
    public bool HasCustomPrefab() => !string.IsNullOrEmpty(Prefab);

    public string[] GetPlacementZones()
    {
        if (Zones != null && Zones.Length > 0)
        {
            var validZones = new List<string>();
            foreach (var zone in Zones)
            {
                if (!string.IsNullOrEmpty(zone.Trim()))
                {
                    validZones.Add(zone.Trim());
                }
            }
            return validZones.ToArray();
        }

        if (!string.IsNullOrEmpty(Zone))
        {
            return new string[] { Zone.Trim() };
        }

        return new string[0];
    }
}

[Serializable]
public class FilmDataCollection
{
    public FilmDataEntry[] Entries;
}

// Enhanced persistent layout storage with better error handling
[Serializable]
public class VideoTransformData
{
    public string publicUrl;         // Unique key - the video URL
    public string zoneName;         // Zone where this instance is placed
    public Vector3 position;
    public Vector3 rotation;
    public Vector3 scale = Vector3.one;
    public string lastKnownTitle;   // For debugging/reference only
    public string lastUpdated;

    public VideoTransformData()
    {
        lastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public VideoTransformData(string url, string zone, Transform transform, string title = "")
    {
        publicUrl = url;
        zoneName = zone;
        position = transform.position;
        rotation = transform.rotation.eulerAngles;
        scale = transform.localScale;
        lastKnownTitle = title;
        lastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public void ApplyToTransform(Transform target)
    {
        if (target == null) return;

        target.position = position;
        target.rotation = Quaternion.Euler(rotation);
        target.localScale = scale;
    }

    // Generate a unique key for this video/zone combination
    public string GetUniqueKey() => $"{publicUrl}|{zoneName}";

    // Validation method
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(publicUrl) && !string.IsNullOrEmpty(zoneName);
    }
}

[Serializable]
public class PersistentLayoutData
{
    public List<VideoTransformData> videoTransforms = new List<VideoTransformData>();
    public string lastSaved;
    public int version = 1;

    public PersistentLayoutData()
    {
        lastSaved = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    // Find transform data by URL and zone
    public VideoTransformData FindTransform(string publicUrl, string zoneName)
    {
        if (string.IsNullOrEmpty(publicUrl) || string.IsNullOrEmpty(zoneName))
            return null;

        string key = $"{publicUrl}|{zoneName}";
        return videoTransforms.FirstOrDefault(vt => vt.GetUniqueKey() == key);
    }

    // Save or update transform data
    public void SaveTransform(string publicUrl, string zoneName, Transform transform, string title = "")
    {
        if (string.IsNullOrEmpty(publicUrl) || string.IsNullOrEmpty(zoneName) || transform == null)
            return;

        string key = $"{publicUrl}|{zoneName}";

        // Remove existing entry if it exists
        videoTransforms.RemoveAll(vt => vt.GetUniqueKey() == key);

        // Add new entry
        videoTransforms.Add(new VideoTransformData(publicUrl, zoneName, transform, title));
        lastSaved = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    // Remove transform data
    public bool RemoveTransform(string publicUrl, string zoneName)
    {
        if (string.IsNullOrEmpty(publicUrl) || string.IsNullOrEmpty(zoneName))
            return false;

        string key = $"{publicUrl}|{zoneName}";
        int removed = videoTransforms.RemoveAll(vt => vt.GetUniqueKey() == key);
        if (removed > 0)
        {
            lastSaved = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
        return removed > 0;
    }

    // Get all zones for a specific video URL
    public List<string> GetZonesForVideo(string publicUrl)
    {
        if (string.IsNullOrEmpty(publicUrl))
            return new List<string>();

        return videoTransforms
            .Where(vt => vt.publicUrl == publicUrl)
            .Select(vt => vt.zoneName)
            .ToList();
    }

    // Clean up orphaned entries and invalid data
    public int CleanupOrphanedEntries(List<string> validPublicUrls)
    {
        if (validPublicUrls == null)
            validPublicUrls = new List<string>();

        int originalCount = videoTransforms.Count;

        // Remove entries with invalid URLs or missing data
        videoTransforms.RemoveAll(vt =>
            !vt.IsValid() ||
            !validPublicUrls.Contains(vt.publicUrl));

        int removedCount = originalCount - videoTransforms.Count;

        if (removedCount > 0)
        {
            lastSaved = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        return removedCount;
    }
}

// Film zone definition (unchanged)
[Serializable]
public class FilmZone
{
    public string zoneName;
    public List<Vector3> polygonPoints = new List<Vector3>();
    public Color gizmoColor = Color.blue;
    public bool showGizmos = true;

    public bool IsPointInZone(Vector3 point)
    {
        if (polygonPoints.Count < 3) return false;

        Vector2 testPoint = new Vector2(point.x, point.z);
        Vector2[] polygon = new Vector2[polygonPoints.Count];

        for (int i = 0; i < polygonPoints.Count; i++)
        {
            polygon[i] = new Vector2(polygonPoints[i].x, polygonPoints[i].z);
        }

        return IsPointInPolygon(testPoint, polygon);
    }

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

    public float GetZoneHeight()
    {
        if (polygonPoints.Count == 0) return 0f;
        float totalHeight = 0f;
        foreach (var point in polygonPoints) totalHeight += point.y;
        return totalHeight / polygonPoints.Count;
    }

    public Vector3 GetRandomPointInZoneAtZoneHeight()
    {
        if (polygonPoints.Count < 3) return Vector3.zero;

        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;

        foreach (var point in polygonPoints)
        {
            minX = Mathf.Min(minX, point.x);
            maxX = Mathf.Max(maxX, point.x);
            minZ = Mathf.Min(minZ, point.z);
            maxZ = Mathf.Max(maxZ, point.z);
        }

        float zoneHeight = GetZoneHeight();

        for (int attempts = 0; attempts < 100; attempts++)
        {
            float x = UnityEngine.Random.Range(minX, maxX);
            float z = UnityEngine.Random.Range(minZ, maxZ);
            Vector2 testPoint2D = new Vector2(x, z);

            if (IsPointInZone2D(testPoint2D))
            {
                return new Vector3(x, zoneHeight, z);
            }
        }

        return new Vector3((minX + maxX) / 2, zoneHeight, (minZ + maxZ) / 2);
    }

    public Vector3 GetRandomPointInZone()
    {
        if (polygonPoints.Count < 3) return Vector3.zero;

        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;

        foreach (var point in polygonPoints)
        {
            minX = Mathf.Min(minX, point.x);
            maxX = Mathf.Max(maxX, point.x);
            minZ = Mathf.Min(minZ, point.z);
            maxZ = Mathf.Max(maxZ, point.z);
        }

        for (int attempts = 0; attempts < 100; attempts++)
        {
            float x = UnityEngine.Random.Range(minX, maxX);
            float z = UnityEngine.Random.Range(minZ, maxZ);
            Vector3 testPoint = new Vector3(x, 0, z);

            if (IsPointInZone(testPoint))
            {
                Terrain terrain = Terrain.activeTerrain;
                if (terrain != null)
                {
                    testPoint.y = terrain.SampleHeight(testPoint);
                }
                return testPoint;
            }
        }

        Vector3 center = new Vector3((minX + maxX) / 2, 0, (minZ + maxZ) / 2);
        Terrain activeTerrain = Terrain.activeTerrain;
        if (activeTerrain != null)
        {
            center.y = activeTerrain.SampleHeight(center);
        }
        return center;
    }

    public void ClearAllPoints()
    {
        polygonPoints.Clear();
        Debug.Log($"Cleared all polygon points for zone: {zoneName}");
    }
}

// COMPLETE Main FilmZoneManager component with HIERARCHY ORGANIZATION AND INSPECTOR BUTTONS + NEW EVENTSYSTEM FIX METHODS
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
    public bool placeOnTerrain = false;
    public LayerMask terrainLayer = 1;

    [Header("HIERARCHY ORGANIZATION")]
    [Space(5)]
    [Tooltip("When enabled, all video prefabs will be organized under Targets/[ZoneName]/Films/ folders")]
    public bool enableHierarchyOrganization = true;
    [Tooltip("Automatically organize videos when they are created")]
    public bool autoOrganizeOnCreate = true;

    [Header("File Paths (Separate Files)")]
    public string filmDataPath = "filmdata.json";           // Gets overwritten by DB
    public string layoutDataPath = "video_layout.json";     // Persistent layout storage
    public bool autoSaveOnPositionChange = true;
    public bool autoCleanupOrphanedPositions = true;

    [Header("Debug")]
    public bool showZoneGizmos = true;
    public bool showDebugInfo = false;
    [SerializeField] private bool enableDebugLogging = true;

    private Dictionary<string, GameObject> prefabDict;
    private Dictionary<string, GameObject> activePrefabs = new Dictionary<string, GameObject>();
    private PersistentLayoutData persistentLayout;

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
        BuildPrefabDictionary();
        LoadPersistentLayout();

        if (enableDebugLogging)
        {
            LogZoneDebugInfo();
        }
    }

    private void BuildPrefabDictionary()
    {
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

    // ===== NEW: EVENTSYSTEM MANAGEMENT METHODS =====

    [ContextMenu("🚨 Fix Multiple EventSystems Issue")]
    public void FixMultipleEventSystems()
    {
        Debug.Log("🔍 Checking for multiple EventSystems in scene...");

        EventSystem[] eventSystems = FindObjectsOfType<EventSystem>();

        if (eventSystems.Length == 1)
        {
            Debug.Log("✅ Perfect! Exactly 1 EventSystem found in scene.");

            // Validate the EventSystem is properly configured
            ValidateEventSystemConfiguration(eventSystems[0]);
            return;
        }

        if (eventSystems.Length == 0)
        {
            Debug.LogError("❌ No EventSystem found in scene! Creating one...");
            CreateEventSystem();
            return;
        }

        Debug.LogWarning($"🚨 FOUND {eventSystems.Length} EventSystems - this causes input conflicts!");

        // List all EventSystems
        for (int i = 0; i < eventSystems.Length; i++)
        {
            Debug.Log($"  EventSystem {i}: {eventSystems[i].gameObject.name} (Scene: {eventSystems[i].gameObject.scene.name})");
        }

        // Keep the first one, remove the others
        for (int i = eventSystems.Length - 1; i >= 1; i--)
        {
            GameObject toDestroy = eventSystems[i].gameObject;
            Debug.Log($"🗑️ Removing EventSystem: {toDestroy.name}");

            if (Application.isPlaying)
            {
                Destroy(toDestroy);
            }
            else
            {
                DestroyImmediate(toDestroy);
            }
        }

        Debug.Log($"✅ Fixed! Removed {eventSystems.Length - 1} extra EventSystems.");
        Debug.Log("🎯 Your video interactions should now work properly!");

        // Validate the remaining EventSystem
        ValidateEventSystemConfiguration(eventSystems[0]);
    }

    private void ValidateEventSystemConfiguration(EventSystem eventSystem)
    {
        if (eventSystem == null) return;

        Debug.Log($"🔧 Validating EventSystem: {eventSystem.gameObject.name}");

        // Check for Input Module
        var inputModules = eventSystem.GetComponents<BaseInputModule>();

        if (inputModules.Length == 0)
        {
            Debug.LogWarning("⚠️ EventSystem has no Input Module! Adding Standalone Input Module...");
            eventSystem.gameObject.AddComponent<StandaloneInputModule>();
        }
        else
        {
            Debug.Log($"✅ EventSystem has {inputModules.Length} Input Module(s):");
            foreach (var module in inputModules)
            {
                Debug.Log($"  - {module.GetType().Name} (enabled: {module.enabled})");
            }
        }

        // Check first selected object
        if (eventSystem.firstSelectedGameObject == null)
        {
            Debug.Log("ℹ️ EventSystem firstSelectedGameObject is null (this is usually fine for VR)");
        }

        Debug.Log("✅ EventSystem validation complete!");
    }

    private void CreateEventSystem()
    {
        GameObject eventSystemGO = new GameObject("EventSystem");
        eventSystemGO.AddComponent<EventSystem>();
        eventSystemGO.AddComponent<StandaloneInputModule>();

        Debug.Log("✅ Created new EventSystem with Standalone Input Module");
    }

    [ContextMenu("🔍 Debug All Video EventTriggers")]
    public void DebugAllVideoEventTriggers()
    {
        Debug.Log("🔍 Debugging all video EventTriggers in scene...");

        // First check EventSystems
        EventSystem[] eventSystems = FindObjectsOfType<EventSystem>();
        Debug.Log($"EventSystems in scene: {eventSystems.Length}");

        if (eventSystems.Length != 1)
        {
            Debug.LogError($"❌ CRITICAL: Found {eventSystems.Length} EventSystems (should be exactly 1)");
            Debug.LogError("🚨 This will prevent video interactions from working!");
            Debug.LogError("💡 Use 'Fix Multiple EventSystems Issue' button to fix this.");
        }

        // Check all videos
        EnhancedVideoPlayer[] allVideos = FindObjectsOfType<EnhancedVideoPlayer>();
        Debug.Log($"Found {allVideos.Length} EnhancedVideoPlayer components");

        int correctlyConfigured = 0;
        int needsFix = 0;
        int missingUrl = 0;

        foreach (var video in allVideos)
        {
            if (video == null) continue;

            bool hasIssues = false;
            List<string> issues = new List<string>();

            // Check VideoURL
            if (string.IsNullOrEmpty(video.VideoUrlLink))
            {
                issues.Add("Missing VideoUrlLink");
                missingUrl++;
                hasIssues = true;
            }

            // Check BoxCollider
            BoxCollider boxCollider = video.GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                issues.Add("Missing BoxCollider");
                hasIssues = true;
            }
            else if (!boxCollider.isTrigger)
            {
                issues.Add("BoxCollider not set as trigger");
                hasIssues = true;
            }

            // Check EventTrigger
            EventTrigger eventTrigger = video.GetComponent<EventTrigger>();
            if (eventTrigger == null)
            {
                issues.Add("Missing EventTrigger");
                hasIssues = true;
            }
            else if (eventTrigger.triggers == null || eventTrigger.triggers.Count == 0)
            {
                issues.Add("EventTrigger has no triggers");
                hasIssues = true;
            }

            if (hasIssues)
            {
                Debug.LogWarning($"⚠️ {video.gameObject.name}: {string.Join(", ", issues)}");
                needsFix++;
            }
            else
            {
                correctlyConfigured++;
            }
        }

        Debug.Log($"📊 EventTrigger Debug Summary:");
        Debug.Log($"  ✅ Correctly configured: {correctlyConfigured}");
        Debug.Log($"  ⚠️ Need fixes: {needsFix}");
        Debug.Log($"  ❌ Missing URLs: {missingUrl}");

        if (needsFix > 0)
        {
            Debug.Log("💡 Use 'Fix All Video EventTriggers' button to fix the issues.");
        }
    }

    [ContextMenu("🔧 Fix All Video EventTriggers")]
    public void FixAllVideoEventTriggers()
    {
        Debug.Log("🔧 Fixing EventTriggers on all videos...");

        // First fix EventSystems issue
        FixMultipleEventSystems();

        EnhancedVideoPlayer[] allVideos = FindObjectsOfType<EnhancedVideoPlayer>();
        int fixedCount = 0;
        int errorCount = 0;

        foreach (var video in allVideos)
        {
            if (video == null || video.gameObject == null)
            {
                errorCount++;
                continue;
            }

            try
            {
                // Ensure BoxCollider
                BoxCollider boxCollider = video.GetComponent<BoxCollider>();
                if (boxCollider == null)
                {
                    boxCollider = video.gameObject.AddComponent<BoxCollider>();
                }
                boxCollider.isTrigger = true;

                // Setup EventTrigger with enhanced logging
                EventTrigger eventTrigger = video.GetComponent<EventTrigger>();
                if (eventTrigger == null)
                {
                    eventTrigger = video.gameObject.AddComponent<EventTrigger>();
                }

                if (eventTrigger.triggers == null)
                {
                    eventTrigger.triggers = new List<EventTrigger.Entry>();
                }

                // Clear and re-add triggers with debug logging
                eventTrigger.triggers.Clear();

                // Add PointerEnter with debug
                EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
                pointerEnter.eventID = EventTriggerType.PointerEnter;
                pointerEnter.callback.AddListener((data) => {
                    Debug.Log($"🎯 PointerEnter: {video.title} - URL: {video.VideoUrlLink}");
                    video.MouseHoverChangeScene();
                });
                eventTrigger.triggers.Add(pointerEnter);

                // Add PointerExit with debug
                EventTrigger.Entry pointerExit = new EventTrigger.Entry();
                pointerExit.eventID = EventTriggerType.PointerExit;
                pointerExit.callback.AddListener((data) => {
                    Debug.Log($"🎯 PointerExit: {video.title}");
                    video.MouseExit();
                });
                eventTrigger.triggers.Add(pointerExit);

                // Optional: Add PointerClick for immediate activation
                EventTrigger.Entry pointerClick = new EventTrigger.Entry();
                pointerClick.eventID = EventTriggerType.PointerClick;
                pointerClick.callback.AddListener((data) => {
                    Debug.Log($"🎯 PointerClick: {video.title} - Immediate activation");

                    // Save video data to PlayerPrefs immediately
                    if (!string.IsNullOrEmpty(video.VideoUrlLink))
                    {
                        PlayerPrefs.SetString("VideoUrl", video.VideoUrlLink);
                        PlayerPrefs.SetString("videoTitle", video.title ?? "");
                        PlayerPrefs.SetString("videoDescription", video.description ?? "");
                        PlayerPrefs.SetString("lastknownzone", video.LastKnownZone ?? "");
                        PlayerPrefs.Save();

                        Debug.Log($"💾 Saved to PlayerPrefs: {video.VideoUrlLink}");
                    }

                    video.SetVideoUrl();
                });
                eventTrigger.triggers.Add(pointerClick);

                fixedCount++;

                // Validate the video has a URL
                if (string.IsNullOrEmpty(video.VideoUrlLink))
                {
                    Debug.LogWarning($"⚠️ {video.gameObject.name} has no VideoUrlLink! This video won't work.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to fix EventTriggers on {video.gameObject.name}: {ex.Message}");
                errorCount++;
            }
        }

        Debug.Log($"✅ EventTrigger fix complete! Fixed: {fixedCount}, Errors: {errorCount}");

        // Final validation
        DebugAllVideoEventTriggers();
    }

    [ContextMenu("🧪 Test Random Video Interaction")]
    public void TestRandomVideoInteraction()
    {
        EnhancedVideoPlayer[] allVideos = FindObjectsOfType<EnhancedVideoPlayer>();

        if (allVideos.Length == 0)
        {
            Debug.LogWarning("No EnhancedVideoPlayer components found in scene!");
            return;
        }

        // Pick a random video
        EnhancedVideoPlayer randomVideo = allVideos[UnityEngine.Random.Range(0, allVideos.Length)];

        Debug.Log($"🧪 Testing interaction on random video: {randomVideo.title}");
        Debug.Log($"VideoURL: {randomVideo.VideoUrlLink}");

        // Test the interaction chain
        Debug.Log("1. Simulating PointerEnter...");
        randomVideo.MouseHoverChangeScene();

        // Wait a moment then exit
        StartCoroutine(TestVideoInteractionSequence(randomVideo));
    }

    private System.Collections.IEnumerator TestVideoInteractionSequence(EnhancedVideoPlayer video)
    {
        yield return new WaitForSeconds(1f);

        Debug.Log("2. Simulating PointerExit...");
        video.MouseExit();

        yield return new WaitForSeconds(0.5f);

        Debug.Log("3. Testing direct URL saving...");
        if (!string.IsNullOrEmpty(video.VideoUrlLink))
        {
            PlayerPrefs.SetString("VideoUrl", video.VideoUrlLink);
            PlayerPrefs.SetString("videoTitle", video.title ?? "");
            PlayerPrefs.Save();

            string savedUrl = PlayerPrefs.GetString("VideoUrl");
            string savedTitle = PlayerPrefs.GetString("videoTitle");

            Debug.Log($"✅ Test Results:");
            Debug.Log($"  Saved URL: {savedUrl}");
            Debug.Log($"  Saved Title: {savedTitle}");
            Debug.Log($"  URLs match: {savedUrl == video.VideoUrlLink}");
        }
        else
        {
            Debug.LogError("❌ Test failed: Video has no URL to save!");
        }
    }

    [ContextMenu("📋 Generate System Health Report")]
    public void GenerateSystemHealthReport()
    {
        Debug.Log("📋 === FILM ZONE SYSTEM HEALTH REPORT ===");

        // EventSystem Check
        EventSystem[] eventSystems = FindObjectsOfType<EventSystem>();
        Debug.Log($"🎮 EventSystems: {eventSystems.Length} {(eventSystems.Length == 1 ? "✅" : "❌")}");

        // Video Components Check
        EnhancedVideoPlayer[] allVideos = FindObjectsOfType<EnhancedVideoPlayer>();
        Debug.Log($"🎬 EnhancedVideoPlayer components: {allVideos.Length}");

        int videosWithUrls = 0;
        int videosWithEventTriggers = 0;
        int videosWithBoxColliders = 0;
        int videosWithZones = 0;

        foreach (var video in allVideos)
        {
            if (!string.IsNullOrEmpty(video.VideoUrlLink)) videosWithUrls++;
            if (video.GetComponent<EventTrigger>() != null) videosWithEventTriggers++;
            if (video.GetComponent<BoxCollider>() != null) videosWithBoxColliders++;
            if (!string.IsNullOrEmpty(video.LastKnownZone) && video.LastKnownZone != "Home") videosWithZones++;
        }

        Debug.Log($"📊 Video Component Health:");
        Debug.Log($"  Videos with URLs: {videosWithUrls}/{allVideos.Length} {(videosWithUrls == allVideos.Length ? "✅" : "⚠️")}");
        Debug.Log($"  Videos with EventTriggers: {videosWithEventTriggers}/{allVideos.Length} {(videosWithEventTriggers == allVideos.Length ? "✅" : "⚠️")}");
        Debug.Log($"  Videos with BoxColliders: {videosWithBoxColliders}/{allVideos.Length} {(videosWithBoxColliders == allVideos.Length ? "✅" : "⚠️")}");
        Debug.Log($"  Videos with Zones: {videosWithZones}/{allVideos.Length} {(videosWithZones == allVideos.Length ? "✅" : "⚠️")}");

        // Zone System Check
        Debug.Log($"🗺️ Zones defined: {zones.Count}");
        int validZones = zones.Count(z => z.polygonPoints.Count >= 3);
        Debug.Log($"  Valid zones (3+ points): {validZones}/{zones.Count} {(validZones == zones.Count ? "✅" : "⚠️")}");

        // Prefab System Check
        Debug.Log($"🎯 Prefab mappings: {prefabMappings.Count}");
        Debug.Log($"🏠 Zone prefab mappings: {zonePrefabMappings.Count}");
        Debug.Log($"📦 Default prefab set: {(defaultPrefab != null ? "✅" : "❌")}");

        // File System Check
        string filmDataFullPath = System.IO.Path.Combine(Application.streamingAssetsPath, filmDataPath);
        string layoutDataFullPath = System.IO.Path.Combine(Application.streamingAssetsPath, layoutDataPath);

        Debug.Log($"📁 Film data file exists: {System.IO.File.Exists(filmDataFullPath)} {(System.IO.File.Exists(filmDataFullPath) ? "✅" : "❌")}");
        Debug.Log($"📁 Layout data file exists: {System.IO.File.Exists(layoutDataFullPath)} {(System.IO.File.Exists(layoutDataFullPath) ? "✅" : "⚠️")}");

        // Overall Health Score
        int healthScore = 0;
        int maxScore = 7;

        if (eventSystems.Length == 1) healthScore++;
        if (videosWithUrls == allVideos.Length) healthScore++;
        if (videosWithEventTriggers == allVideos.Length) healthScore++;
        if (videosWithBoxColliders == allVideos.Length) healthScore++;
        if (defaultPrefab != null) healthScore++;
        if (validZones == zones.Count && zones.Count > 0) healthScore++;
        if (System.IO.File.Exists(filmDataFullPath)) healthScore++;

        float healthPercentage = (float)healthScore / maxScore * 100f;
        string healthEmoji = healthPercentage >= 85f ? "🟢" : healthPercentage >= 60f ? "🟡" : "🔴";

        Debug.Log($"{healthEmoji} OVERALL SYSTEM HEALTH: {healthScore}/{maxScore} ({healthPercentage:F0}%)");

        if (healthScore < maxScore)
        {
            Debug.Log("💡 Recommended actions:");
            if (eventSystems.Length != 1) Debug.Log("  - Fix EventSystems using 'Fix Multiple EventSystems Issue'");
            if (videosWithUrls < allVideos.Length) Debug.Log("  - Some videos missing URLs - check film data import");
            if (videosWithEventTriggers < allVideos.Length) Debug.Log("  - Fix EventTriggers using 'Fix All Video EventTriggers'");
            if (defaultPrefab == null) Debug.Log("  - Set default prefab in FilmZoneManager");
            if (validZones < zones.Count) Debug.Log("  - Complete zone definitions (need 3+ points each)");
        }

        Debug.Log("📋 === END SYSTEM HEALTH REPORT ===");
    }

    [ContextMenu("🚀 QUICK FIX: Solve All Issues")]
    public void QuickFixAllIssues()
    {
        Debug.Log("🚀 === QUICK FIX: Solving all common issues ===");

        try
        {
            // Step 1: Fix EventSystems
            Debug.Log("1. Fixing EventSystems...");
            FixMultipleEventSystems();

            // Step 2: Fix all video EventTriggers
            Debug.Log("2. Fixing video EventTriggers...");
            FixAllVideoEventTriggers();

            // Step 3: Save current positions
            Debug.Log("3. Saving current positions...");
            SaveCurrentPositions();

            // Step 4: Generate health report
            Debug.Log("4. Generating final health report...");
            GenerateSystemHealthReport();

            Debug.Log("🎉 QUICK FIX COMPLETE! Try your video interactions now.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Quick fix failed: {ex.Message}");
        }
    }

    // ===== EXISTING INSPECTOR BUTTON METHODS =====

    [ContextMenu("Load Film Data and Apply Positions")]
    public void LoadFilmDataAndApplyPositions()
    {
        try
        {
            Debug.Log("🔄 Loading film data and applying saved positions...");

            // Load film data from JSON
            FilmDataCollection filmData = LoadFilmDataFromFile();
            if (filmData?.Entries == null)
            {
                Debug.LogError("❌ Failed to load film data!");
                return;
            }

            // Clear existing prefabs properly
            ClearAllVideoPrefabs();

            // Create prefabs with position restoration
            int restoredCount = 0;
            int newCount = 0;
            int errorCount = 0;

            foreach (var entry in filmData.Entries)
            {
                // Validate entry before processing
                if (string.IsNullOrEmpty(entry.PublicUrl))
                {
                    Debug.LogError($"❌ Entry '{entry.Title}' has no PublicUrl - skipping");
                    errorCount++;
                    continue;
                }

                string[] zones = entry.GetPlacementZones();
                if (zones.Length == 0)
                {
                    Debug.LogWarning($"⚠️ Entry '{entry.Title}' has no placement zones - skipping");
                    errorCount++;
                    continue;
                }

                Debug.Log($"Processing film '{entry.Title}' for zones: {string.Join(", ", zones)}");

                foreach (string zoneName in zones)
                {
                    FilmZone zone = GetZoneByName(zoneName);
                    if (zone == null)
                    {
                        Debug.LogError($"❌ Zone '{zoneName}' NOT FOUND for film '{entry.Title}'");
                        errorCount++;
                        continue;
                    }

                    try
                    {
                        bool wasRestored = CreatePrefabWithPositionRestore(entry, zoneName);
                        if (wasRestored)
                            restoredCount++;
                        else
                            newCount++;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"❌ Failed to create prefab for '{entry.Title}' in zone '{zoneName}': {ex.Message}");
                        errorCount++;
                    }
                }
            }

            // POST-IMPORT: Force setup EventTriggers on all created videos
            Debug.Log("🔧 Post-import: Setting up EventTriggers on all videos...");
            FixAllVideoEventTriggers();

            // Clean up orphaned position data with validation
            if (autoCleanupOrphanedPositions && persistentLayout != null)
            {
                List<string> validUrls = filmData.Entries
                    .Where(e => !string.IsNullOrEmpty(e.PublicUrl))
                    .Select(e => e.PublicUrl)
                    .ToList();

                int cleaned = persistentLayout.CleanupOrphanedEntries(validUrls);
                if (cleaned > 0)
                {
                    Debug.Log($"🧹 Cleaned up {cleaned} orphaned position entries");
                    SavePersistentLayout();
                }
            }

            Debug.Log($"✅ Import Complete! Restored: {restoredCount}, New: {newCount}, Errors: {errorCount}");

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Import failed: {ex.Message}\n{ex.StackTrace}");
        }
    }

    [ContextMenu("Organize Videos Into Hierarchy")]
    public void OrganizeExistingVideosIntoHierarchy()
    {
        if (!enableHierarchyOrganization)
        {
            Debug.LogWarning("⚠️ Hierarchy organization is disabled. Enable it in the inspector first.");
            return;
        }

        EnhancedVideoPlayer[] allVideos = FindObjectsOfType<EnhancedVideoPlayer>();
        int organizedCount = 0;
        int errorCount = 0;

        Debug.Log("🗂️ Organizing existing videos into proper hierarchy...");

        foreach (var video in allVideos)
        {
            if (video == null || video.gameObject == null)
            {
                errorCount++;
                continue;
            }

            try
            {
                string zoneName = video.LastKnownZone;
                if (string.IsNullOrEmpty(zoneName) || zoneName == "Home")
                {
                    Debug.LogWarning($"Video {video.gameObject.name} has no valid zone assignment, skipping");
                    continue;
                }

                // Find or create proper hierarchy
                Transform targetParent = FindOrCreateZoneHierarchy(zoneName);
                if (targetParent != null && video.transform.parent != targetParent)
                {
                    string oldPath = GetHierarchyPath(video.transform);
                    video.transform.SetParent(targetParent, true); // Keep world position
                    string newPath = GetHierarchyPath(video.transform);

                    Debug.Log($"📁 Moved {video.gameObject.name} from {oldPath} to {newPath}");
                    organizedCount++;

#if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(video.gameObject);
#endif
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to organize {video.gameObject.name}: {ex.Message}");
                errorCount++;
            }
        }

        Debug.Log($"✅ Organized {organizedCount} videos into proper hierarchy (Errors: {errorCount})");
    }

    [ContextMenu("Validate Video Hierarchy")]
    public void ValidateVideoHierarchy()
    {
        EnhancedVideoPlayer[] allVideos = FindObjectsOfType<EnhancedVideoPlayer>();
        int correctlyPlaced = 0;
        int incorrectlyPlaced = 0;
        int missingZone = 0;

        Debug.Log("🔍 Validating video hierarchy placement...");

        foreach (var video in allVideos)
        {
            if (video == null || video.gameObject == null) continue;

            string zoneName = video.LastKnownZone;
            if (string.IsNullOrEmpty(zoneName) || zoneName == "Home")
            {
                missingZone++;
                Debug.LogWarning($"❌ {video.gameObject.name}: Missing valid zone assignment");
                continue;
            }

            // Check if it's in the correct hierarchy
            string expectedPath = $"Targets/{zoneName}/Films";
            string actualPath = GetHierarchyPath(video.transform.parent);

            if (actualPath.Contains($"Targets/{zoneName}/Films"))
            {
                correctlyPlaced++;
                if (enableDebugLogging)
                {
                    Debug.Log($"✅ {video.gameObject.name}: Correctly placed in {actualPath}");
                }
            }
            else
            {
                incorrectlyPlaced++;
                Debug.LogWarning($"⚠️ {video.gameObject.name}: Should be in '{expectedPath}' but is in '{actualPath}'");
            }
        }

        Debug.Log($"📊 Hierarchy Validation Results:");
        Debug.Log($"   ✅ Correctly placed: {correctlyPlaced}");
        Debug.Log($"   ⚠️ Incorrectly placed: {incorrectlyPlaced}");
        Debug.Log($"   ❌ Missing zone: {missingZone}");
        Debug.Log($"   📁 Total videos: {allVideos.Length}");

        if (incorrectlyPlaced > 0)
        {
            Debug.Log("💡 Use 'Organize Videos Into Hierarchy' button to fix placement issues");
        }
    }

    [ContextMenu("Save Current Positions")]
    public void SaveCurrentPositions()
    {
        if (persistentLayout == null)
        {
            persistentLayout = new PersistentLayoutData();
        }

        int savedCount = 0;

        // Find all video prefabs using consistent approach
        EnhancedVideoPlayer[] allVideos = FindObjectsOfType<EnhancedVideoPlayer>();

        foreach (var video in allVideos)
        {
            // Use consistent property names and validation
            if (!string.IsNullOrEmpty(video.VideoUrlLink) && !string.IsNullOrEmpty(video.LastKnownZone))
            {
                persistentLayout.SaveTransform(
                    video.VideoUrlLink,  // Use VideoUrlLink consistently
                    video.LastKnownZone,
                    video.transform,
                    video.title
                );
                savedCount++;

                if (enableDebugLogging)
                {
                    Debug.Log($"Saved position for: {video.title} ({video.VideoUrlLink}) in zone: {video.LastKnownZone}");
                }
            }
            else
            {
                Debug.LogWarning($"Skipped saving {video.gameObject.name}: missing VideoUrlLink ({video.VideoUrlLink}) or LastKnownZone ({video.LastKnownZone})");
            }
        }

        SavePersistentLayout();
        Debug.Log($"✅ Saved positions for {savedCount} videos to persistent layout");
    }

    [ContextMenu("Create Zone Hierarchy Structure")]
    public void CreateZoneHierarchyStructure()
    {
        if (zones == null || zones.Count == 0)
        {
            Debug.LogWarning("⚠️ No zones defined. Create zones first before building hierarchy.");
            return;
        }

        int createdCount = 0;

        foreach (var zone in zones)
        {
            if (zone != null && !string.IsNullOrEmpty(zone.zoneName))
            {
                Transform hierarchyParent = FindOrCreateZoneHierarchy(zone.zoneName);
                if (hierarchyParent != null)
                {
                    createdCount++;
                }
            }
        }

        Debug.Log($"✅ Created/verified hierarchy structure for {createdCount} zones");
    }

    // ===== PERSISTENT LAYOUT MANAGEMENT =====

    private void LoadPersistentLayout()
    {
        try
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, layoutDataPath);

            if (File.Exists(fullPath))
            {
                string jsonContent = File.ReadAllText(fullPath);
                persistentLayout = JsonUtility.FromJson<PersistentLayoutData>(jsonContent);

                if (persistentLayout == null)
                {
                    Debug.LogWarning("Failed to parse persistent layout JSON, creating new layout data");
                    persistentLayout = new PersistentLayoutData();
                }
                else
                {
                    Debug.Log($"✅ Loaded persistent layout data: {persistentLayout.videoTransforms.Count} entries");
                }
            }
            else
            {
                persistentLayout = new PersistentLayoutData();
                Debug.Log("📝 Created new persistent layout data (file not found)");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Failed to load persistent layout: {ex.Message}");
            persistentLayout = new PersistentLayoutData();
        }
    }

    private void SavePersistentLayout()
    {
        if (persistentLayout == null)
        {
            Debug.LogError("Cannot save: persistentLayout is null");
            return;
        }

        try
        {
            // Ensure StreamingAssets directory exists
            string streamingAssetsPath = Application.streamingAssetsPath;
            if (!Directory.Exists(streamingAssetsPath))
            {
                Directory.CreateDirectory(streamingAssetsPath);
            }

            string fullPath = Path.Combine(streamingAssetsPath, layoutDataPath);
            string jsonContent = JsonUtility.ToJson(persistentLayout, true);
            File.WriteAllText(fullPath, jsonContent);

            Debug.Log($"✅ Saved persistent layout data: {persistentLayout.videoTransforms.Count} entries to {fullPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Failed to save persistent layout: {ex.Message}");
        }
    }

    // FIXED: Enhanced prefab creation with proper hierarchy organization
    private bool CreatePrefabWithPositionRestore(FilmDataEntry entry, string zoneName)
    {
        if (entry == null || string.IsNullOrEmpty(entry.PublicUrl) || string.IsNullOrEmpty(zoneName))
        {
            Debug.LogError("Invalid entry or zone name");
            return false;
        }

        // Check for saved position
        VideoTransformData savedTransform = persistentLayout?.FindTransform(entry.PublicUrl, zoneName);

        // Get the appropriate prefab
        GameObject prefabToUse = GetPrefabForEntry(entry, zoneName);
        if (prefabToUse == null)
        {
            Debug.LogError($"❌ No prefab available for {entry.Title} in zone {zoneName}");
            return false;
        }

        // Create the prefab instance
        GameObject newPrefab = null;
        try
        {
#if UNITY_EDITOR
            newPrefab = UnityEditor.PrefabUtility.InstantiatePrefab(prefabToUse) as GameObject;
#endif
            if (newPrefab == null)
            {
                newPrefab = Instantiate(prefabToUse);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to instantiate prefab: {ex.Message}");
            return false;
        }

        // Set name
        string prefabId = $"Video_{SanitizeFileName(entry.Title)}_{zoneName}";
        newPrefab.name = prefabId;

        // FIXED: Set proper parent hierarchy - find or create zone folder structure
        if (enableHierarchyOrganization && autoOrganizeOnCreate)
        {
            Transform zoneParent = FindOrCreateZoneHierarchy(zoneName);
            if (zoneParent != null)
            {
                newPrefab.transform.SetParent(zoneParent, false);
                Debug.Log($"✅ Placed {prefabId} under {GetHierarchyPath(zoneParent)}");
            }
            else
            {
                Debug.LogWarning($"⚠️ Could not find zone hierarchy for {zoneName}, placing at root");
            }
        }
        else
        {
            Debug.Log($"📍 Hierarchy organization disabled, placing {prefabId} at root");
        }

        // Configure video data with consistent property usage
        ConfigureVideoPrefab(newPrefab, entry, zoneName);

        // Apply transform
        bool wasRestored = false;
        if (savedTransform != null && savedTransform.IsValid())
        {
            // Restore saved position
            savedTransform.ApplyToTransform(newPrefab.transform);
            Debug.Log($"🔄 Restored position for {entry.Title} in {zoneName} at {savedTransform.position}");
            wasRestored = true;
        }
        else
        {
            // Place at random position in zone
            // Place at random position in zone
            PlacePrefabInZone(newPrefab, zoneName);
            Debug.Log($"🎲 Placed {entry.Title} at random position in zone {zoneName}");
            wasRestored = false;
        }

        // Configure video data with consistent property usage
        ConfigureVideoPrefab(newPrefab, entry, zoneName);

        // Track active prefabs
        string prefabKey = $"{entry.PublicUrl}|{zoneName}";
        if (!activePrefabs.ContainsKey(prefabKey))
        {
            activePrefabs.Add(prefabKey, newPrefab);
        }

        Debug.Log($"✅ Created {(wasRestored ? "restored" : "new")} prefab: {entry.Title} in zone {zoneName}");

#if UNITY_EDITOR
    UnityEditor.EditorUtility.SetDirty(newPrefab);
#endif

        return wasRestored;
    }

    // FIXED: Method to find or create the proper zone hierarchy
    private Transform FindOrCreateZoneHierarchy(string zoneName)
    {
        if (string.IsNullOrEmpty(zoneName))
        {
            Debug.LogWarning("Zone name is null or empty");
            return null;
        }

        // First, try to find existing hierarchy structure
        Transform existingZoneParent = FindExistingZoneHierarchy(zoneName);
        if (existingZoneParent != null)
        {
            return existingZoneParent;
        }

        // If not found, create the hierarchy structure
        return CreateZoneHierarchy(zoneName);
    }

    private Transform FindExistingZoneHierarchy(string zoneName)
    {
        // Look for: Targets/[ZoneName]/Films pattern
        GameObject targetsRoot = GameObject.Find("Targets");
        if (targetsRoot == null)
        {
            if (enableDebugLogging) Debug.Log("Targets object not found in scene");
            return null;
        }

        // Look for zone folder
        Transform zoneTransform = targetsRoot.transform.Find(zoneName);
        if (zoneTransform == null)
        {
            if (enableDebugLogging) Debug.Log($"Zone folder '{zoneName}' not found under Targets");
            return null;
        }

        // Look for Films folder under the zone
        Transform filmsTransform = zoneTransform.Find("Films");
        if (filmsTransform == null)
        {
            if (enableDebugLogging) Debug.Log($"Films folder not found under {zoneName}");
            return null;
        }

        Debug.Log($"✅ Found existing hierarchy: Targets/{zoneName}/Films");
        return filmsTransform;
    }

    private Transform CreateZoneHierarchy(string zoneName)
    {
        try
        {
            // Find or create Targets object
            GameObject targetsRoot = GameObject.Find("Targets");
            if (targetsRoot == null)
            {
                targetsRoot = new GameObject("Targets");
                Debug.Log("✅ Created Targets object");
            }

            // Find or create zone folder
            Transform zoneTransform = targetsRoot.transform.Find(zoneName);
            if (zoneTransform == null)
            {
                GameObject zoneObject = new GameObject(zoneName);
                zoneObject.transform.SetParent(targetsRoot.transform, false);
                zoneTransform = zoneObject.transform;
                Debug.Log($"✅ Created zone folder: {zoneName}");
            }

            // Find or create Films folder under zone
            Transform filmsTransform = zoneTransform.Find("Films");
            if (filmsTransform == null)
            {
                GameObject filmsObject = new GameObject("Films");
                filmsObject.transform.SetParent(zoneTransform, false);
                filmsTransform = filmsObject.transform;
                Debug.Log($"✅ Created Films folder under {zoneName}");
            }

            Debug.Log($"✅ Created/verified hierarchy: Targets/{zoneName}/Films");
            return filmsTransform;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to create zone hierarchy for {zoneName}: {ex.Message}");
            return null;
        }
    }

    // Helper method to get full hierarchy path for debugging
    private string GetHierarchyPath(Transform transform)
    {
        if (transform == null) return "null";

        string path = transform.name;
        Transform parent = transform.parent;

        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }

    // Helper method to sanitize filenames
    private string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return "Unknown";

        foreach (char c in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(c, '_');
        }
        return fileName;
    }

    private FilmDataCollection LoadFilmDataFromFile()
    {
        try
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath, filmDataPath);

            if (!File.Exists(fullPath))
            {
                Debug.LogError($"❌ Film data file not found: {fullPath}");
                return null;
            }

            string jsonContent = File.ReadAllText(fullPath);
            if (string.IsNullOrEmpty(jsonContent))
            {
                Debug.LogError("❌ Film data file is empty");
                return null;
            }

            FilmDataCollection filmData = JsonUtility.FromJson<FilmDataCollection>(jsonContent);

            if (filmData == null)
            {
                Debug.LogError("❌ Failed to parse film data JSON");
                return null;
            }

            Debug.Log($"✅ Loaded film data: {filmData.Entries?.Length ?? 0} entries");
            return filmData;
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Failed to load film data: {ex.Message}");
            return null;
        }
    }

    // FIXED: Enhanced video prefab configuration with proper EventTrigger setup
    private void ConfigureVideoPrefab(GameObject prefab, FilmDataEntry entry, string zoneName)
    {
        if (prefab == null || entry == null)
        {
            Debug.LogError("Cannot configure video prefab: prefab or entry is null");
            return;
        }

        Debug.Log($"🔧 Configuring video prefab: {entry.Title}");

        // Ensure we have EnhancedVideoPlayer
        EnhancedVideoPlayer videoPlayer = prefab.GetComponent<EnhancedVideoPlayer>();
        if (videoPlayer == null)
        {
            videoPlayer = prefab.AddComponent<EnhancedVideoPlayer>();
            Debug.Log($"Added EnhancedVideoPlayer to {entry.Title}");
        }

        // STEP 1: Configure video data FIRST
        videoPlayer.VideoUrlLink = entry.PublicUrl;  // Use PublicUrl consistently
        videoPlayer.title = entry.Title ?? "";
        videoPlayer.description = entry.Description ?? "";
        videoPlayer.zoneName = zoneName;
        videoPlayer.LastKnownZone = zoneName;

        // Set defaults if not configured
        if (string.IsNullOrEmpty(videoPlayer.returntoscene))
            videoPlayer.returntoscene = "mainVR";
        if (string.IsNullOrEmpty(videoPlayer.nextscene))
            videoPlayer.nextscene = "360VideoApp";

        // Set prefab type for categorization
        if (!string.IsNullOrEmpty(entry.Prefab))
        {
            videoPlayer.prefabType = entry.Prefab;
        }

        // Set category based on zone name
        videoPlayer.category = zoneName;

        Debug.Log($"Configured video data - URL: {videoPlayer.VideoUrlLink}, Zone: {videoPlayer.LastKnownZone}");

        // STEP 2: Ensure collider is properly configured
        BoxCollider boxCollider = prefab.GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = prefab.AddComponent<BoxCollider>();
        }
        boxCollider.isTrigger = true;

        // STEP 3: Update text components
        UpdateTextComponents(prefab, entry);

        // STEP 4: Setup EventTriggers AFTER all data is configured
        SetupEventTriggersForVideo(prefab, videoPlayer);

        // STEP 5: Final validation
        ValidateVideoPrefabConfiguration(prefab, videoPlayer);

#if UNITY_EDITOR
    UnityEditor.EditorUtility.SetDirty(videoPlayer);
    UnityEditor.EditorUtility.SetDirty(prefab);
#endif

        Debug.Log($"✅ Configured video: {entry.Title} -> {entry.PublicUrl} in zone: {zoneName}");
    }

    // FIXED: Enhanced EventTrigger setup method with proper PlayerPrefs saving
    private void SetupEventTriggersForVideo(GameObject prefab, EnhancedVideoPlayer videoPlayer)
    {
        if (prefab == null || videoPlayer == null)
        {
            Debug.LogError("Cannot setup EventTriggers: prefab or videoPlayer is null");
            return;
        }

        // Ensure VideoUrlLink is populated before setting up triggers
        if (string.IsNullOrEmpty(videoPlayer.VideoUrlLink))
        {
            Debug.LogWarning($"VideoUrlLink is empty for {prefab.name} - EventTriggers may not work properly");
        }

        EventTrigger eventTrigger = prefab.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = prefab.AddComponent<EventTrigger>();
            Debug.Log($"Added EventTrigger component to {prefab.name}");
        }

        // Initialize triggers list if null
        if (eventTrigger.triggers == null)
        {
            eventTrigger.triggers = new List<EventTrigger.Entry>();
        }

        // Clear existing triggers to avoid duplicates
        eventTrigger.triggers.Clear();
        Debug.Log($"Cleared existing triggers for {prefab.name}");

        // Add PointerEnter trigger
        EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
        pointerEnter.eventID = EventTriggerType.PointerEnter;
        pointerEnter.callback = new EventTrigger.TriggerEvent();
        pointerEnter.callback.AddListener((data) => {
            if (videoPlayer != null)
            {
                Debug.Log($"PointerEnter triggered for {videoPlayer.title}");
                videoPlayer.MouseHoverChangeScene();
            }
        });
        eventTrigger.triggers.Add(pointerEnter);

        // Add PointerExit trigger
        EventTrigger.Entry pointerExit = new EventTrigger.Entry();
        pointerExit.eventID = EventTriggerType.PointerExit;
        pointerExit.callback = new EventTrigger.TriggerEvent();
        pointerExit.callback.AddListener((data) => {
            if (videoPlayer != null)
            {
                Debug.Log($"PointerExit triggered for {videoPlayer.title}");
                videoPlayer.MouseExit();
            }
        });
        eventTrigger.triggers.Add(pointerExit);

        // Add PointerClick trigger with comprehensive PlayerPrefs saving
        EventTrigger.Entry pointerClick = new EventTrigger.Entry();
        pointerClick.eventID = EventTriggerType.PointerClick;
        pointerClick.callback = new EventTrigger.TriggerEvent();
        pointerClick.callback.AddListener((data) => {
            if (videoPlayer == null)
            {
                Debug.LogError("VideoPlayer is null on click!");
                return;
            }

            Debug.Log($"PointerClick triggered for {videoPlayer.title} with URL: {videoPlayer.VideoUrlLink}");

            // Validate URL before triggering
            if (string.IsNullOrEmpty(videoPlayer.VideoUrlLink))
            {
                Debug.LogError($"VideoUrlLink is empty for {videoPlayer.title}! Cannot play video.");
                return;
            }

            // FIXED: Save all video data to PlayerPrefs before triggering
            PlayerPrefs.SetString("VideoUrl", videoPlayer.VideoUrlLink);
            PlayerPrefs.SetString("lastknownzone", videoPlayer.LastKnownZone ?? "");
            PlayerPrefs.SetString("nextscene", videoPlayer.nextscene ?? "360VideoApp");
            PlayerPrefs.SetString("returntoscene", videoPlayer.returntoscene ?? "mainVR");

            // Optional: Save title and description for the video player scene
            if (!string.IsNullOrEmpty(videoPlayer.title))
            {
                PlayerPrefs.SetString("videoTitle", videoPlayer.title);
            }
            if (!string.IsNullOrEmpty(videoPlayer.description))
            {
                PlayerPrefs.SetString("videoDescription", videoPlayer.description);
            }

            PlayerPrefs.Save();

            Debug.Log($"✅ Saved to PlayerPrefs - VideoUrl: {videoPlayer.VideoUrlLink}, Zone: {videoPlayer.LastKnownZone}");

            videoPlayer.SetVideoUrl();
        });
        eventTrigger.triggers.Add(pointerClick);

        // Validate final setup
        Debug.Log($"✅ Set up {eventTrigger.triggers.Count} EventTriggers for {videoPlayer.title}:");
        Debug.Log($"   - VideoUrlLink: {videoPlayer.VideoUrlLink}");
        Debug.Log($"   - LastKnownZone: {videoPlayer.LastKnownZone}");
        Debug.Log($"   - Triggers: {string.Join(", ", eventTrigger.triggers.Select(t => t.eventID.ToString()))}");

#if UNITY_EDITOR
    // Mark as dirty to ensure changes are saved
    UnityEditor.EditorUtility.SetDirty(eventTrigger);
    UnityEditor.EditorUtility.SetDirty(prefab);
#endif
    }

    // NEW: Validation method to ensure everything is set up correctly
    private void ValidateVideoPrefabConfiguration(GameObject prefab, EnhancedVideoPlayer videoPlayer)
    {
        List<string> issues = new List<string>();

        // Check essential components
        if (videoPlayer == null)
        {
            issues.Add("Missing EnhancedVideoPlayer");
        }
        else
        {
            if (string.IsNullOrEmpty(videoPlayer.VideoUrlLink))
                issues.Add("VideoUrlLink not set");
            if (string.IsNullOrEmpty(videoPlayer.LastKnownZone))
                issues.Add("LastKnownZone not set");
            if (string.IsNullOrEmpty(videoPlayer.title))
                issues.Add("Title not set");
        }

        // Check EventTrigger
        EventTrigger eventTrigger = prefab.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            issues.Add("Missing EventTrigger component");
        }
        else if (eventTrigger.triggers == null || eventTrigger.triggers.Count == 0)
        {
            issues.Add("EventTrigger has no triggers");
        }
        else if (eventTrigger.triggers.Count < 3)
        {
            issues.Add($"EventTrigger only has {eventTrigger.triggers.Count} triggers (expected 3)");
        }

        // Check BoxCollider
        BoxCollider boxCollider = prefab.GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            issues.Add("Missing BoxCollider");
        }
        else if (!boxCollider.isTrigger)
        {
            issues.Add("BoxCollider is not set as trigger");
        }

        if (issues.Count > 0)
        {
            Debug.LogWarning($"⚠️ Validation issues for {prefab.name}: {string.Join(", ", issues)}");
        }
        else
        {
            Debug.Log($"✅ Validation passed for {prefab.name}");
        }
    }

    // NEW: Method to force EventTrigger setup on all existing videos
    [ContextMenu("Force Setup EventTriggers on All Videos")]
    public void ForceSetupEventTriggersOnAllVideos()
    {
        EnhancedVideoPlayer[] allVideos = FindObjectsOfType<EnhancedVideoPlayer>();
        int fixedCount = 0;
        int errorCount = 0;

        Debug.Log($"🔧 Force setting up EventTriggers on {allVideos.Length} videos...");

        foreach (var video in allVideos)
        {
            if (video == null || video.gameObject == null)
            {
                errorCount++;
                continue;
            }

            try
            {
                SetupEventTriggersForVideo(video.gameObject, video);
                fixedCount++;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to setup EventTriggers on {video.gameObject.name}: {ex.Message}");
                errorCount++;
            }
        }

        Debug.Log($"✅ EventTrigger setup complete! Fixed: {fixedCount}, Errors: {errorCount}");
    }

    // NEW: Context menu to test EventTriggers (FIXED with proper Editor wrapping)
    [ContextMenu("Test EventTriggers on Selected Video")]
    public void TestEventTriggersOnSelectedVideo()
    {
#if UNITY_EDITOR
    if (UnityEditor.Selection.activeGameObject == null)
    {
        Debug.LogWarning("No GameObject selected. Select a video prefab to test its EventTriggers.");
        return;
    }

    EnhancedVideoPlayer videoPlayer = UnityEditor.Selection.activeGameObject.GetComponent<EnhancedVideoPlayer>();
    if (videoPlayer == null)
    {
        Debug.LogWarning("Selected GameObject doesn't have an EnhancedVideoPlayer component.");
        return;
    }

    EventTrigger eventTrigger = UnityEditor.Selection.activeGameObject.GetComponent<EventTrigger>();
    if (eventTrigger == null)
    {
        Debug.LogWarning("Selected GameObject doesn't have an EventTrigger component.");
        return;
    }

    Debug.Log($"🧪 Testing EventTriggers on {videoPlayer.title}:");
    Debug.Log($"   VideoUrlLink: {videoPlayer.VideoUrlLink}");
    Debug.Log($"   LastKnownZone: {videoPlayer.LastKnownZone}");
    Debug.Log($"   EventTrigger triggers count: {eventTrigger.triggers?.Count ?? 0}");

    if (eventTrigger.triggers != null)
    {
        foreach (var trigger in eventTrigger.triggers)
        {
            Debug.Log($"   - {trigger.eventID}: {trigger.callback?.GetPersistentEventCount() ?? 0} persistent callbacks");
        }
    }

    // Test the callbacks manually
    Debug.Log("🔥 Manually triggering PointerEnter...");
    videoPlayer.MouseHoverChangeScene();

    Debug.Log("🔥 Manually triggering PointerExit...");
    videoPlayer.MouseExit();
#else
        Debug.LogWarning("TestEventTriggersOnSelectedVideo is only available in the Unity Editor.");
#endif
    }

    // FIXED: Enhanced UpdateTextComponents method in FilmZoneManager.cs
    private void UpdateTextComponents(GameObject instance, FilmDataEntry entry)
    {
        if (instance == null || entry == null)
            return;

        bool titleUpdated = false;
        bool descriptionUpdated = false;

        // Find and update TextMeshPro components
        TMPro.TextMeshPro[] tmpComponents = instance.GetComponentsInChildren<TMPro.TextMeshPro>(true); // Include inactive objects

        if (enableDebugLogging)
        {
            Debug.Log($"Found {tmpComponents.Length} TextMeshPro components in {instance.name}");
            foreach (var tmp in tmpComponents)
            {
                Debug.Log($"  - TextMeshPro: '{tmp.name}' with text: '{tmp.text}'");
            }
        }

        foreach (var tmp in tmpComponents)
        {
            string componentName = tmp.name.ToLower();

            // Check for title components
            if (!titleUpdated && (componentName.Contains("title") || tmp.text.ToLower().Contains("title") || tmp.text.ToLower().Contains("sample")))
            {
                string oldText = tmp.text;
                tmp.text = entry.Title ?? "Unknown Title";
                titleUpdated = true;

                if (enableDebugLogging)
                {
                    Debug.Log($"✅ Updated TITLE TextMeshPro '{tmp.name}': '{oldText}' → '{tmp.text}'");
                }
            }
            // Check for description components
            else if (!descriptionUpdated && (componentName.Contains("description") || componentName.Contains("desc")))
            {
                string oldText = tmp.text;
                tmp.text = entry.Description ?? "No description available";
                descriptionUpdated = true;

                if (enableDebugLogging)
                {
                    Debug.Log($"✅ Updated DESCRIPTION TextMeshPro '{tmp.name}': '{oldText}' → '{tmp.text}'");
                }
            }
        }

        // Also try regular Text components as fallback
        UnityEngine.UI.Text[] textComponents = instance.GetComponentsInChildren<UnityEngine.UI.Text>(true);

        if (enableDebugLogging && textComponents.Length > 0)
        {
            Debug.Log($"Found {textComponents.Length} UI Text components in {instance.name}");
            foreach (var text in textComponents)
            {
                Debug.Log($"  - UI Text: '{text.name}' with text: '{text.text}'");
            }
        }

        foreach (var text in textComponents)
        {
            string componentName = text.name.ToLower();

            // Check for title components
            if (!titleUpdated && (componentName.Contains("title") || text.text.ToLower().Contains("title") || text.text.ToLower().Contains("sample")))
            {
                string oldText = text.text;
                text.text = entry.Title ?? "Unknown Title";
                titleUpdated = true;

                if (enableDebugLogging)
                {
                    Debug.Log($"✅ Updated TITLE UI Text '{text.name}': '{oldText}' → '{text.text}'");
                }
            }
            // Check for description components
            else if (!descriptionUpdated && (componentName.Contains("description") || componentName.Contains("desc")))
            {
                string oldText = text.text;
                text.text = entry.Description ?? "No description available";
                descriptionUpdated = true;

                if (enableDebugLogging)
                {
                    Debug.Log($"✅ Updated DESCRIPTION UI Text '{text.name}': '{oldText}' → '{text.text}'");
                }
            }
        }

        // Enhanced fallback: try to find any text component that might be for title/description
        if (!titleUpdated || !descriptionUpdated)
        {
            // Try by GameObject names in hierarchy
            Transform[] allTransforms = instance.GetComponentsInChildren<Transform>(true);

            foreach (Transform t in allTransforms)
            {
                string objName = t.name.ToLower();

                if (!titleUpdated && objName.Contains("title"))
                {
                    // Check if this GameObject has any text component
                    var tmpComp = t.GetComponent<TMPro.TextMeshPro>();
                    var uiTextComp = t.GetComponent<UnityEngine.UI.Text>();

                    if (tmpComp != null)
                    {
                        string oldText = tmpComp.text;
                        tmpComp.text = entry.Title ?? "Unknown Title";
                        titleUpdated = true;

                        if (enableDebugLogging)
                        {
                            Debug.Log($"✅ Updated TITLE by GameObject name '{t.name}': '{oldText}' → '{tmpComp.text}'");
                        }
                    }
                    else if (uiTextComp != null)
                    {
                        string oldText = uiTextComp.text;
                        uiTextComp.text = entry.Title ?? "Unknown Title";
                        titleUpdated = true;

                        if (enableDebugLogging)
                        {
                            Debug.Log($"✅ Updated TITLE by GameObject name '{t.name}': '{oldText}' → '{uiTextComp.text}'");
                        }
                    }
                }

                if (!descriptionUpdated && (objName.Contains("description") || objName.Contains("desc")))
                {
                    // Check if this GameObject has any text component
                    var tmpComp = t.GetComponent<TMPro.TextMeshPro>();
                    var uiTextComp = t.GetComponent<UnityEngine.UI.Text>();

                    if (tmpComp != null)
                    {
                        string oldText = tmpComp.text;
                        tmpComp.text = entry.Description ?? "No description available";
                        descriptionUpdated = true;

                        if (enableDebugLogging)
                        {
                            Debug.Log($"✅ Updated DESCRIPTION by GameObject name '{t.name}': '{oldText}' → '{tmpComp.text}'");
                        }
                    }
                    else if (uiTextComp != null)
                    {
                        string oldText = uiTextComp.text;
                        uiTextComp.text = entry.Description ?? "No description available";
                        descriptionUpdated = true;

                        if (enableDebugLogging)
                        {
                            Debug.Log($"✅ Updated DESCRIPTION by GameObject name '{t.name}': '{oldText}' → '{uiTextComp.text}'");
                        }
                    }
                }
            }
        }

        // Final validation and warning
        if (!titleUpdated)
        {
            Debug.LogWarning($"⚠️ Could not find Title text component in {instance.name} for '{entry.Title}'");

            // List all text components found for debugging
            if (enableDebugLogging)
            {
                Debug.LogWarning($"Available text components in {instance.name}:");
                foreach (var tmp in tmpComponents)
                {
                    Debug.LogWarning($"  - TextMeshPro: '{tmp.name}'");
                }
                foreach (var text in textComponents)
                {
                    Debug.LogWarning($"  - UI Text: '{text.name}'");
                }
            }
        }

        if (!descriptionUpdated)
        {
            Debug.LogWarning($"⚠️ Could not find Description text component in {instance.name} for '{entry.Title}'");
        }

        // Force refresh the text components
        if (titleUpdated || descriptionUpdated)
        {
            foreach (var tmp in tmpComponents)
            {
                if (tmp != null)
                {
                    tmp.ForceMeshUpdate();
                }
            }
        }
    }

    private void PlacePrefabInZone(GameObject prefab, string zoneName)
    {
        if (prefab == null || string.IsNullOrEmpty(zoneName))
            return;

        FilmZone zone = GetZoneByName(zoneName);
        if (zone != null)
        {
            Vector3 position = placeOnTerrain ? zone.GetRandomPointInZone() : zone.GetRandomPointInZoneAtZoneHeight();
            prefab.transform.position = position;

            if (enableDebugLogging)
            {
                Debug.Log($"Placed {prefab.name} at position {position} in zone {zoneName}");
            }
        }
        else
        {
            Debug.LogWarning($"⚠️ Zone '{zoneName}' not found, placing at origin");
            prefab.transform.position = Vector3.zero;
        }
    }

    // FIXED: Enhanced cleanup method that respects hierarchy
    private void ClearAllVideoPrefabs()
    {
        // Find all video components in the scene
        EnhancedVideoPlayer[] existingVideos = FindObjectsOfType<EnhancedVideoPlayer>();

        int clearedCount = 0;

        // Clear EnhancedVideoPlayer objects
        foreach (var video in existingVideos)
        {
            if (video != null && video.gameObject != null)
            {
                if (enableDebugLogging)
                {
                    Debug.Log($"Destroying video: {GetHierarchyPath(video.transform)}");
                }
                DestroyImmediate(video.gameObject);
                clearedCount++;
            }
        }

        // Also clean up empty zone folders
        CleanupEmptyZoneFolders();

        activePrefabs.Clear();
        Debug.Log($"🧹 Cleared {clearedCount} existing video prefabs");
    }

    // Method to clean up empty zone folders after clearing videos
    private void CleanupEmptyZoneFolders()
    {
        GameObject targetsObject = GameObject.Find("Targets");
        if (targetsObject == null) return;

        // Check each zone folder
        for (int i = targetsObject.transform.childCount - 1; i >= 0; i--)
        {
            Transform zoneTransform = targetsObject.transform.GetChild(i);

            // Check if Films folder exists and is empty
            Transform filmsTransform = zoneTransform.Find("Films");
            if (filmsTransform != null && filmsTransform.childCount == 0)
            {
                Debug.Log($"Films folder under {zoneTransform.name} is empty, keeping structure for future use");
                // Don't destroy - keep the structure for future video placement
            }
        }
    }

    // ADD this method to FilmZoneManager.cs for debugging prefab structure

    [ContextMenu("Inspect Prefab Structure")]
    public void InspectPrefabStructure()
    {
        if (defaultPrefab == null)
        {
            Debug.LogError("No default prefab set!");
            return;
        }

        Debug.Log($"=== INSPECTING PREFAB STRUCTURE: {defaultPrefab.name} ===");

        // Create a temporary instance to inspect
        GameObject tempInstance = Instantiate(defaultPrefab);
        tempInstance.name = "TEMP_INSPECT_" + defaultPrefab.name;

        try
        {
            // Find all text components
            TMPro.TextMeshPro[] tmpComponents = tempInstance.GetComponentsInChildren<TMPro.TextMeshPro>(true);
            UnityEngine.UI.Text[] uiTextComponents = tempInstance.GetComponentsInChildren<UnityEngine.UI.Text>(true);
            Transform[] allTransforms = tempInstance.GetComponentsInChildren<Transform>(true);

            Debug.Log($"📊 COMPONENT SUMMARY:");
            Debug.Log($"   TextMeshPro components: {tmpComponents.Length}");
            Debug.Log($"   UI Text components: {uiTextComponents.Length}");
            Debug.Log($"   Total GameObjects: {allTransforms.Length}");

            Debug.Log($"\n📝 TEXTMESHPRO COMPONENTS:");
            for (int i = 0; i < tmpComponents.Length; i++)
            {
                var tmp = tmpComponents[i];
                string hierarchy = GetHierarchyPath(tmp.transform, tempInstance.transform);
                Debug.Log($"   [{i}] Name: '{tmp.name}' | Path: '{hierarchy}' | Text: '{tmp.text}' | Active: {tmp.gameObject.activeInHierarchy}");
            }

            Debug.Log($"\n📝 UI TEXT COMPONENTS:");
            for (int i = 0; i < uiTextComponents.Length; i++)
            {
                var text = uiTextComponents[i];
                string hierarchy = GetHierarchyPath(text.transform, tempInstance.transform);
                Debug.Log($"   [{i}] Name: '{text.name}' | Path: '{hierarchy}' | Text: '{text.text}' | Active: {text.gameObject.activeInHierarchy}");
            }

            Debug.Log($"\n🏗️ FULL HIERARCHY:");
            PrintHierarchy(tempInstance.transform, 0);

            Debug.Log($"\n🎯 POTENTIAL TITLE/DESCRIPTION MATCHES:");
            foreach (Transform t in allTransforms)
            {
                string name = t.name.ToLower();
                if (name.Contains("title") || name.Contains("description") || name.Contains("desc") || name.Contains("text"))
                {
                    var tmp = t.GetComponent<TMPro.TextMeshPro>();
                    var uiText = t.GetComponent<UnityEngine.UI.Text>();

                    string componentType = "No Text Component";
                    string currentText = "";

                    if (tmp != null)
                    {
                        componentType = "TextMeshPro";
                        currentText = tmp.text;
                    }
                    else if (uiText != null)
                    {
                        componentType = "UI Text";
                        currentText = uiText.text;
                    }

                    string hierarchy = GetHierarchyPath(t, tempInstance.transform);
                    Debug.Log($"   • '{t.name}' ({componentType}) | Path: '{hierarchy}' | Text: '{currentText}'");
                }
            }

            Debug.Log($"=== END PREFAB INSPECTION ===\n");
        }
        finally
        {
            // Clean up
            DestroyImmediate(tempInstance);
        }
    }

    // Helper method to get hierarchy path
    private string GetHierarchyPath(Transform transform, Transform root)
    {
        if (transform == root) return transform.name;

        string path = transform.name;
        Transform parent = transform.parent;

        while (parent != null && parent != root)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }

    // Helper method to print hierarchy
    private void PrintHierarchy(Transform parent, int depth)
    {
        string indent = new string(' ', depth * 2);

        // Check what components this GameObject has
        var tmp = parent.GetComponent<TMPro.TextMeshPro>();
        var uiText = parent.GetComponent<UnityEngine.UI.Text>();
        var enhancedVideo = parent.GetComponent<EnhancedVideoPlayer>();

        string components = "";
        if (tmp != null) components += "[TMP]";
        if (uiText != null) components += "[UIText]";
        if (enhancedVideo != null) components += "[EnhancedVideo]";

        Debug.Log($"{indent}• {parent.name} {components} (Active: {parent.gameObject.activeInHierarchy})");

        for (int i = 0; i < parent.childCount; i++)
        {
            PrintHierarchy(parent.GetChild(i), depth + 1);
        }
    }

    // ===== UTILITY METHODS =====

    private void LogZoneDebugInfo()
    {
        Debug.Log($"=== FilmZoneManager Debug Info ===");
        Debug.Log($"Total zones: {zones.Count}");
        Debug.Log($"Show zone gizmos: {showZoneGizmos}");
        Debug.Log($"Hierarchy organization: {enableHierarchyOrganization}");

        for (int i = 0; i < zones.Count; i++)
        {
            if (zones[i] != null)
            {
                Debug.Log($"Zone {i}: '{zones[i].zoneName}' ({zones[i].polygonPoints.Count} points)");
            }
        }
        Debug.Log($"=== End Debug Info ===");
    }

    public FilmZone GetZoneByName(string zoneName)
    {
        if (zones == null || string.IsNullOrEmpty(zoneName)) return null;
        return zones.Find(z => z.zoneName.Equals(zoneName, StringComparison.OrdinalIgnoreCase));
    }

    public GameObject GetPrefabForEntry(FilmDataEntry entry, string zoneName = null)
    {
        if (prefabDict == null) BuildPrefabDictionary();

        // Priority 1: Film-specific prefab
        if (entry != null && !string.IsNullOrEmpty(entry.Prefab))
        {
            if (prefabDict.ContainsKey(entry.Prefab))
            {
                return prefabDict[entry.Prefab];
            }

            // Try case-insensitive match
            foreach (var kvp in prefabDict)
            {
                if (string.Equals(kvp.Key, entry.Prefab, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value;
                }
            }
        }

        // Priority 2: Zone-specific prefab
        if (!string.IsNullOrEmpty(zoneName) && zonePrefabMappings != null)
        {
            var zonePrefabMapping = zonePrefabMappings.FirstOrDefault(z =>
                string.Equals(z.zoneName, zoneName, StringComparison.OrdinalIgnoreCase));

            if (zonePrefabMapping?.prefab != null)
            {
                return zonePrefabMapping.prefab;
            }
        }

        // Priority 3: Default prefab
        return defaultPrefab;
    }

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

        RaycastHit hit;
        if (Physics.Raycast(worldPosition + Vector3.up * 100f, Vector3.down, out hit, 200f, terrainLayer))
        {
            return hit.point;
        }

        return worldPosition;
    }

    // ===== ADDITIONAL CONTEXT MENU METHODS =====

    [ContextMenu("Show Persistent Layout Stats")]
    public void ShowPersistentLayoutStats()
    {
        if (persistentLayout == null)
        {
            Debug.Log("📊 No persistent layout data loaded");
            return;
        }

        Debug.Log($"📊 Persistent Layout Stats:");
        Debug.Log($"   Total saved positions: {persistentLayout.videoTransforms.Count}");
        Debug.Log($"   Last saved: {persistentLayout.lastSaved}");
        Debug.Log($"   Version: {persistentLayout.version}");

        var zoneGroups = persistentLayout.videoTransforms.GroupBy(vt => vt.zoneName);
        foreach (var group in zoneGroups)
        {
            Debug.Log($"   Zone '{group.Key}': {group.Count()} videos");
        }
    }

    [ContextMenu("Cleanup Orphaned Positions")]
    public void CleanupOrphanedPositions()
    {
        if (persistentLayout == null) return;

        // Get all current video URLs in scene
        EnhancedVideoPlayer[] allVideos = FindObjectsOfType<EnhancedVideoPlayer>();
        List<string> validUrls = allVideos
            .Where(v => !string.IsNullOrEmpty(v.VideoUrlLink))
            .Select(v => v.VideoUrlLink)
            .Distinct()
            .ToList();

        int cleaned = persistentLayout.CleanupOrphanedEntries(validUrls);

        if (cleaned > 0)
        {
            SavePersistentLayout();
            Debug.Log($"🧹 Cleaned up {cleaned} orphaned position entries");
        }
        else
        {
            Debug.Log("✅ No orphaned positions found");
        }
    }

    // Add these methods to your FilmZoneManager class

    [ContextMenu("Force Clear All Zone Data")]
    public void ForceClearAllZoneData()
    {
        Debug.Log("🔥 FORCE CLEARING ALL ZONE DATA 🔥");

        if (zones != null)
        {
            zones.Clear();
        }
        else
        {
            zones = new List<FilmZone>();
        }

#if UNITY_EDITOR
    UnityEditor.EditorUtility.SetDirty(this);
    UnityEditor.SceneView.RepaintAll();
#endif

        Debug.Log("✅ All zone data has been forcibly cleared!");
    }

    public void NuclearDeleteZone(string zoneName)
    {
        if (zones == null || string.IsNullOrEmpty(zoneName))
        {
            Debug.LogWarning("Cannot delete zone: zones list is null or zone name is empty");
            return;
        }

        Debug.Log($"🔥 NUCLEAR DELETE for zone: '{zoneName}' 🔥");

        for (int i = zones.Count - 1; i >= 0; i--)
        {
            if (zones[i] != null && zones[i].zoneName.Equals(zoneName, StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"💥 Completely destroying zone: '{zones[i].zoneName}' at index {i}");
                zones.RemoveAt(i);

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.SceneView.RepaintAll();
#endif

                Debug.Log($"✅ Zone '{zoneName}' has been NUCLEAR DELETED!");
                return;
            }
        }

        Debug.LogWarning($"❌ Zone '{zoneName}' not found for nuclear deletion!");
    }

    public void NuclearDeleteZone(int zoneIndex)
    {
        if (zones == null || zoneIndex < 0 || zoneIndex >= zones.Count)
        {
            Debug.LogWarning("Cannot delete zone: invalid zone index");
            return;
        }

        string zoneName = zones[zoneIndex].zoneName;
        Debug.Log($"🔥 NUCLEAR DELETE for zone at index {zoneIndex}: '{zoneName}' 🔥");

        zones.RemoveAt(zoneIndex);

#if UNITY_EDITOR
    UnityEditor.EditorUtility.SetDirty(this);
    UnityEditor.SceneView.RepaintAll();
#endif

        Debug.Log($"✅ Zone '{zoneName}' has been NUCLEAR DELETED!");
    }

    [ContextMenu("Clear All Polygon Points")]
    public void ClearAllPolygonPoints()
    {
        Debug.Log("🧹 Clearing all polygon points from all zones...");

        int clearedCount = 0;
        if (zones != null)
        {
            foreach (var zone in zones)
            {
                if (zone.polygonPoints.Count > 0)
                {
                    zone.ClearAllPoints();
                    clearedCount++;
                }
            }
        }

#if UNITY_EDITOR
    UnityEditor.EditorUtility.SetDirty(this);
    UnityEditor.SceneView.RepaintAll();
#endif

        Debug.Log($"✅ Cleared polygon points from {clearedCount} zones!");
    }

    public void ClearZonePoints(string zoneName)
    {
        var zone = GetZoneByName(zoneName);
        if (zone != null)
        {
            zone.ClearAllPoints();
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneView.RepaintAll();
#endif
            Debug.Log($"✅ Cleared points from zone: {zoneName}");
        }
        else
        {
            Debug.LogWarning($"Zone '{zoneName}' not found!");
        }
    }

    public void ClearZonePoints(int zoneIndex)
    {
        if (zones == null || zoneIndex < 0 || zoneIndex >= zones.Count)
        {
            Debug.LogWarning("Cannot clear zone points: invalid zone index");
            return;
        }

        string zoneName = zones[zoneIndex].zoneName;
        zones[zoneIndex].ClearAllPoints();

#if UNITY_EDITOR
    UnityEditor.EditorUtility.SetDirty(this);
    UnityEditor.SceneView.RepaintAll();
#endif

        Debug.Log($"✅ Cleared points from zone: {zoneName}");
    }

    [ContextMenu("Toggle All Zone Gizmos")]
    public void ToggleAllZoneGizmos()
    {
        showZoneGizmos = !showZoneGizmos;

        if (zones != null)
        {
            foreach (var zone in zones)
            {
                zone.showGizmos = showZoneGizmos;
            }
        }

#if UNITY_EDITOR
    UnityEditor.EditorUtility.SetDirty(this);
    UnityEditor.SceneView.RepaintAll();
#endif

        Debug.Log($"🎯 All zone gizmos: {(showZoneGizmos ? "ENABLED" : "DISABLED")}");
    }

    [ContextMenu("Generate Debug Report")]
    public void GenerateDebugReport()
    {
        LogZoneDebugInfo();

        System.Text.StringBuilder report = new System.Text.StringBuilder();
        report.AppendLine("=== COMPREHENSIVE ZONE DEBUG REPORT ===");
        report.AppendLine($"GameObject: {gameObject.name}");
        report.AppendLine($"Film Data Path: {filmDataPath}");
        report.AppendLine($"Layout Data Path: {layoutDataPath}");
        report.AppendLine($"Auto Save Positions: {autoSaveOnPositionChange}");
        report.AppendLine($"Auto Cleanup Orphaned: {autoCleanupOrphanedPositions}");
        report.AppendLine($"Hierarchy Organization: {enableHierarchyOrganization}");
        report.AppendLine($"Auto Organize On Create: {autoOrganizeOnCreate}");
        report.AppendLine();

        if (persistentLayout != null)
        {
            report.AppendLine($"PERSISTENT LAYOUT DATA:");
            report.AppendLine($"  - Total saved positions: {persistentLayout.videoTransforms.Count}");
            report.AppendLine($"  - Last saved: {persistentLayout.lastSaved}");
            report.AppendLine($"  - Version: {persistentLayout.version}");

            var zoneGroups = persistentLayout.videoTransforms.GroupBy(vt => vt.zoneName);
            foreach (var group in zoneGroups)
            {
                report.AppendLine($"  - Zone '{group.Key}': {group.Count()} saved positions");
            }
        }
        else
        {
            report.AppendLine("⚠️ No persistent layout data loaded!");
        }

        report.AppendLine();
        if (zones == null)
        {
            report.AppendLine("⚠️ zones list is NULL!");
        }
        else
        {
            report.AppendLine($"ZONE DEFINITIONS: {zones.Count} total");
            for (int i = 0; i < zones.Count; i++)
            {
                var zone = zones[i];
                if (zone == null)
                {
                    report.AppendLine($"Zone {i}: NULL");
                }
                else
                {
                    report.AppendLine($"Zone {i}: '{zone.zoneName}' ({zone.polygonPoints?.Count ?? 0} points)");
                }
            }
        }

        report.AppendLine("=== END REPORT ===");
        Debug.Log(report.ToString());
    }

    [ContextMenu("List All Defined Zones")]
    public void ListAllDefinedZones()
    {
        Debug.Log("=== DEFINED ZONES ===");
        for (int i = 0; i < zones.Count; i++)
        {
            Debug.Log($"Zone {i}: '{zones[i].zoneName}'");
        }
    }

    [ContextMenu("Debug File Paths")]
    public void DebugFilePaths()
    {
        string fullFilmPath = Path.Combine(Application.streamingAssetsPath, filmDataPath);
        string fullLayoutPath = Path.Combine(Application.streamingAssetsPath, layoutDataPath);

        Debug.Log($"=== FILE PATH DEBUG ===");
        Debug.Log($"StreamingAssets path: {Application.streamingAssetsPath}");
        Debug.Log($"Film data path: {filmDataPath}");
        Debug.Log($"Full film path: {fullFilmPath}");
        Debug.Log($"Film file exists: {File.Exists(fullFilmPath)}");
        Debug.Log($"Layout data path: {layoutDataPath}");
        Debug.Log($"Full layout path: {fullLayoutPath}");
        Debug.Log($"Layout file exists: {File.Exists(fullLayoutPath)}");

        if (File.Exists(fullFilmPath))
        {
            string fileContent = File.ReadAllText(fullFilmPath);
            Debug.Log($"Film file size: {fileContent.Length} characters");
            Debug.Log($"Film file starts with: {fileContent.Substring(0, Math.Min(100, fileContent.Length))}...");
        }
    }

    // ===== GIZMO DRAWING =====

    private void OnDrawGizmos()
    {
        if (!showZoneGizmos) return;
        if (zones == null || zones.Count == 0) return;

        foreach (var zone in zones)
        {
            if (zone == null || !zone.showGizmos) continue;
            if (zone.polygonPoints == null || zone.polygonPoints.Count < 3) continue;

            Gizmos.color = zone.gizmoColor;

            // Draw polygon outline
            for (int i = 0; i < zone.polygonPoints.Count; i++)
            {
                Vector3 current = zone.polygonPoints[i];
                Vector3 next = zone.polygonPoints[(i + 1) % zone.polygonPoints.Count];
                Gizmos.DrawLine(current, next);
            }

            // Draw zone center
            if (zone.polygonPoints.Count > 0)
            {
                Vector3 center = Vector3.zero;
                foreach (var point in zone.polygonPoints)
                {
                    center += point;
                }
                center /= zone.polygonPoints.Count;

                Gizmos.color = zone.gizmoColor * 0.8f;
                Gizmos.DrawSphere(center, 0.5f);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugInfo) return;

        OnDrawGizmos();

        if (zones != null)
        {
            Gizmos.color = Color.yellow;
            foreach (var zone in zones)
            {
                if (zone?.polygonPoints != null && zone.polygonPoints.Count >= 3)
                {
                    Vector3 min = zone.polygonPoints[0];
                    Vector3 max = zone.polygonPoints[0];

                    foreach (var point in zone.polygonPoints)
                    {
                        if (point.x < min.x) min.x = point.x;
                        if (point.y < min.y) min.y = point.y;
                        if (point.z < min.z) min.z = point.z;
                        if (point.x > max.x) max.x = point.x;
                        if (point.y > max.y) max.y = point.y;
                        if (point.z > max.z) max.z = point.z;
                    }

                    Vector3 size = max - min;
                    Vector3 center = (min + max) * 0.5f;
                    Gizmos.DrawWireCube(center, size);
                }
            }
        }
    }
}

// Legacy compatibility structures
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

// Metadata component for tracking video objects
[System.Serializable]
public class VideoObjectMetadata : MonoBehaviour
{
    public FilmDataEntry entry;
    public string zoneName;
    public DateTime lastUpdated;

    private void Awake()
    {
        lastUpdated = DateTime.Now;
    }
}
