using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Specialized manager for placing videos in target zone folders and ensuring reticle interaction
/// </summary>
public class TargetZoneVideoManager : MonoBehaviour
{
    [Header("Database Reference")]
    [SerializeField] private VideoDatabaseManager databaseManager;

    [Header("Target Structure")]
    [SerializeField] private GameObject targetRootObject;
    [SerializeField] private bool createChildrenForIndividualVideos = true;

    [Header("Prefab Settings")]
    [SerializeField] private GameObject defaultVideoPrefab;
    [SerializeField] private List<PrefabTypeMapping> prefabMappings = new List<PrefabTypeMapping>();

    [Header("Runtime Settings")]
    [SerializeField] private bool placeVideosOnStart = true;
    [SerializeField] private float startDelay = 0.5f;
    [SerializeField] private bool debugMode = true;

    [System.Serializable]
    public class PrefabTypeMapping
    {
        public string typeName;
        public GameObject prefab;
    }

    private Dictionary<string, GameObject> prefabTypeMap = new Dictionary<string, GameObject>();
    private Dictionary<string, Transform> zoneTransforms = new Dictionary<string, Transform>();

    private void Awake()
    {
        if (databaseManager == null)
        {
            databaseManager = FindObjectOfType<VideoDatabaseManager>();
            if (databaseManager == null)
            {
                Debug.LogError("No VideoDatabaseManager found! Please assign one in the inspector.");
            }
        }

        if (targetRootObject == null)
        {
            targetRootObject = GameObject.Find("Targets");
            if (targetRootObject == null)
            {
                Debug.LogError("Target root object not found! Please assign one in the inspector.");
            }
        }

        // Build prefab type map
        BuildPrefabMappings();

        // Discover target zone folders
        DiscoverTargetZones();
    }

    private void Start()
    {
        if (placeVideosOnStart)
        {
            Invoke("PlaceAllVideos", startDelay);
        }
    }

    private void BuildPrefabMappings()
    {
        prefabTypeMap.Clear();

        // Add default mapping
        if (defaultVideoPrefab != null)
        {
            prefabTypeMap["Default"] = defaultVideoPrefab;
        }

        // Add custom mappings
        foreach (var mapping in prefabMappings)
        {
            if (!string.IsNullOrEmpty(mapping.typeName) && mapping.prefab != null)
            {
                prefabTypeMap[mapping.typeName] = mapping.prefab;
                if (debugMode) Debug.Log($"Added prefab mapping: {mapping.typeName}");
            }
        }
    }

    private void DiscoverTargetZones()
    {
        zoneTransforms.Clear();

        if (targetRootObject == null) return;

        // Find all immediate children of the target root
        foreach (Transform child in targetRootObject.transform)
        {
            string zoneName = child.name;

            // Skip any objects with names that start with underscore or are hidden
            if (zoneName.StartsWith("_") || !child.gameObject.activeInHierarchy)
                continue;

            zoneTransforms[zoneName] = child;
            if (debugMode) Debug.Log($"Found zone: {zoneName}");
        }
    }

    public void PlaceAllVideos()
    {
        if (databaseManager == null || targetRootObject == null)
        {
            Debug.LogError("Cannot place videos: Missing database manager or target root");
            return;
        }

        // First, clear existing videos (optional)
        ClearExistingVideos();

        // Process each zone
        foreach (var zoneEntry in zoneTransforms)
        {
            string zoneName = zoneEntry.Key;
            Transform zoneTransform = zoneEntry.Value;

            // Get videos for this zone
            List<VideoEntry> zoneVideos = databaseManager.GetEntriesForZone(zoneName);

            if (zoneVideos == null || zoneVideos.Count == 0)
            {
                if (debugMode) Debug.Log($"No videos found for zone: {zoneName}");
                continue;
            }

            if (debugMode) Debug.Log($"Placing {zoneVideos.Count} videos in zone: {zoneName}");

            // Create video objects for this zone
            foreach (var video in zoneVideos)
            {
                CreateVideoForZone(video, zoneTransform);
            }
        }
    }

