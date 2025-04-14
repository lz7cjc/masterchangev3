using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;

#if UNITY_EDITOR
public class VideoDatabaseEditor : EditorWindow
{
    private VideoDatabaseManager databaseManager;
    private PolygonZoneManager zoneManager;
    private Vector2 scrollPosition;
    private List<VideoEntry> videoEntries = new List<VideoEntry>();
    private List<string> allZones = new List<string>();
    private List<string> availablePrefabs = new List<string>() {
        "Default", "Ticket", "Canal boat", "Picture Frame",
        "Bicycle", "Buddha", "Kite"
    };
    private string searchFilter = "";
    private bool showAllVideos = true;
    private bool showOnlyAssignedVideos = false;
    private bool showOnlyUnassignedVideos = false;
    private int selectedCategoryIndex = 0;
    private List<string> categoryList = new List<string>();

    [MenuItem("Tools/Video System/Video Database Editor")]
    public static void ShowWindow()
    {
        GetWindow<VideoDatabaseEditor>("Video Database Editor");
    }

    private void OnEnable()
    {
        // Find the required components
        databaseManager = FindObjectOfType<VideoDatabaseManager>();
        zoneManager = FindObjectOfType<PolygonZoneManager>();

        RefreshData();
    }

    private void RefreshData()
    {
        if (databaseManager == null)
        {
            return;
        }

        // Load or reload video entries
        if (!databaseManager.IsInitialized)
        {
            // Try to load from JSON first
            databaseManager.LoadDatabaseFromJson();
        }

        videoEntries = databaseManager.GetAllEntries();

        // Get all zone names
        allZones.Clear();

        // First add zones from the zone manager
        if (zoneManager != null && zoneManager.zones != null)
        {
            foreach (var zone in zoneManager.zones)
            {
                if (!string.IsNullOrEmpty(zone.Name) && !allZones.Contains(zone.Name))
                {
                    allZones.Add(zone.Name);
                }
            }
        }

        // Then add any additional zones mentioned in video entries
        foreach (var entry in videoEntries)
        {
            if (entry.Zones != null)
            {
                foreach (var zone in entry.Zones)
                {
                    if (!string.IsNullOrEmpty(zone) && !allZones.Contains(zone))
                    {
                        allZones.Add(zone);
                    }
                }
            }
        }

        // Sort zones alphabetically
        allZones.Sort();

        // Get all categories
        categoryList.Clear();
        categoryList.Add("All Categories");

        HashSet<string> categories = new HashSet<string>();
        foreach (var entry in videoEntries)
        {
            if (!string.IsNullOrEmpty(entry.Category))
            {
                categories.Add(entry.Category);
            }
        }

        categoryList.AddRange(categories.OrderBy(c => c));
    }

    private void OnGUI()
    {
        GUILayout.Label("Video Database Editor", EditorStyles.boldLabel);

        // Ensure we have a database manager
        if (databaseManager == null)
        {
            EditorGUILayout.HelpBox("No VideoDatabaseManager found in the scene!", MessageType.Error);
            if (GUILayout.Button("Create Database Manager"))
            {
                GameObject obj = new GameObject("VideoDatabaseManager");
                databaseManager = obj.AddComponent<VideoDatabaseManager>();
                RefreshData();
            }
            return;
        }

        // Ensure we have a zone manager
        if (zoneManager == null)
        {
            EditorGUILayout.HelpBox("No PolygonZoneManager found in the scene!", MessageType.Warning);
            if (GUILayout.Button("Create Zone Manager"))
            {
                GameObject obj = new GameObject("PolygonZoneManager");
                zoneManager = obj.AddComponent<PolygonZoneManager>();
                RefreshData();
            }
        }

        // Database actions
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Reload Database"))
        {
            databaseManager.LoadDatabaseFromJson();
            RefreshData();
        }

        if (GUILayout.Button("Save Database"))
        {
            databaseManager.SaveDatabaseToJson();
        }

        if (GUILayout.Button("Generate Video Links"))
        {
            if (zoneManager != null)
            {
                zoneManager.GenerateVideoLinks();
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "No PolygonZoneManager found in the scene!", "OK");
            }
        }

