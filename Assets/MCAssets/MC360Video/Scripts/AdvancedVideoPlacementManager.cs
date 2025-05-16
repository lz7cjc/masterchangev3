using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;

/// <summary>
/// Advanced manager for video placement with zone management, manual adjustments, and position caching
/// </summary>
public class AdvancedVideoPlacementManager : MonoBehaviour
{
    // Cache settings
    [Header("Cache Settings")]
    [SerializeField] private string cacheFolderPath = "Assets/Resources";
    [SerializeField] private string cacheFileName = "VideoPlacementCache.json";
    [SerializeField] private bool loadCacheOnStart = true;
    [SerializeField] private bool saveCacheOnExit = true;
    [SerializeField] private bool debugMode = true;

    // Collider settings
    [Header("Collider Settings")]
    [SerializeField] private bool addBoxCollider = true;
    [SerializeField] private Vector3 colliderSize = new Vector3(2, 2, 0.2f);
    [SerializeField] private bool isTrigger = true;

    // Selection settings
    [Header("Selection Settings")]
    [SerializeField] private bool addEventTrigger = true;
    [SerializeField] private float selectionTimeThreshold = 2.0f;
    [SerializeField] private string playerPrefsKey = "SelectedVideoURL";
    [SerializeField] private string videoAppScene = "VideoPlayerScene";

    // Hierarchy settings
    [Header("Hierarchy Settings")]
    [SerializeField] private bool useHierarchicalPlacement = true;
    [SerializeField] private string filmsFolderName = "Films";
    [SerializeField] private bool createMissingFolders = true;

    // Data structures
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

    // Dictionary to track placed videos
    private Dictionary<string, GameObject> placedVideos = new Dictionary<string, GameObject>();
    private PlacementCache placementCache = new PlacementCache();
    private VideoDatabaseManager databaseManager;
    private PolygonZoneManager zoneManager;
    private VideoPlacementCache cacheManager;
    private bool hasManuallyAdjusted = false;
    private bool isInitialized = false;

    private void Awake()
    {
        FindRequiredComponents();
    }

    private void Start()
    {
        if (loadCacheOnStart && DoesCacheExist())
        {
            LoadPlacementCache();
        }
    }

    private void OnDestroy()
    {
        if (saveCacheOnExit && hasManuallyAdjusted)
        {
            SavePlacementCache();
        }
    }

    private void FindRequiredComponents()
    {
        // Find database manager
        if (databaseManager == null)
        {
            databaseManager = FindObjectOfType<VideoDatabaseManager>();
            if (databaseManager == null)
            {
                Debug.LogError("No VideoDatabaseManager found in scene - video placement will not work");
            }
        }

        // Find zone manager
        if (zoneManager == null)
        {
            zoneManager = FindObjectOfType<PolygonZoneManager>();
            if (zoneManager == null)
            {
                Debug.LogWarning("No PolygonZoneManager found in scene - random placement will be used");
            }
        }

        // Find cache manager
        if (cacheManager == null)
        {
            cacheManager = FindObjectOfType<VideoPlacementCache>();
        }

        isInitialized = (databaseManager != null);
    }

