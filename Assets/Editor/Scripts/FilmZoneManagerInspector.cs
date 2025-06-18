using UnityEngine;
using UnityEditor;
using System.Linq;

// Custom inspector for FilmZoneManager
[CustomEditor(typeof(FilmZoneManager))]
public class FilmZoneManagerInspector : Editor
{
    private FilmZoneManager zoneManager;
    private bool showZones = true;
    private bool showPrefabs = true;
    private bool showZonePrefabs = true;
    private bool showSettings = true;
    private bool showDebug = false;

    private void OnEnable()
    {
        zoneManager = (FilmZoneManager)target;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawHeader();
        EditorGUILayout.Space(10);

        DrawZonesSection();
        EditorGUILayout.Space(10);

        DrawPrefabsSection();
        EditorGUILayout.Space(10);

        DrawZonePrefabsSection();
        EditorGUILayout.Space(10);

        DrawSettingsSection();
        EditorGUILayout.Space(10);

        DrawDebugSection();
        EditorGUILayout.Space(10);

        DrawUtilityButtons();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical("box");

        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
        headerStyle.fontSize = 16;
        headerStyle.alignment = TextAnchor.MiddleCenter;

        EditorGUILayout.LabelField("Film Zone Video Manager", headerStyle);

        EditorGUILayout.Space(5);

        // Status information
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Zones: {zoneManager.zones.Count}", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"Film Prefabs: {zoneManager.prefabMappings.Count}", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"Zone Prefabs: {zoneManager.zonePrefabMappings.Count}", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();

        // Updated to use EnhancedVideoPlayer instead of VideoZonePrefab
        int totalVideos = Object.FindObjectsOfType<EnhancedVideoPlayer>().Length;
        EditorGUILayout.LabelField($"Videos in Scene: {totalVideos}", EditorStyles.miniLabel);

        EditorGUILayout.EndVertical();
    }

