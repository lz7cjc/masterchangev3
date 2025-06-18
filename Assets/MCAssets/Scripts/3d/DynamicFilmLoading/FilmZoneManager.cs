using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;
using static UnityEditor.ShaderData;
using static UnityEditorInternal.ReorderableList;
using UnityEngine.InputSystem;

/// <summary>
/// Main Purpose: Manages video placement in 3D zones with a smart prefab selection system.
/// ENHANCED VERSION: Includes comprehensive clearing functionality for persistent zone data
/// </summary>

// Data structures for JSON parsing
[Serializable]
public class FilmDataEntry
{
    public string FileName;          // IGNORED
    public string PublicUrl;         // MANDATORY - video URL
    public string BucketPath;        // IGNORED
    public string Title;             // MANDATORY - video title
    public string Description;       // OPTIONAL - video description
    public string Prefab;           // OPTIONAL - specific prefab name (uses default if empty)
    public string Zone;             // IGNORED - legacy field
    public string[] Zones;          // MANDATORY - array of zones to place this video in

    // Helper method to get the video URL
    public string GetVideoUrl()
    {
        return PublicUrl;
    }

    // Helper method to check if custom prefab is specified
    public bool HasCustomPrefab()
    {
        return !string.IsNullOrEmpty(Prefab);
    }

    // Helper method to get zones for placement (handles both single Zone and Zones array)
    public string[] GetPlacementZones()
    {
        // Primary: use Zones array if it exists and has content
        if (Zones != null && Zones.Length > 0)
        {
            // Filter out empty strings
            var validZones = new System.Collections.Generic.List<string>();
            foreach (var zone in Zones)
            {
                if (!string.IsNullOrEmpty(zone.Trim()))
                {
                    validZones.Add(zone.Trim());
                }
            }
            return validZones.ToArray();
        }

        // Fallback: if Zones array is empty but Zone field exists, use that
        if (!string.IsNullOrEmpty(Zone))
        {
            return new string[] { Zone.Trim() };
        }

        // No zones specified
        return new string[0];
    }
}

[Serializable]
public class FilmDataCollection
{
    public FilmDataEntry[] Entries;
}

// Film zone definition for polygonal areas
[Serializable]
public class FilmZone
{
    public string zoneName;
    public List<Vector3> polygonPoints = new List<Vector3>();
    public Color gizmoColor = Color.blue;
    public bool showGizmos = true;

    // Check if a point is inside this polygonal zone
    public bool IsPointInZone(Vector3 point)
    {
        if (polygonPoints.Count < 3) return false;

        // Convert 3D points to 2D for polygon calculation (using X,Z coordinates)
        Vector2 testPoint = new Vector2(point.x, point.z);
        Vector2[] polygon = new Vector2[polygonPoints.Count];

        for (int i = 0; i < polygonPoints.Count; i++)
        {
            polygon[i] = new Vector2(polygonPoints[i].x, polygonPoints[i].z);
        }

        return IsPointInPolygon(testPoint, polygon);
    }

    // Check if a 2D point is inside this zone (ignoring Y coordinate)
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

    // Ray casting algorithm for point-in-polygon test
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

    // Get the average height of the zone
    public float GetZoneHeight()
    {
        if (polygonPoints.Count == 0) return 0f;

        float totalHeight = 0f;
        foreach (var point in polygonPoints)
        {
            totalHeight += point.y;
        }
        return totalHeight / polygonPoints.Count;
    }

    // Get a random position within the zone bounds at zone height
    public Vector3 GetRandomPointInZoneAtZoneHeight()
    {
        if (polygonPoints.Count < 3) return Vector3.zero;

        // Get bounding box
        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;

        foreach (var point in polygonPoints)
        {
            minX = Mathf.Min(minX, point.x);
            maxX = Mathf.Max(maxX, point.x);
            minZ = Mathf.Min(minZ, point.z);
            maxZ = Mathf.Max(maxZ, point.z);
        }

        // Get the zone's average height
        float zoneHeight = GetZoneHeight();

        // Try to find a valid point within the polygon
        for (int attempts = 0; attempts < 100; attempts++)
        {
            float x = UnityEngine.Random.Range(minX, maxX);
            float z = UnityEngine.Random.Range(minZ, maxZ);
            Vector2 testPoint2D = new Vector2(x, z);

            if (IsPointInZone2D(testPoint2D))
            {
                // Return position at zone height
                return new Vector3(x, zoneHeight, z);
            }
        }

        // Fallback to center of bounding box at zone height
        Vector3 center = new Vector3((minX + maxX) / 2, zoneHeight, (minZ + maxZ) / 2);
        return center;
    }

