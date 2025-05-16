using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

#if UNITY_EDITOR
/// <summary>
/// Editor window to manage video zone assignments
/// </summary>
public class ZoneAssignmentEditor : EditorWindow
{
    private VideoDatabaseManager databaseManager;
    private PolygonZoneManager zoneManager;
    private AdvancedVideoPlacementManager placementManager;

    private Vector2 scrollPosition;
    private string searchQuery = "";
    private int selectedTabIndex = 0;
    private Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();
    private Dictionary<string, bool> videoFoldoutStates = new Dictionary<string, bool>();
    private List<VideoEntry> filteredVideos = new List<VideoEntry>();
    private GUIStyle headerStyle;
    private GUIStyle subHeaderStyle;
    private GUIStyle italicLabelStyle; // Added new style for italic labels

    // Create menu item
    [MenuItem("Tools/Video Management/Zone Assignment Editor")]
    public static void ShowWindow()
    {
        ZoneAssignmentEditor window = GetWindow<ZoneAssignmentEditor>("Zone Assignment Editor");
        window.minSize = new Vector2(400, 300);
        window.Show();
    }

    private void OnEnable()
    {
        // Find required components
        databaseManager = FindObjectOfType<VideoDatabaseManager>();
        zoneManager = FindObjectOfType<PolygonZoneManager>();
        placementManager = FindObjectOfType<AdvancedVideoPlacementManager>();

        // Set up styles
        headerStyle = new GUIStyle();
        headerStyle.fontSize = 14;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
        headerStyle.margin = new RectOffset(5, 5, 10, 10);

        subHeaderStyle = new GUIStyle();
        subHeaderStyle.fontSize = 12;
        subHeaderStyle.fontStyle = FontStyle.Bold;
        subHeaderStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
        subHeaderStyle.margin = new RectOffset(5, 5, 5, 5);

        // Create custom italic label style
        italicLabelStyle = new GUIStyle(EditorStyles.label);
        italicLabelStyle.fontStyle = FontStyle.Italic;
    }

    private void OnGUI()
    {
        if (databaseManager == null)
        {
            databaseManager = FindObjectOfType<VideoDatabaseManager>();
            if (databaseManager == null)
            {
                EditorGUILayout.HelpBox("VideoDatabaseManager not found in the scene!", MessageType.Error);
                if (GUILayout.Button("Find VideoDatabaseManager"))
                {
                    databaseManager = FindObjectOfType<VideoDatabaseManager>();
                }
                return;
            }
        }

        if (zoneManager == null)
        {
            zoneManager = FindObjectOfType<PolygonZoneManager>();
            if (zoneManager == null)
            {
                EditorGUILayout.HelpBox("PolygonZoneManager not found in the scene!", MessageType.Warning);
            }
        }

        if (placementManager == null)
        {
            placementManager = FindObjectOfType<AdvancedVideoPlacementManager>();
        }

        // Top toolbar
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        // Refresh button
        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            ReloadData();
        }

        // Search field
        EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
        string newSearch = EditorGUILayout.TextField(searchQuery, EditorStyles.toolbarTextField);
        if (newSearch != searchQuery)
        {
            searchQuery = newSearch;
            FilterVideos();
        }