        EditorGUILayout.EndHorizontal();

        // Import TextAsset
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        var textAsset = (TextAsset)EditorGUILayout.ObjectField(
            "Import Database File", null, typeof(TextAsset), false);

        if (textAsset != null)
        {
            databaseManager.cloudDatabaseFile = textAsset;
            databaseManager.ParseCloudDatabase();
            RefreshData();
        }

        EditorGUILayout.EndHorizontal();

        // Import JSON file using FilmJsonImporter
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        var jsonFile = (TextAsset)EditorGUILayout.ObjectField(
            "Import JSON File", null, typeof(TextAsset), false);

        if (jsonFile != null)
        {
            // Check if FilmJsonImporter exists
            var importer = FindObjectOfType<FilmJsonImporter>();
            if (importer == null)
            {
                if (EditorUtility.DisplayDialog("Create Film JSON Importer",
                    "No FilmJsonImporter found in the scene. Would you like to create one?",
                    "Yes", "No"))
                {
                    GameObject obj = new GameObject("FilmJsonImporter");
                    importer = obj.AddComponent<FilmJsonImporter>();
                }
            }

            if (importer != null)
            {
                // Set the JSON file and trigger import
                var serializedImporter = new SerializedObject(importer);
                serializedImporter.FindProperty("jsonFile").objectReferenceValue = jsonFile;
                serializedImporter.ApplyModifiedProperties();

                importer.ImportJson();
                RefreshData();
            }
        }

        EditorGUILayout.EndHorizontal();

        // Search and filters
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Filters", EditorStyles.boldLabel);

        // Search box
        searchFilter = EditorGUILayout.TextField("Search:", searchFilter);

        // Category filter
        selectedCategoryIndex = EditorGUILayout.Popup("Category:", selectedCategoryIndex, categoryList.ToArray());

        // Video status filter
        EditorGUILayout.BeginHorizontal();
        bool newShowAllVideos = EditorGUILayout.ToggleLeft("Show All", showAllVideos, GUILayout.Width(100));
        bool newShowOnlyAssignedVideos = EditorGUILayout.ToggleLeft("Only Assigned", showOnlyAssignedVideos, GUILayout.Width(120));
        bool newShowOnlyUnassignedVideos = EditorGUILayout.ToggleLeft("Only Unassigned", showOnlyUnassignedVideos, GUILayout.Width(130));
        EditorGUILayout.EndHorizontal();

        // Handle filter logic
        if (newShowAllVideos != showAllVideos)
        {
            showAllVideos = newShowAllVideos;
            if (showAllVideos)
            {
                showOnlyAssignedVideos = false;
                showOnlyUnassignedVideos = false;
            }
        }
        else if (newShowOnlyAssignedVideos != showOnlyAssignedVideos)
        {
            showOnlyAssignedVideos = newShowOnlyAssignedVideos;
            if (showOnlyAssignedVideos)
            {
                showAllVideos = false;
                showOnlyUnassignedVideos = false;
            }
        }
        else if (newShowOnlyUnassignedVideos != showOnlyUnassignedVideos)
        {
            showOnlyUnassignedVideos = newShowOnlyUnassignedVideos;
            if (showOnlyUnassignedVideos)
            {
                showAllVideos = false;
                showOnlyAssignedVideos = false;
            }
        }

        // If none selected, default to show all
        if (!showAllVideos && !showOnlyAssignedVideos && !showOnlyUnassignedVideos)
        {
            showAllVideos = true;
        }

        // Video entry list
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Video Entries", EditorStyles.boldLabel);

