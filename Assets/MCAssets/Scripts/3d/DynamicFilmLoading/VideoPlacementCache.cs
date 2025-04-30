using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

/// <summary>
/// Models and utilities for serializing video placement data
/// </summary>
public class VideoPlacementCache : MonoBehaviour
{
    [System.Serializable]
    public class PlacementData
    {
        public string videoUrl;
        public string zoneName;
        public Vector3Data position;
        public QuaternionData rotation;
        public string prefabType;
        public string title;
        public bool isManuallyPlaced = false;
        public long lastModifiedTimestamp;

        public PlacementData() { }

        public PlacementData(VideoEntry video, Transform transform)
        {
            videoUrl = video.PublicUrl;
            zoneName = video.Zones.Count > 0 ? video.Zones[0] : "";
            position = new Vector3Data(transform.position);
            rotation = new QuaternionData(transform.rotation);
            prefabType = video.Prefab;
            title = video.Title;
            lastModifiedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }

    [System.Serializable]
    public class Vector3Data
    {
        public float x;
        public float y;
        public float z;

        public Vector3Data() { }

        public Vector3Data(Vector3 vector)
        {
            x = vector.x;
            y = vector.y;
            z = vector.z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }

    [System.Serializable]
    public class QuaternionData
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public QuaternionData() { }

        public QuaternionData(Quaternion quaternion)
        {
            x = quaternion.x;
            y = quaternion.y;
            z = quaternion.z;
            w = quaternion.w;
        }

        public Quaternion ToQuaternion()
        {
            return new Quaternion(x, y, z, w);
        }
    }

    [System.Serializable]
    public class SceneCache
    {
        public string sceneName;
        public List<PlacementData> placementData = new List<PlacementData>();
        public long lastSavedTimestamp;
    }

    [Header("Cache Settings")]
    [SerializeField] private string cacheFilePath = "Assets/Resources/VideoPlacementCache.json";
    [SerializeField] private bool saveOnApplicationQuit = true;
    [SerializeField] private bool debugMode = false;

    private SceneCache currentCache = new SceneCache();
    private bool isDirty = false;

    // Singleton pattern
    private static VideoPlacementCache _instance;
    public static VideoPlacementCache Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<VideoPlacementCache>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("VideoPlacementCache");
                    _instance = obj.AddComponent<VideoPlacementCache>();
                    DontDestroyOnLoad(obj);
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        // Singleton setup
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Initialize cache
        InitializeCache();
    }

    private void OnApplicationQuit()
    {
        if (saveOnApplicationQuit && isDirty)
        {
            SaveCache();
        }
    }

    private void InitializeCache()
    {
        currentCache.sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        LoadCache();
    }

    /// <summary>
    /// Load the placement cache from disk
    /// </summary>
    public void LoadCache()
    {
        try
        {
            string fullPath = Path.GetFullPath(cacheFilePath);

            if (!File.Exists(fullPath))
            {
                if (debugMode) Debug.Log($"Cache file not found at {fullPath}. Starting with empty cache.");
                currentCache.placementData.Clear();
                currentCache.lastSavedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                return;
            }

            string json = File.ReadAllText(fullPath);
            currentCache = JsonUtility.FromJson<SceneCache>(json);

            if (currentCache == null)
            {
                currentCache = new SceneCache();
                currentCache.sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                currentCache.placementData = new List<PlacementData>();
                currentCache.lastSavedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }

            if (debugMode) Debug.Log($"Loaded placement cache with {currentCache.placementData.Count} entries");

            isDirty = false;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading placement cache: {e.Message}");

            // Reset to empty cache
            currentCache = new SceneCache();
            currentCache.sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            currentCache.placementData = new List<PlacementData>();
            currentCache.lastSavedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }

    /// <summary>
    /// Save the current placement cache to disk
    /// </summary>
    public void SaveCache()
    {
        try
        {
            // Update timestamp
            currentCache.lastSavedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Create directory if it doesn't exist
            string directory = Path.GetDirectoryName(cacheFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonUtility.ToJson(currentCache, true);
            File.WriteAllText(cacheFilePath, json);

            if (debugMode) Debug.Log($"Saved placement cache with {currentCache.placementData.Count} entries");

            isDirty = false;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error saving placement cache: {e.Message}");
        }
    }

    /// <summary>
    /// Update or create a placement entry for a video
    /// </summary>
    public void UpdatePlacement(VideoEntry video, Transform transform, bool isManual = false)
    {
        if (video == null || transform == null) return;

        // Find existing entry
        PlacementData existingData = currentCache.placementData.Find(p => p.videoUrl == video.PublicUrl);

        if (existingData != null)
        {
            // Update existing entry
            existingData.position = new Vector3Data(transform.position);
            existingData.rotation = new QuaternionData(transform.rotation);

            if (isManual)
            {
                existingData.isManuallyPlaced = true;
            }

            existingData.lastModifiedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
        else
        {
            // Create new entry
            PlacementData newData = new PlacementData(video, transform);
            newData.isManuallyPlaced = isManual;
            currentCache.placementData.Add(newData);
        }

        isDirty = true;
    }

    /// <summary>
    /// Get the placement data for a video if it exists
    /// </summary>
    public bool TryGetPlacement(string videoUrl, out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;

        if (string.IsNullOrEmpty(videoUrl)) return false;

        PlacementData data = currentCache.placementData.Find(p => p.videoUrl == videoUrl);

        if (data != null)
        {
            position = data.position.ToVector3();
            rotation = data.rotation.ToQuaternion();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Remove a video from the cache
    /// </summary>
    public void RemoveVideo(string videoUrl)
    {
        if (string.IsNullOrEmpty(videoUrl)) return;

        int index = currentCache.placementData.FindIndex(p => p.videoUrl == videoUrl);

        if (index >= 0)
        {
            currentCache.placementData.RemoveAt(index);
            isDirty = true;
        }
    }

    /// <summary>
    /// Clear all cached placements
    /// </summary>
    public void ClearCache()
    {
        currentCache.placementData.Clear();
        currentCache.lastSavedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        isDirty = true;

        if (debugMode) Debug.Log("Cleared placement cache");
    }

    /// <summary>
    /// Get all placement data for a specific zone
    /// </summary>
    public List<PlacementData> GetPlacementsForZone(string zoneName)
    {
        if (string.IsNullOrEmpty(zoneName)) return new List<PlacementData>();

        return currentCache.placementData.FindAll(p => p.zoneName == zoneName);
    }

    /// <summary>
    /// Get all manually placed videos
    /// </summary>
    public List<PlacementData> GetManualPlacements()
    {
        return currentCache.placementData.FindAll(p => p.isManuallyPlaced);
    }
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(VideoPlacementCache))]
public class VideoPlacementCacheEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        VideoPlacementCache cache = (VideoPlacementCache)target;

        UnityEditor.EditorGUILayout.Space(10);

        if (GUILayout.Button("Load Cache"))
        {
            cache.LoadCache();
        }

        if (GUILayout.Button("Save Cache"))
        {
            cache.SaveCache();
        }

        if (GUILayout.Button("Clear Cache"))
        {
            if (UnityEditor.EditorUtility.DisplayDialog("Clear Cache",
                "Are you sure you want to clear the video placement cache?",
                "Yes", "No"))
            {
                cache.ClearCache();
            }
        }

        UnityEditor.EditorGUILayout.HelpBox(
            "This component manages saving and loading of video placement data.\n" +
            "It automatically caches positions when videos are manually moved and\n" +
            "restores them when the scene is loaded.",
            UnityEditor.MessageType.Info);
    }
}
#endif