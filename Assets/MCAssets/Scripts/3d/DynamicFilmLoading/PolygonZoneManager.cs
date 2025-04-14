using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UnityEditor;

// Define a serializable class for polygon-based zone data
[Serializable]
public class PolygonZone
{
    public string Name;
    public List<Vector3> Points = new List<Vector3>();
    public float MinSpacing = 3.0f;
    public int MaxVideos = 10;

    // Categories are now optional and only used as a fallback
    public List<string> Categories = new List<string>();

    // Visual settings
    public bool ShowDebugVisuals = true;
    public Color ZoneColor = new Color(0.2f, 0.8f, 0.2f, 0.5f); // Increased alpha for better visibility

    // Calculate bounds from polygon points
    public Bounds GetBounds()
    {
        if (Points.Count == 0)
            return new Bounds(Vector3.zero, Vector3.zero);

        Vector3 min = Points[0];
        Vector3 max = Points[0];

        foreach (Vector3 point in Points)
        {
            min = Vector3.Min(min, point);
            max = Vector3.Max(max, point);
        }

        return new Bounds((min + max) / 2, max - min);
    }

    // Check if a point is inside the polygon
    public bool ContainsPoint(Vector3 point)
    {
        if (Points.Count < 3)
            return false;

        // Polygon containment check (2D, ignore Y-axis)
        bool isInside = false;

        for (int i = 0, j = Points.Count - 1; i < Points.Count; j = i++)
        {
            Vector3 pi = Points[i];
            Vector3 pj = Points[j];

            if ((pi.z > point.z) != (pj.z > point.z) &&
                (point.x < (pj.x - pi.x) * (point.z - pi.z) / (pj.z - pi.z) + pi.x))
            {
                isInside = !isInside;
            }
        }

        return isInside;
    }
}

// The main manager class for handling video link placement in polygon zones
public class PolygonZoneManager : MonoBehaviour
{
    [SerializeField]
    public List<PolygonZone> zones = new List<PolygonZone>();

    [SerializeField]
    private float placementHeight = 0.1f;

    [SerializeField]
    private float avoidObstacleRadius = 1.0f;

    [SerializeField]
    private int maxPlacementAttempts = 50;

    [SerializeField]
    private LayerMask obstacleLayer;

    [SerializeField]
    private Transform zoneEditParent;

    [SerializeField]
    private bool debugMode = true;

    private VideoDatabaseManager databaseManager;
    private VideoPlacementController placementController;
    private Terrain terrain;
    private TerrainCollider terrainCollider;

    private void Awake()
    {
        FindRequiredComponents();
        FindTerrainComponents();
    }

    private void OnEnable()
    {
        FindRequiredComponents();
    }

    // Find terrain and terrain collider components
    private void FindTerrainComponents()
    {
        // Find terrain if not already assigned
        if (terrain == null)
        {
            terrain = FindObjectOfType<Terrain>();
        }

        // Find terrain collider
        terrainCollider = FindObjectOfType<TerrainCollider>();
        if (terrainCollider == null)
        {
            if (debugMode) Debug.LogWarning("No TerrainCollider found in scene.");
        }
        else if (debugMode)
        {
            Debug.Log("Found TerrainCollider for height sampling.");
        }
    }

    // Find the required components in the scene
    private void FindRequiredComponents()
    {
        // Find the database manager
        if (databaseManager == null)
        {
            databaseManager = FindObjectOfType<VideoDatabaseManager>();
        }

        // Find the placement controller
        if (placementController == null)
        {
            placementController = FindObjectOfType<VideoPlacementController>();
        }
    }

    // Generate video links for all defined zones
    public void GenerateVideoLinks()
    {
        // Try to find components again in case they weren't found in Awake
        FindRequiredComponents();

        if (databaseManager == null)
        {
            Debug.LogError("Cannot generate video links: No database manager available");
            return;
        }

        if (placementController == null)
        {
            Debug.LogError("Cannot generate video links: No placement controller available");
            return;
        }

        // Let the placement controller handle the actual placement
        placementController.PlaceAllVideoLinks();
    }