        // Stats about video entries
        EditorGUILayout.LabelField($"Total Videos: {videoEntries.Count}", EditorStyles.boldLabel);
        int assignedCount = videoEntries.Count(v => v.Zones != null && v.Zones.Count > 0);
        EditorGUILayout.LabelField($"Assigned to Zones: {assignedCount}", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        var filteredEntries = videoEntries.Where(entry =>
        {
            // Apply search filter
            bool matchesSearch = string.IsNullOrEmpty(searchFilter) ||
                                 (entry.Title != null && entry.Title.ToLower().Contains(searchFilter.ToLower())) ||
                                 (entry.PublicUrl != null && entry.PublicUrl.ToLower().Contains(searchFilter.ToLower()));

            // Apply category filter
            bool matchesCategory = selectedCategoryIndex == 0 || // "All Categories"
                                  (entry.Category == categoryList[selectedCategoryIndex]);

            // Apply assigned/unassigned filter
            bool matchesAssignmentFilter = showAllVideos ||
                                          (showOnlyAssignedVideos && entry.Zones != null && entry.Zones.Count > 0) ||
                                          (showOnlyUnassignedVideos && (entry.Zones == null || entry.Zones.Count == 0));

            return matchesSearch && matchesCategory && matchesAssignmentFilter;
        }).ToList();

        if (filteredEntries.Count == 0)
        {
            EditorGUILayout.HelpBox("No videos match the current filters.", MessageType.Info);
        }

        for (int i = 0; i < filteredEntries.Count; i++)
        {
            var entry = filteredEntries[i];
            bool isExpanded = EditorGUILayout.Foldout(SessionState.GetBool($"VideoFoldout_{i}", false),
                                                    GetVideoDisplayTitle(entry), true);
            SessionState.SetBool($"VideoFoldout_{i}", isExpanded);

            if (isExpanded)
            {
                EditorGUI.indentLevel++;

                // URL (read-only)
                EditorGUILayout.LabelField("Full Path:", entry.PublicUrl);

                // Title
                entry.Title = EditorGUILayout.TextField("Title:", entry.Title ?? "");

                // Description
                entry.Description = EditorGUILayout.TextField("Description:", entry.Description ?? "");

                // Prefab
                int prefabIndex = availablePrefabs.IndexOf(entry.Prefab ?? "Default");
                if (prefabIndex < 0) prefabIndex = 0;

                int newPrefabIndex = EditorGUILayout.Popup("Prefab:", prefabIndex, availablePrefabs.ToArray());
                if (newPrefabIndex != prefabIndex)
                {
                    entry.Prefab = availablePrefabs[newPrefabIndex];
                }

                // Category and Subcategory (read-only)
                EditorGUILayout.LabelField("Category:", entry.Category ?? "");
                EditorGUILayout.LabelField("Subcategory:", entry.SubCategory ?? "");

                // Zone assignments
                EditorGUILayout.LabelField("Zones:", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;

                if (entry.Zones == null)
                {
                    entry.Zones = new List<string>();
                }

                // Display current zones with remove buttons
                for (int j = entry.Zones.Count - 1; j >= 0; j--)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(entry.Zones[j]);

                    if (GUILayout.Button("X", GUILayout.Width(30)))
                    {
                        entry.Zones.RemoveAt(j);
                        // Mark database as dirty so changes are saved
                        EditorUtility.SetDirty(databaseManager);
                    }

                    EditorGUILayout.EndHorizontal();
                }

                // Add new zone
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Add Zone:", GUILayout.Width(80));

                // Dropdown for available zones
                int selectedZoneIndex = -1;
                string[] availableZones = allZones
                    .Where(z => !entry.Zones.Contains(z))
                    .ToArray();

                if (availableZones.Length > 0)
                {
                    selectedZoneIndex = EditorGUILayout.Popup(selectedZoneIndex, availableZones);

                    if (selectedZoneIndex >= 0 && GUILayout.Button("Add", GUILayout.Width(60)))
                    {
                        entry.Zones.Add(availableZones[selectedZoneIndex]);
                        // Mark database as dirty so changes are saved
                        EditorUtility.SetDirty(databaseManager);
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("No more zones available");
                }

                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel--;

                EditorGUILayout.Space(5);
                EditorGUI.indentLevel--;
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private string GetVideoDisplayTitle(VideoEntry entry)
    {
        string title = string.IsNullOrEmpty(entry.Title)
            ? Path.GetFileNameWithoutExtension(entry.FileName)
            : entry.Title;

        // Add zone info to title
        string zoneInfo = entry.Zones != null && entry.Zones.Count > 0
            ? $" ({entry.Zones.Count} zones)"
            : " (no zones)";

        return title + zoneInfo;
    }
}
#endif