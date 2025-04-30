using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script organizes video prefabs in the hierarchy by their zone assignments
public class ZoneHierarchyOrganizer : MonoBehaviour
{
    [SerializeField] private bool organizeOnStart = true;
    [SerializeField] private bool reorganizeExistingPrefabs = true;
    [SerializeField] private Transform hierarchyRoot;

    private Dictionary<string, Transform> zoneParents = new Dictionary<string, Transform>();

    private void Awake()
    {
        if (hierarchyRoot == null)
        {
            hierarchyRoot = transform;
        }
    }

    private void Start()
    {
        if (organizeOnStart)
        {
            OrganizeHierarchy();
        }
    }

    public void OrganizeHierarchy()
    {
        // Clear existing zone parent references
        zoneParents.Clear();

        if (reorganizeExistingPrefabs)
        {
            // Find all EnhancedVideoPlayer components in the scene
            EnhancedVideoPlayer[] allPlayers = FindObjectsOfType<EnhancedVideoPlayer>();

            foreach (EnhancedVideoPlayer currentPlayer in allPlayers)
            {
                OrganizeVideoPrefab(currentPlayer.gameObject);
            }
        }
    }

    public void OrganizeVideoPrefab(GameObject videoPrefab)
    {
        if (videoPrefab == null) return;

        // Get zone information from the prefab
        EnhancedVideoPlayer targetPlayer = videoPrefab.GetComponent<EnhancedVideoPlayer>();
        if (targetPlayer == null) return;

        string zoneName = !string.IsNullOrEmpty(targetPlayer.zoneName) ?
            targetPlayer.zoneName : "Unassigned";

        // Get or create parent for this zone
        Transform zoneParent = GetOrCreateZoneParent(zoneName);

        // Set the prefab as a child of the zone parent
        videoPrefab.transform.SetParent(zoneParent, true);
    }

    private Transform GetOrCreateZoneParent(string zoneName)
    {
        // Check if we already have a parent for this zone
        if (zoneParents.TryGetValue(zoneName, out Transform parent) && parent != null)
        {
            return parent;
        }

        // Look for existing zone parent
        Transform existingParent = hierarchyRoot.Find(zoneName);
        if (existingParent != null)
        {
            zoneParents[zoneName] = existingParent;
            return existingParent;
        }

        // Create new parent for this zone
        GameObject zoneParentObj = new GameObject(zoneName);
        zoneParentObj.transform.SetParent(hierarchyRoot);
        zoneParentObj.transform.localPosition = Vector3.zero;

        zoneParents[zoneName] = zoneParentObj.transform;
        return zoneParentObj.transform;
    }

    // Utility method to add a new video prefab to the correct zone hierarchy
    public void AddVideoPrefabToHierarchy(GameObject videoPrefab, string zoneName)
    {
        if (videoPrefab == null) return;

        if (string.IsNullOrEmpty(zoneName))
        {
            // Try to get zone from video player component
            EnhancedVideoPlayer objectPlayer = videoPrefab.GetComponent<EnhancedVideoPlayer>();
            if (objectPlayer != null && !string.IsNullOrEmpty(objectPlayer.zoneName))
            {
                zoneName = objectPlayer.zoneName;
            }
            else
            {
                zoneName = "Unassigned";
            }
        }

        // Get or create parent for this zone
        Transform zoneParent = GetOrCreateZoneParent(zoneName);

        // Set the prefab as a child of the zone parent
        videoPrefab.transform.SetParent(zoneParent, true);

        // If it has an EnhancedVideoPlayer component, update its zoneName
        EnhancedVideoPlayer gameObjectPlayer = videoPrefab.GetComponent<EnhancedVideoPlayer>();
        if (gameObjectPlayer != null && string.IsNullOrEmpty(gameObjectPlayer.zoneName))
        {
            gameObjectPlayer.zoneName = zoneName;
        }
    }

    // Utility method to reorganize all existing prefabs by their zone
    public void ReorganizeExistingPrefabs()
    {
        // Find all EnhancedVideoPlayer components
        EnhancedVideoPlayer[] sceneVideoPlayers = FindObjectsOfType<EnhancedVideoPlayer>();

        // Group objects by zone
        Dictionary<string, List<GameObject>> zoneGroups = new Dictionary<string, List<GameObject>>();

        foreach (EnhancedVideoPlayer scenePlayer in sceneVideoPlayers)
        {
            string zoneName = !string.IsNullOrEmpty(scenePlayer.zoneName) ?
                scenePlayer.zoneName : "Unassigned";

            if (!zoneGroups.ContainsKey(zoneName))
            {
                zoneGroups[zoneName] = new List<GameObject>();
            }

            zoneGroups[zoneName].Add(scenePlayer.gameObject);
        }

        // Create parents and organize
        foreach (var zoneGroup in zoneGroups)
        {
            Transform zoneParent = GetOrCreateZoneParent(zoneGroup.Key);

            foreach (GameObject obj in zoneGroup.Value)
            {
                obj.transform.SetParent(zoneParent, true);
            }
        }
    }
}