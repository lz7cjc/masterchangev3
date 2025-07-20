using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// FIXED VideoIntegrationHelper with consistent property usage and improved position management
/// Key fixes:
/// - Consistent use of VideoUrlLink property throughout
/// - Better synchronization between components
/// - Enhanced error handling and validation
/// - Improved position saving logic
/// </summary>

[System.Serializable]
public class VideoEvents
{
    public UnityEvent OnVideoHoverStart;
    public UnityEvent OnVideoHoverEnd;
    public UnityEvent OnVideoPlay;
    public UnityEvent<string> OnZoneChanged;
    public UnityEvent OnPositionSaved;
}

public class VideoIntegrationHelper : MonoBehaviour
{
    [Header("Integration Settings")]
    public bool autoConfigureEventTriggers = true;
    public bool updatePlayerPrefsOnPlay = true;

    [Header("Hover Timing")]
    public float hoverTimeRequired = 3.0f;

    [Header("FIXED: Position Management")]
    public bool autoSaveOnPositionChange = true;
    public bool autoSaveOnZoneChange = true;
    public float positionSaveThreshold = 0.1f;
    public float saveDelay = 1.0f;

    [Header("Events")]
    public VideoEvents videoEvents;

    [Header("UI References")]
    public TMP_Text zoneLabel;
    public TMP_Text videoCountLabel;
    public GameObject progressIndicator;

    // Core components
    private EnhancedVideoPlayer enhancedVideoPlayer;
    private EventTrigger eventTrigger;
    private FilmZoneManager zoneManager;

    // Hover state tracking
    private bool isHovering = false;
    private float hoverTimer = 0f;

    // FIXED: Enhanced position tracking with validation
    private Vector3 lastSavedPosition;
    private Quaternion lastSavedRotation;
    private Vector3 lastSavedScale;
    private float lastMoveTime;
    private bool hasPendingSave = false;
    private bool isInitialized = false;

    // Cached data
    private string currentZone;
    private int videosInCurrentZone;

    private void Awake()
    {
        // Get required components with validation
        enhancedVideoPlayer = GetComponent<EnhancedVideoPlayer>();
        if (enhancedVideoPlayer == null)
        {
            Debug.LogError($"VideoIntegrationHelper on {gameObject.name} requires EnhancedVideoPlayer component!");
            enabled = false;
            return;
        }

        eventTrigger = GetComponent<EventTrigger>();
        zoneManager = FindObjectOfType<FilmZoneManager>();

        if (zoneManager == null)
        {
            Debug.LogWarning($"No FilmZoneManager found in scene for {gameObject.name}");
        }

        if (autoConfigureEventTriggers)
        {
            SetupEventTriggers();
        }

        // Initialize position tracking
        InitializePositionTracking();
    }

    private void InitializePositionTracking()
    {
        if (transform != null)
        {
            lastSavedPosition = transform.position;
            lastSavedRotation = transform.rotation;
            lastSavedScale = transform.localScale;
            lastMoveTime = 0f;
            hasPendingSave = false;
            isInitialized = true;
        }
    }

    private void SetupEventTriggers()
    {
        // Ensure we have an EventTrigger component
        if (eventTrigger == null)
        {
            eventTrigger = gameObject.AddComponent<EventTrigger>();
        }

        if (eventTrigger.triggers == null)
        {
            eventTrigger.triggers = new System.Collections.Generic.List<EventTrigger.Entry>();
        }

        // Clear existing entries to avoid duplicates
        eventTrigger.triggers.Clear();

        // Set up Pointer Enter
        EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
        pointerEnter.eventID = EventTriggerType.PointerEnter;
        pointerEnter.callback.AddListener((data) => { OnPointerEnterEvent(); });
        eventTrigger.triggers.Add(pointerEnter);

        // Set up Pointer Exit
        EventTrigger.Entry pointerExit = new EventTrigger.Entry();
        pointerExit.eventID = EventTriggerType.PointerExit;
        pointerExit.callback.AddListener((data) => { OnPointerExitEvent(); });
        eventTrigger.triggers.Add(pointerExit);

        // Set up Pointer Click for immediate activation
        EventTrigger.Entry pointerClick = new EventTrigger.Entry();
        pointerClick.eventID = EventTriggerType.PointerClick;
        pointerClick.callback.AddListener((data) => { OnPointerClickEvent(); });
        eventTrigger.triggers.Add(pointerClick);

        Debug.Log($"✅ Set up EventTriggers for VideoIntegrationHelper on {gameObject.name}");
    }

