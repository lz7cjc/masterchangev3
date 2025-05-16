using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

#if UNITY_EDITOR
// Custom Editor Window to visually create and edit polygon zones
public class ZoneEditorWindow : EditorWindow
{
    private PolygonZoneManager targetManager;
    private SerializedObject serializedManager;
    private SerializedProperty zonesProperty;

    private Vector2 scrollPosition;
    private bool isPlacingPoints = false;
    private int selectedZoneIndex = -1;
    private Tool lastTool;

    private GUIStyle headerStyle;
    private GUIStyle subHeaderStyle;
    private Color zoneColor = new Color(0.2f, 0.8f, 0.2f, 0.2f);

    [MenuItem("Tools/Video System/Polygon Zone Editor")]
    public static void ShowWindow()
    {
        GetWindow<ZoneEditorWindow>("Polygon Zone Editor");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;

        // Initialize styles
        headerStyle = new GUIStyle();
        headerStyle.fontSize = 16;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.normal.textColor = Color.white;
        headerStyle.margin = new RectOffset(5, 5, 10, 10);

        subHeaderStyle = new GUIStyle();
        subHeaderStyle.fontSize = 13;
        subHeaderStyle.fontStyle = FontStyle.Bold;
        subHeaderStyle.normal.textColor = Color.white;
        subHeaderStyle.margin = new RectOffset(5, 5, 5, 5);
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;

        // Restore the original tool
        if (isPlacingPoints)
        {
            Tools.current = lastTool;
            isPlacingPoints = false;
        }
    }

