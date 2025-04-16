using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using TMPro;
using UnityEditor;

// Data model for a video entry
[System.Serializable]
public class VideoEntry
{
    public string FileName;           // Raw filename
    public string PublicUrl;          // Complete URL to the video
    public string BucketPath;         // Google Cloud Storage path
    public string Title;              // Display title
    public string Description;        // Description text
    public string Prefab;             // Which prefab to use for displaying
    public string Zone;              // Direct zone assignment (string format)
    public List<string> Zones = new List<string>();  // Parsed zones list for easier access

    [NonSerialized]
    private string _category;
    [NonSerialized]
    private string _subCategory;

    // Calculated fields
    public string Category
    {
        get
        {
            if (string.IsNullOrEmpty(_category))
                _category = GetCategory();
            return _category;
        }
    }

    public string SubCategory
    {
        get
        {
            if (string.IsNullOrEmpty(_subCategory))
                _subCategory = GetSubCategory();
            return _subCategory;
        }
    }

    // Parse the category from the URL path
    private string GetCategory()
    {
        if (string.IsNullOrEmpty(PublicUrl)) return "";

        string path = GetPathFromUrl(PublicUrl);
        string[] parts = path.Split('/');
        if (parts.Length >= 2)
        {
            return parts[0];
        }
        return "";
    }

    // Parse the subcategory from the URL path
    private string GetSubCategory()
    {
        if (string.IsNullOrEmpty(PublicUrl)) return "";

        string path = GetPathFromUrl(PublicUrl);
        string[] parts = path.Split('/');
        if (parts.Length >= 3)
        {
            return parts[1];
        }
        return "";
    }

    // Extract path portion from URL
    private string GetPathFromUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return "";

        // Remove base domain
        int startIndex = url.IndexOf("masterchange/");
        if (startIndex >= 0)
        {
            string path = url.Substring(startIndex + "masterchange/".Length);
            return path;
        }

        return "";
    }
}

// Complete database collection
[System.Serializable]
public class VideoDatabase
{
    public List<VideoEntry> Entries = new List<VideoEntry>();

    // This Dictionary can't be serialized directly by Unity's JsonUtility, so we'll rebuild it after loading
    [NonSerialized]
    public Dictionary<string, List<string>> CategoryToSubCategories = new Dictionary<string, List<string>>();

    // This Dictionary maps zone names to lists of videos assigned to that zone
    [NonSerialized]
    public Dictionary<string, List<VideoEntry>> ZoneToVideos = new Dictionary<string, List<VideoEntry>>();

    // Helper method to build the category lookup
    public void BuildCategoryLookup()
    {
        CategoryToSubCategories.Clear();

        foreach (var entry in Entries)
        {
            string category = entry.Category;
            string subCategory = entry.SubCategory;

            if (!string.IsNullOrEmpty(category) && !string.IsNullOrEmpty(subCategory))
            {
                if (!CategoryToSubCategories.ContainsKey(category))
                {
                    CategoryToSubCategories[category] = new List<string>();
                }

                if (!CategoryToSubCategories[category].Contains(subCategory))
                {
                    CategoryToSubCategories[category].Add(subCategory);
                }
            }
        }
    }

    // Helper method to build the zone lookup and parse zone strings
    public void BuildZoneLookup()
    {
        ZoneToVideos.Clear();

        foreach (var entry in Entries)
        {
            // If we have a Zone string but the Zones list is empty, parse it
            if (!string.IsNullOrEmpty(entry.Zone) && entry.Zones.Count == 0)
            {
                // Parse comma-separated zones and add to the list
                string[] zoneArray = entry.Zone.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string zone in zoneArray)
                {
                    string trimmedZone = zone.Trim();
                    if (!string.IsNullOrEmpty(trimmedZone) && !entry.Zones.Contains(trimmedZone))
                    {
                        entry.Zones.Add(trimmedZone);
                    }
                }
            }

            // Build the lookup dictionary
            foreach (string zoneName in entry.Zones)
            {
                if (!ZoneToVideos.ContainsKey(zoneName))
                {
                    ZoneToVideos[zoneName] = new List<VideoEntry>();
                }

                if (!ZoneToVideos[zoneName].Contains(entry))
                {
                    ZoneToVideos[zoneName].Add(entry);
                }
            }
        }
    }

    // Get all entries for a specific category
    public List<VideoEntry> GetEntriesForCategory(string category, string subCategory = null)
    {
        return Entries.Where(e =>
            e.Category == category &&
            (subCategory == null || e.SubCategory == subCategory)
        ).ToList();
    }

    // Get entries that match a search term
    public List<VideoEntry> SearchEntries(string searchTerm)
    {
        if (string.IsNullOrEmpty(searchTerm))
            return new List<VideoEntry>();

        return Entries.Where(e =>
            (e.Title != null && e.Title.ToLower().Contains(searchTerm.ToLower())) ||
            (e.Description != null && e.Description.ToLower().Contains(searchTerm.ToLower())) ||
            (e.Category != null && e.Category.ToLower().Contains(searchTerm.ToLower())) ||
            (e.SubCategory != null && e.SubCategory.ToLower().Contains(searchTerm.ToLower()))
        ).ToList();
    }
}