    // Get a random point within the zone bounds (legacy method for terrain sampling)
    public Vector3 GetRandomPointInZone()
    {
        if (polygonPoints.Count < 3) return Vector3.zero;

        // Get bounding box
        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;

        foreach (var point in polygonPoints)
        {
            minX = Mathf.Min(minX, point.x);
            maxX = Mathf.Max(maxX, point.x);
            minZ = Mathf.Min(minZ, point.z);
            maxZ = Mathf.Max(maxZ, point.z);
        }

        // Try to find a valid point within the polygon
        for (int attempts = 0; attempts < 100; attempts++)
        {
            float x = UnityEngine.Random.Range(minX, maxX);
            float z = UnityEngine.Random.Range(minZ, maxZ);
            Vector3 testPoint = new Vector3(x, 0, z);

            if (IsPointInZone(testPoint))
            {
                // Sample terrain height at this position
                Terrain terrain = Terrain.activeTerrain;
                if (terrain != null)
                {
                    float height = terrain.SampleHeight(testPoint);
                    testPoint.y = height;
                }

                return testPoint;
            }
        }

        // Fallback to center of bounding box
        Vector3 center = new Vector3((minX + maxX) / 2, 0, (minZ + maxZ) / 2);
        Terrain activeTerrain = Terrain.activeTerrain;
        if (activeTerrain != null)
        {
            center.y = activeTerrain.SampleHeight(center);
        }

        return center;
    }

    // ENHANCED: Clear all polygon points for this zone
    public void ClearAllPoints()
    {
        polygonPoints.Clear();
        Debug.Log($"Cleared all polygon points for zone: {zoneName}");
    }

    // ENHANCED: Get debug info about this zone
    public string GetDebugInfo()
    {
        return $"Zone '{zoneName}': {polygonPoints.Count} points, Color: {gizmoColor}, ShowGizmos: {showGizmos}";
    }
}

// Main film zone manager component
public class FilmZoneManager : MonoBehaviour
{
    [Header("Film Zone Configuration")]
    public List<FilmZone> zones = new List<FilmZone>();

    [Header("Prefab Settings")]
    public GameObject defaultPrefab;
    public List<PrefabMapping> prefabMappings = new List<PrefabMapping>();
    public List<ZonePrefabMapping> zonePrefabMappings = new List<ZonePrefabMapping>();

    [Header("Placement Settings")]
    public float prefabSpacing = 2f;
    public bool placeOnTerrain = false; // Changed default to false to use zone height
    public LayerMask terrainLayer = 1;

    [Header("Debug")]
    public bool showZoneGizmos = true;
    public bool showDebugInfo = false;

    // ENHANCED: Force clear options
    [Header("Enhanced Clearing (Debug)")]
    [SerializeField] private bool forceShowAllGizmos = false;
    [SerializeField] private bool enableDebugLogging = true;

    private Dictionary<string, GameObject> prefabDict;

    [Serializable]
    public class PrefabMapping
    {
        public string prefabName;
        public GameObject prefab;
    }

    [Serializable]
    public class ZonePrefabMapping
    {
        public string zoneName;
        public GameObject prefab;
    }

    private void Awake()
    {
        // Build prefab dictionary
        prefabDict = new Dictionary<string, GameObject>();

        if (prefabMappings != null)
        {
            foreach (var mapping in prefabMappings)
            {
                if (!string.IsNullOrEmpty(mapping.prefabName) && mapping.prefab != null)
                {
                    prefabDict[mapping.prefabName] = mapping.prefab;
                }
            }
        }

        // ENHANCED: Debug logging
        if (enableDebugLogging)
        {
            LogZoneDebugInfo();
        }
    }

    // ENHANCED: Debug logging method
    private void LogZoneDebugInfo()
    {
        Debug.Log($"=== FilmZoneManager Debug Info ===");
        Debug.Log($"Total zones: {zones.Count}");
        Debug.Log($"Show zone gizmos: {showZoneGizmos}");
        Debug.Log($"Force show all gizmos: {forceShowAllGizmos}");

        for (int i = 0; i < zones.Count; i++)
        {
            Debug.Log($"Zone {i}: {zones[i].GetDebugInfo()}");
        }
        Debug.Log($"=== End Debug Info ===");
    }

