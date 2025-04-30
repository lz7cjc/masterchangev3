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

    // Enhanced placement settings
    public bool UseCirclePlacement = true;
    public float InitialCircleRadius = 5.0f;
    public int ItemsPerCircle = 8; // Can be calculated automatically now
    public float CircleRadiusIncrement = 3.0f;

    // Banded placement settings
    public bool UseBandedPlacement = true; // Whether to use circular bands for placement after initial circle
    public float BandWidth = 3.0f; // Width of each circular band
    public int ItemsPerBand = 8; // Target number of items to place in each band
    public int MaxBands = 5; // Maximum number of bands (used for visualization)

    // Auto-calculation flag
    public bool AutoCalculateItemsPerCircle = true;

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

    // Calculate the maximum size circle that can fit inside this polygon
    public float CalculateMaxInscribedCircleRadius(Vector3 center)
    {
        if (Points.Count < 3)
            return 0f;

        // Find minimum distance from center to any edge of the polygon
        float minDist = float.MaxValue;

        for (int i = 0; i < Points.Count; i++)
        {
            int nextIndex = (i + 1) % Points.Count;
            Vector3 p1 = Points[i];
            Vector3 p2 = Points[nextIndex];

            // Project center onto the line segment p1-p2
            Vector3 lineDir = p2 - p1;
            float lineLengthSq = lineDir.sqrMagnitude;

            if (lineLengthSq < 0.0001f) // Almost identical points
                continue;

            float t = Mathf.Clamp01(Vector3.Dot(center - p1, lineDir) / lineLengthSq);
            Vector3 projection = p1 + t * lineDir;

            float distance = Vector3.Distance(new Vector3(center.x, 0, center.z),
                                             new Vector3(projection.x, 0, projection.z));

            minDist = Mathf.Min(minDist, distance);
        }

        return minDist;
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

    // Maximum distance to place items (to avoid infinite loops)
    [SerializeField]
    private float maxPlacementDistance = 50.0f;

    // Reticle pointer range (for circle placement)
    [SerializeField]
    public float reticleRange = 15.0f;

    // New setting for minimum spacing from obstacles
    [SerializeField]
    private float minObstacleSpacing = 1.5f;

    private VideoDatabaseManager databaseManager;
    private AdvancedVideoPlacementManager placementManager;
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
        if (placementManager == null)
        {
            placementManager = FindObjectOfType<AdvancedVideoPlacementManager>();
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
            // Create one to avoid null reference exceptions
            GameObject dbManagerObj = new GameObject("VideoDatabaseManager");
            databaseManager = dbManagerObj.AddComponent<VideoDatabaseManager>();
            Debug.LogWarning("Created a VideoDatabaseManager, but it needs configuration");
            return;
        }

        if (placementManager == null)
        {
            Debug.LogError("Cannot generate video links: No placement controller available");
            return;
        }

        // Let the placement controller handle the actual placement
        placementManager.PlaceAllVideos();
    }

    // Helper method for VideoPlacementController to get zone name by ID
    public string GetZoneNameByID(string zoneID)
    {
        if (string.IsNullOrEmpty(zoneID))
            return string.Empty;

        // In this implementation, the zone ID is the same as the zone name
        PolygonZone zone = zones.FirstOrDefault(z => z.Name == zoneID);
        return zone != null ? zone.Name : zoneID;
    }

    // Helper method for VideoPlacementController to get a random position in a zone
    public bool GetRandomPositionInZone(string zoneID, out Vector2 position)
    {
        position = Vector2.zero;

        if (string.IsNullOrEmpty(zoneID))
            return false;

        PolygonZone zone = zones.FirstOrDefault(z => z.Name == zoneID);
        if (zone == null || zone.Points.Count < 3)
            return false;

        Vector3 pos3D = FindRandomPositionInZone(zone, new List<Vector3>(), 0.5f);
        if (pos3D == Vector3.zero)
            return false;

        position = new Vector2(pos3D.x, pos3D.z);
        return true;
    }

    // Modified FindValidPositionInZone to consider prefab dimensions
    public Vector3 FindValidPositionInZone(PolygonZone zone, List<Vector3> existingPositions, VideoEntry video = null)
    {
        // Get prefab size information if available
        float prefabSize = EstimatePrefabSize(video);

        // Auto-calculate items per circle if enabled
        if (zone.AutoCalculateItemsPerCircle && zone.UseCirclePlacement)
        {
            CalculateOptimalItemsPerCircle(zone, prefabSize);
        }

        // Always check first circle placement
        if (zone.UseCirclePlacement && existingPositions.Count < zone.ItemsPerCircle)
        {
            return FindPositionInCircularPattern(zone, existingPositions, prefabSize);
        }
        // Then use banded placement if enabled
        else if (zone.UseBandedPlacement && zone.UseCirclePlacement)
        {
            return FindPositionInBand(zone, existingPositions, prefabSize);
        }
        // Fall back to original circle placement if banded placement is disabled
        else if (zone.UseCirclePlacement)
        {
            return FindPositionInCircularPattern(zone, existingPositions, prefabSize);
        }
        // Fall back to random placement if circle placement is disabled
        else
        {
            return FindRandomPositionInZone(zone, existingPositions, prefabSize);
        }
    }

    // Helper method to estimate the size of a prefab
    private float EstimatePrefabSize(VideoEntry video)
    {
        // Default radius if we can't determine the actual size
        float defaultPrefabRadius = 0.5f;

        if (video == null)
            return defaultPrefabRadius;

        // Use prefab type to estimate size
        if (!string.IsNullOrEmpty(video.Prefab))
        {
            // You can add custom size mappings based on prefab type
            switch (video.Prefab.ToLower())
            {
                case "large":
                    return 1.5f;
                case "medium":
                    return 1.0f;
                case "small":
                    return 0.5f;
                default:
                    return defaultPrefabRadius;
            }
        }

        return defaultPrefabRadius;
    }

    // Modify the automatic calculation of items per circle to consider prefab size
    private void CalculateOptimalItemsPerCircle(PolygonZone zone, float estimatedPrefabSize)
    {
        // Calculate zone center
        Vector3 zoneCenter = CalculateZoneCenter(zone);

        // Calculate max inscribed circle radius
        float maxRadius = zone.CalculateMaxInscribedCircleRadius(zoneCenter);

        // For safety, don't exceed reticle range
        float safeRadius = Mathf.Min(zone.InitialCircleRadius, reticleRange, maxRadius);

        // Calculate circumference
        float circumference = 2f * Mathf.PI * safeRadius;

        // Calculate how many items can fit around the circle with minimum spacing
        // Now accounting for the prefab size (each prefab needs minSpacing + 2*prefabRadius)
        float requiredSpacePerItem = zone.MinSpacing + (2 * estimatedPrefabSize);
        int maxItems = Mathf.FloorToInt(circumference / requiredSpacePerItem);

        // Limit to a reasonable number (at least 3, at most 24)
        zone.ItemsPerCircle = Mathf.Clamp(maxItems, 3, 24);

        if (debugMode)
        {
            Debug.Log($"Auto-calculated {zone.ItemsPerCircle} items per circle for zone '{zone.Name}' " +
                      $"(radius: {safeRadius}, circumference: {circumference}, " +
                      $"required space per item: {requiredSpacePerItem} = min spacing {zone.MinSpacing} + 2 × prefab radius {estimatedPrefabSize})");
        }
    }

    // Modify circle placement to consider prefab size
    private Vector3 FindPositionInCircularPattern(PolygonZone zone, List<Vector3> existingPositions, float prefabSize)
    {
        // Calculate zone center for centering the circles
        Vector3 zoneCenter = CalculateZoneCenter(zone);

        // Limit initial radius to reticle range if it's larger
        float initialRadius = Mathf.Min(zone.InitialCircleRadius, reticleRange);

        // Determine which circle we should be placing in based on how many items are already placed
        int currentCircle = existingPositions.Count / zone.ItemsPerCircle;
        int positionInCircle = existingPositions.Count % zone.ItemsPerCircle;

        // Calculate the radius for the current circle
        float currentRadius = initialRadius + (currentCircle * zone.CircleRadiusIncrement);

        // Don't exceed max placement distance
        if (currentRadius > maxPlacementDistance)
        {
            if (debugMode) Debug.LogWarning($"Reached maximum placement distance for zone {zone.Name}");
            return Vector3.zero;
        }

        // Don't exceed reticle range
        if (currentRadius > reticleRange)
        {
            if (debugMode) Debug.Log($"Circle radius {currentRadius} exceeds reticle range {reticleRange}, reverting to random placement");
            return FindRandomPositionInReticleRange(zone, existingPositions, zoneCenter, prefabSize);
        }

        // Calculate position on the circle
        float angle = (360f / zone.ItemsPerCircle) * positionInCircle;
        float radians = angle * Mathf.Deg2Rad;

        Vector3 position = new Vector3(
            zoneCenter.x + Mathf.Cos(radians) * currentRadius,
            0, // We'll set the proper Y later
            zoneCenter.z + Mathf.Sin(radians) * currentRadius
        );

        // Make sure the position is inside the zone
        if (!zone.ContainsPoint(position))
        {
            if (debugMode) Debug.Log($"Circle position outside zone boundary, trying random placement");
            return FindRandomPositionInReticleRange(zone, existingPositions, zoneCenter, prefabSize);
        }

        // Get terrain height
        position.y = GetTerrainHeight(position);

        // Adjusted radius for obstacle check based on prefab size
        float obstacleCheckRadius = Mathf.Max(avoidObstacleRadius, prefabSize) + minObstacleSpacing;

        // Check for obstacles with increased radius to ensure proper spacing
        if (Physics.CheckSphere(position, obstacleCheckRadius, obstacleLayer))
        {
            if (debugMode) Debug.Log($"Obstacle detected at circle position, trying random placement");
            return FindRandomPositionInReticleRange(zone, existingPositions, zoneCenter, prefabSize);
        }

        // Ensure minimum spacing from other objects, accounting for prefab size
        foreach (Vector3 existingPos in existingPositions)
        {
            float minDistance = zone.MinSpacing + prefabSize;
            if (Vector3.Distance(new Vector3(position.x, 0, position.z),
                            new Vector3(existingPos.x, 0, existingPos.z)) < minDistance)
            {
                if (debugMode) Debug.Log($"Circle position too close to existing object, trying random placement");
                return FindRandomPositionInReticleRange(zone, existingPositions, zoneCenter, prefabSize);
            }
        }

        return position;
    }

    // Modify band placement to consider prefab size
    private Vector3 FindPositionInBand(PolygonZone zone, List<Vector3> existingPositions, float prefabSize)
    {
        Vector3 zoneCenter = CalculateZoneCenter(zone);

        // Calculate which band we should be placing in
        // First band starts after the initial circle
        int placedItemsAfterFirstCircle = existingPositions.Count - zone.ItemsPerCircle;
        int currentBandIndex = placedItemsAfterFirstCircle / zone.ItemsPerBand;

        // Calculate inner and outer radius for this band
        float innerRadius = zone.InitialCircleRadius + (currentBandIndex * zone.BandWidth);
        float outerRadius = innerRadius + zone.BandWidth;

        // Make sure we don't exceed the maximum placement distance or reticle range
        outerRadius = Mathf.Min(outerRadius, maxPlacementDistance, reticleRange);

        if (debugMode) Debug.Log($"Placing in band {currentBandIndex + 1} (radius {innerRadius} to {outerRadius})");

        // Adjusted radius for obstacle check based on prefab size
        float obstacleCheckRadius = Mathf.Max(avoidObstacleRadius, prefabSize) + minObstacleSpacing;

        // Try to find a valid position in this band
        for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
        {
            // Generate random angle
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;

            // Generate random distance within the band
            float distance = UnityEngine.Random.Range(innerRadius, outerRadius);

            Vector3 position = new Vector3(
                zoneCenter.x + Mathf.Cos(angle) * distance,
                0, // We'll set the proper Y later
                zoneCenter.z + Mathf.Sin(angle) * distance
            );

            // The most important check - ensure the position is within the polygon boundary
            if (!zone.ContainsPoint(position))
                continue;

            // Get terrain height
            position.y = GetTerrainHeight(position);

            // Check for obstacles with increased radius to ensure proper spacing
            if (Physics.CheckSphere(position, obstacleCheckRadius, obstacleLayer))
                continue;

            // Ensure minimum spacing from other objects, accounting for prefab size
            bool tooClose = false;
            foreach (Vector3 existingPos in existingPositions)
            {
                float minDistance = zone.MinSpacing + prefabSize;
                if (Vector3.Distance(new Vector3(position.x, 0, position.z),
                                    new Vector3(existingPos.x, 0, existingPos.z)) < minDistance)
                {
                    tooClose = true;
                    break;
                }
            }

            if (tooClose)
                continue;

            return position;
        }

        // If we couldn't find a position in the current band, try the next band
        if (debugMode) Debug.Log($"Couldn't find position in band {currentBandIndex + 1}, trying next band");

        // Force the count to move to the next band by pretending we've already placed items in this band
        List<Vector3> tempPositions = new List<Vector3>(existingPositions);
        for (int i = 0; i < zone.ItemsPerBand; i++)
        {
            tempPositions.Add(Vector3.zero); // Add temporary positions
        }

        // Try to place in the next band
        Vector3 result = FindPositionInBand(zone, tempPositions, prefabSize);

        // If we still couldn't find a position, fall back to random placement within the zone
        if (result == Vector3.zero)
        {
            if (debugMode) Debug.LogWarning("Couldn't find position in any band, falling back to random placement");
            return FindRandomPositionInZone(zone, existingPositions, prefabSize);
        }

        return result;
    }

    // Modify reticle range placement to consider prefab size
    private Vector3 FindRandomPositionInReticleRange(PolygonZone zone, List<Vector3> existingPositions, Vector3 center, float prefabSize)
    {
        // Adjusted radius for obstacle check based on prefab size
        float obstacleCheckRadius = Mathf.Max(avoidObstacleRadius, prefabSize) + minObstacleSpacing;

        for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
        {
            // Generate random angle and distance within reticle range
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = UnityEngine.Random.Range(0f, reticleRange);

            Vector3 position = new Vector3(
                center.x + Mathf.Cos(angle) * distance,
                0, // We'll set the proper Y later
                center.z + Mathf.Sin(angle) * distance
            );

            // Make sure the position is inside the zone
            if (!zone.ContainsPoint(position))
                continue;

            // Get terrain height
            position.y = GetTerrainHeight(position);

            // Check for obstacles with increased radius to ensure proper spacing
            if (Physics.CheckSphere(position, obstacleCheckRadius, obstacleLayer))
                continue;

            // Ensure minimum spacing from other objects, accounting for prefab size
            bool tooClose = false;
            foreach (Vector3 existingPos in existingPositions)
            {
                float minDistance = zone.MinSpacing + prefabSize;
                if (Vector3.Distance(new Vector3(position.x, 0, position.z),
                                    new Vector3(existingPos.x, 0, existingPos.z)) < minDistance)
                {
                    tooClose = true;
                    break;
                }
            }

            if (tooClose)
                continue;

            return position;
        }

        if (debugMode) Debug.LogWarning($"Failed to find position within reticle range after {maxPlacementAttempts} attempts");
        return Vector3.zero;
    }

    // Modify random zone placement to consider prefab size
    private Vector3 FindRandomPositionInZone(PolygonZone zone, List<Vector3> existingPositions, float prefabSize)
    {
        Bounds zoneBounds = zone.GetBounds();

        // Adjusted radius for obstacle check based on prefab size
        float obstacleCheckRadius = Mathf.Max(avoidObstacleRadius, prefabSize) + minObstacleSpacing;

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

            // Get terrain height
            randomPoint.y = GetTerrainHeight(randomPoint);

            // Check distance from other placed objects, accounting for prefab size
            bool tooClose = false;
            foreach (Vector3 existingPos in existingPositions)
            {
                float minDistance = zone.MinSpacing + prefabSize;
                if (Vector3.Distance(new Vector3(randomPoint.x, 0, randomPoint.z),
                                    new Vector3(existingPos.x, 0, existingPos.z)) < minDistance)
                {
                    tooClose = true;
                    break;
                }
            }

            if (tooClose) continue;

            // Check for obstacles with adjusted radius based on prefab size
            if (Physics.CheckSphere(randomPoint, obstacleCheckRadius, obstacleLayer))
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

    // Get terrain height at position
    private float GetTerrainHeight(Vector3 position)
    {
        // First try to use TerrainCollider raycast for more accurate terrain height
        if (terrainCollider != null)
        {
            RaycastHit hit;
            if (Physics.Raycast(new Vector3(position.x, 1000f, position.z), Vector3.down, out hit, 2000f, LayerMask.GetMask("Default", "Terrain")))
            {
                if (debugMode) Debug.Log($"Found terrain height via raycast: {hit.point.y}");
                return hit.point.y;
            }
            else if (debugMode)
            {
                Debug.LogWarning($"Raycast failed to hit terrain at ({position.x}, {position.z})");
            }
        }

        // Fallback to Terrain.SampleHeight if available
        if (terrain != null)
        {
            float height = terrain.SampleHeight(position);
            if (debugMode) Debug.Log($"Using terrain.SampleHeight: {height}");
            return height;
        }

        if (debugMode) Debug.Log("No terrain found, using default height 0");
        return 0; // Default to ground level if no terrain found
    }

    // Calculate center of a zone
    public Vector3 CalculateZoneCenter(PolygonZone zone)
    {
        if (zone.Points.Count == 0)
            return Vector3.zero;

        // Calculate the center of all points
        Vector3 center = Vector3.zero;
        foreach (Vector3 point in zone.Points)
        {
            center += point;
        }
        center /= zone.Points.Count;

        return center;
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

    // Draw gizmos for zones - UPDATED with more visible rendering and continuous updates
    private void OnDrawGizmos()
    {
        DrawZoneGizmos();
    }

    // Also draw in OnDrawGizmosSelected to ensure they remain visible when selected
    private void OnDrawGizmosSelected()
    {
        DrawZoneGizmos();
    }

    // Draw all zone gizmos
    private void DrawZoneGizmos()
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

            // Draw circle placement preview if enabled
            if (zone.UseCirclePlacement && zone.Points.Count >= 3)
            {
                Vector3 center = CalculateZoneCenter(zone);

                // Draw initial circle with visualization of first set of items
                float safeInitialRadius = Mathf.Min(zone.InitialCircleRadius, reticleRange);
                DrawCircleGizmo(center, safeInitialRadius, Color.yellow);

                // Visualize the item positions on the first circle
                VisualizeItemsOnCircle(center, safeInitialRadius, zone.ItemsPerCircle, new Color(1f, 1f, 0f, 0.7f));

                // Draw second circle
                float secondRadius = Mathf.Min(zone.InitialCircleRadius + zone.CircleRadiusIncrement, reticleRange);
                DrawCircleGizmo(center, secondRadius, new Color(1f, 0.5f, 0f, 0.5f)); // Orange

                // If using banded placement, visualize the bands
                if (zone.UseBandedPlacement)
                {
                    // Draw each band
                    for (int i = 0; i < zone.MaxBands; i++)
                    {
                        float bandRadius = zone.InitialCircleRadius + ((i + 1) * zone.BandWidth);

                        // Don't exceed reticle range in visualization
                        if (bandRadius <= reticleRange)
                        {
                            // Use different colors for alternating bands
                            Color bandColor = (i % 2 == 0)
                                ? new Color(0f, 0.8f, 0.2f, 0.2f)  // Green
                                : new Color(0.8f, 0.2f, 0.8f, 0.2f); // Purple

                            DrawCircleGizmo(center, bandRadius, bandColor);
                        }
                        else
                        {
                            break; // Stop drawing bands beyond reticle range
                        }
                    }
                }

                // Draw reticle range limit
                DrawCircleGizmo(center, reticleRange, new Color(1f, 0f, 0f, 0.3f)); // Red, transparent
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

    // Helper method to draw circle gizmos
    private void DrawCircleGizmo(Vector3 center, float radius, Color color)
    {
        Gizmos.color = color;

        const int segments = 32;
        float angleStep = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep * Mathf.Deg2Rad;
            float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;

            Vector3 point1 = new Vector3(
                center.x + Mathf.Cos(angle1) * radius,
                center.y, // Keep at same height
                center.z + Mathf.Sin(angle1) * radius
            );

            Vector3 point2 = new Vector3(
                center.x + Mathf.Cos(angle2) * radius,
                center.y, // Keep at same height
                center.z + Mathf.Sin(angle2) * radius
            );

            Gizmos.DrawLine(point1, point2);
        }
    }

    // Helper method to visualize items on a circle
    private void VisualizeItemsOnCircle(Vector3 center, float radius, int itemCount, Color color)
    {
        Gizmos.color = color;

        // Draw dots for each item position
        for (int i = 0; i < itemCount; i++)
        {
            float angle = (360f / itemCount) * i * Mathf.Deg2Rad;

            Vector3 itemPos = new Vector3(
                center.x + Mathf.Cos(angle) * radius,
                center.y, // Keep at same height
                center.z + Mathf.Sin(angle) * radius
            );

            // Draw a small sphere to represent the item
            Gizmos.DrawSphere(itemPos, 0.3f);

            // Draw a line from center to item for clarity
            Gizmos.DrawLine(center, itemPos);
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
    private SerializedProperty zonesProp;

    // New foldout states
    private bool showBandedPlacementSettings = true;
    private bool showAutoCalculationSettings = true;

    private void OnEnable()
    {
        manager = (PolygonZoneManager)target;
        zonesProp = serializedObject.FindProperty("zones");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        manager = (PolygonZoneManager)target;

        EditorGUILayout.LabelField("Polygon Zone Manager", EditorStyles.boldLabel);

        EditorGUILayout.Space(10);

        // Safety check for null zones list
        if (manager.zones == null)
        {
            manager.zones = new List<PolygonZone>();
            EditorUtility.SetDirty(manager);
        }

        // Global settings
        SerializedProperty reticleRangeProp = serializedObject.FindProperty("reticleRange");
        if (reticleRangeProp != null)
        {
            EditorGUILayout.PropertyField(reticleRangeProp, new GUIContent("Reticle Range", "Maximum distance for object placement"));
        }

        SerializedProperty maxDistanceProp = serializedObject.FindProperty("maxPlacementDistance");
        if (maxDistanceProp != null)
        {
            EditorGUILayout.PropertyField(maxDistanceProp);
        }

        SerializedProperty obstacleProp = serializedObject.FindProperty("minObstacleSpacing");
        if (obstacleProp != null)
        {
            EditorGUILayout.PropertyField(obstacleProp, new GUIContent("Min Obstacle Spacing", "Minimum distance to keep from any obstacles"));
        }

        SerializedProperty debugProp = serializedObject.FindProperty("debugMode");
        if (debugProp != null)
        {
            EditorGUILayout.PropertyField(debugProp);
        }

        EditorGUILayout.Space(10);

        // Zone list
        EditorGUILayout.LabelField("Zones", EditorStyles.boldLabel);

        for (int i = 0; i < manager.zones.Count; i++)
        {
            // Safety check for null zones
            if (manager.zones[i] == null)
            {
                EditorGUILayout.HelpBox($"Zone at index {i} is null!", MessageType.Error);
                continue;
            }

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
                SceneView.RepaintAll(); // Force scene view to update for visualizations
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
                    SceneView.RepaintAll(); // Update visualizations
                    EditorUtility.SetDirty(manager);
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
            SceneView.RepaintAll(); // Update visualizations

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
            // Safety check for the serialized property array
            if (zonesProp == null)
            {
                zonesProp = serializedObject.FindProperty("zones");
                if (zonesProp == null)
                {
                    EditorGUILayout.HelpBox("Could not find 'zones' property", MessageType.Error);
                    return;
                }
            }

            // Update the zones array size in serializedObject if needed
            if (zonesProp.arraySize != manager.zones.Count)
            {
                serializedObject.Update();
            }

            // Additional check to make sure we don't access out of bounds
            if (selectedZoneIndex >= zonesProp.arraySize)
            {
                EditorGUILayout.HelpBox($"Selected zone index {selectedZoneIndex} is out of range (zones prop size: {zonesProp.arraySize})", MessageType.Error);
                return;
            }

            // Get the serialized property for the selected zone
            SerializedProperty zoneProp = zonesProp.GetArrayElementAtIndex(selectedZoneIndex);
            if (zoneProp == null)
            {
                EditorGUILayout.HelpBox("Could not access zone property", MessageType.Error);
                return;
            }

            PolygonZone selectedZone = manager.zones[selectedZoneIndex];
            if (selectedZone == null)
            {
                EditorGUILayout.HelpBox("Selected zone is null", MessageType.Error);
                return;
            }

            EditorGUILayout.LabelField("Zone Details", EditorStyles.boldLabel);

            // Name
            EditorGUI.BeginChangeCheck();
            string newName = EditorGUILayout.TextField("Name", selectedZone.Name);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(manager, "Rename Zone");
                selectedZone.Name = newName;
                EditorUtility.SetDirty(manager);
            }

            // Circle placement settings
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Placement Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            SerializedProperty useCircleProp = zoneProp.FindPropertyRelative("UseCirclePlacement");
            if (useCircleProp != null)
            {
                EditorGUILayout.PropertyField(useCircleProp, new GUIContent("Use Circle Placement"));

                if (useCircleProp.boolValue)
                {
                    EditorGUI.indentLevel++;

                    SerializedProperty radiusProp = zoneProp.FindPropertyRelative("InitialCircleRadius");
                    if (radiusProp != null)
                    {
                        float oldRadius = radiusProp.floatValue;
                        EditorGUILayout.PropertyField(radiusProp, new GUIContent("Initial Circle Radius"));
                        if (radiusProp.floatValue != oldRadius)
                        {
                            SceneView.RepaintAll(); // Force scene view update when radius changes
                        }
                    }

                    // Auto-calculation settings
                    showAutoCalculationSettings = EditorGUILayout.Foldout(showAutoCalculationSettings, "Auto-Calculate Settings", true);
                    if (showAutoCalculationSettings)
                    {
                        EditorGUI.indentLevel++;

                        SerializedProperty autoCalcProp = zoneProp.FindPropertyRelative("AutoCalculateItemsPerCircle");
                        if (autoCalcProp != null)
                        {
                            EditorGUILayout.PropertyField(autoCalcProp, new GUIContent("Auto-Calculate Items Per Circle"));

                            // Only show manual ItemsPerCircle if auto-calculate is disabled
                            if (!autoCalcProp.boolValue)
                            {
                                SerializedProperty itemsProp = zoneProp.FindPropertyRelative("ItemsPerCircle");
                                if (itemsProp != null)
                                {
                                    int oldItems = itemsProp.intValue;
                                    EditorGUILayout.PropertyField(itemsProp, new GUIContent("Items Per Circle"));
                                    if (itemsProp.intValue != oldItems)
                                    {
                                        SceneView.RepaintAll();
                                    }
                                }
                            }
                            else
                            {
                                GUI.enabled = false;
                                EditorGUILayout.IntField("Items Per Circle (Auto)", selectedZone.ItemsPerCircle);
                                GUI.enabled = true;

                                // Display how calculation works
                                EditorGUILayout.HelpBox(
                                    "Auto-calculation divides the circle circumference by Min Spacing to determine optimal item count.",
                                    MessageType.Info);
                            }
                        }

                        EditorGUI.indentLevel--;
                    }

                    SerializedProperty incrementProp = zoneProp.FindPropertyRelative("CircleRadiusIncrement");
                    if (incrementProp != null)
                    {
                        float oldIncrement = incrementProp.floatValue;
                        EditorGUILayout.PropertyField(incrementProp, new GUIContent("Radius Increment"));
                        if (incrementProp.floatValue != oldIncrement)
                        {
                            SceneView.RepaintAll();
                        }
                    }

                    // Banded placement settings
                    showBandedPlacementSettings = EditorGUILayout.Foldout(showBandedPlacementSettings, "Banded Placement Settings", true);
                    if (showBandedPlacementSettings)
                    {
                        EditorGUI.indentLevel++;

                        SerializedProperty useBandedProp = zoneProp.FindPropertyRelative("UseBandedPlacement");
                        if (useBandedProp != null)
                        {
                            EditorGUILayout.PropertyField(useBandedProp, new GUIContent("Use Banded Placement"));

                            if (useBandedProp.boolValue)
                            {
                                SerializedProperty bandWidthProp = zoneProp.FindPropertyRelative("BandWidth");
                                if (bandWidthProp != null)
                                {
                                    EditorGUILayout.PropertyField(bandWidthProp, new GUIContent("Band Width"));
                                }

                                SerializedProperty itemsPerBandProp = zoneProp.FindPropertyRelative("ItemsPerBand");
                                if (itemsPerBandProp != null)
                                {
                                    EditorGUILayout.PropertyField(itemsPerBandProp, new GUIContent("Items Per Band"));
                                }

                                SerializedProperty maxBandsProp = zoneProp.FindPropertyRelative("MaxBands");
                                if (maxBandsProp != null)
                                {
                                    EditorGUILayout.PropertyField(maxBandsProp, new GUIContent("Max Bands (Visualization)"));
                                }

                                EditorGUILayout.HelpBox(
                                    "After placing " + selectedZone.ItemsPerCircle + " items in the first circle, remaining items " +
                                    "will be placed in " + selectedZone.BandWidth + "-unit wide circular bands, with about " +
                                    selectedZone.ItemsPerBand + " items per band. Each band will be filled before moving outward.",
                                    MessageType.Info);
                            }
                        }

                        EditorGUI.indentLevel--;
                    }

                    EditorGUI.indentLevel--;
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                SceneView.RepaintAll(); // Force scene view update
            }

            // Max videos
            EditorGUI.BeginChangeCheck();
            int newMaxVideos = EditorGUILayout.IntField("Max Videos", selectedZone.MaxVideos);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(manager, "Change Max Videos");
                selectedZone.MaxVideos = newMaxVideos;
                EditorUtility.SetDirty(manager);
            }

            // Min spacing
            EditorGUI.BeginChangeCheck();
            float newMinSpacing = EditorGUILayout.FloatField("Min Spacing", selectedZone.MinSpacing);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(manager, "Change Min Spacing");
                selectedZone.MinSpacing = newMinSpacing;
                EditorUtility.SetDirty(manager);
            }

            // Categories - Now optional as they're only used as fallback
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Categories (Optional - Fallback Only)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Categories are only used if a video doesn't have an explicit zone assignment", MessageType.Info);

            if (selectedZone.Categories == null)
            {
                selectedZone.Categories = new List<string>();
            }

            for (int i = 0; i < selectedZone.Categories.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                string category = selectedZone.Categories[i];
                EditorGUI.BeginChangeCheck();
                string newCategory = EditorGUILayout.TextField(category ?? string.Empty);
                if (EditorGUI.EndChangeCheck())
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

                SceneView.RepaintAll(); // Update scene view
            }

            if (GUILayout.Button("Clear All Points"))
            {
                if (EditorUtility.DisplayDialog("Clear Points", $"Are you sure you want to clear all points from '{selectedZone.Name}'?", "Yes", "No"))
                {
                    Undo.RecordObject(manager, "Clear Zone Points");
                    selectedZone.Points.Clear();
                    EditorUtility.SetDirty(manager);
                    SceneView.RepaintAll(); // Update scene view
                }
            }

            GUILayout.EndHorizontal();

            // Visual settings
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Visual Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            bool newShowVisuals = EditorGUILayout.Toggle("Show Debug Visuals", selectedZone.ShowDebugVisuals);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(manager, "Toggle Debug Visuals");
                selectedZone.ShowDebugVisuals = newShowVisuals;
                EditorUtility.SetDirty(manager);
                SceneView.RepaintAll(); // Update scene view
            }

            EditorGUI.BeginChangeCheck();
            Color newColor = EditorGUILayout.ColorField("Zone Color", selectedZone.ZoneColor);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(manager, "Change Zone Color");
                selectedZone.ZoneColor = newColor;
                EditorUtility.SetDirty(manager);
                SceneView.RepaintAll(); // Update scene view
            }
        }

        EditorGUILayout.Space(10);

        // Check if there are valid zones
        bool hasValidZones = false;
        foreach (var zone in manager.zones)
        {
            if (zone != null && zone.Points.Count >= 3)
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
        if (selectedZone == null)
            return;

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
                SceneView.RepaintAll(); // Update scene view
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
                SceneView.RepaintAll(); // Update scene view
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
                SceneView.RepaintAll(); // Update scene view
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