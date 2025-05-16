using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Handles caching of video placements to allow persistence between sessions
/// This component should be added to the same GameObject as AdvancedVideoPlacementManager
/// </summary>
public class VideoPlacementCache : MonoBehaviour
{
    [Header("Cache Settings")]
    [SerializeField] private string cacheFolderPath = "Assets/Resources";
    [SerializeField] private string cacheFileName = "VideoPlacementCache.json";
    [SerializeField] private bool autoLoadOnStart = true;
    [SerializeField] private bool autoSaveOnExit = true;
    [SerializeField] private bool debugMode = true;

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

    private PlacementCache placementCache = new PlacementCache();
    private AdvancedVideoPlacementManager placementManager;
    private VideoDatabaseManager databaseManager;
    private bool hasManuallyAdjusted = false;

    private void Awake()
    {
        FindRequiredComponents();
    }

    private void Start()
    {
        if (autoLoadOnStart && DoesCacheExist())
        {
            LoadCache();
        }
    }

    private void OnDestroy()
    {
        if (autoSaveOnExit && hasManuallyAdjusted)
        {
            SaveCache();
        }
    }

    private void FindRequiredComponents()
    {
        // Find placement manager if not assigned
        if (placementManager == null)
        {
            placementManager = GetComponent<AdvancedVideoPlacementManager>();
            if (placementManager == null)
            {
                placementManager = FindObjectOfType<AdvancedVideoPlacementManager>();
                if (placementManager == null)
                {
                    Debug.LogError("No AdvancedVideoPlacementManager found - cache functionality will be limited");
                }
            }
        }

        // Find database manager if not assigned
        if (databaseManager == null)
        {
            databaseManager = FindObjectOfType<VideoDatabaseManager>();
            if (databaseManager == null)
            {
                Debug.LogError("No VideoDatabaseManager found - cache functionality will be limited");
            }
        }
    }

    /// <summary>
    /// Get placement data for a specific video in a zone
    /// </summary>
    public bool TryGetPlacementData(string videoUrl, string zoneName, out VideoPlacementData data)
    {
        data = placementCache.placements.Find(p =>
            p.videoUrl == videoUrl && p.zoneName == zoneName);
        return data != null;
    }

    /// <summary>
    /// Add or update placement data
    /// </summary>
    public void AddOrUpdatePlacementData(string videoUrl, string zoneName, Vector3 position, Quaternion rotation, string prefabType = "Default")
    {
        hasManuallyAdjusted = true;

        // Check if we already have this entry
        VideoPlacementData existingData = placementCache.placements.Find(p =>
            p.videoUrl == videoUrl && p.zoneName == zoneName);

        if (existingData != null)
        {
            // Update existing data
            existingData.position = position;
            existingData.rotation = rotation;
            existingData.prefabType = prefabType;
        }
        else
        {
            // Create new entry
            VideoPlacementData newData = new VideoPlacementData
            {
                videoUrl = videoUrl,
                zoneName = zoneName,
                position = position,
                rotation = rotation,
                prefabType = prefabType
            };

            placementCache.placements.Add(newData);
        }
    }

    /// <summary>
    /// Load the placement cache from disk
    /// </summary>
    public void LoadCache()
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

            // Apply cache to videos in scene
            RestoreVideoPositions();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading placement cache: {ex.Message}");
        }
    }

    /// <summary>
    /// Save the placement cache to disk
    /// </summary>
    public void SaveCache()
    {
        // Update cache with current positions first
        CollectVideoPositions();

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
    /// Collect current positions of all videos in the scene
    /// </summary>
    private void CollectVideoPositions()
    {
        // Find all EnhancedVideoPlayer components
        EnhancedVideoPlayer[] videoPlayers = FindObjectsOfType<EnhancedVideoPlayer>();

        foreach (EnhancedVideoPlayer player in videoPlayers)
        {
            if (player == null || string.IsNullOrEmpty(player.VideoUrlLink)) continue;

            string videoUrl = player.VideoUrlLink;
            string zoneName = player.zoneName;

            // If zone name is empty, try to extract it from the GameObject's path
            if (string.IsNullOrEmpty(zoneName))
            {
                zoneName = ExtractZoneFromPath(player.gameObject);
                player.zoneName = zoneName;
            }

            if (string.IsNullOrEmpty(zoneName))
            {
                // Skip videos with no zone
                continue;
            }

            // Add to cache
            AddOrUpdatePlacementData(
                videoUrl,
                zoneName,
                player.transform.position,
                player.transform.rotation,
                player.prefabType
            );
        }
    }

    /// <summary>
    /// Extract zone name from GameObject hierarchy path
    /// </summary>
    private string ExtractZoneFromPath(GameObject obj)
    {
        // Try to extract zone from path
        // Assuming structure like Zone/Films/Video
        if (obj == null) return "";

        Transform parent = obj.transform.parent;
        if (parent == null) return "";

        // If parent is "Films", get grandparent
        if (parent.name == "Films")
        {
            Transform grandparent = parent.parent;
            if (grandparent != null)
            {
                return grandparent.name;
            }
        }

        // Otherwise use parent name
        return parent.name;
    }

    /// <summary>
    /// Apply cached positions to videos in the scene
    /// </summary>
    private void RestoreVideoPositions()
    {
        // Find all EnhancedVideoPlayer components
        EnhancedVideoPlayer[] videoPlayers = FindObjectsOfType<EnhancedVideoPlayer>();

        // Check each video if it has a cached position
        foreach (EnhancedVideoPlayer player in videoPlayers)
        {
            if (player == null || string.IsNullOrEmpty(player.VideoUrlLink)) continue;

            string videoUrl = player.VideoUrlLink;
            string zoneName = player.zoneName;

            // If zone name is empty, try to extract it from the GameObject's path
            if (string.IsNullOrEmpty(zoneName))
            {
                zoneName = ExtractZoneFromPath(player.gameObject);
                player.zoneName = zoneName;
            }

            if (string.IsNullOrEmpty(zoneName))
            {
                // Skip videos with no zone
                continue;
            }

            // Try to get cached position
            if (TryGetPlacementData(videoUrl, zoneName, out VideoPlacementData data))
            {
                // Apply position and rotation
                player.transform.position = data.position;
                player.transform.rotation = data.rotation;

                if (debugMode) Debug.Log($"Applied cached position to video: {player.title} in zone: {zoneName}");
            }
        }
    }

    /// <summary>
    /// Check if a cache file exists
    /// </summary>
    public bool DoesCacheExist()
    {
        string fullCachePath = Path.Combine(cacheFolderPath, cacheFileName);
        return File.Exists(fullCachePath);
    }

    /// <summary>
    /// Clear the cache
    /// </summary>
    public void ClearCache()
    {
        placementCache.placements.Clear();

        if (debugMode) Debug.Log("Placement cache cleared");
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(VideoPlacementCache))]
    public class VideoPlacementCacheEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            VideoPlacementCache cacheManager = (VideoPlacementCache)target;

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Cache Controls", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Load Cache"))
            {
                cacheManager.LoadCache();
            }

            if (GUILayout.Button("Save Cache"))
            {
                cacheManager.SaveCache();
            }

            GUILayout.EndHorizontal();

            if (GUILayout.Button("Clear Cache", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Clear Cache",
                    "Are you sure you want to clear the placement cache? This will delete all saved positions.",
                    "Clear Cache", "Cancel"))
                {
                    cacheManager.ClearCache();
                }
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "This component manages the saving and loading of video positions. " +
                "Videos positions are cached to disk and can be restored between sessions.",
                MessageType.Info);
        }
    }
#endif
}