    public FilmZone GetZoneByName(string zoneName)
    {
        if (zones == null || string.IsNullOrEmpty(zoneName)) return null;
        return zones.Find(z => z.zoneName.Equals(zoneName, StringComparison.OrdinalIgnoreCase));
    }

    public GameObject GetPrefabForEntry(FilmDataEntry entry, string zoneName = null)
    {
        // Ensure prefabDict is initialized
        if (prefabDict == null)
        {
            Debug.Log("Initializing prefab dictionary...");
            prefabDict = new Dictionary<string, GameObject>();
            if (prefabMappings != null)
            {
                foreach (var mapping in prefabMappings)
                {
                    if (!string.IsNullOrEmpty(mapping.prefabName) && mapping.prefab != null)
                    {
                        prefabDict[mapping.prefabName] = mapping.prefab;
                        Debug.Log($"Added prefab mapping: '{mapping.prefabName}' -> {mapping.prefab.name}");
                    }
                }
            }
        }

        // PRIORITY 1: Check if entry specifies a custom prefab (Film-level)
        if (entry != null && !string.IsNullOrEmpty(entry.Prefab))
        {
            Debug.Log($"Looking for custom film prefab '{entry.Prefab}' for entry '{entry.Title}'");

            // Try exact match first
            if (prefabDict.ContainsKey(entry.Prefab))
            {
                Debug.Log($"✅ Found exact film prefab match: Using custom prefab '{entry.Prefab}' for entry '{entry.Title}'");
                return prefabDict[entry.Prefab];
            }

            // Try case-insensitive match
            foreach (var kvp in prefabDict)
            {
                if (string.Equals(kvp.Key, entry.Prefab, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.Log($"✅ Found case-insensitive film prefab match: Using custom prefab '{kvp.Key}' for entry '{entry.Title}'");
                    return kvp.Value;
                }
            }

            // Try partial match (contains)
            foreach (var kvp in prefabDict)
            {
                if (kvp.Key.ToLower().Contains(entry.Prefab.ToLower()) ||
                    entry.Prefab.ToLower().Contains(kvp.Key.ToLower()))
                {
                    Debug.Log($"✅ Found partial film prefab match: Using custom prefab '{kvp.Key}' for entry '{entry.Title}' (searched for '{entry.Prefab}')");
                    return kvp.Value;
                }
            }

            Debug.LogWarning($"❌ Custom film prefab '{entry.Prefab}' not found in prefab mappings for entry '{entry.Title}'. Checking zone prefabs...");
        }

        // PRIORITY 2: Check for zone-specific prefab (Zone-level)
        if (!string.IsNullOrEmpty(zoneName) && zonePrefabMappings != null)
        {
            Debug.Log($"Looking for zone prefab for zone '{zoneName}'");

            var zonePrefabMapping = zonePrefabMappings.FirstOrDefault(z =>
                string.Equals(z.zoneName, zoneName, StringComparison.OrdinalIgnoreCase));

            if (zonePrefabMapping != null && zonePrefabMapping.prefab != null)
            {
                Debug.Log($"✅ Found zone prefab: Using zone prefab '{zonePrefabMapping.prefab.name}' for zone '{zoneName}' and entry '{entry?.Title ?? "Unknown"}'");
                return zonePrefabMapping.prefab;
            }
            else
            {
                Debug.Log($"No zone prefab found for zone '{zoneName}'. Falling back to default prefab.");
            }
        }

        // PRIORITY 3: Return default prefab (Default-level)
        if (defaultPrefab != null)
        {
            Debug.Log($"Using default prefab '{defaultPrefab.name}' for entry '{entry?.Title ?? "Unknown"}' in zone '{zoneName ?? "Unknown"}'");
            return defaultPrefab;
        }
        else
        {
            Debug.LogError($"❌ No default prefab set! Cannot place entry '{entry?.Title ?? "Unknown"}'. Please assign a default prefab in the FilmZoneManager.");
            return null;
        }
    }

    // Helper method to get zone prefab (for UI display purposes)
    public GameObject GetZonePrefab(string zoneName)
    {
        if (string.IsNullOrEmpty(zoneName) || zonePrefabMappings == null)
            return null;

        var zonePrefabMapping = zonePrefabMappings.FirstOrDefault(z =>
            string.Equals(z.zoneName, zoneName, StringComparison.OrdinalIgnoreCase));

        return zonePrefabMapping?.prefab;
    }

    public Vector3 GetTerrainPosition(Vector3 worldPosition)
    {
        if (!placeOnTerrain) return worldPosition;

        Terrain terrain = Terrain.activeTerrain;
        if (terrain != null)
        {
            float height = terrain.SampleHeight(worldPosition);
            return new Vector3(worldPosition.x, height, worldPosition.z);
        }

        // Fallback: raycast downward
        RaycastHit hit;
        if (Physics.Raycast(worldPosition + Vector3.up * 100f, Vector3.down, out hit, 200f, terrainLayer))
        {
            return hit.point;
        }

        return worldPosition;
    }

    // ===== ENHANCED CLEARING METHODS =====

    /// <summary>
    /// NUCLEAR OPTION: Clear all zone data completely
    /// </summary>
    [ContextMenu("Force Clear All Zone Data")]
    public void ForceClearAllZoneData()
    {
        Debug.Log("🔥 FORCE CLEARING ALL ZONE DATA 🔥");

        if (zones != null)
        {
            zones.Clear();
        }
        else
        {
            zones = new List<FilmZone>();
        }

        // Force Unity to mark this object as dirty
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);

        // Force scene view repaint
        UnityEditor.SceneView.RepaintAll();
#endif

        Debug.Log("✅ All zone data has been forcibly cleared!");
        LogZoneDebugInfo();
    }