    private void Start()
    {
        if (enhancedVideoPlayer != null)
        {
            // FIXED: Use consistent property names
            currentZone = enhancedVideoPlayer.LastKnownZone;

            // Sync hover time with EnhancedVideoPlayer
            if (enhancedVideoPlayer.hoverTimeRequired != hoverTimeRequired)
            {
                enhancedVideoPlayer.hoverTimeRequired = hoverTimeRequired;
            }

            // FIXED: Validate video configuration
            ValidateVideoConfiguration();

            UpdateUIElements();
            CountVideosInZone();
        }
    }

    // FIXED: Add validation method for video configuration
    private void ValidateVideoConfiguration()
    {
        if (enhancedVideoPlayer == null) return;

        bool hasIssues = false;

        // Check essential properties
        if (string.IsNullOrEmpty(enhancedVideoPlayer.VideoUrlLink))
        {
            Debug.LogWarning($"VideoIntegrationHelper on {gameObject.name}: VideoUrlLink is not set!");
            hasIssues = true;
        }

        if (string.IsNullOrEmpty(enhancedVideoPlayer.LastKnownZone) || enhancedVideoPlayer.LastKnownZone == "Home")
        {
            Debug.LogWarning($"VideoIntegrationHelper on {gameObject.name}: LastKnownZone is not properly set ({enhancedVideoPlayer.LastKnownZone})");
            hasIssues = true;
        }

        // Sync zone properties if they're different
        if (!string.IsNullOrEmpty(enhancedVideoPlayer.zoneName) &&
            enhancedVideoPlayer.zoneName != enhancedVideoPlayer.LastKnownZone)
        {
            Debug.Log($"Syncing zone properties for {gameObject.name}: {enhancedVideoPlayer.zoneName} -> {enhancedVideoPlayer.LastKnownZone}");
            enhancedVideoPlayer.zoneName = enhancedVideoPlayer.LastKnownZone;
            hasIssues = true;
        }

        if (hasIssues)
        {
            Debug.Log($"⚠️ VideoIntegrationHelper validation found issues on {gameObject.name}");
        }
    }

    private void Update()
    {
        if (!isInitialized || enhancedVideoPlayer == null) return;

        // Handle hover timing
        if (isHovering)
        {
            hoverTimer += Time.deltaTime;
            UpdateProgressDisplay();

            if (hoverTimer >= hoverTimeRequired)
            {
                isHovering = false;
                hoverTimer = 0f;
                TriggerVideoPlay();
            }
        }

        // Handle position change detection and auto-save
        if (autoSaveOnPositionChange)
        {
            CheckForPositionChanges();
        }

        // Handle pending save with delay
        if (hasPendingSave && Time.time > lastMoveTime + saveDelay)
        {
            ExecutePendingSave();
        }
    }

    private void CheckForPositionChanges()
    {
        if (transform == null) return;

        bool positionChanged = Vector3.Distance(transform.position, lastSavedPosition) > positionSaveThreshold;
        bool rotationChanged = Quaternion.Angle(transform.rotation, lastSavedRotation) > 1f; // 1 degree threshold
        bool scaleChanged = Vector3.Distance(transform.localScale, lastSavedScale) > 0.01f;

        if (positionChanged || rotationChanged || scaleChanged)
        {
            // Mark that we have a pending save and update the last move time
            hasPendingSave = true;
            lastMoveTime = Time.time;

            if (Application.isEditor)
            {
                // In editor, show some visual feedback that position changed
                Debug.Log($"🔄 Position changed for {enhancedVideoPlayer?.title ?? gameObject.name}");
            }
        }
    }