    /// <summary>
    /// Place videos from database into a specific zone
    /// </summary>
    public void PlaceVideosInZone(string zoneName)
    {
        if (!isInitialized)
        {
            FindRequiredComponents();
            if (!isInitialized)
            {
                Debug.LogError("Cannot place videos: Required components not found");
                return;
            }
        }

        // Place videos using database from VideoDatabaseManager
        if (databaseManager == null || !databaseManager.IsInitialized)
        {
            Debug.LogError("Database manager not ready");
            return;
        }

        // Get videos for this zone
        var zoneVideos = databaseManager.GetEntriesForZone(zoneName);

        if (zoneVideos == null || zoneVideos.Count == 0)
        {
            Debug.LogWarning($"No videos found for zone: {zoneName}");
            return;
        }

        int placedCount = 0;
        foreach (var video in zoneVideos)
        {
            if (video == null || string.IsNullOrEmpty(video.PublicUrl)) continue;

            // Try to get cached placement first
            Vector3 position = Vector3.zero;
            Quaternion rotation = Quaternion.identity;
            if (TryGetCachedPlacement(video.PublicUrl, zoneName, out VideoPlacementData cachedData))
            {
                position = cachedData.position;
                rotation = cachedData.rotation;
            }
            else
            {
                // Get placement from zone manager
                if (zoneManager != null)
                {
                    Vector2 point2D;
                    Vector3 zonePosition = Vector3.zero;
                    if (zoneManager.GetRandomPositionInZone(zoneName, out point2D))
                    {
                        // Convert Vector2 to Vector3 (using current Y position)
                        zonePosition = new Vector3(point2D.x, 1.5f, point2D.y);

                        // Use zone position
                        position = zonePosition;
                        // Random rotation around Y axis
                        rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0);
                    }
                    else
                    {
                        // Fallback to random in scene
                        position = new Vector3(
                            UnityEngine.Random.Range(-10f, 10f),
                            1.5f,
                            UnityEngine.Random.Range(-10f, 10f)
                        );
                        rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0);
                    }
                }
                else
                {
                    // Random placement
                    position = new Vector3(
                        UnityEngine.Random.Range(-10f, 10f),
                        1.5f,
                        UnityEngine.Random.Range(-10f, 10f)
                    );
                    rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0);
                }
            }

            // Place the video
            GameObject placedObj = PlaceVideoAtPosition(video, position, rotation, zoneName);
            if (placedObj != null)
            {
                placedCount++;
            }
        }

        if (debugMode) Debug.Log($"Placed {placedCount} videos in zone {zoneName}");
    }

    /// <summary>
    /// Place all videos from database in their appropriate zones
    /// </summary>
    public void PlaceAllVideos()
    {
        if (!isInitialized)
        {
            FindRequiredComponents();
            if (!isInitialized)
            {
                Debug.LogError("Cannot place videos: Required components not found");
                return;
            }
        }

        // Place videos using database from VideoDatabaseManager
        if (databaseManager == null || !databaseManager.IsInitialized)
        {
            Debug.LogError("Database manager not ready");
            return;
        }

        // Get all video entries from database
        var allVideos = databaseManager.GetAllEntries();

        if (allVideos == null || allVideos.Count == 0)
        {
            Debug.LogWarning("No videos found in database");
            return;
        }

        int placedCount = 0;
        foreach (var video in allVideos)
        {
            if (video == null || string.IsNullOrEmpty(video.PublicUrl)) continue;

            // Get zone for this video
            string zoneName = video.Zone;
            if (string.IsNullOrEmpty(zoneName))
            {
                zoneName = "Default";
            }

            // Try to get cached placement first
            Vector3 position = Vector3.zero;
            Quaternion rotation = Quaternion.identity;
            if (TryGetCachedPlacement(video.PublicUrl, zoneName, out VideoPlacementData cachedData))
            {
                position = cachedData.position;
                rotation = cachedData.rotation;
            }
            else
            {
                // Get placement from zone manager
                if (zoneManager != null)
                {
                    Vector2 point2D;
                    Vector3 zonePosition = Vector3.zero;
                    if (zoneManager.GetRandomPositionInZone(zoneName, out point2D))
                    {
                        // Convert Vector2 to Vector3 (using current Y position)
                        zonePosition = new Vector3(point2D.x, 1.5f, point2D.y);

                        // Use zone position
                        position = zonePosition;
                        // Random rotation around Y axis
                        rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0);
                    }
                    else
                    {
                        // Fallback to random in scene
                        position = new Vector3(
                            UnityEngine.Random.Range(-10f, 10f),
                            1.5f,
                            UnityEngine.Random.Range(-10f, 10f)
                        );
                        rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0);
                    }
                }
                else
                {
                    // Random placement
                    position = new Vector3(
                        UnityEngine.Random.Range(-10f, 10f),
                        1.5f,
                        UnityEngine.Random.Range(-10f, 10f)
                    );
                    rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0);
                }
            }

            // Place the video
            GameObject placedObj = PlaceVideoAtPosition(video, position, rotation, zoneName);
            if (placedObj != null)
            {
                placedCount++;
            }
        }

        if (debugMode) Debug.Log($"Placed {placedCount} videos from database");
    }

    /// <summary>
    /// Place a single video at the given position
    /// </summary>
    private GameObject PlaceVideoAtPosition(VideoEntry video, Vector3 position, Quaternion rotation, string zoneName)
    {
        if (video == null || string.IsNullOrEmpty(video.PublicUrl))
        {
            return null;
        }

        // Create a key for tracking this video+zone combination
        string videoZoneKey = video.PublicUrl + "_" + zoneName;

        // Check if we've already placed this video in this zone
        if (placedVideos.ContainsKey(videoZoneKey))
        {
            if (debugMode) Debug.Log($"Video already placed: {video.Title} in zone {zoneName}");
            return placedVideos[videoZoneKey];
        }

        // Create the video object
        GameObject videoObj = new GameObject($"Video_{video.Title}_{zoneName}");

        // Find hierarchy parent
        if (useHierarchicalPlacement)
        {
            // Find or create zone parent
            GameObject zoneObj = GameObject.Find(zoneName);
            if (zoneObj == null && createMissingFolders)
            {
                zoneObj = new GameObject(zoneName);
            }

            if (zoneObj != null)
            {
                // Find or create Films folder
                Transform filmsTransform = zoneObj.transform.Find(filmsFolderName);
                GameObject filmsObj;
                if (filmsTransform == null && createMissingFolders)
                {
                    filmsObj = new GameObject(filmsFolderName);
                    filmsObj.transform.parent = zoneObj.transform;
                }
                else if (filmsTransform != null)
                {
                    filmsObj = filmsTransform.gameObject;
                }
                else
                {
                    // No films folder, use zone as parent
                    filmsObj = zoneObj;
                }

                // Set parent
                videoObj.transform.parent = filmsObj.transform;
            }
        }

        // Set position and rotation
        videoObj.transform.position = position;
        videoObj.transform.rotation = rotation;

        // Add video player component
        EnhancedVideoPlayer videoPlayer = videoObj.AddComponent<EnhancedVideoPlayer>();
        videoPlayer.VideoUrlLink = video.PublicUrl;
        videoPlayer.title = video.Title;
        videoPlayer.zoneName = zoneName;
        videoPlayer.prefabType = video.Prefab;

        // Add collider if needed
        if (addBoxCollider)
        {
            BoxCollider boxCollider = videoObj.AddComponent<BoxCollider>();
            boxCollider.size = colliderSize;
            boxCollider.isTrigger = isTrigger;
        }

        // Add video adjustment handler
        VideoAdjustmentHandler adjustmentHandler = videoObj.AddComponent<VideoAdjustmentHandler>();

        // Track the video
        placedVideos[videoZoneKey] = videoObj;

        // Add to cache
        AddToPlacementCache(video, position, rotation, zoneName);

        return videoObj;
    }

    /// <summary>
    /// Clear all videos placed by this system
    /// </summary>
    public void ClearAllVideos(bool clearAllInScene = false)
    {
        if (clearAllInScene)
        {
            // Find all videos in scene
            EnhancedVideoPlayer[] allVideos = FindObjectsOfType<EnhancedVideoPlayer>();
            foreach (var video in allVideos)
            {
                if (video != null && video.gameObject != null)
                {
                    DestroyImmediate(video.gameObject);
                }
            }

            if (debugMode) Debug.Log($"Cleared all {allVideos.Length} videos from scene");
        }
        else
        {
            // Only clear videos tracked by this system
            int count = 0;
            foreach (var pair in placedVideos)
            {
                GameObject videoObj = pair.Value;
                if (videoObj != null)
                {
                    DestroyImmediate(videoObj);
                    count++;
                }
            }

            if (debugMode) Debug.Log($"Cleared {count} tracked videos");
        }

        // Clear tracking dictionary
        placedVideos.Clear();
    }

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
            existingData.prefabType = video.Prefab ?? "Default";
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

        int successfulPlacements = 0;
        foreach (var placementData in placementCache.placements)
        {
            // Find the video in the database
            VideoEntry video = databaseManager.GetEntryByUrl(placementData.videoUrl);

            if (video != null)
            {
                GameObject placedObj = PlaceVideoAtPosition(video, placementData.position, placementData.rotation, placementData.zoneName);
                if (placedObj != null)
                {
                    successfulPlacements++;
                }
            }
            else if (debugMode)
            {
                Debug.LogWarning($"Video not found in database: {placementData.videoUrl}");
            }
        }

        if (debugMode) Debug.Log($"Placed {successfulPlacements} videos from cache");
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
        string videoZoneKey = "";
        foreach (var pair in placedVideos)
        {
            if (pair.Value == videoObject)
            {
                videoZoneKey = pair.Key;
                break;
            }
        }

        if (string.IsNullOrEmpty(videoZoneKey))
        {
            // Try to find info from EnhancedVideoPlayer component
            EnhancedVideoPlayer videoPlayer = videoObject.GetComponent<EnhancedVideoPlayer>();
            if (videoPlayer != null && !string.IsNullOrEmpty(videoPlayer.VideoUrlLink) && !string.IsNullOrEmpty(videoPlayer.zoneName))
            {
                videoZoneKey = videoPlayer.VideoUrlLink + "_" + videoPlayer.zoneName;
            }
            else
            {
                return; // Can't find necessary information
            }
        }

        // Split the key to get videoUrl and zoneName
        string[] keyParts = videoZoneKey.Split(new char[] { '_' }, 2);
        if (keyParts.Length < 2) return;

        string videoUrl = keyParts[0];
        string zoneName = keyParts[1];

        // Get prefab type if available
        string prefabType = "Default";
        EnhancedVideoPlayer videoPlayer2 = videoObject.GetComponent<EnhancedVideoPlayer>();
        if (videoPlayer2 != null && !string.IsNullOrEmpty(videoPlayer2.prefabType))
        {
            prefabType = videoPlayer2.prefabType;
        }

        // Update the cache
        VideoPlacementData data = placementCache.placements.Find(p =>
            p.videoUrl == videoUrl && p.zoneName == zoneName);

        if (data != null)
        {
            data.position = videoObject.transform.position;
            data.rotation = videoObject.transform.rotation;

            if (debugMode) Debug.Log($"Updated cached position for video at {videoObject.transform.position}");
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
                prefabType = prefabType
            };

            placementCache.placements.Add(newData);

            if (debugMode) Debug.Log($"Created new cache entry for manually moved video");
        }
    }

    #endregion

