using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;
using System;

// ===== DATA STRUCTURES FOR EDITOR =====
// These are data-only classes for JSON parsing, not MonoBehaviour duplicates

[System.Serializable]
public class FilmDataEntry
{
    public string FileName;          // IGNORED
    public string PublicUrl;         // MANDATORY - video URL (used as unique key)
    public string BucketPath;        // IGNORED
    public string Title;             // MANDATORY - video title
    public string Description;       // OPTIONAL - video description
    public string Prefab;           // OPTIONAL - specific prefab name
    public string Zone;             // IGNORED - legacy field
    public string[] Zones;          // MANDATORY - array of zones to place this video in

    public string GetVideoUrl() => PublicUrl;
    public bool HasCustomPrefab() => !string.IsNullOrEmpty(Prefab);

    public string[] GetPlacementZones()
    {
        if (Zones != null && Zones.Length > 0)
        {
            var validZones = new List<string>();
            foreach (var zone in Zones)
            {
                if (!string.IsNullOrEmpty(zone.Trim()))
                {
                    validZones.Add(zone.Trim());
                }
            }
            return validZones.ToArray();
        }

        if (!string.IsNullOrEmpty(Zone))
        {
            return new string[] { Zone.Trim() };
        }

        return new string[0];
    }
}

[System.Serializable]
public class FilmDataCollection
{
    public FilmDataEntry[] Entries;
}

[System.Serializable]
public class FilmZone
{
    public string zoneName;
    public List<Vector3> polygonPoints = new List<Vector3>();
    public Color gizmoColor = Color.blue;
    public bool showGizmos = true;

    public bool IsPointInZone(Vector3 point)
    {
        if (polygonPoints.Count < 3) return false;

        Vector2 testPoint = new Vector2(point.x, point.z);
        Vector2[] polygon = new Vector2[polygonPoints.Count];

        for (int i = 0; i < polygonPoints.Count; i++)
        {
            polygon[i] = new Vector2(polygonPoints[i].x, polygonPoints[i].z);
        }

        return IsPointInPolygon(testPoint, polygon);
    }

    private bool IsPointInPolygon(Vector2 point, Vector2[] polygon)
    {
        int count = 0;
        for (int i = 0; i < polygon.Length; i++)
        {
            Vector2 p1 = polygon[i];
            Vector2 p2 = polygon[(i + 1) % polygon.Length];

            if (((p1.y > point.y) != (p2.y > point.y)) &&
                (point.x < (p2.x - p1.x) * (point.y - p1.y) / (p2.y - p1.y) + p1.x))
            {
                count++;
            }
        }
        return (count % 2) == 1;
    }

    public float GetZoneHeight()
    {
        if (polygonPoints.Count == 0) return 0f;
        float totalHeight = 0f;
        foreach (var point in polygonPoints) totalHeight += point.y;
        return totalHeight / polygonPoints.Count;
    }

    public Vector3 GetRandomPointInZoneAtZoneHeight()
    {
        if (polygonPoints.Count < 3) return Vector3.zero;

        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;

        foreach (var point in polygonPoints)
        {
            minX = Mathf.Min(minX, point.x);
            maxX = Mathf.Max(maxX, point.x);
            minZ = Mathf.Min(minZ, point.z);
            maxZ = Mathf.Max(maxZ, point.z);
        }

        float zoneHeight = GetZoneHeight();

        for (int attempts = 0; attempts < 100; attempts++)
        {
            float x = UnityEngine.Random.Range(minX, maxX);
            float z = UnityEngine.Random.Range(minZ, maxZ);
            Vector2 testPoint2D = new Vector2(x, z);

            if (IsPointInZone2D(testPoint2D))
            {
                return new Vector3(x, zoneHeight, z);
            }
        }

        return new Vector3((minX + maxX) / 2, zoneHeight, (minZ + maxZ) / 2);
    }

    public bool IsPointInZone2D(Vector2 point)
    {
        if (polygonPoints.Count < 3) return false;

        Vector2[] polygon = new Vector2[polygonPoints.Count];
        for (int i = 0; i < polygonPoints.Count; i++)
        {
            polygon[i] = new Vector2(polygonPoints[i].x, polygonPoints[i].z);
        }

        return IsPointInPolygon(point, polygon);
    }

    public void ClearAllPoints()
    {
        polygonPoints.Clear();
        Debug.Log($"Cleared all polygon points for zone: {zoneName}");
    }
}

// ===== EDITOR WINDOW =====