    private void ExecutePendingSave()
    {
        hasPendingSave = false;

        // FIXED: Use consistent property validation
        if (zoneManager != null && enhancedVideoPlayer != null &&
            !string.IsNullOrEmpty(enhancedVideoPlayer.VideoUrlLink) &&
            !string.IsNullOrEmpty(enhancedVideoPlayer.LastKnownZone))
        {
            try
            {
                // Save the current position
                zoneManager.SaveCurrentPositions();

                // Update our tracking variables
                if (transform != null)
                {
                    lastSavedPosition = transform.position;
                    lastSavedRotation = transform.rotation;
                    lastSavedScale = transform.localScale;
                }

                // Trigger event
                videoEvents.OnPositionSaved?.Invoke();

                Debug.Log($"💾 Auto-saved position for {enhancedVideoPlayer.title} in zone {enhancedVideoPlayer.LastKnownZone}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to auto-save position for {enhancedVideoPlayer.title}: {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"Cannot save position for {gameObject.name}: missing required data (VideoUrlLink: {enhancedVideoPlayer?.VideoUrlLink}, Zone: {enhancedVideoPlayer?.LastKnownZone})");
        }
    }

    private void UpdateProgressDisplay()
    {
        if (progressIndicator != null)
        {
            // If progress indicator has a slider or image component, update it
            UnityEngine.UI.Slider slider = progressIndicator.GetComponent<UnityEngine.UI.Slider>();
            if (slider != null)
            {
                slider.value = hoverTimer / hoverTimeRequired;
            }

            UnityEngine.UI.Image progressImage = progressIndicator.GetComponent<UnityEngine.UI.Image>();
            if (progressImage != null && progressImage.type == UnityEngine.UI.Image.Type.Filled)
            {
                progressImage.fillAmount = hoverTimer / hoverTimeRequired;
            }
        }
    }

    // ===== EVENT TRIGGER CALLBACKS =====

    public void OnPointerEnterEvent()
    {
        isHovering = true;
        hoverTimer = 0f;

        if (progressIndicator != null)
        {
            progressIndicator.SetActive(true);
        }

        // Also trigger the EnhancedVideoPlayer's hover method
        if (enhancedVideoPlayer != null)
        {
            enhancedVideoPlayer.MouseHoverChangeScene();
        }

        videoEvents.OnVideoHoverStart?.Invoke();

        Debug.Log($"Pointer enter on video: {enhancedVideoPlayer?.title ?? gameObject.name}");
    }

    public void OnPointerExitEvent()
    {
        isHovering = false;
        hoverTimer = 0f;

        if (progressIndicator != null)
        {
            progressIndicator.SetActive(false);
        }

        // Also trigger the EnhancedVideoPlayer's exit method
        if (enhancedVideoPlayer != null)
        {
            enhancedVideoPlayer.MouseExit();
        }

        videoEvents.OnVideoHoverEnd?.Invoke();

        Debug.Log($"Pointer exit on video: {enhancedVideoPlayer?.title ?? gameObject.name}");
    }

    public void OnPointerClickEvent()
    {
        // Immediate trigger on click
        TriggerVideoPlay();
    }

    private void TriggerVideoPlay()
    {
        if (progressIndicator != null)
        {
            progressIndicator.SetActive(false);
        }

        // FIXED: Update PlayerPrefs with consistent property usage
        if (updatePlayerPrefsOnPlay && !string.IsNullOrEmpty(currentZone))
        {
            PlayerPrefs.SetString("lastknownzone", currentZone);

            if (enhancedVideoPlayer != null)
            {
                // FIXED: Use VideoUrlLink consistently
                PlayerPrefs.SetString("VideoUrl", enhancedVideoPlayer.VideoUrlLink ?? "");
                PlayerPrefs.SetString("nextscene", enhancedVideoPlayer.nextscene ?? "360VideoApp");
                PlayerPrefs.SetString("returntoscene", enhancedVideoPlayer.returntoscene ?? "mainVR");

                if (!string.IsNullOrEmpty(enhancedVideoPlayer.title))
                {
                    PlayerPrefs.SetString("videoTitle", enhancedVideoPlayer.title);
                }

                if (!string.IsNullOrEmpty(enhancedVideoPlayer.description))
                {
                    PlayerPrefs.SetString("videoDescription", enhancedVideoPlayer.description);
                }
            }

            PlayerPrefs.Save();
        }

        // Trigger events
        videoEvents.OnVideoPlay?.Invoke();

        Debug.Log($"Video play triggered in zone: {currentZone}");

        // Use EnhancedVideoPlayer's SetVideoUrl method if available
        if (enhancedVideoPlayer != null)
        {
            enhancedVideoPlayer.SetVideoUrl();
        }
        else
        {
            // Fallback: Load the scene directly
            UnityEngine.SceneManagement.SceneManager.LoadScene("360VideoApp", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }

    private void UpdateUIElements()
    {
        if (zoneLabel != null)
        {
            zoneLabel.text = $"Zone: {currentZone ?? "Unknown"}";
        }

        if (videoCountLabel != null)
        {
            videoCountLabel.text = $"Videos: {videosInCurrentZone}";
        }
    }

    private void CountVideosInZone()
    {
        if (string.IsNullOrEmpty(currentZone))
        {
            videosInCurrentZone = 0;
            return;
        }

        EnhancedVideoPlayer[] allVideos = FindObjectsOfType<EnhancedVideoPlayer>();
        videosInCurrentZone = 0;

        foreach (var video in allVideos)
        {
            if (video != null && video.LastKnownZone != null &&
                video.LastKnownZone.Equals(currentZone, System.StringComparison.OrdinalIgnoreCase))
            {
                videosInCurrentZone++;
            }
        }

        UpdateUIElements();
    }

    // ===== PUBLIC METHODS FOR EXTERNAL CONTROL =====

    public void PlayVideo()
    {
        TriggerVideoPlay();
    }

    public void SetZone(string newZone)
    {
        if (currentZone != newZone)
        {
            string oldZone = currentZone;
            currentZone = newZone;

            if (enhancedVideoPlayer != null)
            {
                enhancedVideoPlayer.LastKnownZone = newZone;
                enhancedVideoPlayer.zoneName = newZone;

#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(enhancedVideoPlayer);
#endif
            }

            // Auto-save position when zone changes
            if (autoSaveOnZoneChange && zoneManager != null)
            {
                ExecutePendingSave(); // Save immediately on zone change
            }

            CountVideosInZone();
            videoEvents.OnZoneChanged?.Invoke(newZone);

            Debug.Log($"Zone changed from '{oldZone}' to '{newZone}' for {gameObject.name}");
        }
    }

    public string GetCurrentZone()
    {
        return currentZone;
    }

    public int GetVideosInCurrentZone()
    {
        return videosInCurrentZone;
    }

    public bool IsCurrentlyHovering()
    {
        return isHovering;
    }

    public float GetHoverProgress()
    {
        return hoverTimeRequired > 0 ? (hoverTimer / hoverTimeRequired) : 0f;
    }

    // ===== ENHANCED: POSITION MANAGEMENT METHODS =====

    [ContextMenu("Force Save Position")]
    public void ForceSavePosition()
    {
        ExecutePendingSave();
    }

    [ContextMenu("Reset Position Tracking")]
    public void ResetPositionTracking()
    {
        InitializePositionTracking();
        Debug.Log($"Reset position tracking for {enhancedVideoPlayer?.title ?? gameObject.name}");
    }

    public bool HasUnsavedChanges()
    {
        return hasPendingSave;
    }

    public void EnableAutoSave(bool enable)
    {
        autoSaveOnPositionChange = enable;
        if (!enable)
        {
            hasPendingSave = false; // Clear pending save if disabling
        }
    }

    // FIXED: Enhanced integration stats with better validation
    public VideoIntegrationStats GetIntegrationStats()
    {
        return new VideoIntegrationStats
        {
            videoTitle = enhancedVideoPlayer?.title ?? "Unknown",
            videoUrl = enhancedVideoPlayer?.VideoUrlLink ?? "",
            zoneName = currentZone ?? "",
            videoCount = videosInCurrentZone,
            hasValidZone = zoneManager != null && !string.IsNullOrEmpty(currentZone) && zoneManager.GetZoneByName(currentZone) != null,
            isHovering = isHovering,
            hoverProgress = GetHoverProgress(),
            hasUnsavedChanges = hasPendingSave,
            lastMoveTime = lastMoveTime,
            autoSaveEnabled = autoSaveOnPositionChange,
            position = transform != null ? transform.position : Vector3.zero,
            rotation = transform != null ? transform.rotation.eulerAngles : Vector3.zero,
            scale = transform != null ? transform.localScale : Vector3.one,
            isInitialized = isInitialized,
            hasValidVideoPlayer = enhancedVideoPlayer != null,
            hasValidVideoUrl = enhancedVideoPlayer != null && !string.IsNullOrEmpty(enhancedVideoPlayer.VideoUrlLink)
        };
    }

    // ===== ENHANCED: SYNCHRONIZATION METHODS =====

    [ContextMenu("Synchronize With Enhanced Video Player")]
    public void SynchronizeWithEnhancedVideoPlayer()
    {
        if (enhancedVideoPlayer == null)
        {
            Debug.LogError($"Cannot synchronize: No EnhancedVideoPlayer found on {gameObject.name}");
            return;
        }

        bool madeChanges = false;

        // Sync zone information
        if (currentZone != enhancedVideoPlayer.LastKnownZone)
        {
            currentZone = enhancedVideoPlayer.LastKnownZone;
            madeChanges = true;
        }

        // Sync hover time
        if (hoverTimeRequired != enhancedVideoPlayer.hoverTimeRequired)
        {
            hoverTimeRequired = enhancedVideoPlayer.hoverTimeRequired;
            madeChanges = true;
        }

        // Ensure zone name consistency
        if (!string.IsNullOrEmpty(enhancedVideoPlayer.LastKnownZone) &&
            enhancedVideoPlayer.zoneName != enhancedVideoPlayer.LastKnownZone)
        {
            enhancedVideoPlayer.zoneName = enhancedVideoPlayer.LastKnownZone;
            madeChanges = true;
        }

        if (madeChanges)
        {
            UpdateUIElements();
            CountVideosInZone();

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.EditorUtility.SetDirty(enhancedVideoPlayer);
#endif

            Debug.Log($"✅ Synchronized VideoIntegrationHelper with EnhancedVideoPlayer on {gameObject.name}");
        }
    }

    [ContextMenu("Validate Configuration")]
    public void ValidateConfiguration()
    {
        List<string> issues = new List<string>();
        List<string> warnings = new List<string>();

        // Check essential components
        if (enhancedVideoPlayer == null)
        {
            issues.Add("Missing EnhancedVideoPlayer component");
        }
        else
        {
            // Check video configuration
            if (string.IsNullOrEmpty(enhancedVideoPlayer.VideoUrlLink))
            {
                issues.Add("VideoUrlLink not set");
            }

            if (string.IsNullOrEmpty(enhancedVideoPlayer.LastKnownZone) || enhancedVideoPlayer.LastKnownZone == "Home")
            {
                issues.Add("LastKnownZone not properly set");
            }

            if (enhancedVideoPlayer.zoneName != enhancedVideoPlayer.LastKnownZone)
            {
                warnings.Add("zoneName and LastKnownZone mismatch");
            }
        }

        // Check EventTrigger setup
        if (eventTrigger == null)
        {
            warnings.Add("No EventTrigger component found");
        }
        else if (eventTrigger.triggers == null || eventTrigger.triggers.Count == 0)
        {
            warnings.Add("EventTrigger has no triggers configured");
        }

        // Check BoxCollider
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            warnings.Add("No BoxCollider found");
        }
        else if (!boxCollider.isTrigger)
        {
            warnings.Add("BoxCollider is not set as trigger");
        }

        // Check zone validity
        if (zoneManager != null && !string.IsNullOrEmpty(currentZone))
        {
            var zone = zoneManager.GetZoneByName(currentZone);
            if (zone == null)
            {
                issues.Add($"Zone '{currentZone}' not found in FilmZoneManager");
            }
        }
        else if (zoneManager == null)
        {
            warnings.Add("No FilmZoneManager found in scene");
        }

        // Report results
        if (issues.Count == 0 && warnings.Count == 0)
        {
            Debug.Log($"✅ {gameObject.name}: Configuration is valid");
        }
        else
        {
            if (issues.Count > 0)
            {
                Debug.LogError($"❌ {gameObject.name} - Issues: {string.Join(", ", issues)}");
            }
            if (warnings.Count > 0)
            {
                Debug.LogWarning($"⚠️ {gameObject.name} - Warnings: {string.Join(", ", warnings)}");
            }
        }
    }

    // ===== UTILITY METHODS =====

    private void OnValidate()
    {
        // Ensure thresholds are sensible
        positionSaveThreshold = Mathf.Max(0.01f, positionSaveThreshold);
        saveDelay = Mathf.Max(0.1f, saveDelay);
        hoverTimeRequired = Mathf.Max(0.1f, hoverTimeRequired);

        // Sync with EnhancedVideoPlayer if available
        if (Application.isPlaying && enhancedVideoPlayer != null)
        {
            enhancedVideoPlayer.hoverTimeRequired = hoverTimeRequired;
        }
    }

    private void OnDestroy()
    {
        // Save any pending changes before destruction
        if (hasPendingSave && autoSaveOnPositionChange)
        {
            ExecutePendingSave();
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Draw position change indicator in editor
        if (hasPendingSave && autoSaveOnPositionChange)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 3f, 0.5f);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 3f);
        }

        // Draw zone indicator if in a valid zone
        if (!string.IsNullOrEmpty(currentZone) && zoneManager != null)
        {
            FilmZone zone = zoneManager.GetZoneByName(currentZone);
            if (zone != null)
            {
                Gizmos.color = zone.gizmoColor * 0.5f;
                Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one * 0.5f);
            }
        }

        // Draw video URL validation indicator
        if (enhancedVideoPlayer != null)
        {
            if (string.IsNullOrEmpty(enhancedVideoPlayer.VideoUrlLink))
            {
                // Red sphere for missing video URL
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(transform.position + Vector3.up * 4f, 0.3f);
            }
            else
            {
                // Green sphere for valid video URL
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(transform.position + Vector3.up * 4f, 0.2f);
            }
        }
    }
#endif
}

