using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using UnityEditor;
using System;

/// <summary>
/// Advanced manager for video placement with zone management, manual adjustments, and position caching
/// Updated with support for placing videos in hierarchical folder structure
/// </summary>
public class AdvancedVideoPlacementManager : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private VideoDatabaseManager databaseManager;
    [SerializeField] private PolygonZoneManager zoneManager;
    [SerializeField] private bool placeOnStart = true;
    [SerializeField] private float startDelay = 1.0f;

    [Header("Prefab Settings")]
    [SerializeField] private GameObject defaultPrefab;
    [SerializeField] private List<PrefabTypeMapping> prefabMappings = new List<PrefabTypeMapping>();

    [Header("Caching Settings")]
    [SerializeField] private string cacheFolderPath = "Assets/Resources";
    [SerializeField] private string cacheFileName = "VideoPlacementCache.json";
    [SerializeField] private bool saveCacheOnExit = true;
    [SerializeField] private bool loadCacheOnStart = true;

    [Header("Hierarchy Settings")]
    [SerializeField] private bool useHierarchicalPlacement = true;
    [SerializeField] private string filmsFolderName = "Films";
    [SerializeField] private bool createMissingFolders = true;

    [Header("Debug Options")]
    [SerializeField] private bool debugMode = true;

    // Mappings for different prefab types
    [System.Serializable]
    public class PrefabTypeMapping
    {
        public string typeName;
        public GameObject prefab;
    }

    // Data structure to store and cache placement data
    [System.Serializable]
    public class VideoPlacementData
    {
        public string videoUrl;
        public string zoneName;
        public Vector3 position;
        public Quaternion rotation;
        public string prefabType;
    }

    [System.Serializable]
    public class PlacementCache
    {
        public List<VideoPlacementData> placements = new List<VideoPlacementData>();
    }

    // Keep track of all placed videos - key is a composite of videoUrl and zoneName
    private Dictionary<string, GameObject> placedVideos = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> prefabTypeMap = new Dictionary<string, GameObject>();
    private PlacementCache placementCache = new PlacementCache();

    private bool hasManuallyAdjusted = false;

    private void Awake()
    {
        FindDependencies();
        InitializePrefabMappings();
    }

    private void Start()
    {
        if (loadCacheOnStart && DoesCacheExist())
        {
            LoadPlacementCache();
            return; // Skip auto-placement if we loaded a cache
        }

        if (placeOnStart)
        {
            Invoke("PlaceAllVideos", startDelay);
        }
    }

    private void OnDestroy()
    {
        if (saveCacheOnExit && hasManuallyAdjusted)
        {
            SavePlacementCache();
        }
    }

    #region Initialization

    private void FindDependencies()
    {
        // Find database manager if not assigned
        if (databaseManager == null)
        {
            databaseManager = FindObjectOfType<VideoDatabaseManager>();
            if (databaseManager == null)
            {
                Debug.LogError("VideoDatabaseManager not found - videos cannot be placed");
            }
        }

        // Find zone manager if not assigned
        if (zoneManager == null)
        {
            zoneManager = FindObjectOfType<PolygonZoneManager>();
            if (zoneManager == null)
            {
                Debug.LogWarning("PolygonZoneManager not found - zone-based placement will not work");
            }
        }
    }

    private void InitializePrefabMappings()
    {
        prefabTypeMap.Clear();

        // Add default prefab
        if (defaultPrefab != null)
        {
            prefabTypeMap["Default"] = defaultPrefab;
        }

        // Add custom mappings
        foreach (var mapping in prefabMappings)
        {
            if (!string.IsNullOrEmpty(mapping.typeName) && mapping.prefab != null)
            {
                prefabTypeMap[mapping.typeName] = mapping.prefab;
                if (debugMode) Debug.Log($"Added prefab mapping: {mapping.typeName}");
            }
        }
    }

    #endregion

    #region Video Placement

    /// <summary>
    /// Place all videos from the database into their zones
    /// </summary>
    public void PlaceAllVideos()
    {
        if (databaseManager == null || !databaseManager.IsInitialized)
        {
            Debug.LogError("Cannot place videos: Database manager not ready");
            return;
        }

        if (debugMode) Debug.Log("Starting placement of all videos...");

        // Clear existing videos
        ClearAllVideos();

        // Get all zones with videos
        List<string> allZones = databaseManager.GetAllZones();

        foreach (string zoneName in allZones)
        {
            PlaceVideosInZone(zoneName);
        }

        if (debugMode) Debug.Log($"Completed placement of {placedVideos.Count} videos");
    }

    /// <summary>
    /// Place all videos for a specific zone
    /// </summary>
    public void PlaceVideosInZone(string zoneName)
    {
        if (string.IsNullOrEmpty(zoneName)) return;

        // Find the zone in the zone manager
        PolygonZone zone = null;
        if (zoneManager != null)
        {
            zone = zoneManager.FindZoneByName(zoneName);
        }

        // Get videos for this zone
        List<VideoEntry> zoneVideos = databaseManager.GetEntriesForZone(zoneName);

        if (zoneVideos == null || zoneVideos.Count == 0)
        {
            if (debugMode) Debug.Log($"No videos found for zone: {zoneName}");
            return;
        }

        if (debugMode) Debug.Log($"Placing {zoneVideos.Count} videos in zone: {zoneName}");

        // Limit the number of videos if max is configured
        int maxVideos = zone != null ? zone.MaxVideos : zoneVideos.Count;
        int videoCount = Mathf.Min(zoneVideos.Count, maxVideos);

        // List of positions already used in this zone to avoid overlaps
        List<Vector3> placedPositions = new List<Vector3>();

        // Place each video
        for (int i = 0; i < videoCount; i++)
        {
            VideoEntry video = zoneVideos[i];

            // Generate a unique key for this video+zone combination
            string videoZoneKey = video.PublicUrl + "_" + zoneName;

            // Check if we already have a cached position for this video in this zone
            if (TryGetCachedPlacement(video.PublicUrl, zoneName, out VideoPlacementData placementData))
            {
                PlaceVideoAtPosition(video, placementData.position, placementData.rotation, zoneName);
                placedPositions.Add(placementData.position);
                continue;
            }

            // Place using zone manager if available
            if (zone != null && zoneManager != null)
            {
                Vector3 position = zoneManager.FindValidPositionInZone(zone, placedPositions, video);

                if (position != Vector3.zero)
                {
                    // Use a default rotation facing upward
                    Quaternion rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0);

                    PlaceVideoAtPosition(video, position, rotation, zoneName);
                    placedPositions.Add(position);
                    continue;
                }
            }

            // Fallback to basic placement
            FallbackPlacement(video, zoneName, placedPositions);
        }
    }

    /// <summary>
    /// Place a video at a specified position and add it to the proper hierarchical structure
    /// </summary>
    public GameObject PlaceVideoAtPosition(VideoEntry video, Vector3 position, Quaternion rotation, string zoneName)
    {
        if (video == null) return null;

        // Get the appropriate prefab
        GameObject prefab = GetPrefabForVideo(video);
        if (prefab == null)
        {
            Debug.LogError($"No prefab available for video: {video.Title}");
            return null;
        }

        // Create a unique key for this video+zone combination
        string videoZoneKey = video.PublicUrl + "_" + zoneName;

        // Don't duplicate videos in the same zone
        if (placedVideos.ContainsKey(videoZoneKey))
        {
            GameObject existingVideo = placedVideos[videoZoneKey];
            existingVideo.transform.position = position;
            existingVideo.transform.rotation = rotation;
            return existingVideo;
        }

        // Create the instance
        GameObject instance = Instantiate(prefab, position, rotation);

        // Give it a clear name with zone info
        instance.name = $"Video_{(string.IsNullOrEmpty(video.Title) ? video.FileName : video.Title)}";
        // Place in proper hierarchy if enabled
        if (useHierarchicalPlacement)
        {
            Transform parent = FindOrCreateZoneFolder(zoneName);
            if (parent != null)
            {
                instance.transform.SetParent(parent);
            }
        }

        // Setup video player component
        EnhancedVideoPlayer videoPlayer = instance.GetComponent<EnhancedVideoPlayer>();
        if (videoPlayer == null)
        {
            videoPlayer = instance.AddComponent<EnhancedVideoPlayer>();
        }

        // Configure the video player
        videoPlayer.VideoUrlLink = video.PublicUrl;
        videoPlayer.title = string.IsNullOrEmpty(video.Title) ? video.FileName : video.Title;
        videoPlayer.description = video.Description ?? "";
        videoPlayer.prefabType = video.Prefab;
        videoPlayer.zoneName = zoneName; // Set the zone name so it can be tracked

        // Setup UI elements
        SetupVideoUI(instance, video);

        // Add VideoLinkInteraction component
        if (instance.GetComponent<VideoLinkInteraction>() == null)
        {
            instance.AddComponent<VideoLinkInteraction>();
        }

        // Add VideoAdjustmentHandler component for manual adjustments
        VideoAdjustmentHandler adjustmentHandler = instance.GetComponent<VideoAdjustmentHandler>();
        if (adjustmentHandler == null)
        {
            adjustmentHandler = instance.AddComponent<VideoAdjustmentHandler>();
        }

        // Add to tracking dictionary
        placedVideos[videoZoneKey] = instance;

        // Add to cache for later saving
        AddToPlacementCache(video, position, rotation, zoneName);

        return instance;
    }

    /// <summary>
    /// Finds or creates the folder structure for a zone based on hierarchy
    /// </summary>
    private Transform FindOrCreateZoneFolder(string zoneName)
    {
        if (!useHierarchicalPlacement || string.IsNullOrEmpty(zoneName))
            return null;

        // Split the zone path (e.g., "Mindfulness/Travel" -> ["Mindfulness", "Travel"])
        string[] pathParts = zoneName.Split('/');

        // Start from the scene root
        Transform currentParent = null;
        GameObject currentObject = null;
        string currentPath = "";

        // Build the path part by part
        for (int i = 0; i < pathParts.Length; i++)
        {
            string partName = pathParts[i].Trim();
            if (string.IsNullOrEmpty(partName))
                continue;

            currentPath += (currentPath.Length > 0 ? "/" : "") + partName;

            // Look for existing object at this level
            Transform child = null;

            if (currentParent == null)
            {
                // Search at root level
                currentObject = GameObject.Find(partName);
            }
            else
            {
                // Search within current parent
                child = currentParent.Find(partName);
                currentObject = child?.gameObject;
            }

            // Create if not found and creation is enabled
            if (currentObject == null && createMissingFolders)
            {
                currentObject = new GameObject(partName);
                if (currentParent != null)
                {
                    currentObject.transform.SetParent(currentParent);
                }

                if (debugMode)
                    Debug.Log($"Created folder in hierarchy: {currentPath}");
            }
            else if (currentObject == null)
            {
                if (debugMode)
                    Debug.LogWarning($"Folder not found in hierarchy: {currentPath} and creation is disabled");
                return null;
            }

            // Update the current parent for the next level
            currentParent = currentObject.transform;
        }

        // If we have a valid parent, find or create the Films folder under it
        if (currentParent != null && !string.IsNullOrEmpty(filmsFolderName))
        {
            Transform filmsFolder = currentParent.Find(filmsFolderName);

            if (filmsFolder == null && createMissingFolders)
            {
                GameObject filmsObj = new GameObject(filmsFolderName);
                filmsObj.transform.SetParent(currentParent);
                filmsFolder = filmsObj.transform;

                if (debugMode)
                    Debug.Log($"Created films folder: {currentPath}/{filmsFolderName}");
            }

            return filmsFolder ?? currentParent;
        }

        return currentParent;
    }

    private void FallbackPlacement(VideoEntry video, string zoneName, List<Vector3> existingPositions)
    {
        // Simple random placement with obstacle avoidance
        for (int attempt = 0; attempt < 30; attempt++)
        {
            Vector3 position = new Vector3(
                UnityEngine.Random.Range(-10f, 10f),
                1.0f,
                UnityEngine.Random.Range(-10f, 10f)
            );

            // Check if too close to other videos
            bool tooClose = false;
            foreach (Vector3 existingPos in existingPositions)
            {
                if (Vector3.Distance(position, existingPos) < 2.0f)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
            {
                Quaternion rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0);
                PlaceVideoAtPosition(video, position, rotation, zoneName);
                existingPositions.Add(position);
                return;
            }
        }

        // Last resort placement
        Vector3 lastResortPos = new Vector3(
            UnityEngine.Random.Range(-20f, 20f),
            1.0f,
            UnityEngine.Random.Range(-20f, 20f)
        );
        Quaternion lastResortRot = Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0);
        PlaceVideoAtPosition(video, lastResortPos, lastResortRot, zoneName);
        existingPositions.Add(lastResortPos);
    }

    private GameObject GetPrefabForVideo(VideoEntry video)
    {
        if (video == null) return defaultPrefab;

        // Try to find prefab by type from video data
        if (!string.IsNullOrEmpty(video.Prefab) && prefabTypeMap.TryGetValue(video.Prefab, out GameObject prefab))
        {
            return prefab;
        }

        // Fall back to default
        return defaultPrefab;
    }

    private void SetupVideoUI(GameObject instance, VideoEntry video)
    {
        // Find TextMeshPro components in children
        TMPro.TextMeshProUGUI[] textComponents = instance.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);

        if (textComponents.Length > 0)
        {
            // Try to find title component
            TMPro.TextMeshProUGUI titleComponent = System.Array.Find(textComponents,
                text => text.gameObject.name.ToLower().Contains("title"));

            // If not found by name, use the first component
            if (titleComponent == null && textComponents.Length > 0)
            {
                titleComponent = textComponents[0];
            }

            // Set title text
            if (titleComponent != null)
            {
                titleComponent.text = string.IsNullOrEmpty(video.Title) ? video.FileName : video.Title;

                // Link to video player
                EnhancedVideoPlayer videoPlayer = instance.GetComponent<EnhancedVideoPlayer>();
                if (videoPlayer != null)
                {
                    videoPlayer.TMP_title = titleComponent;
                    videoPlayer.hasText = true;
                }
            }

            // Try to find description component
            TMPro.TextMeshProUGUI descComponent = System.Array.Find(textComponents,
                text => text.gameObject.name.ToLower().Contains("desc"));

            // If not found by name and we have more than one text component, use the second one
            if (descComponent == null && textComponents.Length > 1 && titleComponent != textComponents[1])
            {
                descComponent = textComponents[1];
            }

            // Set description text
            if (descComponent != null && !string.IsNullOrEmpty(video.Description))
            {
                descComponent.text = video.Description;

                // Link to video player
                EnhancedVideoPlayer videoPlayer = instance.GetComponent<EnhancedVideoPlayer>();
                if (videoPlayer != null)
                {
                    videoPlayer.TMP_description = descComponent;
                }
            }
        }
    }

    /// <summary>
    /// Clear all placed videos from the scene
    /// </summary>
    public void ClearAllVideos()
    {
        foreach (var videoObj in placedVideos.Values)
        {
            if (videoObj != null)
            {
                DestroyImmediate(videoObj);
            }
        }

        placedVideos.Clear();
    }

    #endregion

    #region Position Cache Management

    private void AddToPlacementCache(VideoEntry video, Vector3 position, Quaternion rotation, string zoneName)
    {
        if (video == null) return;

        // First check if this video+zone combination is already in the cache
        VideoPlacementData existingData = placementCache.placements.Find(p =>
            p.videoUrl == video.PublicUrl && p.zoneName == zoneName);

        if (existingData != null)
        {
            // Update position and rotation
            existingData.position = position;
            existingData.rotation = rotation;
        }
        else
        {
            // Create new placement data
            VideoPlacementData newData = new VideoPlacementData
            {
                videoUrl = video.PublicUrl,
                zoneName = zoneName,
                position = position,
                rotation = rotation,
                prefabType = video.Prefab ?? "Default"
            };

            placementCache.placements.Add(newData);
        }
    }

    private bool TryGetCachedPlacement(string videoUrl, string zoneName, out VideoPlacementData placementData)
    {
        placementData = placementCache.placements.Find(p =>
            p.videoUrl == videoUrl && p.zoneName == zoneName);
        return placementData != null;
    }

    /// <summary>
    /// Save the current placement cache to disk
    /// </summary>
    public void SavePlacementCache()
    {
        // Update cache with current positions
        UpdateCacheFromCurrentPositions();

        try
        {
            // Get the full cache path
            string fullCachePath = Path.Combine(cacheFolderPath, cacheFileName);

            // Create directory if it doesn't exist
            if (!Directory.Exists(cacheFolderPath))
            {
                Directory.CreateDirectory(cacheFolderPath);
                if (debugMode) Debug.Log($"Created cache directory: {cacheFolderPath}");
            }

            // Save as JSON
            string json = JsonUtility.ToJson(placementCache, true);
            File.WriteAllText(fullCachePath, json);

            if (debugMode) Debug.Log($"Saved placement cache with {placementCache.placements.Count} entries to {fullCachePath}");

#if UNITY_EDITOR
            // Refresh asset database in editor
            AssetDatabase.Refresh();
#endif
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error saving placement cache: {ex.Message}");
        }
    }

    /// <summary>
    /// Load placement cache from disk
    /// </summary>
    public void LoadPlacementCache()
    {
        try
        {
            string fullCachePath = Path.Combine(cacheFolderPath, cacheFileName);

            if (!File.Exists(fullCachePath))
            {
                if (debugMode) Debug.Log($"No placement cache file found at: {fullCachePath}");
                return;
            }

            string json = File.ReadAllText(fullCachePath);
            placementCache = JsonUtility.FromJson<PlacementCache>(json);

            if (debugMode) Debug.Log($"Loaded placement cache with {placementCache.placements.Count} entries from {fullCachePath}");

            // Place videos using cached positions
            PlaceVideosFromCache();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading placement cache: {ex.Message}");
        }
    }

    private void PlaceVideosFromCache()
    {
        // Clear existing videos
        ClearAllVideos();

        if (databaseManager == null || !databaseManager.IsInitialized)
        {
            Debug.LogError("Cannot place videos from cache: Database manager not ready");
            return;
        }

        foreach (var placementData in placementCache.placements)
        {
            // Find the video in the database
            VideoEntry video = databaseManager.GetEntryByUrl(placementData.videoUrl);

            if (video != null)
            {
                PlaceVideoAtPosition(video, placementData.position, placementData.rotation, placementData.zoneName);
            }
            else if (debugMode)
            {
                Debug.LogWarning($"Video not found in database: {placementData.videoUrl}");
            }
        }
    }

    private void UpdateCacheFromCurrentPositions()
    {
        // Update cache with current positions of all placed videos
        foreach (var pair in placedVideos)
        {
            string videoZoneKey = pair.Key;
            GameObject videoObj = pair.Value;

            if (videoObj == null) continue;

            // Parse the key to extract videoUrl and zoneName
            string[] keyParts = videoZoneKey.Split(new char[] { '_' }, 2);
            if (keyParts.Length < 2) continue;

            string videoUrl = keyParts[0];
            string zoneName = keyParts[1];

            // Find or create cache entry
            VideoPlacementData data = placementCache.placements.Find(p =>
                p.videoUrl == videoUrl && p.zoneName == zoneName);

            if (data != null)
            {
                // Update position and rotation
                data.position = videoObj.transform.position;
                data.rotation = videoObj.transform.rotation;
            }
            else
            {
                // Create new cache entry
                VideoPlacementData newData = new VideoPlacementData
                {
                    videoUrl = videoUrl,
                    zoneName = zoneName,
                    position = videoObj.transform.position,
                    rotation = videoObj.transform.rotation,
                    prefabType = "Default" // Fallback value
                };

                // Try to get the prefab type from the video player
                EnhancedVideoPlayer videoPlayer = videoObj.GetComponent<EnhancedVideoPlayer>();
                if (videoPlayer != null && !string.IsNullOrEmpty(videoPlayer.prefabType))
                {
                    newData.prefabType = videoPlayer.prefabType;
                }

                placementCache.placements.Add(newData);
            }
        }
    }

    private bool DoesCacheExist()
    {
        string fullCachePath = Path.Combine(cacheFolderPath, cacheFileName);
        return File.Exists(fullCachePath);
    }

    #endregion

    #region Manual Adjustment Handling

    /// <summary>
    /// Called when a video is manually moved
    /// </summary>
    public void NotifyVideoMoved(GameObject videoObject)
    {
        if (videoObject == null) return;

        hasManuallyAdjusted = true;

        // Find the video's key in our dictionary
        string videoZoneKey = placedVideos.FirstOrDefault(x => x.Value == videoObject).Key;
        if (string.IsNullOrEmpty(videoZoneKey)) return;

        // Split the key to get videoUrl and zoneName
        string[] keyParts = videoZoneKey.Split(new char[] { '_' }, 2);
        if (keyParts.Length < 2) return;

        string videoUrl = keyParts[0];
        string zoneName = keyParts[1];

        // Update the cache
        VideoPlacementData data = placementCache.placements.Find(p =>
            p.videoUrl == videoUrl && p.zoneName == zoneName);

        if (data != null)
        {
            data.position = videoObject.transform.position;
            data.rotation = videoObject.transform.rotation;
        }
        else
        {
            // Create new cache entry if none exists
            VideoPlacementData newData = new VideoPlacementData
            {
                videoUrl = videoUrl,
                zoneName = zoneName,
                position = videoObject.transform.position,
                rotation = videoObject.transform.rotation,
                prefabType = "Default" // Fallback value
            };

            // Try to get the prefab type from the video player
            EnhancedVideoPlayer videoPlayer = videoObject.GetComponent<EnhancedVideoPlayer>();
            if (videoPlayer != null && !string.IsNullOrEmpty(videoPlayer.prefabType))
            {
                newData.prefabType = videoPlayer.prefabType;
            }

            placementCache.placements.Add(newData);
        }
    }

    #endregion
}

