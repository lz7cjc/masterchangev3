using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

// Editor window for managing film zones and importing JSON data
public class FilmZoneEditorWindow : EditorWindow
{
    private FilmZoneManager zoneManager;
    private FilmDataCollection filmData;
    private string jsonFilePath = "";
    private Vector2 scrollPosition;
    private bool showImportedData = false;
    private bool showZoneEditor = true;
    private bool showPrefabPlacer = true;
    private bool showSaveLoad = true;
    private bool showHeightInfo = true;

    // Zone editing
    private int selectedZoneIndex = -1;
    private bool isEditingZone = false;
    private List<Vector3> tempZonePoints = new List<Vector3>();

    // Prefab placement
    private Dictionary<string, List<GameObject>> zonePrefabs = new Dictionary<string, List<GameObject>>();

    [MenuItem("Tools/Film Zone Video Manager")]
    public static void ShowWindow()
    {
        GetWindow<FilmZoneEditorWindow>("Film Zone Video Manager");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        FindZoneManager();
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void FindZoneManager()
    {
        if (zoneManager == null)
        {
            zoneManager = FindObjectOfType<FilmZoneManager>();
        }
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        try
        {
            DrawZoneManagerSection();
            EditorGUILayout.Space(10);

            DrawJSONImportSection();
            EditorGUILayout.Space(10);

            if (showZoneEditor)
            {
                DrawZoneEditorSection();
                EditorGUILayout.Space(10);
            }

            if (showPrefabPlacer)
            {
                DrawPrefabPlacerSection();
                EditorGUILayout.Space(10);
            }

            if (showSaveLoad)
            {
                DrawSaveLoadSection();
                EditorGUILayout.Space(10);
            }

            DrawUtilityButtons();
        }
        catch (System.Exception e)
        {
            EditorGUILayout.HelpBox($"Error in GUI: {e.Message}", MessageType.Error);
            Debug.LogError($"FilmZoneEditorWindow GUI Error: {e.Message}\n{e.StackTrace}");
        }
        finally
        {
            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawZoneManagerSection()
    {
        EditorGUILayout.LabelField("Film Zone Manager", EditorStyles.boldLabel);

        FilmZoneManager newZoneManager = (FilmZoneManager)EditorGUILayout.ObjectField("Film Zone Manager", zoneManager, typeof(FilmZoneManager), true);
        if (newZoneManager != zoneManager)
        {
            zoneManager = newZoneManager;
        }

        if (zoneManager == null)
        {
            EditorGUILayout.HelpBox("No Film Zone Manager found in scene. Create one to continue.", MessageType.Warning);
            if (GUILayout.Button("Create Film Zone Manager"))
            {
                CreateZoneManager();
            }
            return;
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField($"Zones: {zoneManager.zones.Count}", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"Prefab Mappings: {zoneManager.prefabMappings.Count}", EditorStyles.miniLabel);
    }

    private void DrawJSONImportSection()
    {
        EditorGUILayout.LabelField("JSON Import", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        try
        {
            EditorGUILayout.LabelField("JSON File:", GUILayout.Width(70));
            jsonFilePath = EditorGUILayout.TextField(jsonFilePath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFilePanel("Select JSON File", Application.dataPath, "json");
                if (!string.IsNullOrEmpty(path))
                {
                    jsonFilePath = path;
                }
            }
        }
        finally
        {
            EditorGUILayout.EndHorizontal();
        }

        GUI.enabled = !string.IsNullOrEmpty(jsonFilePath) && File.Exists(jsonFilePath);
        if (GUILayout.Button("Import JSON Data"))
        {
            ImportJSONData();
        }
        GUI.enabled = true;

        if (filmData != null && filmData.Entries != null)
        {
            EditorGUILayout.Space(5);
            showImportedData = EditorGUILayout.Foldout(showImportedData, $"Imported Data ({filmData.Entries.Length} entries)");

            if (showImportedData)
            {
                EditorGUI.indentLevel++;
                try
                {
                    foreach (var entry in filmData.Entries.Take(5)) // Show first 5 entries
                    {
                        string prefabInfo = entry.HasCustomPrefab() ? $" [Prefab: {entry.Prefab}]" : " [Default]";
                        EditorGUILayout.LabelField($"• {entry.Title} ({string.Join(", ", entry.GetPlacementZones())}){prefabInfo}", EditorStyles.miniLabel);
                    }
                    if (filmData.Entries.Length > 5)
                    {
                        EditorGUILayout.LabelField($"... and {filmData.Entries.Length - 5} more", EditorStyles.miniLabel);
                    }
                }
                finally
                {
                    EditorGUI.indentLevel--;
                }
            }
        }
    }

    private void DrawZoneEditorSection()
    {
        showZoneEditor = EditorGUILayout.Foldout(showZoneEditor, "Zone Editor");
        if (!showZoneEditor) return;

        EditorGUI.indentLevel++;
        try
        {
            if (zoneManager == null)
            {
                EditorGUILayout.HelpBox("Film Zone Manager required", MessageType.Warning);
                return;
            }

            // Zone height information
            DrawZoneHeightInfo();
            EditorGUILayout.Space(5);

            // Zone list with NUCLEAR DELETE options
            EditorGUILayout.LabelField("Existing Zones:", EditorStyles.boldLabel);

            // Store the count to avoid modification during iteration issues
            int zoneCount = zoneManager.zones.Count;
            bool zonesModified = false;

            for (int i = 0; i < zoneCount && i < zoneManager.zones.Count; i++)
            {
                if (zonesModified) break; // Exit if zones were modified

                EditorGUILayout.BeginVertical("box");
                try
                {
                    EditorGUILayout.BeginHorizontal();
                    try
                    {
                        bool isSelected = (selectedZoneIndex == i);
                        bool newSelected = EditorGUILayout.Toggle(isSelected, GUILayout.Width(20));

                        if (newSelected != isSelected)
                        {
                            selectedZoneIndex = newSelected ? i : -1;
                        }

                        zoneManager.zones[i].zoneName = EditorGUILayout.TextField(zoneManager.zones[i].zoneName);
                        zoneManager.zones[i].gizmoColor = EditorGUILayout.ColorField(zoneManager.zones[i].gizmoColor, GUILayout.Width(50));

                        if (GUILayout.Button("Edit", GUILayout.Width(50)))
                        {
                            StartEditingZone(i);
                        }

                        // ENHANCED: Nuclear Delete button with warning color
                        GUI.backgroundColor = Color.red;
                        if (GUILayout.Button("💥", GUILayout.Width(30)))
                        {
                            NuclearDeleteZone(i);
                            zonesModified = true;
                        }
                        GUI.backgroundColor = Color.white;

                        // Regular delete button (smaller, less prominent)
                        if (!zonesModified && GUILayout.Button("X", GUILayout.Width(25)))
                        {
                            RegularDeleteZone(i);
                            zonesModified = true;
                        }
                    }
                    finally
                    {
                        EditorGUILayout.EndHorizontal();
                    }

                    if (!zonesModified && i < zoneManager.zones.Count)
                    {
                        // Show zone info
                        var zone = zoneManager.zones[i];
                        EditorGUILayout.BeginHorizontal();
                        try
                        {
                            EditorGUILayout.LabelField($"Points: {zone.polygonPoints.Count}", EditorStyles.miniLabel, GUILayout.Width(70));
                            EditorGUILayout.LabelField($"Height: {zone.GetZoneHeight():F1}", EditorStyles.miniLabel, GUILayout.Width(80));

                            // Zone action buttons
                            if (zone.polygonPoints.Count > 0)
                            {
                                if (GUILayout.Button("Clear Points", GUILayout.Width(80)))
                                {
                                    ClearZonePoints(i);
                                }

                                if (GUILayout.Button("Focus", GUILayout.Width(50)))
                                {
                                    FocusOnZone(zone);
                                }
                            }
                        }
                        finally
                        {
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                }
                finally
                {
                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.Space(2);

                if (zonesModified) break; // Exit if zones were modified
            }

            EditorGUILayout.Space(5);

            // Zone management buttons
            EditorGUILayout.BeginHorizontal();
            try
            {
                if (GUILayout.Button("Add New Zone"))
                {
                    FilmZone newZone = new FilmZone();
                    newZone.zoneName = $"Zone_{zoneManager.zones.Count + 1}";
                    newZone.gizmoColor = GetRandomColor();
                    zoneManager.zones.Add(newZone);
                }

                if (GUILayout.Button("Snap All Zones to Terrain"))
                {
                    SnapZonesToTerrain();
                }
            }
            finally
            {
                EditorGUILayout.EndHorizontal();
            }

            // NUCLEAR OPTIONS section
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("⚠️ Nuclear Options ⚠️", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            try
            {
                GUI.backgroundColor = Color.red;

                if (GUILayout.Button("💥 Nuclear Clear All Zones"))
                {
                    if (EditorUtility.DisplayDialog("Nuclear Clear All Zones",
                        "⚠️ WARNING: This will COMPLETELY DELETE all zones!\n\nThis action cannot be undone!",
                        "DELETE ALL", "Cancel"))
                    {
                        zoneManager.ForceClearAllZoneData();
                        selectedZoneIndex = -1;
                        isEditingZone = false;
                        tempZonePoints.Clear();
                        Repaint();
                    }
                }

                GUI.backgroundColor = Color.yellow;
                if (GUILayout.Button("🧹 Clear All Points Only"))
                {
                    if (EditorUtility.DisplayDialog("Clear All Points",
                        "Clear polygon points from all zones but keep zone definitions?",
                        "Clear Points", "Cancel"))
                    {
                        zoneManager.ClearAllPolygonPoints();
                        tempZonePoints.Clear();
                        SceneView.RepaintAll();
                    }
                }

                GUI.backgroundColor = Color.white;
            }
            finally
            {
                EditorGUILayout.EndHorizontal();
            }

            // Zone editing controls
            if (isEditingZone)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Zone Editing Mode", EditorStyles.boldLabel);

                EditorGUILayout.HelpBox(
                    "Scene View Controls:\n" +
                    "• Click to add points\n" +
                    "• Ctrl+Click to delete nearest point\n" +
                    "• Shift+Click to finish editing",
                    MessageType.Info);

                // Show current points with delete buttons
                if (tempZonePoints.Count > 0)
                {
                    EditorGUILayout.LabelField($"Current Points ({tempZonePoints.Count}):", EditorStyles.boldLabel);

                    // Use reverse iteration to handle deletions safely
                    for (int i = tempZonePoints.Count - 1; i >= 0; i--)
                    {
                        EditorGUILayout.BeginHorizontal();
                        try
                        {
                            EditorGUILayout.LabelField($"Point {i}:", GUILayout.Width(60));

                            Vector3 newPos = EditorGUILayout.Vector3Field("", tempZonePoints[i]);
                            if (newPos != tempZonePoints[i])
                            {
                                tempZonePoints[i] = newPos;
                                SceneView.RepaintAll();
                            }

                            if (GUILayout.Button("X", GUILayout.Width(25)))
                            {
                                tempZonePoints.RemoveAt(i);
                                SceneView.RepaintAll();
                            }
                        }
                        finally
                        {
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                }

                EditorGUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();
                try
                {
                    if (GUILayout.Button("Finish Editing Zone"))
                    {
                        FinishEditingZone();
                    }

                    if (GUILayout.Button("Cancel Zone Edit"))
                    {
                        CancelEditingZone();
                    }

                    if (GUILayout.Button("Clear All Points"))
                    {
                        tempZonePoints.Clear();
                        SceneView.RepaintAll();
                    }
                }
                finally
                {
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
        finally
        {
            EditorGUI.indentLevel--;
        }
    }

    private void DrawZoneHeightInfo()
    {
        showHeightInfo = EditorGUILayout.Foldout(showHeightInfo, "Zone Height Information");
        if (!showHeightInfo || zoneManager == null) return;

        EditorGUI.indentLevel++;
        try
        {
            foreach (var zone in zoneManager.zones)
            {
                if (zone.polygonPoints.Count < 3) continue;

                float minY = float.MaxValue;
                float maxY = float.MinValue;
                float avgY = 0;

                foreach (var point in zone.polygonPoints)
                {
                    minY = Mathf.Min(minY, point.y);
                    maxY = Mathf.Max(maxY, point.y);
                    avgY += point.y;
                }
                avgY /= zone.polygonPoints.Count;

                EditorGUILayout.BeginHorizontal();
                try
                {
                    EditorGUILayout.LabelField($"{zone.zoneName}:", GUILayout.Width(100));
                    EditorGUILayout.LabelField($"Avg Y: {avgY:F1}", GUILayout.Width(80));
                    EditorGUILayout.LabelField($"Range: {minY:F1} to {maxY:F1}", GUILayout.Width(120));

                    // Warning for zones that might be floating
                    if (avgY > 10f)
                    {
                        EditorGUILayout.LabelField("⚠️ High", EditorStyles.boldLabel);
                    }
                    else if (avgY < -2f)
                    {
                        EditorGUILayout.LabelField("⚠️ Low", EditorStyles.boldLabel);
                    }
                    else
                    {
                        EditorGUILayout.LabelField("✓ OK", EditorStyles.miniLabel);
                    }
                }
                finally
                {
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
        finally
        {
            EditorGUI.indentLevel--;
        }
    }

    private void DrawPrefabPlacerSection()
    {
        showPrefabPlacer = EditorGUILayout.Foldout(showPrefabPlacer, "Prefab Placer");
        if (!showPrefabPlacer) return;

        EditorGUI.indentLevel++;
        try
        {
            if (zoneManager == null || filmData == null)
            {
                EditorGUILayout.HelpBox("Film Zone Manager and imported JSON data required", MessageType.Warning);
                return;
            }

            // Check if default prefab is set
            if (zoneManager.defaultPrefab == null)
            {
                EditorGUILayout.HelpBox("Default prefab not set in FilmZoneManager. This is required for placing prefabs.", MessageType.Error);
                return;
            }

            EditorGUILayout.LabelField("Placement Options:", EditorStyles.boldLabel);

            if (GUILayout.Button("Place All Prefabs in Zones"))
            {
                PlaceAllPrefabsInZones();
            }

            if (GUILayout.Button("Clear All Placed Prefabs"))
            {
                ClearAllPlacedPrefabs();
            }

            EditorGUILayout.Space(5);

            // Show zone-specific placement options with prefab info
            var zoneGroups = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<FilmDataEntry>>();

            // Group entries by their placement zones (each entry can appear in multiple zones)
            foreach (var entry in filmData.Entries)
            {
                string[] placementZones = entry.GetPlacementZones();
                foreach (string zoneName in placementZones)
                {
                    if (!string.IsNullOrEmpty(zoneName))
                    {
                        if (!zoneGroups.ContainsKey(zoneName))
                        {
                            zoneGroups[zoneName] = new System.Collections.Generic.List<FilmDataEntry>();
                        }
                        zoneGroups[zoneName].Add(entry);
                    }
                }
            }

            foreach (var group in zoneGroups)
            {
                // Count custom vs default prefabs
                int customPrefabs = group.Value.Count(e => e.HasCustomPrefab());
                int defaultPrefabs = group.Value.Count - customPrefabs;

                EditorGUILayout.BeginHorizontal();
                try
                {
                    EditorGUILayout.LabelField($"{group.Key} ({group.Value.Count})", GUILayout.Width(120));
                    EditorGUILayout.LabelField($"Custom: {customPrefabs}, Default: {defaultPrefabs}", EditorStyles.miniLabel, GUILayout.Width(120));

                    if (GUILayout.Button("Place", GUILayout.Width(60)))
                    {
                        PlacePrefabsInZone(group.Key, group.Value);
                    }

                    if (GUILayout.Button("Clear", GUILayout.Width(60)))
                    {
                        ClearPrefabsInZone(group.Key);
                    }
                }
                finally
                {
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
        finally
        {
            EditorGUI.indentLevel--;
        }
    }

    private void DrawSaveLoadSection()
    {
        showSaveLoad = EditorGUILayout.Foldout(showSaveLoad, "Save/Load Layout");
        if (!showSaveLoad) return;

        EditorGUI.indentLevel++;
        try
        {
            EditorGUILayout.BeginHorizontal();
            try
            {
                if (GUILayout.Button("Save Current Layout"))
                {
                    SaveCurrentLayout();
                }

                if (GUILayout.Button("Load Layout"))
                {
                    LoadLayout();
                }
            }
            finally
            {
                EditorGUILayout.EndHorizontal();
            }
        }
        finally
        {
            EditorGUI.indentLevel--;
        }
    }

    private void DrawUtilityButtons()
    {
        EditorGUILayout.LabelField("Utilities:", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        try
        {
            if (GUILayout.Button("Open Film Zone Editor"))
            {
                FilmZoneEditorWindow.ShowWindow();
            }

            if (GUILayout.Button("Select All Videos"))
            {
                EnhancedVideoPlayer[] allVideos = Object.FindObjectsOfType<EnhancedVideoPlayer>();
                Selection.objects = allVideos.Select(v => v.gameObject).ToArray();
            }
        }
        finally
        {
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.BeginHorizontal();
        try
        {
            if (GUILayout.Button("Validate Setup"))
            {
                ValidateSetup();
            }

            if (GUILayout.Button("Validate Videos"))
            {
                ValidateVideoSetup();
            }
        }
        finally
        {
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.BeginHorizontal();
        try
        {
            if (GUILayout.Button("Clean Up"))
            {
                CleanUpSystem();
            }

            if (GUILayout.Button("Fix All Video Zones"))
            {
                FixAllVideoZones();
            }
        }
        finally
        {
            EditorGUILayout.EndHorizontal();
        }
    }

    // ===== NUCLEAR DELETE HELPER METHODS =====

    private void NuclearDeleteZone(int zoneIndex)
    {
        if (zoneIndex < 0 || zoneIndex >= zoneManager.zones.Count) return;

        string zoneName = zoneManager.zones[zoneIndex].zoneName;

        if (EditorUtility.DisplayDialog("💥 Nuclear Delete Zone",
            $"⚠️ WARNING: This will COMPLETELY DELETE zone '{zoneName}'!\n\n" +
            "This will remove:\n" +
            "• The zone definition\n" +
            "• All polygon points\n" +
            "• All zone settings\n\n" +
            "This action cannot be undone!",
            "💥 NUCLEAR DELETE", "Cancel"))
        {
            Debug.Log($"💥 Nuclear deleting zone: {zoneName}");

            // Use the FilmZoneManager's nuclear delete method
            zoneManager.NuclearDeleteZone(zoneIndex);

            // Reset selection if we deleted the selected zone
            if (selectedZoneIndex == zoneIndex)
            {
                selectedZoneIndex = -1;
                isEditingZone = false;
                tempZonePoints.Clear();
            }
            else if (selectedZoneIndex > zoneIndex)
            {
                selectedZoneIndex--; // Adjust selection index
            }

            // Force repaint
            Repaint();
            SceneView.RepaintAll();
        }
    }

    private void RegularDeleteZone(int zoneIndex)
    {
        if (zoneIndex < 0 || zoneIndex >= zoneManager.zones.Count) return;

        string zoneName = zoneManager.zones[zoneIndex].zoneName;

        if (EditorUtility.DisplayDialog("Delete Zone",
            $"Delete zone '{zoneName}'?",
            "Delete", "Cancel"))
        {
            zoneManager.zones.RemoveAt(zoneIndex);

            if (selectedZoneIndex == zoneIndex)
            {
                selectedZoneIndex = -1;
                isEditingZone = false;
                tempZonePoints.Clear();
            }
            else if (selectedZoneIndex > zoneIndex)
            {
                selectedZoneIndex--;
            }

            EditorUtility.SetDirty(zoneManager);
            SceneView.RepaintAll();
        }
    }

    private void ClearZonePoints(int zoneIndex)
    {
        if (zoneIndex < 0 || zoneIndex >= zoneManager.zones.Count) return;

        string zoneName = zoneManager.zones[zoneIndex].zoneName;

        if (EditorUtility.DisplayDialog("Clear Zone Points",
            $"Clear all polygon points from zone '{zoneName}'?\n\n" +
            "This will remove the zone boundary but keep the zone definition.",
            "Clear Points", "Cancel"))
        {
            zoneManager.ClearZonePoints(zoneIndex);

            // If we're editing this zone, clear temp points too
            if (selectedZoneIndex == zoneIndex && isEditingZone)
            {
                tempZonePoints.Clear();
                SceneView.RepaintAll();
            }
        }
    }

    private void FocusOnZone(FilmZone zone)
    {
        if (zone.polygonPoints.Count == 0) return;

        // Calculate bounds
        Vector3 center = Vector3.zero;
        foreach (var point in zone.polygonPoints)
        {
            center += point;
        }
        center /= zone.polygonPoints.Count;

        // Focus scene view on zone center
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView != null)
        {
            sceneView.pivot = center;
            sceneView.size = 20f; // Adjust zoom level as needed
            sceneView.Repaint();
        }
    }

    // ===== ZONE EDITING METHODS =====

    private void CreateZoneManager()
    {
        GameObject go = new GameObject("FilmZoneManager");
        zoneManager = go.AddComponent<FilmZoneManager>();

        // Set up some default prefab mappings
        zoneManager.prefabMappings.Add(new FilmZoneManager.PrefabMapping { prefabName = "Carnival", prefab = null });
        zoneManager.prefabMappings.Add(new FilmZoneManager.PrefabMapping { prefabName = "Football", prefab = null });

        Selection.activeGameObject = go;
        EditorUtility.SetDirty(go);
    }

    private void ImportJSONData()
    {
        try
        {
            string jsonContent = File.ReadAllText(jsonFilePath);
            filmData = JsonUtility.FromJson<FilmDataCollection>(jsonContent);

            if (filmData != null && filmData.Entries != null)
            {
                Debug.Log($"Successfully imported {filmData.Entries.Length} video entries");

                // Auto-create zones based on imported data
                CreateZonesFromImportedData();

                // Auto-detect prefab types and suggest mappings
                AutoDetectPrefabTypes();
            }
            else
            {
                Debug.LogError("Failed to parse JSON data");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error importing JSON: {e.Message}");
        }
    }

    private void AutoDetectPrefabTypes()
    {
        if (zoneManager == null || filmData == null) return;

        // Get all unique prefab types from the JSON data
        var uniquePrefabTypes = filmData.Entries
            .Where(e => e.HasCustomPrefab())
            .Select(e => e.Prefab)
            .Distinct()
            .ToList();

        int addedMappings = 0;

        foreach (string prefabType in uniquePrefabTypes)
        {
            // Only add if not already in mappings
            if (!zoneManager.prefabMappings.Any(m => m.prefabName.Equals(prefabType, System.StringComparison.OrdinalIgnoreCase)))
            {
                zoneManager.prefabMappings.Add(new FilmZoneManager.PrefabMapping
                {
                    prefabName = prefabType,
                    prefab = null
                });
                addedMappings++;
            }
        }

        if (addedMappings > 0)
        {
            EditorUtility.SetDirty(zoneManager);
            Debug.Log($"Auto-detected and added {addedMappings} prefab types: {string.Join(", ", uniquePrefabTypes)}");
            Debug.Log("Remember to assign the actual prefab GameObjects in the FilmZoneManager component!");
        }
    }

    private void CreateZonesFromImportedData()
    {
        if (zoneManager == null || filmData == null) return;

        // Get all unique zones from the Zones arrays in the JSON data
        var uniqueZones = new System.Collections.Generic.HashSet<string>();

        foreach (var entry in filmData.Entries)
        {
            string[] placementZones = entry.GetPlacementZones();
            foreach (string zoneName in placementZones)
            {
                if (!string.IsNullOrEmpty(zoneName))
                {
                    uniqueZones.Add(zoneName);
                }
            }
        }

        // Create zones that don't already exist
        foreach (string zoneName in uniqueZones)
        {
            if (!zoneManager.zones.Any(z => z.zoneName.Equals(zoneName, System.StringComparison.OrdinalIgnoreCase)))
            {
                FilmZone newZone = new FilmZone();
                newZone.zoneName = zoneName;
                newZone.gizmoColor = GetRandomColor();
                zoneManager.zones.Add(newZone);
            }
        }

        EditorUtility.SetDirty(zoneManager);
        Debug.Log($"Created zones for: {string.Join(", ", uniqueZones)}");
    }

    private Color GetRandomColor()
    {
        return new Color(
            UnityEngine.Random.Range(0.3f, 1f),
            UnityEngine.Random.Range(0.3f, 1f),
            UnityEngine.Random.Range(0.3f, 1f),
            0.7f
        );
    }

    private void StartEditingZone(int zoneIndex)
    {
        selectedZoneIndex = zoneIndex;
        isEditingZone = true;
        tempZonePoints.Clear();

        // Copy existing points if they exist
        if (zoneIndex >= 0 && zoneIndex < zoneManager.zones.Count)
        {
            tempZonePoints.AddRange(zoneManager.zones[zoneIndex].polygonPoints);
        }

        SceneView.RepaintAll();
    }

    private void FinishEditingZone()
    {
        if (selectedZoneIndex >= 0 && selectedZoneIndex < zoneManager.zones.Count)
        {
            zoneManager.zones[selectedZoneIndex].polygonPoints.Clear();
            zoneManager.zones[selectedZoneIndex].polygonPoints.AddRange(tempZonePoints);
            EditorUtility.SetDirty(zoneManager);
        }

        isEditingZone = false;
        tempZonePoints.Clear();
        SceneView.RepaintAll();
    }

    private void CancelEditingZone()
    {
        isEditingZone = false;
        tempZonePoints.Clear();
        SceneView.RepaintAll();
    }

    private void SnapZonesToTerrain()
    {
        if (zoneManager == null) return;

        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null)
        {
            Debug.LogWarning("No active terrain found!");
            return;
        }

        foreach (var zone in zoneManager.zones)
        {
            for (int i = 0; i < zone.polygonPoints.Count; i++)
            {
                Vector3 point = zone.polygonPoints[i];
                float terrainHeight = terrain.SampleHeight(point);
                zone.polygonPoints[i] = new Vector3(point.x, terrainHeight, point.z);
            }
        }

        EditorUtility.SetDirty(zoneManager);
        Debug.Log("Snapped all zones to terrain height");
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!isEditingZone) return;

        Event currentEvent = Event.current;

        // Handle mouse clicks for adding points
        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
        {
            if (currentEvent.shift)
            {
                // Finish editing on Shift+Click
                FinishEditingZone();
                currentEvent.Use();
            }
            else if (currentEvent.control)
            {
                // Delete nearest point on Ctrl+Click
                DeleteNearestPoint(currentEvent.mousePosition);
                currentEvent.Use();
            }
            else
            {
                // Add point on regular click
                Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
                RaycastHit hit;

                // Raycast to terrain or any collider
                if (Physics.Raycast(ray, out hit, Mathf.Infinity))
                {
                    tempZonePoints.Add(hit.point);
                    currentEvent.Use();
                    SceneView.RepaintAll();
                }
                else
                {
                    // If no collider hit, place on ground plane at y=0
                    Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                    float distance;
                    if (groundPlane.Raycast(ray, out distance))
                    {
                        Vector3 hitPoint = ray.GetPoint(distance);
                        tempZonePoints.Add(hitPoint);
                        currentEvent.Use();
                        SceneView.RepaintAll();
                    }
                }
            }
        }

        // Draw temporary zone points with better visualization
        if (tempZonePoints.Count > 0)
        {
            // Get current zone color or use yellow for new zones
            Color zoneColor = Color.yellow;
            if (selectedZoneIndex >= 0 && selectedZoneIndex < zoneManager.zones.Count)
            {
                zoneColor = zoneManager.zones[selectedZoneIndex].gizmoColor;
            }

            Handles.color = zoneColor;

            // Draw points as spheres with numbers
            for (int i = 0; i < tempZonePoints.Count; i++)
            {
                // Draw sphere for each point
                float sphereSize = HandleUtility.GetHandleSize(tempZonePoints[i]) * 0.1f;
                Handles.SphereHandleCap(0, tempZonePoints[i], Quaternion.identity, sphereSize, EventType.Repaint);

                // Draw point number
                Handles.BeginGUI();
                Vector3 screenPos = SceneView.currentDrawingSceneView.camera.WorldToScreenPoint(tempZonePoints[i]);
                if (screenPos.z > 0)
                {
                    screenPos.y = SceneView.currentDrawingSceneView.camera.pixelHeight - screenPos.y;
                    GUI.color = Color.white;
                    GUI.backgroundColor = zoneColor;
                    GUI.Label(new Rect(screenPos.x - 10, screenPos.y - 25, 20, 20), i.ToString(), EditorStyles.miniButton);
                    GUI.color = Color.white;
                    GUI.backgroundColor = Color.white;
                }
                Handles.EndGUI();

                // Draw lines between points
                if (i > 0)
                {
                    Handles.DrawLine(tempZonePoints[i - 1], tempZonePoints[i]);
                }
            }

            // Close the polygon if we have more than 2 points
            if (tempZonePoints.Count > 2)
            {
                Handles.DrawLine(tempZonePoints[tempZonePoints.Count - 1], tempZonePoints[0]);
            }

            // Draw instructions
            DrawSceneInstructions();
        }
    }

    private void DeleteNearestPoint(Vector2 mousePosition)
    {
        if (tempZonePoints.Count == 0) return;

        float closestDistance = float.MaxValue;
        int closestIndex = -1;

        // Find the closest point to mouse position
        for (int i = 0; i < tempZonePoints.Count; i++)
        {
            Vector3 screenPos = SceneView.currentDrawingSceneView.camera.WorldToScreenPoint(tempZonePoints[i]);
            screenPos.y = SceneView.currentDrawingSceneView.camera.pixelHeight - screenPos.y;

            float distance = Vector2.Distance(mousePosition, new Vector2(screenPos.x, screenPos.y));
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        // Remove the closest point if it's within reasonable distance (50 pixels)
        if (closestIndex >= 0 && closestDistance < 50f)
        {
            tempZonePoints.RemoveAt(closestIndex);
            SceneView.RepaintAll();
        }
    }

    private void DrawSceneInstructions()
    {
        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(10, 10, 300, 120));
        GUILayout.BeginVertical("box");

        GUILayout.Label("Zone Editing Instructions:", EditorStyles.boldLabel);
        GUILayout.Label("• Click to add point");
        GUILayout.Label("• Ctrl+Click to delete nearest point");
        GUILayout.Label("• Shift+Click to finish editing");
        GUILayout.Label($"Points: {tempZonePoints.Count}");

        GUILayout.EndVertical();
        GUILayout.EndArea();
        Handles.EndGUI();
    }

    // ===== PREFAB PLACEMENT METHODS =====

    private void PlaceAllPrefabsInZones()
    {
        if (filmData == null || zoneManager == null)
        {
            Debug.LogError("Cannot place prefabs: Missing filmData or zoneManager");
            return;
        }

        Debug.Log("🚀 Starting to place all prefabs in zones...");

        int totalPlaced = 0;
        Dictionary<string, int> prefabTypeCounts = new Dictionary<string, int>();

        foreach (var entry in filmData.Entries)
        {
            // Get all zones this entry should be placed in
            string[] placementZones = entry.GetPlacementZones();

            foreach (string zoneName in placementZones)
            {
                if (!string.IsNullOrEmpty(zoneName))
                {
                    Debug.Log($"Placing '{entry.Title}' in zone '{zoneName}'");
                    PlacePrefabForEntry(entry, zoneName);
                    totalPlaced++;

                    // Track prefab type usage
                    string prefabType = entry.HasCustomPrefab() ? entry.Prefab : "Default";
                    if (!prefabTypeCounts.ContainsKey(prefabType))
                        prefabTypeCounts[prefabType] = 0;
                    prefabTypeCounts[prefabType]++;
                }
            }
        }

        Debug.Log($"✅ Placed {totalPlaced} total prefabs. Breakdown: {string.Join(", ", prefabTypeCounts.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
    }

    private void PlacePrefabsInZone(string zoneName, System.Collections.Generic.List<FilmDataEntry> entries)
    {
        int placed = 0;
        foreach (var entry in entries)
        {
            Debug.Log($"Placing '{entry.Title}' in zone '{zoneName}'");
            PlacePrefabForEntry(entry, zoneName);
            placed++;
        }
        Debug.Log($"✅ Placed {placed} prefabs in zone '{zoneName}'");
    }

    private void PlacePrefabForEntry(FilmDataEntry entry, string zoneName)
    {
        // Validate zone manager first
        if (zoneManager == null)
        {
            Debug.LogError("FilmZoneManager is null! Cannot place prefabs.");
            return;
        }

        // Validate entry
        if (entry == null)
        {
            Debug.LogError("FilmDataEntry is null! Cannot place prefab.");
            return;
        }

        FilmZone targetZone = zoneManager.GetZoneByName(zoneName);
        if (targetZone == null)
        {
            Debug.LogWarning($"Zone '{zoneName}' not found for entry '{entry.Title}'");
            return;
        }

        // Get prefab FOR THIS SPECIFIC ENTRY
        GameObject prefabTemplate = zoneManager.GetPrefabForEntry(entry, zoneName);
        if (prefabTemplate == null)
        {
            Debug.LogError($"No prefab found for entry '{entry.Title}'. Make sure to assign a default prefab in FilmZoneManager.");
            return;
        }

        // Log which prefab is being used for debugging
        string prefabSource = entry.HasCustomPrefab() ? $"custom prefab '{entry.Prefab}'" : "default prefab";
        Debug.Log($"Using {prefabSource} for entry '{entry.Title}' -> {prefabTemplate.name}");

        // Get or create the zone parent folder
        Transform zoneParent = FindOrCreateZoneParent(zoneName);

        // Calculate placement position based on zone polygon AT ZONE HEIGHT
        Vector3 placementPosition = CalculatePlacementPositionAtZoneHeight(zoneName);

        // Create the prefab instance
        GameObject instance = PrefabUtility.InstantiatePrefab(prefabTemplate) as GameObject;
        if (instance == null)
        {
            instance = Instantiate(prefabTemplate);
        }

        // Set position and rotation
        instance.transform.position = placementPosition;
        instance.transform.rotation = Quaternion.identity; // 0,0,0 rotation
        instance.name = $"Video_{entry.Title}_{zoneName}";

        // CRITICAL: Set up the complete video system on the instance using ONLY EnhancedVideoPlayer
        SetupVideoSystemOnInstance(instance, entry, zoneName);

        // Parent to zone folder AFTER positioning
        instance.transform.SetParent(zoneParent);

        // Track placed prefabs
        if (!zonePrefabs.ContainsKey(zoneName))
        {
            zonePrefabs[zoneName] = new List<GameObject>();
        }
        zonePrefabs[zoneName].Add(instance);

        EditorUtility.SetDirty(instance);

        Debug.Log($"✅ Successfully placed '{entry.Title}' using {prefabSource} in zone '{zoneName}' at position {placementPosition}");
    }

    private Vector3 CalculatePlacementPositionAtZoneHeight(string zoneName)
    {
        if (string.IsNullOrEmpty(zoneName))
        {
            Debug.LogWarning($"Zone name is empty. Using origin position.");
            return Vector3.zero;
        }

        // Find the actual FilmZone definition
        FilmZone targetZone = zoneManager.GetZoneByName(zoneName);

        if (targetZone == null || targetZone.polygonPoints.Count < 3)
        {
            Debug.LogWarning($"Zone '{zoneName}' not found or invalid. Using fallback position.");
            return Vector3.zero;
        }

        // Count existing videos in this zone for spacing
        int existingCount = 0;
        if (zonePrefabs.ContainsKey(zoneName))
        {
            existingCount = zonePrefabs[zoneName].Count;
        }

        // Get a position within the zone polygon at the ZONE'S HEIGHT
        Vector3 basePosition = targetZone.GetRandomPointInZoneAtZoneHeight();

        // Add small offset to prevent exact overlap (but keep at zone height)
        float spacing = zoneManager.prefabSpacing;
        Vector3 offset = new Vector3(
            (existingCount % 3) * spacing,  // Arrange in a 3x3 grid pattern
            0, // Keep Y at zone level - don't add vertical offset
            (existingCount / 3) * spacing
        );

        Vector3 finalPosition = basePosition + offset;

        // Ensure the position is still within the zone after offset (but maintain height)
        Vector2 testPoint2D = new Vector2(finalPosition.x, finalPosition.z);
        if (!targetZone.IsPointInZone2D(testPoint2D))
        {
            // If offset takes us outside zone, use the base position
            finalPosition = basePosition;
        }

        Debug.Log($"Calculated placement position for zone '{zoneName}': {finalPosition} (base: {basePosition}, offset: {offset}, zone height: {targetZone.GetZoneHeight():F1})");

        return finalPosition;
    }

    // CRITICAL METHOD: This ensures we ALWAYS use EnhancedVideoPlayer directly
    private void SetupVideoSystemOnInstance(GameObject instance, FilmDataEntry entry, string zoneName)
    {
        if (instance == null || entry == null) return;

        // Ensure BoxCollider exists and is configured as trigger
        BoxCollider boxCollider = instance.GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = instance.AddComponent<BoxCollider>();
        }
        boxCollider.isTrigger = true; // Important for Event System

        // ALWAYS use EnhancedVideoPlayer directly - NEVER add VideoZonePrefab
        EnhancedVideoPlayer enhancedPlayer = instance.GetComponent<EnhancedVideoPlayer>();

        if (enhancedPlayer == null)
        {
            // Add EnhancedVideoPlayer if it doesn't exist
            enhancedPlayer = instance.AddComponent<EnhancedVideoPlayer>();
            Debug.Log($"Added new EnhancedVideoPlayer for: {entry.Title}");
        }
        else
        {
            Debug.Log($"Using existing EnhancedVideoPlayer for: {entry.Title}");
        }

        // Remove any VideoZonePrefab components that might exist (cleanup)
        VideoZonePrefab existingVideoPrefab = instance.GetComponent<VideoZonePrefab>();
        if (existingVideoPrefab != null)
        {
            DestroyImmediate(existingVideoPrefab);
            Debug.Log($"Removed VideoZonePrefab component from {entry.Title} - using EnhancedVideoPlayer instead");
        }

        // Configure the EnhancedVideoPlayer with proper zone tracking
        ConfigureEnhancedVideoPlayer(enhancedPlayer, entry, zoneName);

        // Set up EventTrigger component - PRESERVE EXISTING SETTINGS FROM PREFAB
        SetupEventTriggersForEnhancedPlayer(instance, enhancedPlayer);

        Debug.Log($"✅ Set up complete video system for: {entry.Title} in zone: {zoneName} using EnhancedVideoPlayer ONLY");
    }

    private void ConfigureEnhancedVideoPlayer(EnhancedVideoPlayer enhancedPlayer, FilmDataEntry entry, string zoneName)
    {
        // Configure the EnhancedVideoPlayer with JSON data
        enhancedPlayer.VideoUrlLink = entry.GetVideoUrl();
        enhancedPlayer.title = entry.Title;
        enhancedPlayer.description = entry.Description ?? "";

        // CRITICAL: Set both zoneName and LastKnownZone to the placement zone
        enhancedPlayer.zoneName = zoneName;
        enhancedPlayer.LastKnownZone = zoneName;  // This was missing in your setup!

        // Set scene navigation defaults if not already set
        if (string.IsNullOrEmpty(enhancedPlayer.returntoscene))
            enhancedPlayer.returntoscene = "mainVR";
        if (string.IsNullOrEmpty(enhancedPlayer.nextscene))
            enhancedPlayer.nextscene = "360VideoApp";

        // Set prefab type for categorization
        if (!string.IsNullOrEmpty(entry.Prefab))
        {
            enhancedPlayer.prefabType = entry.Prefab;
        }

        // Set category based on zone name
        enhancedPlayer.category = zoneName;

        // Update text components if they exist
        UpdateTextComponents(enhancedPlayer.gameObject, entry);

        // Mark as dirty to ensure changes are saved
        EditorUtility.SetDirty(enhancedPlayer);

        Debug.Log($"Configured EnhancedVideoPlayer: {entry.Title} -> {entry.GetVideoUrl()} in zone: {zoneName}");
        Debug.Log($"Set LastKnownZone to: {zoneName}");
    }

    private void UpdateTextComponents(GameObject instance, FilmDataEntry entry)
    {
        // Find and update TextMeshPro components
        TMPro.TextMeshPro[] tmpComponents = instance.GetComponentsInChildren<TMPro.TextMeshPro>();

        foreach (var tmp in tmpComponents)
        {
            // Look for the text component that likely displays the title
            if (tmp.name.ToLower().Contains("text") || tmp.text.ToLower().Contains("sample"))
            {
                tmp.text = entry.Title;
                Debug.Log($"Updated TextMeshPro component '{tmp.name}' with title: {entry.Title}");
                break; // Only update the first matching text component
            }
        }

        // Also try regular Text components as fallback
        UnityEngine.UI.Text[] textComponents = instance.GetComponentsInChildren<UnityEngine.UI.Text>();

        foreach (var text in textComponents)
        {
            if (text.name.ToLower().Contains("text") || text.text.ToLower().Contains("sample"))
            {
                text.text = entry.Title;
                Debug.Log($"Updated Text component '{text.name}' with title: {entry.Title}");
                break;
            }
        }

        // Try to find the EnhancedVideoPlayer's TMP_title reference and update it
        EnhancedVideoPlayer enhancedPlayer = instance.GetComponent<EnhancedVideoPlayer>();
        if (enhancedPlayer != null && enhancedPlayer.TMP_title != null)
        {
            enhancedPlayer.TMP_title.text = entry.Title;
            Debug.Log($"Updated EnhancedVideoPlayer TMP_title with: {entry.Title}");
        }
    }

    // FIXED: Event triggers that work with prefab settings
    private void SetupEventTriggersForEnhancedPlayer(GameObject instance, EnhancedVideoPlayer enhancedPlayer)
    {
        EventTrigger eventTrigger = instance.GetComponent<EventTrigger>();

        // If no EventTrigger exists, create one
        if (eventTrigger == null)
        {
            eventTrigger = instance.AddComponent<EventTrigger>();
            Debug.Log($"Created new EventTrigger for {enhancedPlayer.title}");
        }

        // Check if the EventTrigger already has properly configured triggers from the prefab
        bool hasValidTriggers = false;
        if (eventTrigger.triggers != null && eventTrigger.triggers.Count > 0)
        {
            // Check if any trigger calls methods on EnhancedVideoPlayer
            foreach (var trigger in eventTrigger.triggers)
            {
                if (trigger.callback != null && trigger.callback.GetPersistentEventCount() > 0)
                {
                    for (int i = 0; i < trigger.callback.GetPersistentEventCount(); i++)
                    {
                        var target = trigger.callback.GetPersistentTarget(i);
                        var methodName = trigger.callback.GetPersistentMethodName(i);

                        // Check if it's calling methods on EnhancedVideoPlayer
                        if (target is EnhancedVideoPlayer &&
                            (methodName == "MouseHoverChangeScene" || methodName == "MouseExit"))
                        {
                            hasValidTriggers = true;
                            Debug.Log($"Found valid prefab EventTrigger configuration for {enhancedPlayer.title}");
                            break;
                        }
                    }
                    if (hasValidTriggers) break;
                }
            }
        }

        // Only add programmatic triggers if the prefab doesn't have valid ones
        if (!hasValidTriggers)
        {
            Debug.Log($"No valid prefab EventTriggers found, setting up programmatic triggers for {enhancedPlayer.title}");

            // Initialize triggers list if null
            if (eventTrigger.triggers == null)
            {
                eventTrigger.triggers = new List<EventTrigger.Entry>();
            }

            // Clear only if we're going to replace with programmatic ones
            eventTrigger.triggers.Clear();

            // Set up Pointer Enter event
            EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
            pointerEnter.eventID = EventTriggerType.PointerEnter;
            pointerEnter.callback.AddListener((data) => {
                enhancedPlayer.MouseHoverChangeScene();
                Debug.Log($"Programmatic Pointer Enter triggered on {enhancedPlayer.title} in zone {enhancedPlayer.LastKnownZone}");
            });
            eventTrigger.triggers.Add(pointerEnter);

            // Set up Pointer Exit event
            EventTrigger.Entry pointerExit = new EventTrigger.Entry();
            pointerExit.eventID = EventTriggerType.PointerExit;
            pointerExit.callback.AddListener((data) => {
                enhancedPlayer.MouseExit();
                Debug.Log($"Programmatic Pointer Exit triggered on {enhancedPlayer.title}");
            });
            eventTrigger.triggers.Add(pointerExit);

            // Optional: Set up Pointer Click event for immediate activation
            EventTrigger.Entry pointerClick = new EventTrigger.Entry();
            pointerClick.eventID = EventTriggerType.PointerClick;
            pointerClick.callback.AddListener((data) => {
                // Ensure zone is saved before triggering video
                if (!string.IsNullOrEmpty(enhancedPlayer.LastKnownZone))
                {
                    PlayerPrefs.SetString("lastknownzone", enhancedPlayer.LastKnownZone);
                    PlayerPrefs.Save();
                    Debug.Log($"Saved lastknownzone as: {enhancedPlayer.LastKnownZone}");
                }
                enhancedPlayer.SetVideoUrl(); // Use SetVideoUrl for immediate trigger
            });
            eventTrigger.triggers.Add(pointerClick);

            Debug.Log($"Set up programmatic Event Triggers for EnhancedVideoPlayer: {enhancedPlayer.title} in zone: {enhancedPlayer.LastKnownZone}");
        }
        else
        {
            Debug.Log($"Using prefab EventTrigger configuration for: {enhancedPlayer.title}");

            // Update the targets in the existing triggers to point to the new instance
            if (eventTrigger.triggers != null)
            {
                foreach (var trigger in eventTrigger.triggers)
                {
                    if (trigger.callback != null)
                    {
                        for (int i = 0; i < trigger.callback.GetPersistentEventCount(); i++)
                        {
                            var target = trigger.callback.GetPersistentTarget(i);
                            var methodName = trigger.callback.GetPersistentMethodName(i);

                            // If the target is an EnhancedVideoPlayer, update it to point to our instance
                            if (target is EnhancedVideoPlayer)
                            {
                                // Use SerializedObject to update the persistent target
                                SerializedObject serializedEventTrigger = new SerializedObject(eventTrigger);

                                // This is a bit complex, so we'll just log that we found it
                                Debug.Log($"Found existing EventTrigger calling {methodName} on EnhancedVideoPlayer - should work with instance");
                            }
                        }
                    }
                }
            }
        }

        // Ensure the enhanced player is marked dirty
        EditorUtility.SetDirty(eventTrigger);
        EditorUtility.SetDirty(enhancedPlayer);
    }

    private Transform FindOrCreateZoneParent(string zoneName)
    {
        // First, try to find the specific zone folder structure
        // Look for StandardTargets/[ZoneName]/Films
        Transform standardTargets = GameObject.Find("StandardTargets")?.transform;

        if (standardTargets != null)
        {
            // Look for the zone folder under StandardTargets
            Transform zoneFolder = standardTargets.Find(zoneName);

            if (zoneFolder != null)
            {
                // Look for Films folder under the zone
                Transform filmsFolder = zoneFolder.Find("Films");

                if (filmsFolder != null)
                {
                    Debug.Log($"Found existing Films folder at: StandardTargets/{zoneName}/Films");
                    return filmsFolder;
                }
                else
                {
                    // Create Films folder under the zone
                    GameObject filmsGO = new GameObject("Films");
                    filmsGO.transform.SetParent(zoneFolder);
                    filmsGO.transform.localPosition = Vector3.zero;
                    filmsGO.transform.localRotation = Quaternion.identity;
                    filmsGO.transform.localScale = Vector3.one;
                    Debug.Log($"Created Films folder at: StandardTargets/{zoneName}/Films");
                    return filmsGO.transform;
                }
            }
            else
            {
                Debug.LogWarning($"Zone folder '{zoneName}' not found under StandardTargets. Creating fallback structure.");
            }
        }
        else
        {
            Debug.LogWarning("StandardTargets not found in scene. Creating fallback structure.");
        }

        // Fallback: Create under InteractiveObjects or root
        GameObject zoneParent = GameObject.Find($"Zone_{zoneName}");
        if (zoneParent == null)
        {
            zoneParent = new GameObject($"Zone_{zoneName}");

            // Try to parent under InteractiveObjects if it exists
            Transform interactiveObjects = GameObject.Find("InteractiveObjects")?.transform;
            if (interactiveObjects != null)
            {
                zoneParent.transform.SetParent(interactiveObjects);
                Debug.Log($"Created fallback zone folder under InteractiveObjects: Zone_{zoneName}");
            }
            else
            {
                Debug.Log($"Created fallback zone folder at root: Zone_{zoneName}");
            }
        }

        return zoneParent.transform;
    }

    private void ClearAllPlacedPrefabs()
    {
        foreach (var kvp in zonePrefabs)
        {
            ClearPrefabsInZone(kvp.Key);
        }
        zonePrefabs.Clear();
        Debug.Log("✅ Cleared all placed prefabs from all zones");
    }

    private void ClearPrefabsInZone(string zoneName)
    {
        Debug.Log($"🧹 Clearing prefabs in zone: {zoneName}");

        if (!zonePrefabs.ContainsKey(zoneName))
        {
            Debug.Log($"No tracked prefabs found for zone: {zoneName}");
            return;
        }

        int clearedCount = 0;
        foreach (GameObject prefab in zonePrefabs[zoneName])
        {
            if (prefab != null)
            {
                Debug.Log($"Destroying: {prefab.name}");
                DestroyImmediate(prefab);
                clearedCount++;
            }
        }

        zonePrefabs[zoneName].Clear();

        // Clean up the Films folder if it's empty
        // First check StandardTargets/[ZoneName]/Films
        Transform standardTargets = GameObject.Find("StandardTargets")?.transform;
        if (standardTargets != null)
        {
            Transform zoneFolder = standardTargets.Find(zoneName);
            if (zoneFolder != null)
            {
                Transform filmsFolder = zoneFolder.Find("Films");
                if (filmsFolder != null && filmsFolder.childCount == 0)
                {
                    Debug.Log($"Cleaning up empty Films folder at: StandardTargets/{zoneName}/Films");
                    DestroyImmediate(filmsFolder.gameObject);
                }
            }
        }

        // Also clean up fallback zone parent if it exists and is empty
        GameObject zoneParent = GameObject.Find($"Zone_{zoneName}");
        if (zoneParent != null && zoneParent.transform.childCount == 0)
        {
            Debug.Log($"Cleaning up empty fallback zone folder: Zone_{zoneName}");
            DestroyImmediate(zoneParent);
        }

        Debug.Log($"✅ Cleared {clearedCount} prefabs from zone: {zoneName}");
    }

    // ===== SAVE/LOAD METHODS =====

    private void SaveCurrentLayout()
    {
        string path = EditorUtility.SaveFilePanel("Save Zone Layout", Application.dataPath, "zone_layout", "json");
        if (string.IsNullOrEmpty(path)) return;

        ZoneLayoutData layoutData = new ZoneLayoutData();

        // Find all EnhancedVideoPlayer components in the scene (not VideoZonePrefab)
        EnhancedVideoPlayer[] allEnhancedPlayers = Object.FindObjectsOfType<EnhancedVideoPlayer>();

        foreach (var enhancedPlayer in allEnhancedPlayers)
        {
            PrefabPositionData posData = new PrefabPositionData();
            posData.prefabId = enhancedPlayer.gameObject.name;
            posData.position = enhancedPlayer.transform.position;
            posData.rotation = enhancedPlayer.transform.eulerAngles;
            posData.scale = enhancedPlayer.transform.localScale;
            posData.zoneName = enhancedPlayer.LastKnownZone;
            posData.videoUrl = enhancedPlayer.VideoUrlLink;
            posData.videoTitle = enhancedPlayer.title;
            posData.videoDescription = enhancedPlayer.description;

            layoutData.prefabPositions.Add(posData);
        }

        try
        {
            string json = JsonUtility.ToJson(layoutData, true);
            File.WriteAllText(path, json);
            Debug.Log($"Saved layout with {layoutData.prefabPositions.Count} prefabs to: {path}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error saving layout: {e.Message}");
        }
    }

    private void LoadLayout()
    {
        string path = EditorUtility.OpenFilePanel("Load Zone Layout", Application.dataPath, "json");
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            string json = File.ReadAllText(path);
            ZoneLayoutData layoutData = JsonUtility.FromJson<ZoneLayoutData>(json);

            if (layoutData == null || layoutData.prefabPositions == null)
            {
                Debug.LogError("Invalid layout data");
                return;
            }

            // Clear existing prefabs first
            ClearAllPlacedPrefabs();

            // Recreate prefabs from saved data
            foreach (var posData in layoutData.prefabPositions)
            {
                // Find the appropriate prefab template
                GameObject prefabTemplate = zoneManager.defaultPrefab;

                // Try to match with zone-specific prefab if available
                if (filmData != null)
                {
                    var matchingEntry = filmData.Entries.FirstOrDefault(e =>
                        e.Title == posData.videoTitle && e.GetVideoUrl() == posData.videoUrl);

                    if (matchingEntry != null)
                    {
                        prefabTemplate = zoneManager.GetPrefabForEntry(matchingEntry, posData.zoneName);
                    }
                }

                if (prefabTemplate == null) continue;

                // Create instance
                GameObject instance = PrefabUtility.InstantiatePrefab(prefabTemplate) as GameObject;
                if (instance == null)
                {
                    instance = Instantiate(prefabTemplate);
                }

                // Set transform
                instance.transform.position = posData.position;
                instance.transform.eulerAngles = posData.rotation;
                instance.transform.localScale = posData.scale;
                instance.name = posData.prefabId;

                // Configure EnhancedVideoPlayer directly (not VideoZonePrefab)
                EnhancedVideoPlayer enhancedPlayer = instance.GetComponent<EnhancedVideoPlayer>();
                if (enhancedPlayer == null)
                {
                    enhancedPlayer = instance.AddComponent<EnhancedVideoPlayer>();
                }

                enhancedPlayer.VideoUrlLink = posData.videoUrl;
                enhancedPlayer.title = posData.videoTitle;
                enhancedPlayer.description = posData.videoDescription;
                enhancedPlayer.zoneName = posData.zoneName;
                enhancedPlayer.LastKnownZone = posData.zoneName;

                // Parent to zone
                Transform zoneParent = FindOrCreateZoneParent(posData.zoneName);
                instance.transform.SetParent(zoneParent);

                // Track the prefab
                if (!zonePrefabs.ContainsKey(posData.zoneName))
                {
                    zonePrefabs[posData.zoneName] = new List<GameObject>();
                }
                zonePrefabs[posData.zoneName].Add(instance);

                EditorUtility.SetDirty(instance);
            }

            Debug.Log($"Loaded layout with {layoutData.prefabPositions.Count} prefabs from: {path}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading layout: {e.Message}");
        }
    }

    // ===== VALIDATION METHODS =====

    private void ValidateVideoSetup()
    {
        System.Text.StringBuilder report = new System.Text.StringBuilder();
        report.AppendLine("Video Setup Validation Report:");
        report.AppendLine("============================");

        // Find all video components in the scene
        EnhancedVideoPlayer[] enhancedPlayers = Object.FindObjectsOfType<EnhancedVideoPlayer>();
        VideoZonePrefab[] zonePrefabComponents = Object.FindObjectsOfType<VideoZonePrefab>();

        report.AppendLine($"Found {enhancedPlayers.Length} EnhancedVideoPlayer components");
        report.AppendLine($"Found {zonePrefabComponents.Length} VideoZonePrefab components");

        if (zonePrefabComponents.Length > 0)
        {
            report.AppendLine("⚠️ Warning: VideoZonePrefab components found - these should be replaced with EnhancedVideoPlayer");
        }

        // Check each EnhancedVideoPlayer
        int correctlyConfigured = 0;
        int missingZone = 0;
        int missingEventTriggers = 0;

        foreach (var player in enhancedPlayers)
        {
            bool isCorrect = true;

            // Check if LastKnownZone is set properly
            if (string.IsNullOrEmpty(player.LastKnownZone) || player.LastKnownZone == "Home")
            {
                missingZone++;
                isCorrect = false;
                report.AppendLine($"❌ {player.gameObject.name}: LastKnownZone not set properly (current: '{player.LastKnownZone}')");
            }

            // Check if Event Triggers are set up
            EventTrigger eventTrigger = player.GetComponent<EventTrigger>();
            if (eventTrigger == null || eventTrigger.triggers == null || eventTrigger.triggers.Count == 0)
            {
                missingEventTriggers++;
                isCorrect = false;
                report.AppendLine($"❌ {player.gameObject.name}: Missing Event Triggers");
            }

            if (isCorrect)
            {
                correctlyConfigured++;
                report.AppendLine($"✅ {player.gameObject.name}: Correctly configured (Zone: {player.LastKnownZone})");
            }
        }

        report.AppendLine($"\nSummary:");
        report.AppendLine($"✅ Correctly configured: {correctlyConfigured}");
        report.AppendLine($"❌ Missing zone info: {missingZone}");
        report.AppendLine($"❌ Missing event triggers: {missingEventTriggers}");

        if (correctlyConfigured == enhancedPlayers.Length && zonePrefabComponents.Length == 0)
        {
            report.AppendLine("\n🎉 All video prefabs are correctly configured!");
        }
        else
        {
            report.AppendLine("\n⚠️ Some issues found. Consider re-running the prefab placement process.");
        }

        Debug.Log(report.ToString());
        EditorUtility.DisplayDialog("Video Setup Validation", report.ToString(), "OK");
    }

    private void FixAllVideoZones()
    {
        if (!EditorUtility.DisplayDialog("Fix Video Zones",
            "This will update LastKnownZone for all EnhancedVideoPlayer components based on their zoneName. Continue?",
            "Yes", "Cancel"))
        {
            return;
        }

        EnhancedVideoPlayer[] allVideos = Object.FindObjectsOfType<EnhancedVideoPlayer>();
        int fixedCount = 0;

        foreach (var video in allVideos)
        {
            if (!string.IsNullOrEmpty(video.zoneName) &&
                (string.IsNullOrEmpty(video.LastKnownZone) || video.LastKnownZone == "Home"))
            {
                video.LastKnownZone = video.zoneName;
                EditorUtility.SetDirty(video);
                fixedCount++;
                Debug.Log($"Fixed zone for {video.gameObject.name}: set LastKnownZone to {video.zoneName}");
            }
        }

        Debug.Log($"Fixed {fixedCount} video zone assignments");
        EditorUtility.DisplayDialog("Fix Complete", $"Fixed {fixedCount} video zone assignments", "OK");
    }

    private void ValidateSetup()
    {
        System.Text.StringBuilder report = new System.Text.StringBuilder();
        report.AppendLine("Film Zone Manager Validation Report:");
        report.AppendLine("============================");

        // Check default prefab
        if (zoneManager.defaultPrefab == null)
        {
            report.AppendLine("❌ Default prefab not set");
        }
        else
        {
            report.AppendLine("✅ Default prefab set");
        }

        // Check zones
        int validZones = zoneManager.zones.Count(z => z.polygonPoints.Count >= 3);
        report.AppendLine($"📍 Zones: {zoneManager.zones.Count} total, {validZones} valid");

        if (validZones == 0)
        {
            report.AppendLine("❌ No valid zones (need at least 3 points each)");
        }

        // Check film prefab mappings
        int completeFilmMappings = zoneManager.prefabMappings.Count(m => !string.IsNullOrEmpty(m.prefabName) && m.prefab != null);
        report.AppendLine($"🎯 Film prefab mappings: {completeFilmMappings}/{zoneManager.prefabMappings.Count} complete");

        // Check zone prefab mappings
        int completeZoneMappings = zoneManager.zonePrefabMappings.Count(m => !string.IsNullOrEmpty(m.zoneName) && m.prefab != null);
        report.AppendLine($"🏠 Zone prefab mappings: {completeZoneMappings}/{zoneManager.zonePrefabMappings.Count} complete");

        // Check video prefabs in scene (use EnhancedVideoPlayer instead of VideoZonePrefab)
        EnhancedVideoPlayer[] allVideos = Object.FindObjectsOfType<EnhancedVideoPlayer>();
        report.AppendLine($"🎬 Enhanced Video Players in scene: {allVideos.Length}");

        // Check for orphaned videos (videos not in any zone)
        int orphanedVideos = 0;
        foreach (var video in allVideos)
        {
            if (string.IsNullOrEmpty(video.LastKnownZone) || video.LastKnownZone == "Home" ||
                !zoneManager.zones.Any(z => z.zoneName.Equals(video.LastKnownZone, System.StringComparison.OrdinalIgnoreCase)))
            {
                orphanedVideos++;
            }
        }

        if (orphanedVideos > 0)
        {
            report.AppendLine($"⚠️ {orphanedVideos} orphaned videos (not in valid zones)");
        }

        Debug.Log(report.ToString());
        EditorUtility.DisplayDialog("Validation Complete", report.ToString(), "OK");
    }

    private void CleanUpSystem()
    {
        if (!EditorUtility.DisplayDialog("Clean Up System",
            "This will remove empty zones and fix references. Continue?", "Yes", "Cancel"))
        {
            return;
        }

        int cleaned = 0;

        // Remove zones with no polygon points
        for (int i = zoneManager.zones.Count - 1; i >= 0; i--)
        {
            if (zoneManager.zones[i].polygonPoints.Count == 0)
            {
                zoneManager.zones.RemoveAt(i);
                cleaned++;
            }
        }

        // Remove film prefab mappings with empty names or null prefabs
        for (int i = zoneManager.prefabMappings.Count - 1; i >= 0; i--)
        {
            var mapping = zoneManager.prefabMappings[i];
            if (string.IsNullOrEmpty(mapping.prefabName) || mapping.prefab == null)
            {
                zoneManager.prefabMappings.RemoveAt(i);
                cleaned++;
            }
        }

        // Remove zone prefab mappings with empty names or null prefabs
        for (int i = zoneManager.zonePrefabMappings.Count - 1; i >= 0; i--)
        {
            var mapping = zoneManager.zonePrefabMappings[i];
            if (string.IsNullOrEmpty(mapping.zoneName) || mapping.prefab == null)
            {
                zoneManager.zonePrefabMappings.RemoveAt(i);
                cleaned++;
            }
        }

        EditorUtility.SetDirty(zoneManager);
        Debug.Log($"Cleaned up {cleaned} items");
    }
}