public class VideoDatabaseManager : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private string databaseFilePath = "Assets/Resources/film_data.json";
    [SerializeField] public TextAsset cloudDatabaseFile; // Changed to public
    [SerializeField] private bool parseOnAwake = true;

    [Header("Database Options")]
    [SerializeField] private bool useLocalJson = true;
    [SerializeField] private bool autoSaveJson = true;
    [SerializeField] private bool logDatabaseInfo = true;

    [Header("Remote Loading (Optional)")]
    [SerializeField] private string remoteDatabaseUrl = "";
    [SerializeField] private bool tryRemoteFirst = false;

    private VideoDatabase database = new VideoDatabase();
    private bool databaseInitialized = false;

    // Singleton pattern for easy access
    private static VideoDatabaseManager _instance;
    public static VideoDatabaseManager Instance => _instance;

    // Public properties
    public int EntryCount => database.Entries.Count;
    public bool IsInitialized => databaseInitialized;

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

        if (parseOnAwake)
        {
            // Choose loading method
            if (tryRemoteFirst && !string.IsNullOrEmpty(remoteDatabaseUrl))
            {
                StartCoroutine(DownloadDatabase());
            }
            else if (useLocalJson && File.Exists(databaseFilePath))
            {
                LoadDatabaseFromJson();
            }
            else if (cloudDatabaseFile != null)
            {
                ParseCloudDatabase();
            }
            else
            {
                Debug.LogWarning("No database source available!");
            }
        }
    }

    // Download database from remote URL
    private IEnumerator DownloadDatabase()
    {
        using (UnityWebRequest www = UnityWebRequest.Get(remoteDatabaseUrl))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string text = www.downloadHandler.text;
                ParseDatabaseText(text);
            }
            else
            {
                Debug.LogError($"Failed to download database: {www.error}");

                // Fall back to local options
                if (useLocalJson && File.Exists(databaseFilePath))
                {
                    LoadDatabaseFromJson();
                }
                else if (cloudDatabaseFile != null)
                {
                    ParseCloudDatabase();
                }
            }
        }
    }

    // Parse database from string content
    private void ParseDatabaseText(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            Debug.LogError("Database content is empty!");
            return;
        }

        database.Entries.Clear();

        string[] lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        // Skip the header line
        for (int i = 1; i < lines.Length; i++)
        {
            ProcessDatabaseLine(lines[i]);
        }

        // Build category and zone lookups
        database.BuildCategoryLookup();
        database.BuildZoneLookup();

        if (logDatabaseInfo)
        {
            Debug.Log($"Parsed {database.Entries.Count} video entries from text content");
            Debug.Log($"Found {database.CategoryToSubCategories.Count} categories");
            Debug.Log($"Found {database.ZoneToVideos.Count} zones");
        }

        databaseInitialized = true;

        // Auto save if enabled
        if (autoSaveJson)
        {
            SaveDatabaseToJson();
        }
    }

    // Parse the tab-delimited cloud storage database
    public void ParseCloudDatabase()
    {
        if (cloudDatabaseFile == null)
        {
            Debug.LogError("No cloud database file assigned!");
            return;
        }

        ParseDatabaseText(cloudDatabaseFile.text);
    }

    // Process a single line from the database
    private void ProcessDatabaseLine(string line)
    {
        line = line.Trim();
        if (string.IsNullOrEmpty(line)) return;

        string[] columns = line.Split('\t');
        if (columns.Length < 3) return;

        // Skip directory entries or empty URLs
        if (string.IsNullOrEmpty(columns[1]) || columns[1].EndsWith("/") || columns[1].EndsWith(":"))
            return;

        // Create new entry
        VideoEntry entry = new VideoEntry
        {
            FileName = columns[0],
            PublicUrl = columns[1],
            BucketPath = columns.Length > 2 ? columns[2] : ""
        };

        // Title, Description, Prefab if available
        if (columns.Length > 3 && !string.IsNullOrEmpty(columns[3]))
            entry.Title = columns[3];

        if (columns.Length > 4)
            entry.Description = columns[4];

        if (columns.Length > 5 && !string.IsNullOrEmpty(columns[5]))
            entry.Prefab = columns[5];
        else
            entry.Prefab = "Default"; // Default prefab

        // Parse zones if available
        if (columns.Length > 6 && !string.IsNullOrEmpty(columns[6]))
        {
            entry.Zone = columns[6];

            // Parse comma-separated zones
            string[] zoneArray = columns[6].Split(',');
            foreach (string zone in zoneArray)
            {
                string trimmedZone = zone.Trim();
                if (!string.IsNullOrEmpty(trimmedZone))
                {
                    entry.Zones.Add(trimmedZone);
                }
            }
        }

        database.Entries.Add(entry);
    }

    // Save the database to JSON
    public void SaveDatabaseToJson()
    {
        try
        {
            string json = JsonUtility.ToJson(database, true);
            File.WriteAllText(databaseFilePath, json);

            if (logDatabaseInfo)
            {
                Debug.Log($"Saved database with {database.Entries.Count} entries to {databaseFilePath}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error saving database: {ex.Message}");
        }
    }

    // Load the database from JSON
    public void LoadDatabaseFromJson()
    {
        try
        {
            if (!File.Exists(databaseFilePath))
            {
                Debug.LogWarning($"Database file not found at {databaseFilePath}");
                return;
            }

            string json = File.ReadAllText(databaseFilePath);

            try
            {
                database = JsonUtility.FromJson<VideoDatabase>(json);

                // Rebuild dictionaries (since they're not serialized)
                database.BuildCategoryLookup();
                database.BuildZoneLookup();

                databaseInitialized = true;

                if (logDatabaseInfo)
                {
                    Debug.Log($"Loaded database with {database.Entries.Count} entries from {databaseFilePath}");
                    Debug.Log($"Found {database.CategoryToSubCategories.Count} categories");
                    Debug.Log($"Found {database.ZoneToVideos.Count} zones");

                    // Log zones info
                    foreach (var zonePair in database.ZoneToVideos)
                    {
                        Debug.Log($"Zone '{zonePair.Key}' has {zonePair.Value.Count} videos");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading database: JSON must represent an object type. {ex.Message}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error loading database: {ex.Message}");
        }
    }

    // Get all entries 
    public List<VideoEntry> GetAllEntries()
    {
        return database.Entries;
    }

    // Get entries for a specific zone
    public List<VideoEntry> GetEntriesForZone(string zone)
    {
        if (database.ZoneToVideos.TryGetValue(zone, out List<VideoEntry> entries))
        {
            return entries;
        }

        // Fallback to manual search if dictionary lookup fails
        return database.Entries.Where(e => e.Zones.Contains(zone)).ToList();
    }

    // Get entries for a specific category
    public List<VideoEntry> GetEntriesForCategory(string category, string subCategory = null)
    {
        return database.GetEntriesForCategory(category, subCategory);
    }

    // Get all available categories
    public List<string> GetAllCategories()
    {
        return database.CategoryToSubCategories.Keys.ToList();
    }

    // Get subcategories for a specific category
    public List<string> GetSubcategories(string category)
    {
        if (database.CategoryToSubCategories.ContainsKey(category))
        {
            return database.CategoryToSubCategories[category];
        }
        return new List<string>();
    }

    // Search the database
    public List<VideoEntry> SearchVideos(string searchTerm)
    {
        return database.SearchEntries(searchTerm);
    }

    // Get entry by URL
    public VideoEntry GetEntryByUrl(string url)
    {
        return database.Entries.FirstOrDefault(e => e.PublicUrl == url);
    }

    // Assign a video to a zone
    public void AssignVideoToZone(string videoUrl, string zone)
    {
        VideoEntry entry = database.Entries.FirstOrDefault(e => e.PublicUrl == videoUrl);
        if (entry != null && !entry.Zones.Contains(zone))
        {
            entry.Zones.Add(zone);

            // Update Zone string to match
            if (string.IsNullOrEmpty(entry.Zone))
                entry.Zone = zone;
            else
                entry.Zone += "," + zone;

            // Update the lookup dictionary
            if (!database.ZoneToVideos.ContainsKey(zone))
                database.ZoneToVideos[zone] = new List<VideoEntry>();

            if (!database.ZoneToVideos[zone].Contains(entry))
                database.ZoneToVideos[zone].Add(entry);

            // Auto save if enabled
            if (autoSaveJson)
            {
                SaveDatabaseToJson();
            }
        }
    }

    // Remove a video from a zone
    public void RemoveVideoFromZone(string videoUrl, string zone)
    {
        VideoEntry entry = database.Entries.FirstOrDefault(e => e.PublicUrl == videoUrl);
        if (entry != null && entry.Zones.Contains(zone))
        {
            entry.Zones.Remove(zone);

            // Update Zone string to match
            entry.Zone = string.Join(",", entry.Zones);

            // Update the lookup dictionary
            if (database.ZoneToVideos.ContainsKey(zone))
            {
                database.ZoneToVideos[zone].Remove(entry);
            }

            // Auto save if enabled
            if (autoSaveJson)
            {
                SaveDatabaseToJson();
            }
        }
    }

    // Get random videos from category
    public List<VideoEntry> GetRandomVideos(string category, int count)
    {
        List<VideoEntry> categoryVideos = GetEntriesForCategory(category);

        if (categoryVideos.Count <= count)
            return categoryVideos;

        // Shuffle and take
        List<VideoEntry> randomVideos = new List<VideoEntry>();
        List<int> indexes = new List<int>();

        for (int i = 0; i < categoryVideos.Count; i++)
            indexes.Add(i);

        // Fisher-Yates shuffle
        for (int i = 0; i < count; i++)
        {
            int r = UnityEngine.Random.Range(i, indexes.Count);
            int temp = indexes[i];
            indexes[i] = indexes[r];
            indexes[r] = temp;
        }

        for (int i = 0; i < count; i++)
        {
            randomVideos.Add(categoryVideos[indexes[i]]);
        }

        return randomVideos;
    }

    // Add a new entry to the database
    public void AddEntry(VideoEntry entry)
    {
        if (entry == null)
            return;

        // Check if this entry already exists
        bool exists = database.Entries.Any(e => e.PublicUrl == entry.PublicUrl);

        if (!exists)
        {
            database.Entries.Add(entry);

            // Rebuild lookups
            database.BuildCategoryLookup();
            database.BuildZoneLookup();

            // Auto save if enabled
            if (autoSaveJson)
            {
                SaveDatabaseToJson();
            }
        }
    }

    // Clear all entries (useful when reimporting)
    public void ClearAllEntries()
    {
        database.Entries.Clear();
        database.CategoryToSubCategories.Clear();
        database.ZoneToVideos.Clear();
        databaseInitialized = false;

        if (autoSaveJson)
        {
            SaveDatabaseToJson();
        }
    }

    // Get all unique zones used in the database
    public List<string> GetAllZones()
    {
        return database.ZoneToVideos.Keys.ToList();
    }
}

// Editor utility for the database manager
#if UNITY_EDITOR
[CustomEditor(typeof(VideoDatabaseManager))]
public class VideoDatabaseManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        VideoDatabaseManager manager = (VideoDatabaseManager)target;

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Parse Cloud Database"))
        {
            manager.ParseCloudDatabase();
        }

        if (GUILayout.Button("Load From JSON"))
        {
            manager.LoadDatabaseFromJson();
        }

        if (GUILayout.Button("Save To JSON"))
        {
            manager.SaveDatabaseToJson();
        }

        EditorGUILayout.Space(5);

        if (GUILayout.Button("Clear All Entries"))
        {
            if (EditorUtility.DisplayDialog("Clear Database",
                "Are you sure you want to clear all entries from the database?", "Yes", "No"))
            {
                manager.ClearAllEntries();
            }
        }

        EditorGUILayout.Space(10);

        // Display database info
        if (manager.IsInitialized)
        {
            EditorGUILayout.LabelField("Database Information", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Total Entries: {manager.EntryCount}");

            EditorGUILayout.LabelField("Categories:");
            EditorGUI.indentLevel++;
            foreach (string category in manager.GetAllCategories())
            {
                EditorGUILayout.LabelField(category);

                EditorGUI.indentLevel++;
                foreach (string subCategory in manager.GetSubcategories(category))
                {
                    EditorGUILayout.LabelField(subCategory);
                }
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Zones:");
            EditorGUI.indentLevel++;
            foreach (string zone in manager.GetAllZones())
            {
                int entryCount = manager.GetEntriesForZone(zone).Count;
                EditorGUILayout.LabelField($"{zone} ({entryCount} videos)");
            }
            EditorGUI.indentLevel--;
        }
    }
}
#endif