// ===== ENHANCED DATA STRUCTURES =====

[System.Serializable]
public class VideoIntegrationStats
{
    public string videoTitle;
    public string videoUrl;
    public string zoneName;
    public int videoCount;
    public bool hasValidZone;
    public bool isHovering;
    public float hoverProgress;
    public bool hasUnsavedChanges;
    public float lastMoveTime;
    public bool autoSaveEnabled;
    public Vector3 position;
    public Vector3 rotation;
    public Vector3 scale;

    // FIXED: Additional validation fields
    public bool isInitialized;
    public bool hasValidVideoPlayer;
    public bool hasValidVideoUrl;

    public override string ToString()
    {
        return $"Video: '{videoTitle}' in Zone: '{zoneName}' | " +
               $"URL: {(hasValidVideoUrl ? "Valid" : "Invalid")} | " +
               $"Hovering: {isHovering} | Unsaved: {hasUnsavedChanges} | " +
               $"AutoSave: {autoSaveEnabled} | Pos: {position}";
    }

    public bool IsFullyValid()
    {
        return hasValidVideoPlayer && hasValidVideoUrl && hasValidZone &&
               !string.IsNullOrEmpty(videoTitle) && !string.IsNullOrEmpty(zoneName);
    }
}

// ===== ENHANCED COMPATIBILITY COMPONENTS =====