    // Find a valid position within a zone
    public Vector3 FindValidPositionInZone(PolygonZone zone, List<Vector3> existingPositions)
    {
        Bounds zoneBounds = zone.GetBounds();

        for (int i = 0; i < maxPlacementAttempts; i++)
        {
            // Get random point in the bounds
            Vector3 randomPoint = new Vector3(
                UnityEngine.Random.Range(zoneBounds.min.x, zoneBounds.max.x),
                0,
                UnityEngine.Random.Range(zoneBounds.min.z, zoneBounds.max.z)
            );

            // Check if the point is inside the polygon
            if (!zone.ContainsPoint(randomPoint))
                continue;

            // Set initial height to a high value to raycast down
            randomPoint.y = 1000f;

            // First try to use TerrainCollider raycast for more accurate terrain height
            if (terrainCollider != null)
            {
                RaycastHit hit;
                if (Physics.Raycast(randomPoint, Vector3.down, out hit, 2000f, LayerMask.GetMask("Default", "Terrain")))
                {
                    randomPoint.y = hit.point.y;
                    if (debugMode) Debug.Log($"Found terrain height via raycast: {randomPoint.y}");
                }
                else if (debugMode)
                {
                    Debug.LogWarning($"Raycast failed to hit terrain at ({randomPoint.x}, {randomPoint.z})");
                }
            }
            // Fallback to Terrain.SampleHeight if available
            else if (terrain != null)
            {
                randomPoint.y = terrain.SampleHeight(randomPoint);
                if (debugMode) Debug.Log($"Using terrain.SampleHeight: {randomPoint.y}");
            }
            else
            {
                randomPoint.y = 0; // Default to ground level if no terrain found
                if (debugMode) Debug.Log("No terrain found, using default height 0");
            }

            // Check distance from other placed objects
            bool tooClose = false;
            foreach (Vector3 existingPos in existingPositions)
            {
                if (Vector3.Distance(new Vector3(randomPoint.x, 0, randomPoint.z),
                                    new Vector3(existingPos.x, 0, existingPos.z)) < zone.MinSpacing)
                {
                    tooClose = true;
                    break;
                }
            }

            if (tooClose) continue;

            // Check for obstacles
            if (Physics.CheckSphere(randomPoint, avoidObstacleRadius, obstacleLayer))
            {
                continue;
            }

            // Position is valid
            return randomPoint;
        }

        // Couldn't find a valid position
        if (debugMode)
        {
            Debug.LogWarning($"Could not find valid position in zone {zone.Name} after {maxPlacementAttempts} attempts");
        }
        return Vector3.zero;
    }

    // Create a new zone
    public void CreateNewZone(string zoneName)
    {
        PolygonZone newZone = new PolygonZone();
        newZone.Name = zoneName;
        zones.Add(newZone);
    }

    // Add a point to a zone
    public void AddPointToZone(int zoneIndex, Vector3 point)
    {
        if (zoneIndex >= 0 && zoneIndex < zones.Count)
        {
            zones[zoneIndex].Points.Add(point);
        }
    }

    // Clear all points from a zone
    public void ClearZonePoints(int zoneIndex)
    {
        if (zoneIndex >= 0 && zoneIndex < zones.Count)
        {
            zones[zoneIndex].Points.Clear();
        }
    }

    // Remove a zone
    public void RemoveZone(int index)
    {
        if (index >= 0 && index < zones.Count)
        {
            zones.RemoveAt(index);
        }
    }

    // Find a zone by name
    public PolygonZone FindZoneByName(string zoneName)
    {
        return zones.FirstOrDefault(z => z.Name == zoneName);
    }

