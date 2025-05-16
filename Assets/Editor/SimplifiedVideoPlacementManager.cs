using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using UnityEditor;
using TMPro;

/// <summary>
/// Simplified Video Placement Manager with improved performance and functionality
/// Manages the placement of videos in zones and handles positioning/saving
/// </summary>
public class SimplifiedVideoPlacementManager : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private VideoDatabaseManager databaseManager;
    [SerializeField] private PolygonZoneManager zoneManager;
    [SerializeField] private bool placeOnStart = true;
    [SerializeField] private float startDelay = 1.0f;

    [Header("Prefab Settings")]
    [SerializeField] private GameObject defaultPrefab;
    [SerializeField] private List<PrefabTypeMapping> prefabMappings = new List<PrefabTypeMapping>();

    [Header("Placement Settings")]
    [SerializeField] private Vector3 colliderSize = new Vector3(2, 2, 0.2f);
    [SerializeField] private float selectionTimeThreshold = 2.0f;
    [SerializeField] private string playerPrefsKey = "VideoUrl";
    [SerializeField] private string videoAppScene = "360VideoApp";

    [Header("Caching Settings")]
    [SerializeField] private string cacheFolderPath = "Assets/Resources";
    [SerializeField] private string cacheFileName = "VideoPlacementCache.json";
    [SerializeField] private bool saveCacheOnExit = true;
    [SerializeField] private bool loadCacheOnStart = true;

    [Header("Hierarchy Settings")]
    [SerializeField] private bool useHierarchicalPlacement = true;
    [SerializeField] private string filmsFolderName = "Films";
    [SerializeField] private bool createMissingFolders = true;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    // Mappings for different prefab types
    [System.Serializable]
    public class PrefabTypeMapping
    {
        public string typeName;
        public GameObject prefab;
    }

    // Data structure for placement caching
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

    // Dictionary to track placed videos - key is videoUrl_zoneName
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
        }
        else if (placeOnStart)
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

    // Find required dependencies if not assigned
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

    // Initialize prefab type mappings
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
                    // Use a default rotation facing upward with random offset
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

            // Update the position and rotation if changed
            if (existingVideo.transform.position != position || existingVideo.transform.rotation != rotation)
            {
                existingVideo.transform.position = position;
                existingVideo.transform.rotation = rotation;
                hasManuallyAdjusted = true;
            }

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

        // Add the SimplifiedVideoHandler component
        SimplifiedVideoHandler videoHandler = instance.GetComponent<SimplifiedVideoHandler>();
        if (videoHandler == null)
        {
            videoHandler = instance.AddComponent<SimplifiedVideoHandler>();
        }

        // Configure the video handler
        videoHandler.SetDataFromVideoEntry(video, zoneName);

        // Configure box collider size
        BoxCollider boxCollider = instance.GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            boxCollider.size = colliderSize;
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

    // Fallback placement for when zones don't work
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

    // Get the correct prefab for the video based on type
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

    /// <summary>
    /// Clear all placed videos from the scene and optionally all video-related objects
    /// </summary>
    public void ClearAllVideos(bool removeAllVideoObjects = false)
    {
        // First remove tracked videos
        foreach (var videoObj in placedVideos.Values)
        {
            if (videoObj != null)
            {
                DestroyImmediate(videoObj);
            }
        }
        placedVideos.Clear();

        // If requested, find and remove all remaining video objects
        if (removeAllVideoObjects)
        {
            // Find all SimplifiedVideoHandler components
            SimplifiedVideoHandler[] handlers = FindObjectsOfType<SimplifiedVideoHandler>();
            foreach (SimplifiedVideoHandler handler in handlers)
            {
                if (handler != null && handler.gameObject != null)
                {
                    DestroyImmediate(handler.gameObject);
                }
            }

            // Legacy components cleanup for compatibility
            // Find all EnhancedVideoPlayer components (old system)
            EnhancedVideoPlayer[] videoPlayers = FindObjectsOfType<EnhancedVideoPlayer>();
            foreach (EnhancedVideoPlayer player in videoPlayers)
            {
                if (player != null && player.gameObject != null)
                {
                    DestroyImmediate(player.gameObject);
                }
            }

            // Find any old ToggleShowHideVideo components
            MonoBehaviour[] allMono = FindObjectsOfType<MonoBehaviour>();
            foreach (MonoBehaviour mono in allMono)
            {
                if (mono != null && mono.GetType().Name == "ToggleShowHideVideo")
                {
                    DestroyImmediate(mono.gameObject);
                }
            }

            if (debugMode) Debug.Log("Removed all video objects from the scene");
        }
    }

    #endregion

    #region Position Cache Management

    // Add a video to the placement cache
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

    // Try to get cached placement data for a video
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
        catch (System.Exception ex)
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
        catch (System.Exception ex)
        {
            Debug.LogError($"Error loading placement cache: {ex.Message}");
        }
    }

    // Place videos from the loaded cache
    private void PlaceVideosFromCache()
    {
        // Clear existing videos
        ClearAllVideos();

        if (databaseManager == null || !databaseManager.IsInitialized)
        {
            Debug.LogError("Cannot place videos from cache: Database manager not ready");
            return;
        }

        int placedCount = 0;
        foreach (var placementData in placementCache.placements)
        {
            // Find the video in the database
            VideoEntry video = databaseManager.GetEntryByUrl(placementData.videoUrl);

            if (video != null)
            {
                PlaceVideoAtPosition(video, placementData.position, placementData.rotation, placementData.zoneName);
                placedCount++;
            }
            else if (debugMode)
            {
                Debug.LogWarning($"Video not found in database: {placementData.videoUrl}");
            }
        }

        if (debugMode) Debug.Log($"Placed {placedCount} videos from cache");
    }

    // Update the cache with current positions
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

                // Try to get the prefab type from the video handler
                SimplifiedVideoHandler videoHandler = videoObj.GetComponent<SimplifiedVideoHandler>();
                if (videoHandler != null)
                {
                    // Get prefab type if available
                    var field = videoHandler.GetType().GetField("prefabType", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    if (field != null)
                    {
                        string prefabType = field.GetValue(videoHandler) as string;
                        if (!string.IsNullOrEmpty(prefabType))
                        {
                            newData.prefabType = prefabType;
                        }
                    }
                }

                placementCache.placements.Add(newData);
            }
        }
    }

    /// <summary>
    /// Check if a cache file exists
    /// </summary>
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

        if (string.IsNullOrEmpty(videoZoneKey))
        {
            // Try to find by querying the video handler
            SimplifiedVideoHandler handler = videoObject.GetComponent<SimplifiedVideoHandler>();
            if (handler != null)
            {
                string videoUrl = handler.GetVideoUrl();
                string zoneName = handler.GetZoneName();

                if (!string.IsNullOrEmpty(videoUrl) && !string.IsNullOrEmpty(zoneName))
                {
                    videoZoneKey = videoUrl + "_" + zoneName;

                    // Add to tracked videos if not already there
                    if (!placedVideos.ContainsKey(videoZoneKey))
                    {
                        placedVideos.Add(videoZoneKey, videoObject);
                    }
                }
            }
        }

        if (string.IsNullOrEmpty(videoZoneKey)) return;

        // Split the key to get videoUrl and zoneName
        string[] keyParts = videoZoneKey.Split(new char[] { '_' }, 2);
        if (keyParts.Length < 2) return;

        string url = keyParts[0];
        string zone = keyParts[1];

        // Update the cache
        VideoPlacementData data = placementCache.placements.Find(p =>
            p.videoUrl == url && p.zoneName == zone);

        if (data != null)
        {
            data.position = videoObject.transform.position;
            data.rotation = videoObject.transform.rotation;
        }
        else
        {
            // Create new cache entry if none exists
            string prefabType = "Default"; // Default fallback

            // Try to get prefab type from handler
            SimplifiedVideoHandler handler = videoObject.GetComponent<SimplifiedVideoHandler>();
            if (handler != null)
            {
                var field = handler.GetType().GetField("prefabType", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (field != null)
                {
                    string type = field.GetValue(handler) as string;
                    if (!string.IsNullOrEmpty(type))
                    {
                        prefabType = type;
                    }
                }
            }

            VideoPlacementData newData = new VideoPlacementData
            {
                videoUrl = url,
                zoneName = zone,
                position = videoObject.transform.position,
                rotation = videoObject.transform.rotation,
                prefabType = prefabType
            };

            placementCache.placements.Add(newData);
        }

        // Auto-save if adjustments have been made
        if (saveCacheOnExit)
        {
            SavePlacementCache();
        }
    }

    #endregion

    #region Upgrade Utilities

    /// <summary>
    /// Convert legacy video components to simplified system
    /// </summary>
    public void UpgradeLegacyComponents()
    {
        int upgradedCount = 0;

        // Find all EnhancedVideoPlayer components
        EnhancedVideoPlayer[] legacyPlayers = FindObjectsOfType<EnhancedVideoPlayer>();
        foreach (EnhancedVideoPlayer legacyPlayer in legacyPlayers)
        {
            if (legacyPlayer == null || legacyPlayer.gameObject == null) continue;

            GameObject videoObj = legacyPlayer.gameObject;

            // Skip if already has the new handler
            if (videoObj.GetComponent<SimplifiedVideoHandler>() != null) continue;

            // Add new handler
            SimplifiedVideoHandler newHandler = videoObj.AddComponent<SimplifiedVideoHandler>();

            // Transfer data
            TransferDataFromLegacyPlayer(legacyPlayer, newHandler);

            // Disable but don't destroy the old component
            legacyPlayer.enabled = false;

            upgradedCount++;
        }

        // Find any ToggleShowHideVideo components
        MonoBehaviour[] allMono = FindObjectsOfType<MonoBehaviour>();
        foreach (MonoBehaviour mono in allMono)
        {
            if (mono == null || mono.gameObject == null) continue;
            if (mono.GetType().Name != "ToggleShowHideVideo") continue;

            GameObject videoObj = mono.gameObject;

            // Skip if already has the new handler or was already processed
            if (videoObj.GetComponent<SimplifiedVideoHandler>() != null) continue;

            // Add new handler
            SimplifiedVideoHandler newHandler = videoObj.AddComponent<SimplifiedVideoHandler>();

            // Try to transfer data
            TransferDataFromToggleShowHide(mono, newHandler);

            // Disable the old component
            mono.enabled = false;

            upgradedCount++;
        }

        Debug.Log($"Upgraded {upgradedCount} legacy video components to SimplifiedVideoHandler");
    }

    // Transfer data from EnhancedVideoPlayer to SimplifiedVideoHandler
    private void TransferDataFromLegacyPlayer(EnhancedVideoPlayer legacyPlayer, SimplifiedVideoHandler newHandler)
    {
        // Get reflection access to newHandler fields
        System.Type handlerType = typeof(SimplifiedVideoHandler);

        // Transfer basic properties
        SetPrivateField(handlerType, newHandler, "videoUrl", legacyPlayer.VideoUrlLink);
        SetPrivateField(handlerType, newHandler, "title", legacyPlayer.title);
        SetPrivateField(handlerType, newHandler, "description", legacyPlayer.description);
        SetPrivateField(handlerType, newHandler, "prefabType", legacyPlayer.prefabType);
        SetPrivateField(handlerType, newHandler, "zoneName", legacyPlayer.zoneName);

        // Scene management
        SetPrivateField(handlerType, newHandler, "returntoscene", legacyPlayer.returntoscene);
        SetPrivateField(handlerType, newHandler, "nextscene", legacyPlayer.nextscene);
        SetPrivateField(handlerType, newHandler, "returnstage", legacyPlayer.returnstage);
        SetPrivateField(handlerType, newHandler, "behaviour", legacyPlayer.behaviour);
        SetPrivateField(handlerType, newHandler, "useAdditiveLoading", legacyPlayer.useAdditiveLoading);

        // Selection settings
        SetPrivateField(handlerType, newHandler, "selectionTimeThreshold", legacyPlayer.hoverTimeRequired);

        // UI elements
        SetPrivateField(handlerType, newHandler, "titleText", legacyPlayer.TMP_title);
        SetPrivateField(handlerType, newHandler, "descriptionText", legacyPlayer.TMP_description);

        // Visual settings
        SetPrivateField(handlerType, newHandler, "normalColor", legacyPlayer.normalColor);
        SetPrivateField(handlerType, newHandler, "hoverColor", legacyPlayer.hoverColor);
        SetPrivateField(handlerType, newHandler, "useProgressIndicator", legacyPlayer.useProgressIndicator);
        SetPrivateField(handlerType, newHandler, "rotateOnHover", legacyPlayer.rotateOnHover);

        if (debugMode)
        {
            Debug.Log($"Transferred data from EnhancedVideoPlayer to SimplifiedVideoHandler: {legacyPlayer.title}");
        }
    }

    // Transfer data from legacy ToggleShowHideVideo to SimplifiedVideoHandler using reflection
    private void TransferDataFromToggleShowHide(MonoBehaviour legacyComponent, SimplifiedVideoHandler newHandler)
    {
        if (legacyComponent == null || newHandler == null) return;

        try
        {
            // Get reflection access
            System.Type legacyType = legacyComponent.GetType();
            System.Type handlerType = typeof(SimplifiedVideoHandler);

            // Try to transfer VideoUrlLink
            System.Reflection.FieldInfo urlField = legacyType.GetField("VideoUrlLink");
            if (urlField != null)
            {
                string url = urlField.GetValue(legacyComponent) as string;
                if (!string.IsNullOrEmpty(url))
                {
                    SetPrivateField(handlerType, newHandler, "videoUrl", url);
                }
            }

            // Try to get other fields
            TransferField(legacyType, legacyComponent, handlerType, newHandler, "title", "title");
            TransferField(legacyType, legacyComponent, handlerType, newHandler, "returntoscene", "returntoscene");
            TransferField(legacyType, legacyComponent, handlerType, newHandler, "nextscene", "nextscene");
            TransferField(legacyType, legacyComponent, handlerType, newHandler, "returnstage", "returnstage");
            TransferField(legacyType, legacyComponent, handlerType, newHandler, "behaviour", "behaviour");
            TransferField(legacyType, legacyComponent, handlerType, newHandler, "TMP_title", "titleText");

            // Try to get zone from a known field name
            TransferField(legacyType, legacyComponent, handlerType, newHandler, "zone", "zoneName");

            if (debugMode)
            {
                Debug.Log($"Transferred data from ToggleShowHideVideo to SimplifiedVideoHandler");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error transferring data from ToggleShowHideVideo: {ex.Message}");
        }
    }

    // Helper to set a private field via reflection
    private void SetPrivateField(System.Type type, object target, string fieldName, object value)
    {
        if (type == null || target == null || string.IsNullOrEmpty(fieldName)) return;
        if (value == null) return;

        try
        {
            var field = type.GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }
        catch (System.Exception)
        {
            // Silently ignore errors - field might not exist or types might not match
        }
    }

    // Helper to transfer a field from one object to another via reflection
    private void TransferField(System.Type sourceType, object source, System.Type targetType, object target, string sourceFieldName, string targetFieldName)
    {
        try
        {
            var sourceField = sourceType.GetField(sourceFieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (sourceField != null)
            {
                object value = sourceField.GetValue(source);
                if (value != null)
                {
                    SetPrivateField(targetType, target, targetFieldName, value);
                }
            }
        }
        catch (System.Exception)
        {
            // Silently ignore errors
        }
    }

    #endregion
}

#if UNITY_EDITOR
// Custom editor for SimplifiedVideoPlacementManager
[CustomEditor(typeof(SimplifiedVideoPlacementManager))]
public class SimplifiedVideoPlacementManagerEditor : Editor
{
    private bool showPrefabMappingsFoldout = true;
    private bool showPlacementSettings = true;
    private bool showCacheSettings = true;
    private bool showHierarchySettings = true;
    private bool showUpgradeOptions = true;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SimplifiedVideoPlacementManager manager = (SimplifiedVideoPlacementManager)target;

        // Configuration section
        EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("databaseManager"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("zoneManager"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("placeOnStart"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("startDelay"));

        EditorGUILayout.Space(10);

        // Prefab Settings
        EditorGUILayout.LabelField("Prefab Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultPrefab"));

        showPrefabMappingsFoldout = EditorGUILayout.Foldout(showPrefabMappingsFoldout, "Prefab Type Mappings", true);
        if (showPrefabMappingsFoldout)
        {
            EditorGUI.indentLevel++;
            SerializedProperty mappingsProperty = serializedObject.FindProperty("prefabMappings");
            EditorGUILayout.PropertyField(mappingsProperty, true);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(5);

        // Placement Settings
        showPlacementSettings = EditorGUILayout.Foldout(showPlacementSettings, "Placement Settings", true);
        if (showPlacementSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("colliderSize"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("selectionTimeThreshold"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("playerPrefsKey"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("videoAppScene"));
            EditorGUI.indentLevel--;
        }

        // Cache Settings
        showCacheSettings = EditorGUILayout.Foldout(showCacheSettings, "Cache Settings", true);
        if (showCacheSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cacheFolderPath"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cacheFileName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("saveCacheOnExit"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("loadCacheOnStart"));
            EditorGUI.indentLevel--;
        }

        // Hierarchy Settings
        showHierarchySettings = EditorGUILayout.Foldout(showHierarchySettings, "Hierarchy Settings", true);
        if (showHierarchySettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useHierarchicalPlacement"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("filmsFolderName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("createMissingFolders"));
            EditorGUI.indentLevel--;
        }

        // Debug Settings
        EditorGUILayout.PropertyField(serializedObject.FindProperty("debugMode"));

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(10);

        // Actions
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

        // Place Videos Button
        if (GUILayout.Button("Place All Videos"))
        {
            manager.PlaceAllVideos();
        }

        GUILayout.BeginHorizontal();

        // Clear Videos Button
        if (GUILayout.Button("Clear Tracked Videos"))
        {
            manager.ClearAllVideos(false);
        }

        // Delete ALL Videos button (with confirmation)
        if (GUILayout.Button("Delete ALL Video Objects", GUILayout.Width(180)))
        {
            if (EditorUtility.DisplayDialog("Delete All Videos",
                "Are you sure you want to delete ALL video objects from the scene?\n\nThis will remove both tracked and untracked video objects and cannot be undone.",
                "Yes, Delete All", "Cancel"))
            {
                manager.ClearAllVideos(true);
            }
        }

        GUILayout.EndHorizontal();

        // Cache controls
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

        // Upgrade options
        showUpgradeOptions = EditorGUILayout.Foldout(showUpgradeOptions, "Upgrade Options", true);
        if (showUpgradeOptions)
        {
            if (GUILayout.Button("Upgrade Legacy Components"))
            {
                if (EditorUtility.DisplayDialog("Upgrade Legacy Components",
                    "This will convert all old video components (EnhancedVideoPlayer, ToggleShowHideVideo) to the new SimplifiedVideoHandler.\n\nThe old components will be disabled but not removed.\n\nContinue?",
                    "Yes, Upgrade Components", "Cancel"))
                {
                    manager.UpgradeLegacyComponents();
                }
            }

            EditorGUILayout.HelpBox(
                "Use this option to convert all legacy video components to the new simplified system. " +
                "This will add SimplifiedVideoHandler to all video objects and transfer their data.",
                MessageType.Info);
        }

        EditorGUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "This component simplifies video placement and management.\n\n" +
            "• Each video gets a SimplifiedVideoHandler component\n" +
            "• Videos are placed in zones with proper hierarchy\n" +
            "• Positions are saved to and loaded from cache\n" +
            "• Legacy components can be upgraded automatically",
            MessageType.Info);
    }
}
#endif