// Enhanced VR compatibility component with better error handling
public class VRCompatibleVideoTrigger : MonoBehaviour
{
    [Header("VR Integration")]
    public bool enableVRMode = false;
    public Camera vrCamera;

    [Header("VR Position Management")]
    public bool vrAutoSavePositions = true;
    public float vrGrabThreshold = 0.2f;

    private VideoIntegrationHelper integrationHelper;
    private Vector3 vrGrabStartPosition;
    private bool isBeingGrabbed = false;

    private void Awake()
    {
        integrationHelper = GetComponent<VideoIntegrationHelper>();
        if (integrationHelper == null)
        {
            integrationHelper = gameObject.AddComponent<VideoIntegrationHelper>();
            Debug.Log($"Added VideoIntegrationHelper to {gameObject.name} for VR compatibility");
        }
    }

    private void Start()
    {
        if (enableVRMode && vrCamera == null)
        {
            vrCamera = Camera.main;
            if (vrCamera == null)
            {
                Debug.LogWarning($"VR mode enabled but no camera found for {gameObject.name}");
            }
        }
    }

    // Methods that can be called by VR reticle pointer
    public void OnVRPointerEnter()
    {
        if (integrationHelper != null)
        {
            integrationHelper.OnPointerEnterEvent();
        }
    }