        // Add button to go to video placement manager
        if (GUILayout.Button("Place Videos", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            if (placementManager != null)
            {
                Selection.activeGameObject = placementManager.gameObject;
                EditorGUIUtility.PingObject(placementManager.gameObject);
            }
            else
            {
                EditorUtility.DisplayDialog("Manager Not Found",
                    "AdvancedVideoPlacementManager not found in the scene.", "OK");
            }
        }

        EditorGUILayout.EndHorizontal();

        // Tab selection
        string[] tabs = new string[] { "By Zone", "By Video" };
        selectedTabIndex = GUILayout.Toolbar(selectedTabIndex, tabs);

        EditorGUILayout.Space();

        // Display the selected tab content
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        if (selectedTabIndex == 0)
        {
            DrawZonesTab();
        }
        else
        {
            DrawVideosTab();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        // Bottom buttons
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Save Changes", GUILayout.Height(30)))
        {
            if (databaseManager != null)
            {
                databaseManager.SaveDatabaseToJson();
                EditorUtility.DisplayDialog("Saved", "Changes saved successfully.", "OK");
            }
        }

        if (GUILayout.Button("Apply & Regenerate", GUILayout.Height(30)))
        {
            if (databaseManager != null)
            {
                databaseManager.SaveDatabaseToJson();

                if (placementManager != null)
                {
                    placementManager.ClearAllVideos();
                    placementManager.PlaceAllVideos();
                    EditorUtility.DisplayDialog("Success",
                        "Changes saved and videos regenerated.", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Partial Success",
                        "Changes saved but videos could not be regenerated (PlacementManager not found).", "OK");
                }
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    private void ReloadData()
    {
        if (databaseManager != null)
        {
            databaseManager.LoadDatabaseFromJson();
            FilterVideos();
        }
    }

    private void FilterVideos()
    {
        if (databaseManager == null) return;

        filteredVideos.Clear();

        if (string.IsNullOrEmpty(searchQuery))
        {
            filteredVideos = databaseManager.GetAllEntries();
        }
        else
        {
            string query = searchQuery.ToLower();

            foreach (VideoEntry video in databaseManager.GetAllEntries())
            {
                if ((video.Title != null && video.Title.ToLower().Contains(query)) ||
                    (video.Description != null && video.Description.ToLower().Contains(query)) ||
                    (video.PublicUrl != null && video.PublicUrl.ToLower().Contains(query)) ||
                    (video.Category != null && video.Category.ToLower().Contains(query)) ||
                    (video.SubCategory != null && video.SubCategory.ToLower().Contains(query)))
                {
                    filteredVideos.Add(video);
                }
                else
                {
                    // Check zones
                    foreach (string zone in video.Zones)
                    {
                        if (zone.ToLower().Contains(query))
                        {
                            filteredVideos.Add(video);
                            break;
                        }
                    }
                }
            }
        }
    }

    private void DrawZonesTab()
    {
        if (databaseManager == null) return;

        // Get all zones
        List<string> allZones = databaseManager.GetAllZones();

        // Add a section for unassigned videos
        bool hasUnassigned = false;
        List<VideoEntry> allVideos = databaseManager.GetAllEntries();

        foreach (VideoEntry video in allVideos)
        {
            if (video.Zones.Count == 0)
            {
                hasUnassigned = true;
                break;
            }
        }

        if (hasUnassigned)
        {
            allZones.Add("Unassigned");
        }

        if (allZones.Count == 0)
        {
            EditorGUILayout.LabelField("No zones found in the database.", EditorStyles.boldLabel);
            return;
        }

        foreach (string zoneName in allZones)
        {
            if (!foldoutStates.ContainsKey(zoneName))
            {
                foldoutStates[zoneName] = false;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Zone header with count
            List<VideoEntry> zoneVideos;

            if (zoneName == "Unassigned")
            {
                zoneVideos = allVideos.Where(v => v.Zones.Count == 0).ToList();
            }
            else
            {
                zoneVideos = databaseManager.GetEntriesForZone(zoneName);
            }

            string headerText = $"{zoneName} ({zoneVideos.Count} videos)";

            // Use BeginFoldoutHeaderGroup for better UI
            foldoutStates[zoneName] = EditorGUILayout.BeginFoldoutHeaderGroup(
                foldoutStates[zoneName], headerText);

            if (foldoutStates[zoneName])
            {
                // Zone actions
                EditorGUILayout.BeginHorizontal();

                if (zoneManager != null && zoneName != "Unassigned")
                {
                    if (GUILayout.Button("Show Zone in Scene"))
                    {
                        PolygonZone zone = zoneManager.FindZoneByName(zoneName);
                        if (zone != null)
                        {
                            // Highlight the zone in the scene view
                            Selection.activeGameObject = zoneManager.gameObject;
                            EditorGUIUtility.PingObject(zoneManager.gameObject);

                            // Find the index
                            int zoneIndex = zoneManager.zones.IndexOf(zone);
                            if (zoneIndex >= 0)
                            {
                                EditorPrefs.SetInt("SelectedZoneIndex", zoneIndex);
                            }
                        }
                    }
                }

                if (placementManager != null && zoneName != "Unassigned")
                {
                    if (GUILayout.Button("Place Videos in Zone"))
                    {
                        placementManager.PlaceVideosInZone(zoneName);
                    }
                }

                EditorGUILayout.EndHorizontal();

                // Zone videos
                EditorGUILayout.Space();

                if (zoneVideos.Count == 0)
                {
                    EditorGUILayout.LabelField("No videos in this zone.", italicLabelStyle);
                }
                else
                {
                    foreach (VideoEntry video in zoneVideos)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                        // Video title and controls
                        EditorGUILayout.BeginHorizontal();

                        EditorGUILayout.LabelField(
                            string.IsNullOrEmpty(video.Title) ? video.FileName : video.Title,
                            EditorStyles.boldLabel);

                        // Remove from zone button
                        if (zoneName != "Unassigned")
                        {
                            if (GUILayout.Button("Remove", GUILayout.Width(70)))
                            {
                                if (EditorUtility.DisplayDialog("Confirm Remove",
                                    $"Remove video '{video.Title}' from zone '{zoneName}'?",
                                    "Yes", "No"))
                                {
                                    databaseManager.RemoveVideoFromZone(video.PublicUrl, zoneName);
                                    break; // Break out of the loop since we modified the collection
                                }
                            }
                        }
                        else // For unassigned videos
                        {
                            if (GUILayout.Button("Assign", GUILayout.Width(70)))
                            {
                                List<string> availableZones = databaseManager.GetAllZones();
                                GenericMenu menu = new GenericMenu();

                                foreach (string availZone in availableZones)
                                {
                                    menu.AddItem(new GUIContent(availZone), false, () => {
                                        databaseManager.AssignVideoToZone(video.PublicUrl, availZone);
                                    });
                                }

                                menu.ShowAsContext();
                            }
                        }

                        EditorGUILayout.EndHorizontal();

                        // Video details
                        EditorGUI.indentLevel++;

                        if (!string.IsNullOrEmpty(video.Description))
                        {
                            EditorGUILayout.LabelField("Description:", EditorStyles.miniBoldLabel);
                            EditorGUILayout.LabelField(video.Description, EditorStyles.wordWrappedLabel);
                        }

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Prefab:", GUILayout.Width(60));
                        string newPrefab = EditorGUILayout.TextField(string.IsNullOrEmpty(video.Prefab) ? "Default" : video.Prefab);
                        if (newPrefab != video.Prefab)
                        {
                            video.Prefab = newPrefab;
                        }
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("URL:", GUILayout.Width(60));
                        EditorGUILayout.SelectableLabel(video.PublicUrl, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                        EditorGUILayout.EndHorizontal();

                        EditorGUI.indentLevel--;

                        EditorGUILayout.EndVertical();
                    }
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }
    }

    private void DrawVideosTab()
    {
        if (databaseManager == null) return;

        // Make sure filtered videos are populated
        if (filteredVideos.Count == 0 && string.IsNullOrEmpty(searchQuery))
        {
            filteredVideos = databaseManager.GetAllEntries();
        }

        if (filteredVideos.Count == 0)
        {
            EditorGUILayout.LabelField("No videos found matching your search criteria.", EditorStyles.boldLabel);
            return;
        }

        // Display videos
        foreach (VideoEntry video in filteredVideos)
        {
            string videoKey = video.PublicUrl;

            if (!videoFoldoutStates.ContainsKey(videoKey))
            {
                videoFoldoutStates[videoKey] = false;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Video title and basic info
            EditorGUILayout.BeginHorizontal();

            videoFoldoutStates[videoKey] = EditorGUILayout.Foldout(
                videoFoldoutStates[videoKey],
                string.IsNullOrEmpty(video.Title) ? video.FileName : video.Title,
                true, EditorStyles.foldoutHeader);

            // Display number of zones assigned
            EditorGUILayout.LabelField(
                video.Zones.Count > 0 ? $"({video.Zones.Count} zones)" : "(Unassigned)",
                EditorStyles.miniLabel, GUILayout.Width(80));

            EditorGUILayout.EndHorizontal();

            if (videoFoldoutStates[videoKey])
            {
                EditorGUI.indentLevel++;

                // Title
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Title:", GUILayout.Width(60));
                string newTitle = EditorGUILayout.TextField(video.Title);
                if (newTitle != video.Title)
                {
                    video.Title = newTitle;
                }
                EditorGUILayout.EndHorizontal();

                // Description
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Description:", GUILayout.Width(80));
                string newDesc = EditorGUILayout.TextField(video.Description);
                if (newDesc != video.Description)
                {
                    video.Description = newDesc;
                }
                EditorGUILayout.EndHorizontal();

                // Prefab
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Prefab:", GUILayout.Width(60));
                string newPrefab = EditorGUILayout.TextField(string.IsNullOrEmpty(video.Prefab) ? "Default" : video.Prefab);
                if (newPrefab != video.Prefab)
                {
                    video.Prefab = newPrefab;
                }
                EditorGUILayout.EndHorizontal();

                // URL
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("URL:", GUILayout.Width(60));
                EditorGUILayout.SelectableLabel(video.PublicUrl, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                EditorGUILayout.EndHorizontal();

                // Zone assignments
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Zone Assignments:", EditorStyles.boldLabel);

                // Current zones
                if (video.Zones.Count > 0)
                {
                    List<string> zonesToRemove = new List<string>();

                    foreach (string zoneName in video.Zones)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(zoneName, GUILayout.ExpandWidth(true));

                        if (GUILayout.Button("Remove", GUILayout.Width(70)))
                        {
                            zonesToRemove.Add(zoneName);
                        }

                        EditorGUILayout.EndHorizontal();
                    }

                    // Process removals
                    foreach (string zoneToRemove in zonesToRemove)
                    {
                        databaseManager.RemoveVideoFromZone(video.PublicUrl, zoneToRemove);
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("No zones assigned", italicLabelStyle);
                }

                // Add zone button
                if (GUILayout.Button("Add to Zone..."))
                {
                    List<string> availableZones = databaseManager.GetAllZones();
                    GenericMenu menu = new GenericMenu();

                    // Filter out already assigned zones
                    foreach (string zoneName in availableZones)
                    {
                        if (!video.Zones.Contains(zoneName))
                        {
                            menu.AddItem(new GUIContent(zoneName), false, () => {
                                databaseManager.AssignVideoToZone(video.PublicUrl, zoneName);
                                Repaint();
                            });
                        }
                    }

                    menu.ShowAsContext();
                }

                // Find in placement button
                if (placementManager != null)
                {
                    EditorGUILayout.Space();

                    if (GUILayout.Button("Place This Video"))
                    {
                        if (video.Zones.Count > 0)
                        {
                            string primaryZone = video.Zones[0];
                            placementManager.PlaceVideosInZone(primaryZone);
                            EditorUtility.DisplayDialog("Video Placed",
                                $"Video placed in zone '{primaryZone}'", "OK");
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Cannot Place",
                                "This video is not assigned to any zone. Please assign it first.", "OK");
                        }
                    }
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }
    }
}
#endif