/// <summary>
/// Editor window for managing film zones and importing JSON data
/// Now uses the actual runtime classes instead of stub duplicates
/// </summary>
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
            zoneManager = UnityEngine.Object.FindObjectsByType<FilmZoneManager>(FindObjectsSortMode.None).FirstOrDefault();
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
        EditorGUILayout.EndHorizontal();

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
                foreach (var entry in filmData.Entries.Take(5))
                {
                    string prefabInfo = entry.HasCustomPrefab() ? $" [Prefab: {entry.Prefab}]" : " [Default]";
                    EditorGUILayout.LabelField($"• {entry.Title} ({string.Join(", ", entry.GetPlacementZones())}){prefabInfo}", EditorStyles.miniLabel);
                }
                if (filmData.Entries.Length > 5)
                {
                    EditorGUILayout.LabelField($"... and {filmData.Entries.Length - 5} more", EditorStyles.miniLabel);
                }
                EditorGUI.indentLevel--;
            }
        }
    }

    private void DrawZoneEditorSection()
    {
        showZoneEditor = EditorGUILayout.Foldout(showZoneEditor, "Zone Editor");
        if (!showZoneEditor) return;

        EditorGUI.indentLevel++;

        if (zoneManager == null)
        {
            EditorGUILayout.HelpBox("Film Zone Manager required", MessageType.Warning);
            EditorGUI.indentLevel--;
            return;
        }

        // Zone height information
        DrawZoneHeightInfo();
        EditorGUILayout.Space(5);

        // Zone list
        EditorGUILayout.LabelField("Existing Zones:", EditorStyles.boldLabel);

        for (int i = 0; i < zoneManager.zones.Count; i++)
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
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

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("💥", GUILayout.Width(30)))
            {
                NuclearDeleteZone(i);
                break;
            }
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                RegularDeleteZone(i);
                break;
            }
            EditorGUILayout.EndHorizontal();

            var zone = zoneManager.zones[i];
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Points: {zone.polygonPoints.Count}", EditorStyles.miniLabel, GUILayout.Width(70));
            EditorGUILayout.LabelField($"Height: {zone.GetZoneHeight():F1}", EditorStyles.miniLabel, GUILayout.Width(80));

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
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
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
        EditorGUILayout.EndHorizontal();

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

            if (tempZonePoints.Count > 0)
            {
                EditorGUILayout.LabelField($"Current Points ({tempZonePoints.Count}):", EditorStyles.boldLabel);

                for (int i = tempZonePoints.Count - 1; i >= 0; i--)
                {
                    EditorGUILayout.BeginHorizontal();
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
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
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
            EditorGUILayout.EndHorizontal();
        }

        EditorGUI.indentLevel--;
    }

    private void DrawZoneHeightInfo()
    {
        showHeightInfo = EditorGUILayout.Foldout(showHeightInfo, "Zone Height Information");
        if (!showHeightInfo || zoneManager == null) return;

        EditorGUI.indentLevel++;

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
            EditorGUILayout.LabelField($"{zone.zoneName}:", GUILayout.Width(100));
            EditorGUILayout.LabelField($"Avg Y: {avgY:F1}", GUILayout.Width(80));
            EditorGUILayout.LabelField($"Range: {minY:F1} to {maxY:F1}", GUILayout.Width(120));

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
            EditorGUILayout.EndHorizontal();
        }

        EditorGUI.indentLevel--;
    }

    private void DrawPrefabPlacerSection()
    {
        showPrefabPlacer = EditorGUILayout.Foldout(showPrefabPlacer, "Prefab Placer");
        if (!showPrefabPlacer) return;

        EditorGUI.indentLevel++;

        if (zoneManager == null || filmData == null)
        {
            EditorGUILayout.HelpBox("Film Zone Manager and imported JSON data required", MessageType.Warning);
            EditorGUI.indentLevel--;
            return;
        }

        if (zoneManager.defaultPrefab == null)
        {
            EditorGUILayout.HelpBox("Default prefab not set in FilmZoneManager. This is required for placing prefabs.", MessageType.Error);
            EditorGUI.indentLevel--;
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

        EditorGUI.indentLevel--;
    }

    private void DrawSaveLoadSection()
    {
        showSaveLoad = EditorGUILayout.Foldout(showSaveLoad, "Save/Load Layout");
        if (!showSaveLoad) return;

        EditorGUI.indentLevel++;

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Save Current Layout"))
        {
            SaveCurrentLayout();
        }

        if (GUILayout.Button("Load Layout"))
        {
            LoadLayout();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.indentLevel--;
    }

    private void DrawUtilityButtons()
    {
        EditorGUILayout.LabelField("Utilities:", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Select All Videos"))
        {
            EnhancedVideoPlayer[] allVideos = UnityEngine.Object.FindObjectsByType<EnhancedVideoPlayer>(FindObjectsSortMode.None);
            Selection.objects = allVideos.Select(v => v.gameObject).ToArray();
        }

        if (GUILayout.Button("Validate Setup"))
        {
            ValidateSetup();
        }
        EditorGUILayout.EndHorizontal();
    }

    // ===== ZONE MANAGEMENT METHODS =====

    private void CreateZoneManager()
    {
        GameObject go = new GameObject("FilmZoneManager");
        zoneManager = go.AddComponent<FilmZoneManager>();
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
                CreateZonesFromImportedData();
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

    private void CreateZonesFromImportedData()
    {
        if (zoneManager == null || filmData == null) return;

        var uniqueZones = new HashSet<string>();

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

    // Continue with rest of methods...
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

    private void NuclearDeleteZone(int zoneIndex)
    {
        if (zoneIndex < 0 || zoneIndex >= zoneManager.zones.Count) return;

        string zoneName = zoneManager.zones[zoneIndex].zoneName;

        if (EditorUtility.DisplayDialog("💥 Nuclear Delete Zone",
            $"⚠️ WARNING: This will COMPLETELY DELETE zone '{zoneName}'!",
            "💥 NUCLEAR DELETE", "Cancel"))
        {
            zoneManager.NuclearDeleteZone(zoneIndex);

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
            $"Clear all polygon points from zone '{zoneName}'?",
            "Clear Points", "Cancel"))
        {
            zoneManager.ClearZonePoints(zoneIndex);

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

        Vector3 center = Vector3.zero;
        foreach (var point in zone.polygonPoints)
        {
            center += point;
        }
        center /= zone.polygonPoints.Count;

        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView != null)
        {
            sceneView.pivot = center;
            sceneView.size = 20f;
            sceneView.Repaint();
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!isEditingZone) return;

        Event currentEvent = Event.current;

        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
        {
            if (currentEvent.shift)
            {
                FinishEditingZone();
                currentEvent.Use();
            }
            else if (currentEvent.control)
            {
                DeleteNearestPoint(currentEvent.mousePosition);
                currentEvent.Use();
            }
            else
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, Mathf.Infinity))
                {
                    tempZonePoints.Add(hit.point);
                    currentEvent.Use();
                    SceneView.RepaintAll();
                }
                else
                {
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

        if (tempZonePoints.Count > 0)
        {
            Color zoneColor = Color.yellow;
            if (selectedZoneIndex >= 0 && selectedZoneIndex < zoneManager.zones.Count)
            {
                zoneColor = zoneManager.zones[selectedZoneIndex].gizmoColor;
            }

            Handles.color = zoneColor;

            for (int i = 0; i < tempZonePoints.Count; i++)
            {
                float sphereSize = HandleUtility.GetHandleSize(tempZonePoints[i]) * 0.1f;
                Handles.SphereHandleCap(0, tempZonePoints[i], Quaternion.identity, sphereSize, EventType.Repaint);

                if (i > 0)
                {
                    Handles.DrawLine(tempZonePoints[i - 1], tempZonePoints[i]);
                }
            }

            if (tempZonePoints.Count > 2)
            {
                Handles.DrawLine(tempZonePoints[tempZonePoints.Count - 1], tempZonePoints[0]);
            }
        }
    }

    private void DeleteNearestPoint(Vector2 mousePosition)
    {
        if (tempZonePoints.Count == 0) return;

        float closestDistance = float.MaxValue;
        int closestIndex = -1;

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

        if (closestIndex >= 0 && closestDistance < 50f)
        {
            tempZonePoints.RemoveAt(closestIndex);
            SceneView.RepaintAll();
        }
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

        foreach (var entry in filmData.Entries)
        {
            string[] placementZones = entry.GetPlacementZones();

            foreach (string zoneName in placementZones)
            {
                if (!string.IsNullOrEmpty(zoneName))
                {
                    PlacePrefabForEntry(entry, zoneName);
                    totalPlaced++;
                }
            }
        }

        Debug.Log($"✅ Placed {totalPlaced} total prefabs");
    }

    private void PlacePrefabForEntry(FilmDataEntry entry, string zoneName)
    {
        if (zoneManager == null || entry == null) return;

        FilmZone targetZone = zoneManager.GetZoneByName(zoneName);
        if (targetZone == null)
        {
            Debug.LogWarning($"Zone '{zoneName}' not found for entry '{entry.Title}'");
            return;
        }

        GameObject prefabTemplate = zoneManager.GetPrefabForEntry(entry, zoneName);
        if (prefabTemplate == null)
        {
            Debug.LogError($"No prefab found for entry '{entry.Title}'");
            return;
        }

        Vector3 placementPosition = targetZone.GetRandomPointInZoneAtZoneHeight();

        GameObject instance = PrefabUtility.InstantiatePrefab(prefabTemplate) as GameObject;
        if (instance == null)
        {
            instance = Instantiate(prefabTemplate);
        }

        instance.transform.position = placementPosition;
        instance.name = $"Video_{entry.Title}_{zoneName}";

        SetupVideoSystemOnInstance(instance, entry, zoneName);

        if (!zonePrefabs.ContainsKey(zoneName))
        {
            zonePrefabs[zoneName] = new List<GameObject>();
        }
        zonePrefabs[zoneName].Add(instance);

        EditorUtility.SetDirty(instance);
    }

    private void SetupVideoSystemOnInstance(GameObject instance, FilmDataEntry entry, string zoneName)
    {
        if (instance == null || entry == null) return;

        BoxCollider boxCollider = instance.GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = instance.AddComponent<BoxCollider>();
        }
        boxCollider.isTrigger = true;

        EnhancedVideoPlayer enhancedPlayer = instance.GetComponent<EnhancedVideoPlayer>();
        if (enhancedPlayer == null)
        {
            enhancedPlayer = instance.AddComponent<EnhancedVideoPlayer>();
        }

        enhancedPlayer.VideoUrlLink = entry.GetVideoUrl();
        enhancedPlayer.title = entry.Title;
        enhancedPlayer.description = entry.Description ?? "";
        enhancedPlayer.zoneName = zoneName;
        enhancedPlayer.LastKnownZone = zoneName;

        if (string.IsNullOrEmpty(enhancedPlayer.returntoscene))
            enhancedPlayer.returntoscene = "mainVR";
        if (string.IsNullOrEmpty(enhancedPlayer.nextscene))
            enhancedPlayer.nextscene = "360VideoApp";

        EditorUtility.SetDirty(enhancedPlayer);
    }

    private void ClearAllPlacedPrefabs()
    {
        // Clear from dictionary tracking
        foreach (var kvp in zonePrefabs)
        {
            foreach (GameObject prefab in kvp.Value)
            {
                if (prefab != null)
                {
                    DestroyImmediate(prefab);
                }
            }
        }
        zonePrefabs.Clear();

        // Also clear any EnhancedVideoPlayer objects in scene
        EnhancedVideoPlayer[] allVideos = UnityEngine.Object.FindObjectsByType<EnhancedVideoPlayer>(FindObjectsSortMode.None);
        foreach (var video in allVideos)
        {
            if (video != null && video.gameObject != null)
            {
                DestroyImmediate(video.gameObject);
            }
        }

        Debug.Log("✅ Cleared all placed prefabs from all zones");
    }

    // ===== SAVE/LOAD METHODS =====

    private void SaveCurrentLayout()
    {
        string path = EditorUtility.SaveFilePanel("Save Zone Layout", Application.dataPath, "zone_layout", "json");
        if (string.IsNullOrEmpty(path)) return;

        ZoneLayoutData layoutData = new ZoneLayoutData();

        EnhancedVideoPlayer[] allEnhancedPlayers = UnityEngine.Object.FindObjectsByType<EnhancedVideoPlayer>(FindObjectsSortMode.None);

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

            ClearAllPlacedPrefabs();

            foreach (var posData in layoutData.prefabPositions)
            {
                GameObject prefabTemplate = zoneManager.defaultPrefab;

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

                GameObject instance = PrefabUtility.InstantiatePrefab(prefabTemplate) as GameObject;
                if (instance == null)
                {
                    instance = Instantiate(prefabTemplate);
                }

                instance.transform.position = posData.position;
                instance.transform.eulerAngles = posData.rotation;
                instance.transform.localScale = posData.scale;
                instance.name = posData.prefabId;

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

    private void ValidateSetup()
    {
        System.Text.StringBuilder report = new System.Text.StringBuilder();
        report.AppendLine("Film Zone Manager Validation Report:");
        report.AppendLine("============================");

        if (zoneManager.defaultPrefab == null)
        {
            report.AppendLine("❌ Default prefab not set");
        }
        else
        {
            report.AppendLine("✅ Default prefab set");
        }

        int validZones = zoneManager.zones.Count(z => z.polygonPoints.Count >= 3);
        report.AppendLine($"📍 Zones: {zoneManager.zones.Count} total, {validZones} valid");

        if (validZones == 0)
        {
            report.AppendLine("❌ No valid zones (need at least 3 points each)");
        }

        EnhancedVideoPlayer[] allVideos = UnityEngine.Object.FindObjectsByType<EnhancedVideoPlayer>(FindObjectsSortMode.None);
        report.AppendLine($"🎬 Enhanced Video Players in scene: {allVideos.Length}");

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
}

// ===== DATA STRUCTURES =====

[System.Serializable]
public class PrefabPositionData
{
    public string prefabId;
    public Vector3 position;
    public Vector3 rotation;
    public Vector3 scale;
    public string zoneName;
    public string videoUrl;
    public string videoTitle;
    public string videoDescription;
}

[System.Serializable]
public class ZoneLayoutData
{
    public List<PrefabPositionData> prefabPositions = new List<PrefabPositionData>();
}