#if UNITY_EDITOR
    [CustomEditor(typeof(AdvancedVideoPlacementManager))]
    public class AdvancedVideoPlacementManagerEditor : Editor
    {
        private AdvancedVideoPlacementManager manager;
        private bool showHierarchySettings = true;
        private bool showColliderSettings = true;
        private bool showSelectionSettings = true;
        private bool showCacheSettings = true;

        private void OnEnable()
        {
            manager = (AdvancedVideoPlacementManager)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Draw default properties up to exclusions
            DrawPropertiesExcluding(serializedObject, new string[] {
                "addBoxCollider", "colliderSize", "isTrigger",
                "addEventTrigger", "selectionTimeThreshold", "playerPrefsKey", "videoAppScene",
                "cacheFolderPath", "cacheFileName", "saveCacheOnExit", "loadCacheOnStart",
                "useHierarchicalPlacement", "filmsFolderName", "createMissingFolders"
            });

            // Draw custom collider settings with foldout
            showColliderSettings = EditorGUILayout.Foldout(showColliderSettings, "Collider Settings", true);
            if (showColliderSettings)
            {
                EditorGUI.indentLevel++;

                // Add box collider
                SerializedProperty addColliderProp = serializedObject.FindProperty("addBoxCollider");
                EditorGUILayout.PropertyField(addColliderProp, new GUIContent("Add Box Collider", "Add a box collider to each video prefab"));

                // Only show other settings if collider is enabled
                if (addColliderProp.boolValue)
                {
                    SerializedProperty colliderSizeProp = serializedObject.FindProperty("colliderSize");
                    EditorGUILayout.PropertyField(colliderSizeProp, new GUIContent("Collider Size", "Size of the box collider"));

                    SerializedProperty isTriggerProp = serializedObject.FindProperty("isTrigger");
                    EditorGUILayout.PropertyField(isTriggerProp, new GUIContent("Is Trigger", "Should the collider be a trigger?"));
                }

                EditorGUI.indentLevel--;
            }

            // Draw custom selection settings with foldout
            showSelectionSettings = EditorGUILayout.Foldout(showSelectionSettings, "Selection Settings", true);
            if (showSelectionSettings)
            {
                EditorGUI.indentLevel++;

                // Add event trigger
                SerializedProperty addEventTriggerProp = serializedObject.FindProperty("addEventTrigger");
                EditorGUILayout.PropertyField(addEventTriggerProp, new GUIContent("Add Event Trigger", "Add event trigger for video selection"));

                // Only show other settings if event trigger is enabled
                if (addEventTriggerProp.boolValue)
                {
                    SerializedProperty thresholdProp = serializedObject.FindProperty("selectionTimeThreshold");
                    EditorGUILayout.PropertyField(thresholdProp, new GUIContent("Selection Time Threshold", "Time in seconds before launching video"));

                    SerializedProperty keyProp = serializedObject.FindProperty("playerPrefsKey");
                    EditorGUILayout.PropertyField(keyProp, new GUIContent("PlayerPrefs Key", "Key for storing the video URL in PlayerPrefs"));

                    SerializedProperty sceneProp = serializedObject.FindProperty("videoAppScene");
                    EditorGUILayout.PropertyField(sceneProp, new GUIContent("Video App Scene", "Scene to load when video is selected"));
                }

                EditorGUI.indentLevel--;
            }

            // Draw custom cache settings with foldout
            showCacheSettings = EditorGUILayout.Foldout(showCacheSettings, "Cache Settings", true);
            if (showCacheSettings)
            {
                EditorGUI.indentLevel++;

                SerializedProperty cacheFolderProp = serializedObject.FindProperty("cacheFolderPath");
                EditorGUILayout.PropertyField(cacheFolderProp, new GUIContent("Cache Folder Path", "Directory to store the cache file"));

                SerializedProperty cacheFileNameProp = serializedObject.FindProperty("cacheFileName");
                EditorGUILayout.PropertyField(cacheFileNameProp, new GUIContent("Cache File Name", "Name of the cache file"));

                SerializedProperty saveOnExitProp = serializedObject.FindProperty("saveCacheOnExit");
                EditorGUILayout.PropertyField(saveOnExitProp, new GUIContent("Save Cache On Exit", "Automatically save cache when exiting play mode"));

                SerializedProperty loadOnStartProp = serializedObject.FindProperty("loadCacheOnStart");
                EditorGUILayout.PropertyField(loadOnStartProp, new GUIContent("Load Cache On Start", "Automatically load cache when starting play mode"));

                EditorGUI.indentLevel--;
            }

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
            EditorGUILayout.LabelField("Placement Controls", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Place All Videos"))
            {
                manager.PlaceAllVideos();
            }

            if (GUILayout.Button("Clear Tracked Videos"))
            {
                manager.ClearAllVideos(false);
            }

            GUILayout.EndHorizontal();

            if (GUILayout.Button("Clear ALL Video Objects", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Clear ALL Videos",
                    "Are you sure you want to clear ALL video objects from the scene? This operation can't be undone.",
                    "Yes, Clear ALL", "Cancel"))
                {
                    manager.ClearAllVideos(true);
                }
            }

            EditorGUILayout.Space(5);

            // Cache Buttons
            EditorGUILayout.LabelField("Cache Controls", EditorStyles.boldLabel);

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

            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "This component manages the placement of videos in your scene based on zones.\n\n" +
                "Use 'Place All Videos' to generate videos from your database in their assigned zones.\n" +
                "Use 'Clear Tracked Videos' to remove videos placed by this system.\n" +
                "Use 'Save Placement Cache' to store current video positions for later loading.\n" +
                "Use 'Load Placement Cache' to restore previously saved positions.",
                MessageType.Info);
        }
    }
#endif
}