    private void OnGUI()
    {
        // Find the zone manager if not already set
        if (targetManager == null)
        {
            targetManager = FindObjectOfType<PolygonZoneManager>();
            if (targetManager != null)
            {
                serializedManager = new SerializedObject(targetManager);
                zonesProperty = serializedManager.FindProperty("zones");
            }
        }

        EditorGUILayout.LabelField("Polygon Zone Editor", headerStyle);

        if (targetManager == null)
        {
            EditorGUILayout.HelpBox("No PolygonZoneManager found in the scene!", MessageType.Error);
            if (GUILayout.Button("Create Manager"))
            {
                GameObject managerObj = new GameObject("PolygonZoneManager");
                targetManager = managerObj.AddComponent<PolygonZoneManager>();
                serializedManager = new SerializedObject(targetManager);
                zonesProperty = serializedManager.FindProperty("zones");
            }
            return;
        }

        serializedManager.Update();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Zone Management", subHeaderStyle);

        // Create new zone button
        if (GUILayout.Button("Create New Zone"))
        {
            string zoneName = "Zone_" + System.DateTime.Now.Ticks.ToString().Substring(10);
            targetManager.CreateNewZone(zoneName);
            selectedZoneIndex = targetManager.zones.Count - 1;
            isPlacingPoints = false;
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Defined Zones", subHeaderStyle);

        // Display the zones list
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

        for (int i = 0; i < targetManager.zones.Count; i++)
        {
            var zone = targetManager.zones[i];
            EditorGUILayout.BeginHorizontal();

            bool isSelected = (i == selectedZoneIndex);
            bool newSelected = EditorGUILayout.ToggleLeft(zone.Name, isSelected, EditorStyles.boldLabel);

            if (newSelected != isSelected)
            {
                if (newSelected)
                {
                    // Stop placing points if we were doing that
                    if (isPlacingPoints)
                    {
                        isPlacingPoints = false;
                        Tools.current = lastTool;
                    }

                    selectedZoneIndex = i;
                }
                else
                {
                    selectedZoneIndex = -1;
                }
            }

            if (GUILayout.Button("Delete", GUILayout.Width(60)))
            {
                if (EditorUtility.DisplayDialog("Delete Zone", $"Are you sure you want to delete '{zone.Name}'?", "Yes", "No"))
                {
                    targetManager.zones.RemoveAt(i);
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

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(10);

        // Zone details if one is selected
        if (selectedZoneIndex >= 0 && selectedZoneIndex < targetManager.zones.Count)
        {
            EditorGUILayout.LabelField("Zone Details", subHeaderStyle);

            PolygonZone selectedZone = targetManager.zones[selectedZoneIndex];

            // Name
            string newName = EditorGUILayout.TextField("Name", selectedZone.Name);
            if (newName != selectedZone.Name)
            {
                Undo.RecordObject(targetManager, "Rename Zone");
                selectedZone.Name = newName;
                EditorUtility.SetDirty(targetManager);
            }

            // Max videos
            int newMaxVideos = EditorGUILayout.IntField("Max Videos", selectedZone.MaxVideos);
            if (newMaxVideos != selectedZone.MaxVideos)
            {
                Undo.RecordObject(targetManager, "Change Max Videos");
                selectedZone.MaxVideos = newMaxVideos;
                EditorUtility.SetDirty(targetManager);
            }

            // Min spacing
            float newMinSpacing = EditorGUILayout.FloatField("Min Spacing", selectedZone.MinSpacing);
            if (newMinSpacing != selectedZone.MinSpacing)
            {
                Undo.RecordObject(targetManager, "Change Min Spacing");
                selectedZone.MinSpacing = newMinSpacing;
                EditorUtility.SetDirty(targetManager);
            }

            // Visual settings
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Visual Settings", EditorStyles.boldLabel);

            bool newShowVisuals = EditorGUILayout.Toggle("Show Debug Visuals", selectedZone.ShowDebugVisuals);
            if (newShowVisuals != selectedZone.ShowDebugVisuals)
            {
                Undo.RecordObject(targetManager, "Toggle Debug Visuals");
                selectedZone.ShowDebugVisuals = newShowVisuals;
                EditorUtility.SetDirty(targetManager);
            }

            Color newColor = EditorGUILayout.ColorField("Zone Color", selectedZone.ZoneColor);
            if (newColor != selectedZone.ZoneColor)
            {
                Undo.RecordObject(targetManager, "Change Zone Color");
                selectedZone.ZoneColor = newColor;
                EditorUtility.SetDirty(targetManager);
            }

            // Categories
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Categories", EditorStyles.boldLabel);

            for (int i = 0; i < selectedZone.Categories.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                string category = selectedZone.Categories[i];
                string newCategory = EditorGUILayout.TextField(category);

                if (newCategory != category)
                {
                    Undo.RecordObject(targetManager, "Edit Category");
                    selectedZone.Categories[i] = newCategory;
                    EditorUtility.SetDirty(targetManager);
                }

                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    Undo.RecordObject(targetManager, "Remove Category");
                    selectedZone.Categories.RemoveAt(i);
                    EditorUtility.SetDirty(targetManager);
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Add Category"))
            {
                Undo.RecordObject(targetManager, "Add Category");
                selectedZone.Categories.Add("");
                EditorUtility.SetDirty(targetManager);
            }

            // Polygon editing
            EditorGUILayout.Space(10);
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
                    SceneView.lastActiveSceneView.Focus();
                    EditorUtility.DisplayDialog("Adding Points",
                        "Click in the Scene view to add polygon points.\n" +
                        "Right-click near a point to remove it.", "OK");
                }
                else
                {
                    Tools.current = lastTool;
                }
            }

            if (GUILayout.Button("Clear All Points"))
            {
                if (EditorUtility.DisplayDialog("Clear Points",
                    $"Are you sure you want to clear all points from '{selectedZone.Name}'?", "Yes", "No"))
                {
                    Undo.RecordObject(targetManager, "Clear Zone Points");
                    selectedZone.Points.Clear();
                    EditorUtility.SetDirty(targetManager);
                }
            }

            GUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(15);

        // Generate button
        if (GUILayout.Button("Generate Video Links"))
        {
            if (Application.isPlaying)
            {
                targetManager.GenerateVideoLinks();
            }
            else
            {
                EditorUtility.DisplayDialog("Runtime Only",
                    "This operation can only be performed in Play mode.", "OK");
            }
        }

        serializedManager.ApplyModifiedProperties();
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!isPlacingPoints || targetManager == null || selectedZoneIndex < 0 || selectedZoneIndex >= targetManager.zones.Count)
            return;

        PolygonZone selectedZone = targetManager.zones[selectedZoneIndex];
        Event e = Event.current;

        if (e.type == EventType.MouseDown && e.button == 0)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Undo.RecordObject(targetManager, "Add Zone Point");
                selectedZone.Points.Add(hit.point);
                EditorUtility.SetDirty(targetManager);
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
                Undo.RecordObject(targetManager, "Remove Zone Point");
                selectedZone.Points.RemoveAt(closestIndex);
                EditorUtility.SetDirty(targetManager);
                e.Use();
            }
        }

        // Draw the current polygon points
        if (selectedZone.Points.Count > 0)
        {
            // Draw points
            Handles.color = Color.white;
            for (int i = 0; i < selectedZone.Points.Count; i++)
            {
                Vector3 point = selectedZone.Points[i];

                // Draw point label
                Handles.Label(point + Vector3.up * 0.5f, $"{i + 1}");

                // Draw movable handle
                EditorGUI.BeginChangeCheck();
                Vector3 newPosition = Handles.PositionHandle(point, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(targetManager, "Move Zone Point");
                    selectedZone.Points[i] = newPosition;
                    EditorUtility.SetDirty(targetManager);
                }
            }

            // Draw connecting lines
            Handles.color = selectedZone.ZoneColor;
            for (int i = 0; i < selectedZone.Points.Count; i++)
            {
                int nextIndex = (i + 1) % selectedZone.Points.Count;
                Handles.DrawLine(selectedZone.Points[i], selectedZone.Points[nextIndex]);
            }

            // Draw fill if we have at least 3 points
            if (selectedZone.Points.Count >= 3)
            {
                // Create temporary vertices array for mesh drawing
                Vector3[] vertices = new Vector3[selectedZone.Points.Count];
                for (int i = 0; i < selectedZone.Points.Count; i++)
                {
                    vertices[i] = selectedZone.Points[i];
                }

                // Draw filled polygon
                Handles.color = new Color(
                    selectedZone.ZoneColor.r,
                    selectedZone.ZoneColor.g,
                    selectedZone.ZoneColor.b,
                    0.2f
                );

                // This is a simplified approach - for complex polygons you'd need more advanced triangulation
                for (int i = 1; i < vertices.Length - 1; i++)
                {
                    Vector3[] triangleVerts = new Vector3[] { vertices[0], vertices[i], vertices[i + 1] };
                    Handles.DrawAAConvexPolygon(triangleVerts);
                }
            }
        }

        // Force the scene to repaint
        if (e.type == EventType.Layout)
            HandleUtility.Repaint();
    }
}

// Runtime component to help with debugging and placement
public class ZoneVisualizer : MonoBehaviour
{
    [SerializeField] private bool showZones = true;
    [SerializeField] private Color zoneColor = new Color(0, 1, 0, 0.3f);

    private PolygonZoneManager zoneManager;

    private void Start()
    {
        zoneManager = GetComponent<PolygonZoneManager>();
        if (zoneManager == null)
        {
            zoneManager = FindObjectOfType<PolygonZoneManager>();
        }
    }

    private void OnDrawGizmos()
    {
        if (!showZones || zoneManager == null)
            return;

        // Draw all zones in the placement manager
        foreach (PolygonZone zone in zoneManager.zones)
        {
            if (!zone.ShowDebugVisuals || zone.Points.Count < 2)
                continue;

            Gizmos.color = zone.ZoneColor;

            // Draw zone outline
            for (int i = 0; i < zone.Points.Count; i++)
            {
                int nextIndex = (i + 1) % zone.Points.Count;
                Gizmos.DrawLine(zone.Points[i], zone.Points[nextIndex]);
            }
        }
    }
}
#endif