    // Draw gizmos for zones - UPDATED with more visible rendering
    private void OnDrawGizmos()
    {
        foreach (PolygonZone zone in zones)
        {
            if (!zone.ShowDebugVisuals || zone.Points.Count < 2)
                continue;

            // Draw zone outline
            Gizmos.color = zone.ZoneColor;
            for (int i = 0; i < zone.Points.Count; i++)
            {
                int nextIndex = (i + 1) % zone.Points.Count;
                Gizmos.DrawLine(zone.Points[i], zone.Points[nextIndex]);
            }

#if UNITY_EDITOR
            // Draw zone fill with simpler, more reliable method
            if (zone.Points.Count >= 3)
            {
                // Use a brighter, more visible color for the fill
                Color fillColor = zone.ZoneColor;
                fillColor.a = 0.3f; // Consistent alpha that works well in the scene view
                Handles.color = fillColor;

                // Draw solid triangles from the center to each edge to fill the polygon
                Vector3 center = Vector3.zero;
                foreach (Vector3 point in zone.Points)
                {
                    center += point;
                }
                center /= zone.Points.Count;

                // Draw each triangle
                for (int i = 0; i < zone.Points.Count; i++)
                {
                    int nextIndex = (i + 1) % zone.Points.Count;
                    Vector3[] trianglePoints = new Vector3[] {
                        center,
                        zone.Points[i],
                        zone.Points[nextIndex]
                    };

                    Handles.DrawAAConvexPolygon(trianglePoints);
                }

                // Draw the zone name
                Handles.Label(center + Vector3.up * 1.0f, zone.Name);
            }
#endif
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(PolygonZoneManager))]
public class PolygonZoneManagerEditor : Editor
{
    private int selectedZoneIndex = -1;
    private bool isPlacingPoints = false;
    private Tool lastTool;
    private PolygonZoneManager manager;