    private void DrawZonesSection()
    {
        showZones = EditorGUILayout.Foldout(showZones, "Zones Configuration", true);
        if (!showZones) return;

        EditorGUI.indentLevel++;

        SerializedProperty zonesProperty = serializedObject.FindProperty("zones");

        if (zoneManager.zones.Count == 0)
        {
            EditorGUILayout.HelpBox("No zones defined. Use the Zone Editor tool to create zones.", MessageType.Info);
        }
        else
        {
            for (int i = 0; i < zoneManager.zones.Count; i++)
            {
                DrawZoneEntry(i);
            }
        }

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Zone"))
        {
            FilmZone newZone = new FilmZone();
            newZone.zoneName = $"Zone_{zoneManager.zones.Count + 1}";
            newZone.gizmoColor = GetRandomColor();
            zoneManager.zones.Add(newZone);
            EditorUtility.SetDirty(zoneManager);
        }

        if (GUILayout.Button("Open Film Zone Editor"))
        {
            FilmZoneEditorWindow.ShowWindow();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.indentLevel--;
    }

    private void DrawZoneEntry(int index)
    {
        FilmZone zone = zoneManager.zones[index];

        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();

        // Zone name
        zone.zoneName = EditorGUILayout.TextField(zone.zoneName, GUILayout.ExpandWidth(true));

        // Gizmo color
        zone.gizmoColor = EditorGUILayout.ColorField(zone.gizmoColor, GUILayout.Width(40));

        // Show gizmos toggle
        zone.showGizmos = EditorGUILayout.Toggle(zone.showGizmos, GUILayout.Width(20));

        // Remove button
        if (GUILayout.Button("X", GUILayout.Width(25)))
        {
            zoneManager.zones.RemoveAt(index);
            EditorUtility.SetDirty(zoneManager);
            return;
        }

        EditorGUILayout.EndHorizontal();

        // Zone info
        EditorGUI.indentLevel++;
        EditorGUILayout.LabelField($"Polygon Points: {zone.polygonPoints.Count}", EditorStyles.miniLabel);

        // Show zone prefab if assigned
        var zonePrefab = zoneManager.GetZonePrefab(zone.zoneName);
        if (zonePrefab != null)
        {
            EditorGUILayout.LabelField($"Zone Prefab: {zonePrefab.name}", EditorStyles.miniLabel);
        }

        // Count videos in this zone - UPDATED to use EnhancedVideoPlayer
        EnhancedVideoPlayer[] videosInZone = Object.FindObjectsOfType<EnhancedVideoPlayer>()
            .Where(v => v.LastKnownZone.Equals(zone.zoneName, System.StringComparison.OrdinalIgnoreCase))
            .ToArray();

        EditorGUILayout.LabelField($"Videos in Zone: {videosInZone.Length}", EditorStyles.miniLabel);

        if (videosInZone.Length > 0)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select Videos", GUILayout.Width(100)))
            {
                Selection.objects = videosInZone.Select(v => v.gameObject).ToArray();
            }

            if (GUILayout.Button("Focus Zone", GUILayout.Width(100)))
            {
                FocusOnZone(zone);
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUI.indentLevel--;

        EditorGUILayout.EndVertical();
    }

    private void DrawPrefabsSection()
    {
        showPrefabs = EditorGUILayout.Foldout(showPrefabs, "Film Prefab Configuration", true);
        if (!showPrefabs) return;

        EditorGUI.indentLevel++;

        // Default prefab
        SerializedProperty defaultPrefabProp = serializedObject.FindProperty("defaultPrefab");
        EditorGUILayout.PropertyField(defaultPrefabProp, new GUIContent("Default Prefab"));

        if (zoneManager.defaultPrefab == null)
        {
            EditorGUILayout.HelpBox("Default prefab is required for video entries without specific prefab assignments.", MessageType.Warning);
        }

        EditorGUILayout.Space(5);

        // Prefab mappings
        EditorGUILayout.LabelField("Film Prefab Mappings:", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("These prefabs are used when a film entry specifies a custom 'Prefab' field in the JSON.", MessageType.Info);

        for (int i = 0; i < zoneManager.prefabMappings.Count; i++)
        {
            DrawPrefabMapping(i);
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Film Mapping"))
        {
            zoneManager.prefabMappings.Add(new FilmZoneManager.PrefabMapping());
            EditorUtility.SetDirty(zoneManager);
        }

        if (GUILayout.Button("Auto-Detect from JSON"))
        {
            AutoDetectPrefabMappings();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.indentLevel--;
    }

    private void DrawZonePrefabsSection()
    {
        showZonePrefabs = EditorGUILayout.Foldout(showZonePrefabs, "Zone Prefab Configuration", true);
        if (!showZonePrefabs) return;

        EditorGUI.indentLevel++;

        EditorGUILayout.LabelField("Zone Prefab Mappings:", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("These prefabs are used for all films in a zone when no film-specific prefab is found.\nPriority: Film Prefab → Zone Prefab → Default Prefab", MessageType.Info);

        for (int i = 0; i < zoneManager.zonePrefabMappings.Count; i++)
        {
            DrawZonePrefabMapping(i);
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Zone Mapping"))
        {
            zoneManager.zonePrefabMappings.Add(new FilmZoneManager.ZonePrefabMapping());
            EditorUtility.SetDirty(zoneManager);
        }

        if (GUILayout.Button("Auto-Add Missing Zones"))
        {
            AutoAddMissingZonePrefabMappings();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.indentLevel--;
    }

    private void DrawPrefabMapping(int index)
    {
        var mapping = zoneManager.prefabMappings[index];

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField("Film Type:", GUILayout.Width(70));
        mapping.prefabName = EditorGUILayout.TextField(mapping.prefabName, GUILayout.Width(100));
        mapping.prefab = (GameObject)EditorGUILayout.ObjectField(mapping.prefab, typeof(GameObject), false);

        if (GUILayout.Button("X", GUILayout.Width(25)))
        {
            zoneManager.prefabMappings.RemoveAt(index);
            EditorUtility.SetDirty(zoneManager);
            return;
        }

        EditorGUILayout.EndHorizontal();

        // Validation
        if (!string.IsNullOrEmpty(mapping.prefabName) && mapping.prefab == null)
        {
            EditorGUILayout.HelpBox($"Film prefab '{mapping.prefabName}' needs a GameObject assigned.", MessageType.Warning);
        }
    }

    private void DrawZonePrefabMapping(int index)
    {
        var mapping = zoneManager.zonePrefabMappings[index];

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField("Zone:", GUILayout.Width(50));

        // Dropdown for zone selection if zones exist
        if (zoneManager.zones.Count > 0)
        {
            string[] zoneNames = zoneManager.zones.Select(z => z.zoneName).ToArray();
            int currentIndex = System.Array.IndexOf(zoneNames, mapping.zoneName);
            if (currentIndex == -1) currentIndex = 0;

            int newIndex = EditorGUILayout.Popup(currentIndex, zoneNames, GUILayout.Width(120));
            if (newIndex >= 0 && newIndex < zoneNames.Length)
            {
                mapping.zoneName = zoneNames[newIndex];
            }
        }
        else
        {
            mapping.zoneName = EditorGUILayout.TextField(mapping.zoneName, GUILayout.Width(120));
        }

        mapping.prefab = (GameObject)EditorGUILayout.ObjectField(mapping.prefab, typeof(GameObject), false);

        if (GUILayout.Button("X", GUILayout.Width(25)))
        {
            zoneManager.zonePrefabMappings.RemoveAt(index);
            EditorUtility.SetDirty(zoneManager);
            return;
        }

        EditorGUILayout.EndHorizontal();

        // Show validation and info
        if (!string.IsNullOrEmpty(mapping.zoneName))
        {
            bool zoneExists = zoneManager.zones.Any(z => z.zoneName.Equals(mapping.zoneName, System.StringComparison.OrdinalIgnoreCase));
            if (!zoneExists)
            {
                EditorGUILayout.HelpBox($"Zone '{mapping.zoneName}' doesn't exist yet.", MessageType.Warning);
            }

            if (mapping.prefab == null)
            {
                EditorGUILayout.HelpBox($"Zone '{mapping.zoneName}' needs a prefab assigned.", MessageType.Warning);
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawSettingsSection()
    {
        showSettings = EditorGUILayout.Foldout(showSettings, "Placement Settings", true);
        if (!showSettings) return;

        EditorGUI.indentLevel++;

        SerializedProperty prefabSpacingProp = serializedObject.FindProperty("prefabSpacing");
        SerializedProperty placeOnTerrainProp = serializedObject.FindProperty("placeOnTerrain");
        SerializedProperty terrainLayerProp = serializedObject.FindProperty("terrainLayer");
        SerializedProperty showZoneGizmosProp = serializedObject.FindProperty("showZoneGizmos");

        EditorGUILayout.PropertyField(prefabSpacingProp);
        EditorGUILayout.PropertyField(placeOnTerrainProp);

        if (zoneManager.placeOnTerrain)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(terrainLayerProp);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.PropertyField(showZoneGizmosProp);

        EditorGUI.indentLevel--;
    }

    private void DrawDebugSection()
    {
        showDebug = EditorGUILayout.Foldout(showDebug, "Debug & Statistics", true);
        if (!showDebug) return;

        EditorGUI.indentLevel++;

        // Zone statistics
        EditorGUILayout.LabelField("Zone Statistics:", EditorStyles.boldLabel);

        foreach (var zone in zoneManager.zones)
        {
            // UPDATED to use EnhancedVideoPlayer instead of VideoZonePrefab
            EnhancedVideoPlayer[] videosInZone = Object.FindObjectsOfType<EnhancedVideoPlayer>()
                .Where(v => v.LastKnownZone.Equals(zone.zoneName, System.StringComparison.OrdinalIgnoreCase))
                .ToArray();

            var zonePrefab = zoneManager.GetZonePrefab(zone.zoneName);
            string zonePrefabInfo = zonePrefab != null ? $" [Zone Prefab: {zonePrefab.name}]" : "";

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"• {zone.zoneName}:", GUILayout.Width(100));
            EditorGUILayout.LabelField($"{videosInZone.Length} videos, {zone.polygonPoints.Count} points{zonePrefabInfo}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(5);

        // Prefab priority system explanation
        EditorGUILayout.LabelField("Prefab Priority System:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("1. Film Prefab (from JSON 'Prefab' field)", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("2. Zone Prefab (assigned to specific zone)", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("3. Default Prefab (fallback for all)", EditorStyles.miniLabel);

        EditorGUILayout.Space(5);

        // System validation
        EditorGUILayout.LabelField("System Validation:", EditorStyles.boldLabel);

        bool hasDefaultPrefab = zoneManager.defaultPrefab != null;
        bool hasZones = zoneManager.zones.Count > 0;
        bool hasValidZones = zoneManager.zones.Any(z => z.polygonPoints.Count >= 3);
        bool hasFilmPrefabs = zoneManager.prefabMappings.Any(p => !string.IsNullOrEmpty(p.prefabName) && p.prefab != null);
        bool hasZonePrefabs = zoneManager.zonePrefabMappings.Any(z => !string.IsNullOrEmpty(z.zoneName) && z.prefab != null);

        DrawValidationItem("Default Prefab Set", hasDefaultPrefab);
        DrawValidationItem("Zones Defined", hasZones);
        DrawValidationItem("Valid Zones (3+ points)", hasValidZones);
        DrawValidationItem("Film Prefabs Configured", hasFilmPrefabs);
        DrawValidationItem("Zone Prefabs Configured", hasZonePrefabs);

        EditorGUI.indentLevel--;
    }

    private void DrawValidationItem(string label, bool isValid)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"• {label}:", GUILayout.Width(200));

        GUIStyle statusStyle = new GUIStyle(EditorStyles.miniLabel);
        statusStyle.normal.textColor = isValid ? Color.green : Color.red;

        EditorGUILayout.LabelField(isValid ? "✓ Valid" : "✗ Invalid", statusStyle);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawUtilityButtons()
    {
        EditorGUILayout.LabelField("Utilities:", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Open Film Zone Editor"))
        {
            FilmZoneEditorWindow.ShowWindow();
        }

        if (GUILayout.Button("Select All Videos"))
        {
            // UPDATED to use EnhancedVideoPlayer instead of VideoZonePrefab
            EnhancedVideoPlayer[] allVideos = Object.FindObjectsOfType<EnhancedVideoPlayer>();
            Selection.objects = allVideos.Select(v => v.gameObject).ToArray();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Validate Setup"))
        {
            ValidateSetup();
        }

        if (GUILayout.Button("Clean Up"))
        {
            CleanUpSystem();
        }

        EditorGUILayout.EndHorizontal();
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

    private void AutoDetectPrefabMappings()
    {
        // UPDATED to analyze existing EnhancedVideoPlayer entries
        EnhancedVideoPlayer[] allVideos = Object.FindObjectsOfType<EnhancedVideoPlayer>();

        var uniquePrefabTypes = allVideos
            .Where(v => !string.IsNullOrEmpty(v.prefabType))
            .Select(v => v.prefabType)
            .Distinct()
            .ToList();

        foreach (string prefabType in uniquePrefabTypes)
        {
            if (!zoneManager.prefabMappings.Any(m => m.prefabName == prefabType))
            {
                zoneManager.prefabMappings.Add(new FilmZoneManager.PrefabMapping
                {
                    prefabName = prefabType,
                    prefab = null
                });
            }
        }

        EditorUtility.SetDirty(zoneManager);
        Debug.Log($"Auto-detected {uniquePrefabTypes.Count} prefab types: {string.Join(", ", uniquePrefabTypes)}");
    }

    private void AutoAddMissingZonePrefabMappings()
    {
        int addedCount = 0;

        foreach (var zone in zoneManager.zones)
        {
            if (!zoneManager.zonePrefabMappings.Any(m => m.zoneName.Equals(zone.zoneName, System.StringComparison.OrdinalIgnoreCase)))
            {
                zoneManager.zonePrefabMappings.Add(new FilmZoneManager.ZonePrefabMapping
                {
                    zoneName = zone.zoneName,
                    prefab = null
                });
                addedCount++;
            }
        }

        if (addedCount > 0)
        {
            EditorUtility.SetDirty(zoneManager);
            Debug.Log($"Added {addedCount} zone prefab mappings. Remember to assign the prefab GameObjects!");
        }
        else
        {
            Debug.Log("All zones already have prefab mappings.");
        }
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

        // Check video prefabs in scene - UPDATED to use EnhancedVideoPlayer
        EnhancedVideoPlayer[] allVideos = Object.FindObjectsOfType<EnhancedVideoPlayer>();
        report.AppendLine($"🎬 Enhanced Video Players in scene: {allVideos.Length}");

        // Check for orphaned videos (videos not in any zone)
        int orphanedVideos = 0;
        foreach (var video in allVideos)
        {
            if (string.IsNullOrEmpty(video.LastKnownZone) ||
                !zoneManager.zones.Any(z => z.zoneName.Equals(video.LastKnownZone, System.StringComparison.OrdinalIgnoreCase)))
            {
                orphanedVideos++;
            }
        }

        if (orphanedVideos > 0)
        {
            report.AppendLine($"⚠️ {orphanedVideos} orphaned videos (not in valid zones)");
        }

        // Prefab priority analysis
        report.AppendLine("\nPrefab Priority Analysis:");
        foreach (var zone in zoneManager.zones)
        {
            var zonePrefab = zoneManager.GetZonePrefab(zone.zoneName);
            string prefabStatus = zonePrefab != null ? $"has zone prefab ({zonePrefab.name})" : "uses default prefab";
            report.AppendLine($"• {zone.zoneName}: {prefabStatus}");
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

    private Color GetRandomColor()
    {
        return new Color(
            UnityEngine.Random.Range(0.3f, 1f),
            UnityEngine.Random.Range(0.3f, 1f),
            UnityEngine.Random.Range(0.3f, 1f),
            0.7f
        );
    }
}