    public void OnVRPointerExit()
    {
        if (integrationHelper != null)
        {
            integrationHelper.OnPointerExitEvent();
        }
    }

    public void OnVRPointerClick()
    {
        if (integrationHelper != null)
        {
            integrationHelper.OnPointerClickEvent();
        }
    }

    // FIXED: Enhanced VR grab/release methods with validation
    public void OnVRGrabStart()
    {
        isBeingGrabbed = true;
        vrGrabStartPosition = transform.position;

        // Disable auto-save during grab to avoid spam
        if (integrationHelper != null)
        {
            integrationHelper.EnableAutoSave(false);
        }

        Debug.Log($"VR grab started on {gameObject.name}");
    }

    public void OnVRGrabEnd()
    {
        isBeingGrabbed = false;

        // Re-enable auto-save and check if we need to save
        if (integrationHelper != null)
        {
            integrationHelper.EnableAutoSave(vrAutoSavePositions);

            // If moved significantly, force save
            if (Vector3.Distance(transform.position, vrGrabStartPosition) > vrGrabThreshold)
            {
                integrationHelper.ForceSavePosition();
                Debug.Log($"VR grab moved {gameObject.name} - position saved");
            }
        }

        Debug.Log($"VR grab ended on {gameObject.name}");
    }

    public bool IsBeingGrabbed()
    {
        return isBeingGrabbed;
    }