    public override void OnInspectorGUI()
    {
        manager = (PolygonZoneManager)target;

        EditorGUILayout.LabelField("Polygon Zone Manager", EditorStyles.boldLabel);

        EditorGUILayout.Space(10);

        // Zone list
        EditorGUILayout.LabelField("Zones", EditorStyles.boldLabel);

        for (int i = 0; i < manager.zones.Count; i++)
        {
            PolygonZone zone = manager.zones[i];

            EditorGUILayout.BeginHorizontal();

            bool isSelected = (i == selectedZoneIndex);
            bool newSelected = EditorGUILayout.ToggleLeft(zone.Name, isSelected, EditorStyles.boldLabel);

            if (newSelected != isSelected)
            {
                if (newSelected)
                {
                    selectedZoneIndex = i;
                }
                else
                {
                    selectedZoneIndex = -1;
                }

                isPlacingPoints = false;
            }

            if (GUILayout.Button("Delete", GUILayout.Width(60)))
            {
                if (EditorUtility.DisplayDialog("Delete Zone", $"Are you sure you want to delete '{zone.Name}'?", "Yes", "No"))
                {
                    manager.zones.RemoveAt(i);
                    if (selectedZoneIndex == i)
                    {
                        selectedZoneIndex = -1;
                        isPlacingPoints = false;
                    }
                    else if (selectedZoneIndex > i)
                    {
                        selectedZoneIndex--;
                    }
                    GUIUtility.ExitGUI();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        // Add zone button
        if (GUILayout.Button("Add New Zone"))
        {
            string zoneName = "Zone_" + System.DateTime.Now.Ticks.ToString().Substring(10);
            manager.CreateNewZone(zoneName);
            selectedZoneIndex = manager.zones.Count - 1;
            isPlacingPoints = false;

            // Auto-switch to rename after creating
            EditorApplication.delayCall += () => {
                isPlacingPoints = false;
                EditorGUIUtility.editingTextField = true;
            };
        }

        EditorGUILayout.Space(10);

        // Zone details if one is selected
        if (selectedZoneIndex >= 0 && selectedZoneIndex < manager.zones.Count)
        {
            PolygonZone selectedZone = manager.zones[selectedZoneIndex];

            EditorGUILayout.LabelField("Zone Details", EditorStyles.boldLabel);

            // Name
            string newName = EditorGUILayout.TextField("Name", selectedZone.Name);
            if (newName != selectedZone.Name)
            {
                Undo.RecordObject(manager, "Rename Zone");
                selectedZone.Name = newName;
                EditorUtility.SetDirty(manager);
            }

            // Max videos
            int newMaxVideos = EditorGUILayout.IntField("Max Videos", selectedZone.MaxVideos);
            if (newMaxVideos != selectedZone.MaxVideos)
            {
                Undo.RecordObject(manager, "Change Max Videos");
                selectedZone.MaxVideos = newMaxVideos;
                EditorUtility.SetDirty(manager);
            }

            // Min spacing
            float newMinSpacing = EditorGUILayout.FloatField("Min Spacing", selectedZone.MinSpacing);
            if (newMinSpacing != selectedZone.MinSpacing)
            {
                Undo.RecordObject(manager, "Change Min Spacing");
                selectedZone.MinSpacing = newMinSpacing;
                EditorUtility.SetDirty(manager);
            }

            // Categories - Now optional as they're only used as fallback
            EditorGUILayout.LabelField("Categories (Optional - Fallback Only)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Categories are only used if a video doesn't have an explicit zone assignment", MessageType.Info);

            for (int i = 0; i < selectedZone.Categories.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                string category = selectedZone.Categories[i];
                string newCategory = EditorGUILayout.TextField(category);

                if (newCategory != category)
                {
                    Undo.RecordObject(manager, "Edit Category");
                    selectedZone.Categories[i] = newCategory;
                    EditorUtility.SetDirty(manager);
                }

                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    Undo.RecordObject(manager, "Remove Category");
                    selectedZone.Categories.RemoveAt(i);
                    EditorUtility.SetDirty(manager);
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Add Category"))
            {
                Undo.RecordObject(manager, "Add Category");
                selectedZone.Categories.Add("");
                EditorUtility.SetDirty(manager);
            }

            EditorGUILayout.Space(10);

            // Polygon editing
            EditorGUILayout.LabelField("Polygon Points", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Points: {selectedZone.Points.Count}");

            GUILayout.BeginHorizontal();

            if (GUILayout.Button(isPlacingPoints ? "Stop Placing Points" : "Start Placing Points"))
            {
                isPlacingPoints = !isPlacingPoints;

                if (isPlacingPoints)
                {
                    lastTool = Tools.current;
                    Tools.current = Tool.None;
                }
                else
                {
                    Tools.current = lastTool;
                }
            }

            if (GUILayout.Button("Clear All Points"))
            {
                if (EditorUtility.DisplayDialog("Clear Points", $"Are you sure you want to clear all points from '{selectedZone.Name}'?", "Yes", "No"))
                {
                    Undo.RecordObject(manager, "Clear Zone Points");
                    selectedZone.Points.Clear();
                    EditorUtility.SetDirty(manager);
                }
            }

            GUILayout.EndHorizontal();

            // Visual settings
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Visual Settings", EditorStyles.boldLabel);

            bool newShowVisuals = EditorGUILayout.Toggle("Show Debug Visuals", selectedZone.ShowDebugVisuals);
            if (newShowVisuals != selectedZone.ShowDebugVisuals)
            {
                Undo.RecordObject(manager, "Toggle Debug Visuals");
                selectedZone.ShowDebugVisuals = newShowVisuals;
                EditorUtility.SetDirty(manager);
            }

            Color newColor = EditorGUILayout.ColorField("Zone Color", selectedZone.ZoneColor);
            if (newColor != selectedZone.ZoneColor)
            {
                Undo.RecordObject(manager, "Change Zone Color");
                selectedZone.ZoneColor = newColor;
                EditorUtility.SetDirty(manager);
            }
        }

        EditorGUILayout.Space(10);

        // Check if there are valid zones
        bool hasValidZones = false;
        foreach (var zone in manager.zones)
        {
            if (zone.Points.Count >= 3)
            {
                hasValidZones = true;
                break;
            }
        }

        if (!hasValidZones)
        {
            EditorGUILayout.HelpBox("No valid zones defined. Zones need at least 3 points.", MessageType.Warning);
        }

        // Placement tools
        if (GUILayout.Button("Generate Video Links"))
        {
            if (Application.isPlaying)
            {
                manager.GenerateVideoLinks();
            }
            else
            {
                EditorUtility.DisplayDialog("Runtime Only",
                    "This operation can only be performed in Play mode.", "OK");
            }
        }

        // Apply any changes
        serializedObject.ApplyModifiedProperties();
    }

    private void OnSceneGUI()
    {
        if (!isPlacingPoints || selectedZoneIndex < 0 || selectedZoneIndex >= manager.zones.Count)
            return;

        PolygonZone selectedZone = manager.zones[selectedZoneIndex];
        Event e = Event.current;

        if (e.type == EventType.MouseDown && e.button == 0)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Undo.RecordObject(manager, "Add Zone Point");
                selectedZone.Points.Add(hit.point);
                EditorUtility.SetDirty(manager);
                e.Use();
            }
        }

        // Allow removing points with right-click when close to them
        if (e.type == EventType.MouseDown && e.button == 1)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

            float closestDistance = float.MaxValue;
            int closestIndex = -1;

            for (int i = 0; i < selectedZone.Points.Count; i++)
            {
                Vector3 point = selectedZone.Points[i];
                float distance = HandleUtility.DistancePointToLine(point, ray.origin, ray.origin + ray.direction * 100);

                if (distance < closestDistance && distance < 1f)
                {
                    closestDistance = distance;
                    closestIndex = i;
                }
            }

            if (closestIndex >= 0)
            {
                Undo.RecordObject(manager, "Remove Zone Point");
                selectedZone.Points.RemoveAt(closestIndex);
                EditorUtility.SetDirty(manager);
                e.Use();
            }
        }

        // Draw movable handles for existing points
        for (int i = 0; i < selectedZone.Points.Count; i++)
        {
            Vector3 point = selectedZone.Points[i];

            Handles.color = Color.white;
            Vector3 newPoint = Handles.PositionHandle(point, Quaternion.identity);

            if (newPoint != point)
            {
                Undo.RecordObject(manager, "Move Zone Point");
                selectedZone.Points[i] = newPoint;
                EditorUtility.SetDirty(manager);
            }
        }

        // Draw zone outline and fill while in editing mode
        if (selectedZone.Points.Count >= 2)
        {
            // Draw lines between points
            Handles.color = selectedZone.ZoneColor;
            for (int i = 0; i < selectedZone.Points.Count; i++)
            {
                int nextIndex = (i + 1) % selectedZone.Points.Count;
                Handles.DrawLine(selectedZone.Points[i], selectedZone.Points[nextIndex]);
            }

            // Draw fill if we have a complete polygon
            if (selectedZone.Points.Count >= 3)
            {
                // Use a more visible fill color
                Color fillColor = selectedZone.ZoneColor;
                fillColor.a = 0.3f; // Set alpha for better visibility
                Handles.color = fillColor;

                // Calculate center
                Vector3 center = Vector3.zero;
                foreach (Vector3 point in selectedZone.Points)
                {
                    center += point;
                }
                center /= selectedZone.Points.Count;

                // Draw triangles from center to each edge
                for (int i = 0; i < selectedZone.Points.Count; i++)
                {
                    int nextIndex = (i + 1) % selectedZone.Points.Count;
                    Vector3[] trianglePoints = new Vector3[] {
                        center,
                        selectedZone.Points[i],
                        selectedZone.Points[nextIndex]
                    };
                    Handles.DrawAAConvexPolygon(trianglePoints);
                }
            }
        }

        // Force scene repaint to update visuals
        if (e.type == EventType.Layout)
            HandleUtility.Repaint();
    }
}
#endif