    /// <summary>
    /// SINGLE ZONE NUCLEAR OPTION: Completely remove a specific zone by name
    /// </summary>
    public void NuclearDeleteZone(string zoneName)
    {
        if (zones == null || string.IsNullOrEmpty(zoneName))
        {
            Debug.LogWarning("Cannot delete zone: zones list is null or zone name is empty");
            return;
        }

        Debug.Log($"🔥 NUCLEAR DELETE for zone: '{zoneName}' 🔥");

        // Find and remove the zone
        for (int i = zones.Count - 1; i >= 0; i--)
        {
            if (zones[i] != null && zones[i].zoneName.Equals(zoneName, StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"💥 Completely destroying zone: '{zones[i].zoneName}' at index {i}");
                zones.RemoveAt(i);

                // Force Unity to mark this object as dirty
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);

                // Force scene view repaint
                UnityEditor.SceneView.RepaintAll();
#endif

                Debug.Log($"✅ Zone '{zoneName}' has been NUCLEAR DELETED!");
                LogZoneDebugInfo();
                return;
            }
        }

        Debug.LogWarning($"❌ Zone '{zoneName}' not found for nuclear deletion!");
        LogZoneDebugInfo();
    }

    /// <summary>
    /// SINGLE ZONE NUCLEAR OPTION: Completely remove a specific zone by index
    /// </summary>
    public void NuclearDeleteZone(int zoneIndex)
    {
        if (zones == null)
        {
            Debug.LogWarning("Cannot delete zone: zones list is null");
            return;
        }

        if (zoneIndex < 0 || zoneIndex >= zones.Count)
        {
            Debug.LogWarning($"Cannot delete zone: invalid index {zoneIndex} (valid range: 0-{zones.Count - 1})");
            return;
        }

        string zoneName = zones[zoneIndex]?.zoneName ?? "Unknown";
        Debug.Log($"🔥 NUCLEAR DELETE for zone at index {zoneIndex}: '{zoneName}' 🔥");

        zones.RemoveAt(zoneIndex);

        // Force Unity to mark this object as dirty
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);

        // Force scene view repaint
        UnityEditor.SceneView.RepaintAll();