    // Get VR-specific status
    public VRIntegrationStats GetVRStats()
    {
        var baseStats = integrationHelper?.GetIntegrationStats();
        return new VRIntegrationStats
        {
            baseStats = baseStats,
            vrModeEnabled = enableVRMode,
            hasVRCamera = vrCamera != null,
            isBeingGrabbed = isBeingGrabbed,
            grabStartPosition = vrGrabStartPosition,
            grabThreshold = vrGrabThreshold,
            autoSaveOnGrab = vrAutoSavePositions
        };
    }
}

[System.Serializable]
public class VRIntegrationStats
{
    public VideoIntegrationStats baseStats;
    public bool vrModeEnabled;
    public bool hasVRCamera;
    public bool isBeingGrabbed;
    public Vector3 grabStartPosition;
    public float grabThreshold;
    public bool autoSaveOnGrab;

    public override string ToString()
    {
        return $"VR Integration - Mode: {vrModeEnabled}, Camera: {hasVRCamera}, " +
               $"Grabbed: {isBeingGrabbed}, AutoSave: {autoSaveOnGrab}";
    }
}

// Enhanced backward compatibility bridge with better validation
public class EnhancedVideoPlayerBridge : MonoBehaviour
{
    [Header("Bridge Settings")]
    public bool autoSetupOnStart = true;

    [Header("Position Bridge")]
    public bool bridgePositionSaving = true;

    [Header("Validation")]
    public bool validateOnStart = true;

    private EnhancedVideoPlayer enhancedPlayer;
    private VideoIntegrationHelper integrationHelper;

    private void Start()
    {
        if (autoSetupOnStart)
        {
            SetupBridge();
        }

        if (validateOnStart)
        {
            ValidateBridgeSetup();
        }
    }