#if UNITY_EDITOR
[CustomEditor(typeof(AdvancedVideoPlacementManager))]
public class AdvancedVideoPlacementManagerEditor : Editor
{
    private AdvancedVideoPlacementManager manager;
    private bool showHierarchySettings = true;

    private void OnEnable()
    {
        manager = (AdvancedVideoPlacementManager)target;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw default properties up to Hierarchy Settings
        DrawPropertiesExcluding(serializedObject, new string[] { "useHierarchicalPlacement", "filmsFolderName", "createMissingFolders" });

        // Draw custom hierarchy settings with foldout
        showHierarchySettings = EditorGUILayout.Foldout(showHierarchySettings, "Hierarchy Settings", true);
        if (showHierarchySettings)
        {
            EditorGUI.indentLevel++;

            // Use hierarchical placement
            SerializedProperty useHierarchyProp = serializedObject.FindProperty("useHierarchicalPlacement");
            EditorGUILayout.PropertyField(useHierarchyProp, new GUIContent("Use Hierarchical Placement", "Place videos in a folder structure matching zone names"));

            // Only show other settings if hierarchy is enabled
            if (useHierarchyProp.boolValue)
            {
                // Films folder name
                SerializedProperty filmsFolderProp = serializedObject.FindProperty("filmsFolderName");
                EditorGUILayout.PropertyField(filmsFolderProp, new GUIContent("Films Folder Name", "Name of the folder to place videos under"));

                // Create missing folders
                SerializedProperty createFoldersProp = serializedObject.FindProperty("createMissingFolders");
                EditorGUILayout.PropertyField(createFoldersProp, new GUIContent("Create Missing Folders", "Automatically create folder structure if it doesn't exist"));

                // Help box explaining the feature
                EditorGUILayout.HelpBox(
                    "With hierarchical placement enabled, videos will be organized in a folder structure " +
                    "matching their zone names. For example, a video in the 'Mindfulness/Travel' zone will be " +
                    "placed in 'Mindfulness/Travel/Films' in the hierarchy.", MessageType.Info);
            }

            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(10);

        // Placement buttons
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Place All Videos"))
        {
            manager.PlaceAllVideos();
        }

        if (GUILayout.Button("Clear All Videos"))
        {
            manager.ClearAllVideos();
        }

        GUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Cache buttons
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Save Placement Cache"))
        {
            manager.SavePlacementCache();
        }

        if (GUILayout.Button("Load Placement Cache"))
        {
            manager.LoadPlacementCache();
        }

        GUILayout.EndHorizontal();

        EditorGUILayout.HelpBox(
            "This component allows you to programmatically place videos in zones\n" +
            "and then manually adjust their positions. Changes are automatically\n" +
            "saved to the cache when you exit play mode or can be saved manually.",
            MessageType.Info);
    }
}
#endif