#endif

        Debug.Log($"✅ Zone '{zoneName}' at index {zoneIndex} has been NUCLEAR DELETED!");
        LogZoneDebugInfo();
    }

    /// <summary>
    /// Interactive method to select and nuclear delete a zone
    /// </summary>
    [ContextMenu("Nuclear Delete Zone (Interactive)")]
    public void InteractiveNuclearDeleteZone()
    {
        if (zones == null || zones.Count == 0)
        {
            Debug.LogWarning("No zones available to delete!");
#if UNITY_EDITOR
            UnityEditor.EditorUtility.DisplayDialog("No Zones", "No zones available to delete!", "OK");
#endif
            return;
        }

        ShowZoneSelectionForDeletion();
    }

    private void ShowZoneSelectionForDeletion()
    {
        System.Text.StringBuilder message = new System.Text.StringBuilder();
        message.AppendLine("⚠️ NUCLEAR DELETE ZONE ⚠️");
        message.AppendLine("This will COMPLETELY REMOVE the zone!");
        message.AppendLine();
        message.AppendLine("Available zones:");

        for (int i = 0; i < zones.Count; i++)
        {
            string zoneName = zones[i]?.zoneName ?? $"Zone_{i}";
            int pointCount = zones[i]?.polygonPoints?.Count ?? 0;
            message.AppendLine($"{i}: {zoneName} ({pointCount} points)");
        }

        message.AppendLine();
        message.AppendLine("Use the console to call:");
        message.AppendLine("NuclearDeleteZone(\"ZoneName\") or NuclearDeleteZone(index)");

        Debug.Log(message.ToString());
#if UNITY_EDITOR
        UnityEditor.EditorUtility.DisplayDialog("Nuclear Delete Zone", message.ToString(), "OK");
#endif
    }

    /// <summary>
    /// Clear all polygon points from all zones but keep zone definitions
    /// </summary>
    [ContextMenu("Clear All Polygon Points")]
    public void ClearAllPolygonPoints()
    {
        Debug.Log("🧹 Clearing all polygon points from all zones...");

        int clearedCount = 0;
        if (zones != null)
        {
            foreach (var zone in zones)
            {
                if (zone.polygonPoints.Count > 0)
                {
                    zone.ClearAllPoints();
                    clearedCount++;
                }
            }
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneView.RepaintAll();
#endif

        Debug.Log($"✅ Cleared polygon points from {clearedCount} zones!");
        LogZoneDebugInfo();
    }

    /// <summary>
    /// Clear polygon points from a specific zone
    /// </summary>
    public void ClearZonePoints(string zoneName)
    {
        var zone = GetZoneByName(zoneName);
        if (zone != null)
        {
            zone.ClearAllPoints();
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.SceneView.RepaintAll();
#endif
            Debug.Log($"✅ Cleared points from zone: {zoneName}");
        }
        else
        {
            Debug.LogWarning($"Zone '{zoneName}' not found!");
        }
    }

    /// <summary>
    /// Clear specific zone by index
    /// </summary>
    public void ClearZonePoints(int zoneIndex)
    {
        if (zones != null && zoneIndex >= 0 && zoneIndex < zones.Count)
        {
            zones[zoneIndex].ClearAllPoints();
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.SceneView.RepaintAll();
#endif
            Debug.Log($"✅ Cleared points from zone at index {zoneIndex}: {zones[zoneIndex].zoneName}");
        }
        else
        {
            Debug.LogWarning($"Invalid zone index: {zoneIndex}");
        }
    }

    /// <summary>
    /// Toggle gizmo visibility for all zones
    /// </summary>
    [ContextMenu("Toggle All Zone Gizmos")]
    public void ToggleAllZoneGizmos()
    {
        showZoneGizmos = !showZoneGizmos;

        if (zones != null)
        {
            foreach (var zone in zones)
            {
                zone.showGizmos = showZoneGizmos;
            }
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneView.RepaintAll();
#endif

        Debug.Log($"🎯 All zone gizmos: {(showZoneGizmos ? "ENABLED" : "DISABLED")}");
    }

    /// <summary>
    /// Force hide all gizmos (emergency option)
    /// </summary>
    [ContextMenu("Force Hide All Gizmos")]
    public void ForceHideAllGizmos()
    {
        showZoneGizmos = false;
        forceShowAllGizmos = false;

        if (zones != null)
        {
            foreach (var zone in zones)
            {
                zone.showGizmos = false;
            }
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneView.RepaintAll();
#endif

        Debug.Log("🚫 FORCE DISABLED all zone gizmos!");
    }

    /// <summary>
    /// Get comprehensive debug report
    /// </summary>
    [ContextMenu("Generate Debug Report")]
    public void GenerateDebugReport()
    {
        LogZoneDebugInfo();

        // Additional detailed report
        System.Text.StringBuilder report = new System.Text.StringBuilder();
        report.AppendLine("=== COMPREHENSIVE ZONE DEBUG REPORT ===");
        report.AppendLine($"GameObject: {gameObject.name}");
        report.AppendLine($"showZoneGizmos: {showZoneGizmos}");
        report.AppendLine($"forceShowAllGizmos: {forceShowAllGizmos}");
        report.AppendLine($"showDebugInfo: {showDebugInfo}");
        report.AppendLine($"enableDebugLogging: {enableDebugLogging}");
        report.AppendLine();

        if (zones == null)
        {
            report.AppendLine("⚠️ zones list is NULL!");
        }
        else
        {
            report.AppendLine($"Total zones: {zones.Count}");

            for (int i = 0; i < zones.Count; i++)
            {
                var zone = zones[i];
                if (zone == null)
                {
                    report.AppendLine($"Zone {i}: NULL");
                }
                else
                {
                    report.AppendLine($"Zone {i}: '{zone.zoneName}'");
                    report.AppendLine($"  - Points: {zone.polygonPoints?.Count ?? 0}");
                    report.AppendLine($"  - Color: {zone.gizmoColor}");
                    report.AppendLine($"  - ShowGizmos: {zone.showGizmos}");

                    if (zone.polygonPoints != null && zone.polygonPoints.Count > 0)
                    {
                        report.AppendLine($"  - First point: {zone.polygonPoints[0]}");
                        report.AppendLine($"  - Last point: {zone.polygonPoints[zone.polygonPoints.Count - 1]}");
                    }
                }
                report.AppendLine();
            }
        }

        report.AppendLine("=== END REPORT ===");
        Debug.Log(report.ToString());
    }

    // ===== ENHANCED GIZMO DRAWING =====

    private void OnDrawGizmos()
    {
        // ENHANCED: Multiple exit conditions
        if (!showZoneGizmos && !forceShowAllGizmos)
        {
            return;
        }

        if (zones == null)
        {
            if (enableDebugLogging)
                Debug.LogWarning("FilmZoneManager: zones list is null!");
            return;
        }

        if (zones.Count == 0)
        {
            if (enableDebugLogging)
                Debug.Log("FilmZoneManager: No zones to draw");
            return;
        }

        // ENHANCED: Draw with debug info
        int drawnZones = 0;
        int totalPoints = 0;

        foreach (var zone in zones)
        {
            if (zone == null)
            {
                if (enableDebugLogging)
                    Debug.LogWarning("FilmZoneManager: Found null zone in list!");
                continue;
            }

            if (!zone.showGizmos && !forceShowAllGizmos) continue;

            if (zone.polygonPoints == null)
            {
                if (enableDebugLogging)
                    Debug.LogWarning($"FilmZoneManager: Zone '{zone.zoneName}' has null polygonPoints!");
                continue;
            }

            if (zone.polygonPoints.Count < 3) continue;

            totalPoints += zone.polygonPoints.Count;
            drawnZones++;

            Gizmos.color = zone.gizmoColor;

            // Draw polygon outline
            for (int i = 0; i < zone.polygonPoints.Count; i++)
            {
                Vector3 current = zone.polygonPoints[i];
                Vector3 next = zone.polygonPoints[(i + 1) % zone.polygonPoints.Count];
                Gizmos.DrawLine(current, next);

                // Draw vertical lines to show height
                Gizmos.color = Color.red;
                Gizmos.DrawLine(current, current + Vector3.down * 2f);
                Gizmos.color = zone.gizmoColor;

                // ENHANCED: Draw point numbers if debug info is enabled
                if (showDebugInfo)
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawSphere(current, 0.2f);
                    Gizmos.color = zone.gizmoColor;
                }
            }

            // Draw zone center with height indicator
            if (zone.polygonPoints.Count > 0)
            {
                Vector3 center = Vector3.zero;
                foreach (var point in zone.polygonPoints)
                {
                    center += point;
                }
                center /= zone.polygonPoints.Count;

                // Draw center sphere
                Gizmos.color = zone.gizmoColor * 0.8f;
                Gizmos.DrawSphere(center, 0.5f);

                // Draw height indicator line
                Gizmos.color = Color.red;
                Gizmos.DrawLine(center, center + Vector3.down * 5f);

                // ENHANCED: Draw zone name in scene view if debug enabled
                if (showDebugInfo)
                {
                    Gizmos.color = Color.white;
                    // Note: Can't draw text in OnDrawGizmos, but we make the center more visible
                    Gizmos.DrawSphere(center + Vector3.up * 2f, 0.3f);
                }
            }
        }

        // ENHANCED: Debug logging for gizmo drawing
        if (enableDebugLogging && drawnZones > 0)
        {
            Debug.Log($"FilmZoneManager: Drew {drawnZones} zones with {totalPoints} total points");
        }
    }

    // ENHANCED: OnDrawGizmosSelected for more detailed info
    private void OnDrawGizmosSelected()
    {
        if (!showDebugInfo) return;

        OnDrawGizmos(); // Draw normal gizmos

        // Draw additional debug info when selected
        if (zones != null)
        {
            Gizmos.color = Color.yellow;
            foreach (var zone in zones)
            {
                if (zone?.polygonPoints != null && zone.polygonPoints.Count >= 3)
                {
                    // Draw bounding box
                    Vector3 min = zone.polygonPoints[0];
                    Vector3 max = zone.polygonPoints[0];

                    foreach (var point in zone.polygonPoints)
                    {
                        if (point.x < min.x) min.x = point.x;
                        if (point.y < min.y) min.y = point.y;
                        if (point.z < min.z) min.z = point.z;
                        if (point.x > max.x) max.x = point.x;
                        if (point.y > max.y) max.y = point.y;
                        if (point.z > max.z) max.z = point.z;
                    }

                    Vector3 size = max - min;
                    Vector3 center = (min + max) * 0.5f;
                    Gizmos.DrawWireCube(center, size);
                }
            }
        }
    }
}

// Component for individual video prefabs that works with your existing Event Trigger system
public class VideoZonePrefab : MonoBehaviour
{
    [Header("Video Data")]
    public string videoUrl;
    public string videoTitle;
    public string videoDescription;
    public string zoneName;

    [Header("Hover Settings")]
    public float hoverTimeRequired = 3.0f;
    public bool mouseHover = false;
    private float hoverTimer = 0;

    [Header("Scene Navigation")]
    public string returnScene = "mainVR";
    public string nextScene = "360VideoApp";

    [Header("Components")]
    public EnhancedVideoPlayer videoPlayer;
    public BoxCollider boxCollider;
    public EventTrigger eventTrigger;

    // Progress indicator (optional)
    private GameObject progressIndicator;
    private Image progressBar;

    private void Awake()
    {
        SetupComponents();
    }

    private void SetupComponents()
    {
        // Ensure we have required components
        if (boxCollider == null)
        {
            boxCollider = GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                boxCollider = gameObject.AddComponent<BoxCollider>();
                boxCollider.isTrigger = true; // Make sure it's a trigger for event system
            }
        }

        // Set up Event Trigger component like your existing system
        if (eventTrigger == null)
        {
            eventTrigger = GetComponent<EventTrigger>();
            if (eventTrigger == null)
            {
                eventTrigger = gameObject.AddComponent<EventTrigger>();
            }
        }

        SetupEventTriggers();

        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<EnhancedVideoPlayer>();
            if (videoPlayer == null)
            {
                videoPlayer = gameObject.AddComponent<EnhancedVideoPlayer>();
            }
        }
    }

    private void SetupEventTriggers()
    {
        if (eventTrigger.triggers == null)
        {
            eventTrigger.triggers = new List<EventTrigger.Entry>();
        }

        // Clear existing entries to avoid duplicates
        eventTrigger.triggers.Clear();

        // Pointer Enter Event
        EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
        pointerEnter.eventID = EventTriggerType.PointerEnter;
        pointerEnter.callback.AddListener((data) => { MouseHoverChangeScene(); });
        eventTrigger.triggers.Add(pointerEnter);

        // Pointer Exit Event
        EventTrigger.Entry pointerExit = new EventTrigger.Entry();
        pointerExit.eventID = EventTriggerType.PointerExit;
        pointerExit.callback.AddListener((data) => { MouseExit(); });
        eventTrigger.triggers.Add(pointerExit);

        // Optional: Pointer Click Event for immediate trigger
        EventTrigger.Entry pointerClick = new EventTrigger.Entry();
        pointerClick.eventID = EventTriggerType.PointerClick;
        pointerClick.callback.AddListener((data) => { MouseHoverChangeScene(); });
        eventTrigger.triggers.Add(pointerClick);
    }

    private void Update()
    {
        // Handle hover timer logic (same as DynamicLoadVideo1)
        if (mouseHover)
        {
            hoverTimer += Time.deltaTime;

            // Update progress indicator if it exists
            UpdateProgressIndicator();

            if (hoverTimer >= hoverTimeRequired)
            {
                mouseHover = false;
                hoverTimer = 0;
                TriggerVideoLoad();
            }
        }
    }

    private void UpdateProgressIndicator()
    {
        if (progressBar != null)
        {
            float progress = Mathf.Clamp01(hoverTimer / hoverTimeRequired);
            progressBar.fillAmount = progress;
        }
    }

    private void Start()
    {
        ConfigureVideoPlayer();
        CreateProgressIndicator();
    }

    public void SetVideoData(FilmDataEntry entry, string placementZone)
    {
        if (entry == null) return;

        videoUrl = entry.GetVideoUrl();  // Use PublicUrl
        videoTitle = entry.Title;
        videoDescription = entry.Description ?? ""; // Handle null descriptions
        zoneName = placementZone;  // The specific zone this instance is placed in

        // Update the gameObject name for clarity
        gameObject.name = $"Video_{entry.Title}_{placementZone}";

        ConfigureVideoPlayer();
    }

    private void ConfigureVideoPlayer()
    {
        if (videoPlayer == null) return;

        // Configure the EnhancedVideoPlayer to match your existing setup
        videoPlayer.VideoUrlLink = videoUrl;
        videoPlayer.title = videoTitle;
        videoPlayer.description = videoDescription;
        videoPlayer.nextscene = nextScene;
        videoPlayer.zoneName = zoneName;
        videoPlayer.returntoscene = returnScene;

        // Set hover time to match this component
        videoPlayer.hoverTimeRequired = hoverTimeRequired;
    }

    private void CreateProgressIndicator()
    {
        // Create a simple progress indicator similar to your existing system
        GameObject canvas = new GameObject("ProgressCanvas");
        canvas.transform.SetParent(transform);
        canvas.transform.localPosition = Vector3.zero;

        Canvas canvasComponent = canvas.AddComponent<Canvas>();
        canvasComponent.renderMode = RenderMode.WorldSpace;
        canvasComponent.worldCamera = Camera.main;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(2, 0.5f);
        canvasRect.localPosition = new Vector3(0, 2, 0);
        canvasRect.localScale = Vector3.one * 0.01f;

        // Progress bar background
        GameObject progressBG = new GameObject("ProgressBackground");
        progressBG.transform.SetParent(canvas.transform);

        Image bgImage = progressBG.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.5f);

        RectTransform bgRect = progressBG.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;

        // Progress bar fill
        GameObject progressFill = new GameObject("ProgressFill");
        progressFill.transform.SetParent(progressBG.transform);

        progressBar = progressFill.AddComponent<Image>();
        progressBar.color = Color.green;
        progressBar.type = Image.Type.Filled;
        progressBar.fillMethod = Image.FillMethod.Horizontal;

        RectTransform fillRect = progressFill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;

        progressIndicator = canvas;
        progressIndicator.SetActive(false);
    }

    // Event Trigger Methods (matching your DynamicLoadVideo1 pattern)
    public void MouseHoverChangeScene()
    {
        mouseHover = true;
        hoverTimer = 0;

        if (progressIndicator != null)
        {
            progressIndicator.SetActive(true);
        }

        Debug.Log($"Mouse hover started on {videoTitle}");
    }

    public void MouseExit()
    {
        mouseHover = false;
        hoverTimer = 0;

        if (progressIndicator != null)
        {
            progressIndicator.SetActive(false);
        }

        Debug.Log($"Mouse exit on {videoTitle}");
    }

    private void TriggerVideoLoad()
    {
        // Set PlayerPrefs exactly like DynamicLoadVideo1
        PlayerPrefs.SetString("nextscene", nextScene);
        PlayerPrefs.SetString("returntoscene", returnScene);
        PlayerPrefs.SetString("VideoUrl", videoUrl);
        PlayerPrefs.SetString("lastknownzone", zoneName);

        // Store video metadata
        if (!string.IsNullOrEmpty(videoTitle))
        {
            PlayerPrefs.SetString("videoTitle", videoTitle);
        }

        if (!string.IsNullOrEmpty(videoDescription))
        {
            PlayerPrefs.SetString("videoDescription", videoDescription);
        }

        Debug.Log($"Loading video: {videoTitle} from zone: {zoneName}");

        // Load the video scene
        UnityEngine.SceneManagement.SceneManager.LoadScene("360VideoApp", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    // Method to manually trigger video playback (for testing)
    [ContextMenu("Play Video")]
    public void PlayVideo()
    {
        TriggerVideoLoad();
    }
}

// Serializable data for saving/loading prefab positions
[Serializable]
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

[Serializable]
public class ZoneLayoutData
{
    public List<PrefabPositionData> prefabPositions = new List<PrefabPositionData>();
}