    public void SetupBridge()
    {
        // Ensure we have both components
        enhancedPlayer = GetComponent<EnhancedVideoPlayer>();
        if (enhancedPlayer == null)
        {
            enhancedPlayer = gameObject.AddComponent<EnhancedVideoPlayer>();
            Debug.Log($"Added EnhancedVideoPlayer to {gameObject.name}");
        }

        integrationHelper = GetComponent<VideoIntegrationHelper>();
        if (integrationHelper == null)
        {
            integrationHelper = gameObject.AddComponent<VideoIntegrationHelper>();
            Debug.Log($"Added VideoIntegrationHelper to {gameObject.name}");
        }

        // Sync settings
        if (enhancedPlayer != null && integrationHelper != null)
        {
            integrationHelper.hoverTimeRequired = enhancedPlayer.hoverTimeRequired;

            // Bridge position saving
            if (bridgePositionSaving)
            {
                integrationHelper.autoSaveOnPositionChange = true;
                integrationHelper.autoSaveOnZoneChange = true;
            }
        }

        Debug.Log($"✅ EnhancedVideoPlayer bridge setup complete for: {gameObject.name}");
    }

    public void ValidateBridgeSetup()
    {
        List<string> issues = new List<string>();

        if (enhancedPlayer == null)
        {
            issues.Add("Missing EnhancedVideoPlayer");
        }
        else
        {
            if (string.IsNullOrEmpty(enhancedPlayer.VideoUrlLink))
            {
                issues.Add("VideoUrlLink not set");
            }
            if (string.IsNullOrEmpty(enhancedPlayer.LastKnownZone))
            {
                issues.Add("LastKnownZone not set");
            }
        }

        if (integrationHelper == null)
        {
            issues.Add("Missing VideoIntegrationHelper");
        }

        if (issues.Count == 0)
        {
            Debug.Log($"✅ Bridge validation passed for {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"⚠️ Bridge validation issues for {gameObject.name}: {string.Join(", ", issues)}");
        }
    }

    public void ConfigureVideo(string videoUrl, string title, string description, string zoneName)
    {
        if (enhancedPlayer != null)
        {
            // FIXED: Use consistent property names
            enhancedPlayer.VideoUrlLink = videoUrl;
            enhancedPlayer.title = title;
            enhancedPlayer.description = description;
            enhancedPlayer.zoneName = zoneName;
            enhancedPlayer.LastKnownZone = zoneName;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(enhancedPlayer);
#endif
        }

        if (integrationHelper != null)
        {
            integrationHelper.SetZone(zoneName);
        }

        Debug.Log($"✅ Configured video: {title} in zone: {zoneName} with URL: {videoUrl}");
    }

    public string GetVideoInfo()
    {
        if (enhancedPlayer != null)
        {
            return $"Title: {enhancedPlayer.title}, Zone: {enhancedPlayer.LastKnownZone}, URL: {enhancedPlayer.VideoUrlLink}";
        }
        return "No EnhancedVideoPlayer found";
    }

    public VideoIntegrationStats GetDetailedStats()
    {
        if (integrationHelper != null)
        {
            return integrationHelper.GetIntegrationStats();
        }
        return new VideoIntegrationStats { videoTitle = "No Integration Helper" };
    }

    public BridgeValidationResult GetValidationResult()
    {
        return new BridgeValidationResult
        {
            hasEnhancedPlayer = enhancedPlayer != null,
            hasIntegrationHelper = integrationHelper != null,
            hasValidVideoUrl = enhancedPlayer != null && !string.IsNullOrEmpty(enhancedPlayer.VideoUrlLink),
            hasValidZone = enhancedPlayer != null && !string.IsNullOrEmpty(enhancedPlayer.LastKnownZone),
            bridgeConfigured = enhancedPlayer != null && integrationHelper != null,
            positionSavingEnabled = integrationHelper != null && integrationHelper.autoSaveOnPositionChange
        };
    }
}

[System.Serializable]
public class BridgeValidationResult
{
    public bool hasEnhancedPlayer;
    public bool hasIntegrationHelper;
    public bool hasValidVideoUrl;
    public bool hasValidZone;
    public bool bridgeConfigured;
    public bool positionSavingEnabled;

    public bool IsFullyValid()
    {
        return hasEnhancedPlayer && hasIntegrationHelper && hasValidVideoUrl && hasValidZone && bridgeConfigured;
    }

    public override string ToString()
    {
        return $"Bridge: {(IsFullyValid() ? "Valid" : "Invalid")} - " +
               $"Player: {hasEnhancedPlayer}, Helper: {hasIntegrationHelper}, " +
               $"URL: {hasValidVideoUrl}, Zone: {hasValidZone}";
    }
}