    private void ClearExistingVideos()
    {
        foreach (var zoneEntry in zoneTransforms)
        {
            Transform zoneTransform = zoneEntry.Value;

            // Find and remove any existing video objects
            List<Transform> toDestroy = new List<Transform>();

            foreach (Transform child in zoneTransform)
            {
                // Only destroy objects that have EnhancedVideoPlayer component
                if (child.GetComponent<EnhancedVideoPlayer>() != null)
                {
                    toDestroy.Add(child);
                }
            }

            // Destroy them outside the loop to avoid modifying during enumeration
            foreach (var child in toDestroy)
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    private void CreateVideoForZone(VideoEntry video, Transform zoneTransform)
    {
        // Determine which prefab to use
        GameObject prefabToUse = GetPrefabForVideo(video);
        if (prefabToUse == null) return;

        // Create container object if needed
        Transform parentTransform = zoneTransform;
        if (createChildrenForIndividualVideos)
        {
            string videoName = string.IsNullOrEmpty(video.Title) ? video.FileName : video.Title;
            GameObject containerObj = new GameObject(videoName);
            containerObj.transform.SetParent(zoneTransform);
            containerObj.transform.localPosition = Vector3.zero;
            parentTransform = containerObj.transform;
        }

        // Create the video instance
        GameObject instance = Instantiate(prefabToUse, parentTransform);
        instance.transform.localPosition = Vector3.zero;
        instance.name = "Video_" + (string.IsNullOrEmpty(video.Title) ? video.FileName : video.Title);

        // Setup video component
        SetupVideoPlayer(instance, video);

        // Setup text components
        SetupTextComponents(instance, video);

        // Setup collider and interaction for reticle
        SetupReticleInteraction(instance);
    }

    private GameObject GetPrefabForVideo(VideoEntry video)
    {
        if (video == null) return defaultVideoPrefab;

        // Try to find prefab by type
        if (!string.IsNullOrEmpty(video.Prefab) && prefabTypeMap.TryGetValue(video.Prefab, out GameObject typedPrefab))
        {
            return typedPrefab;
        }

        // Fall back to default
        return defaultVideoPrefab;
    }

    private void SetupVideoPlayer(GameObject instance, VideoEntry video)
    {
        // Get or add EnhancedVideoPlayer component
        EnhancedVideoPlayer videoPlayer = instance.GetComponent<EnhancedVideoPlayer>();
        if (videoPlayer == null)
        {
            videoPlayer = instance.AddComponent<EnhancedVideoPlayer>();
        }

        // Set basic video properties
        videoPlayer.VideoUrlLink = video.PublicUrl;
        videoPlayer.title = string.IsNullOrEmpty(video.Title) ? video.FileName : video.Title;
        videoPlayer.description = video.Description ?? "";
        videoPlayer.prefabType = video.Prefab;

        // Get zone info
        if (video.Zones != null && video.Zones.Count > 0)
        {
            videoPlayer.zoneName = video.Zones[0];
        }

        // Set return scene info
        videoPlayer.returntoscene = SceneManager.GetActiveScene().name;
        videoPlayer.behaviour = "PlayAndReturn";
    }

    private void SetupTextComponents(GameObject instance, VideoEntry video)
    {
        // Find all TextMeshPro components in the hierarchy
        TextMeshProUGUI[] textComponents = instance.GetComponentsInChildren<TextMeshProUGUI>(true);

        if (textComponents.Length == 0)
        {
            if (debugMode) Debug.LogWarning($"No TextMeshPro components found on video {instance.name}");
            return;
        }

        // Try to identify title and description components
        TextMeshProUGUI titleComponent = null;
        TextMeshProUGUI descComponent = null;

        // First try to find by name
        foreach (var textComp in textComponents)
        {
            string objName = textComp.gameObject.name.ToLower();

            if (objName.Contains("title") || objName.Contains("name"))
            {
                titleComponent = textComp;
            }
            else if (objName.Contains("desc") || objName.Contains("info"))
            {
                descComponent = textComp;
            }
        }

        // If not found by name, take the first component as title
        if (titleComponent == null && textComponents.Length > 0)
        {
            titleComponent = textComponents[0];
        }

        // If we have more than one and no description yet, use the second one
        if (descComponent == null && textComponents.Length > 1 && titleComponent != textComponents[1])
        {
            descComponent = textComponents[1];
        }

        // Set text values
        if (titleComponent != null)
        {
            // Set the title text
            string titleText = string.IsNullOrEmpty(video.Title) ? video.FileName : video.Title;
            titleComponent.text = titleText;
            if (debugMode) Debug.Log($"Set title to '{titleText}' on {instance.name}");

            // Link to video player
            EnhancedVideoPlayer videoPlayer = instance.GetComponent<EnhancedVideoPlayer>();
            if (videoPlayer != null)
            {
                videoPlayer.TMP_title = titleComponent;
                videoPlayer.hasText = true;
            }
        }

        if (descComponent != null && !string.IsNullOrEmpty(video.Description))
        {
            descComponent.text = video.Description;

            // Link to video player
            EnhancedVideoPlayer videoPlayer = instance.GetComponent<EnhancedVideoPlayer>();
            if (videoPlayer != null)
            {
                videoPlayer.TMP_description = descComponent;
            }
        }
    }

    private void SetupReticleInteraction(GameObject instance)
    {
        // Ensure the object has a collider
        Collider collider = instance.GetComponent<Collider>();
        if (collider == null)
        {
            // Add a box collider sized to the object
            BoxCollider boxCollider = instance.AddComponent<BoxCollider>();

            // Calculate size from renderers
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = new Bounds(instance.transform.position, Vector3.zero);
                foreach (Renderer renderer in renderers)
                {
                    bounds.Encapsulate(renderer.bounds);
                }

                boxCollider.center = instance.transform.InverseTransformPoint(bounds.center);
                boxCollider.size = bounds.size;
            }
            else
            {
                // Default size
                boxCollider.size = new Vector3(1f, 1f, 0.1f);
            }
        }

        // Add VideoLinkInteraction if needed
        VideoLinkInteraction interaction = instance.GetComponent<VideoLinkInteraction>();
        if (interaction == null)
        {
            interaction = instance.AddComponent<VideoLinkInteraction>();
        }

        // Set up EventTrigger for handling reticle events
        EventTrigger eventTrigger = instance.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = instance.AddComponent<EventTrigger>();

            // Add pointer enter event
            EventTrigger.Entry enterEntry = new EventTrigger.Entry();
            enterEntry.eventID = EventTriggerType.PointerEnter;
            enterEntry.callback.AddListener((data) => {
                EnhancedVideoPlayer videoPlayer = instance.GetComponent<EnhancedVideoPlayer>();
                if (videoPlayer != null)
                {
                    videoPlayer.MouseHoverChangeScene();
                    if (debugMode) Debug.Log($"Pointer ENTER on {instance.name}");
                }
            });
            eventTrigger.triggers.Add(enterEntry);

            // Add pointer exit event 
            EventTrigger.Entry exitEntry = new EventTrigger.Entry();
            exitEntry.eventID = EventTriggerType.PointerExit;
            exitEntry.callback.AddListener((data) => {
                EnhancedVideoPlayer videoPlayer = instance.GetComponent<EnhancedVideoPlayer>();
                if (videoPlayer != null)
                {
                    videoPlayer.MouseExit();
                    if (debugMode) Debug.Log($"Pointer EXIT on {instance.name}");
                }
            });
            eventTrigger.triggers.Add(exitEntry);
        }
    }
}