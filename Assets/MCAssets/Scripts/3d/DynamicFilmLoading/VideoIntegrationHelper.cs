using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.EventSystems;

// Helper component that bridges the zone system with Unity's Event System for VR and mouse interactions
[System.Serializable]
public class VideoEvents
{
    public UnityEvent OnVideoHoverStart;
    public UnityEvent OnVideoHoverEnd;
    public UnityEvent OnVideoPlay;
    public UnityEvent<string> OnZoneChanged;
}

public class VideoIntegrationHelper : MonoBehaviour
{
    [Header("Integration Settings")]
    public bool autoConfigureEventTriggers = true;
    public bool updatePlayerPrefsOnPlay = true;

    [Header("Hover Timing")]
    public float hoverTimeRequired = 3.0f;

    [Header("Events")]
    public VideoEvents videoEvents;

    [Header("UI References")]
    public TMP_Text zoneLabel;
    public TMP_Text videoCountLabel;
    public GameObject progressIndicator;

    // Updated to work with EnhancedVideoPlayer instead of VideoZonePrefab
    private EnhancedVideoPlayer enhancedVideoPlayer;
    private EventTrigger eventTrigger;
    private FilmZoneManager zoneManager;

    // Hover state tracking
    private bool isHovering = false;
    private float hoverTimer = 0f;

    // Cached data
    private string currentZone;
    private int videosInCurrentZone;

    private void Awake()
    {
        // Get required components - UPDATED to use EnhancedVideoPlayer
        enhancedVideoPlayer = GetComponent<EnhancedVideoPlayer>();
        eventTrigger = GetComponent<EventTrigger>();
        zoneManager = FindObjectOfType<FilmZoneManager>();

        if (autoConfigureEventTriggers)
        {
            SetupEventTriggers();
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

        // Optional: Pointer Click for immediate activation
        EventTrigger.Entry pointerClick = new EventTrigger.Entry();
        pointerClick.eventID = EventTriggerType.PointerClick;
        pointerClick.callback.AddListener((data) => { OnPointerClickEvent(); });
        eventTrigger.triggers.Add(pointerClick);
    }

    private void Start()
    {
        if (enhancedVideoPlayer != null)
        {
            // Use LastKnownZone from EnhancedVideoPlayer
            currentZone = enhancedVideoPlayer.LastKnownZone;

            // Sync hover time with EnhancedVideoPlayer
            if (enhancedVideoPlayer.hoverTimeRequired != hoverTimeRequired)
            {
                enhancedVideoPlayer.hoverTimeRequired = hoverTimeRequired;
            }

            UpdateUIElements();
            CountVideosInZone();
        }
    }

    private void Update()
    {
        // Handle hover timing (matching EnhancedVideoPlayer pattern)
        if (isHovering)
        {
            hoverTimer += Time.deltaTime;

            // Update progress indicator
            UpdateProgressDisplay();

            if (hoverTimer >= hoverTimeRequired)
            {
                isHovering = false;
                hoverTimer = 0f;
                TriggerVideoPlay();
            }
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

    // Event Trigger Callbacks (matching EnhancedVideoPlayer pattern)
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

        // Update PlayerPrefs with zone information (matching EnhancedVideoPlayer)
        if (updatePlayerPrefsOnPlay && !string.IsNullOrEmpty(currentZone))
        {
            PlayerPrefs.SetString("lastknownzone", currentZone);

            if (enhancedVideoPlayer != null)
            {
                PlayerPrefs.SetString("nextscene", enhancedVideoPlayer.nextscene);
                PlayerPrefs.SetString("returntoscene", enhancedVideoPlayer.returntoscene);
              //  PlayerPrefs.SetInt("stage", enhancedVideoPlayer.returnstage);
                //PlayerPrefs.SetString("behaviour", enhancedVideoPlayer.behaviour);
                PlayerPrefs.SetString("VideoUrl", enhancedVideoPlayer.VideoUrlLink);

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
            zoneLabel.text = $"Zone: {currentZone}";
        }

        if (videoCountLabel != null)
        {
            videoCountLabel.text = $"Videos: {videosInCurrentZone}";
        }
    }

    private void CountVideosInZone()
    {
        if (string.IsNullOrEmpty(currentZone)) return;

        // UPDATED to count EnhancedVideoPlayer components instead of VideoZonePrefab
        EnhancedVideoPlayer[] allVideos = FindObjectsOfType<EnhancedVideoPlayer>();
        videosInCurrentZone = 0;

        foreach (var video in allVideos)
        {
            if (video.LastKnownZone.Equals(currentZone, System.StringComparison.OrdinalIgnoreCase))
            {
                videosInCurrentZone++;
            }
        }

        UpdateUIElements();
    }

    // Public methods for external control
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
            }

            CountVideosInZone();
            videoEvents.OnZoneChanged?.Invoke(newZone);

            Debug.Log($"Zone changed from '{oldZone}' to '{newZone}'");
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

    // Utility method to get zone statistics
    public ZoneStatistics GetZoneStatistics()
    {
        return new ZoneStatistics
        {
            zoneName = currentZone,
            videoCount = videosInCurrentZone,
            hasValidZone = zoneManager != null && zoneManager.GetZoneByName(currentZone) != null,
            isHovering = isHovering,
            hoverProgress = GetHoverProgress()
        };
    }
}

[System.Serializable]
public class ZoneStatistics
{
    public string zoneName;
    public int videoCount;
    public bool hasValidZone;
    public bool isHovering;
    public float hoverProgress;
}

// Compatibility component for working with VR systems
public class VRCompatibleVideoTrigger : MonoBehaviour
{
    [Header("VR Integration")]
    public bool enableVRMode = false;
    public Camera vrCamera;

    private VideoIntegrationHelper integrationHelper;

    private void Awake()
    {
        integrationHelper = GetComponent<VideoIntegrationHelper>();
        if (integrationHelper == null)
        {
            integrationHelper = gameObject.AddComponent<VideoIntegrationHelper>();
        }
    }

    private void Start()
    {
        // Configure for VR if needed
        if (enableVRMode && vrCamera == null)
        {
            vrCamera = Camera.main;
        }
    }

    // Methods that can be called by your VR reticle pointer
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
}

// Additional helper for backward compatibility if needed
public class EnhancedVideoPlayerBridge : MonoBehaviour
{
    [Header("Bridge Settings")]
    public bool autoSetupOnStart = true;

    private EnhancedVideoPlayer enhancedPlayer;
    private VideoIntegrationHelper integrationHelper;

    private void Start()
    {
        if (autoSetupOnStart)
        {
            SetupBridge();
        }
    }

    public void SetupBridge()
    {
        // Ensure we have both components
        enhancedPlayer = GetComponent<EnhancedVideoPlayer>();
        if (enhancedPlayer == null)
        {
            enhancedPlayer = gameObject.AddComponent<EnhancedVideoPlayer>();
        }

        integrationHelper = GetComponent<VideoIntegrationHelper>();
        if (integrationHelper == null)
        {
            integrationHelper = gameObject.AddComponent<VideoIntegrationHelper>();
        }

        // Sync settings
        if (enhancedPlayer != null && integrationHelper != null)
        {
            integrationHelper.hoverTimeRequired = enhancedPlayer.hoverTimeRequired;
        }

        Debug.Log($"EnhancedVideoPlayer bridge setup complete for: {gameObject.name}");
    }

    // Public method to manually configure video data
    public void ConfigureVideo(string videoUrl, string title, string description, string zoneName)
    {
        if (enhancedPlayer != null)
        {
            enhancedPlayer.VideoUrlLink = videoUrl;
            enhancedPlayer.title = title;
            enhancedPlayer.description = description;
            enhancedPlayer.zoneName = zoneName;
            enhancedPlayer.LastKnownZone = zoneName;
        }

        if (integrationHelper != null)
        {
            integrationHelper.SetZone(zoneName);
        }

        Debug.Log($"Configured video: {title} in zone: {zoneName}");
    }

    // Utility method to get the current video info
    public string GetVideoInfo()
    {
        if (enhancedPlayer != null)
        {
            return $"Title: {enhancedPlayer.title}, Zone: {enhancedPlayer.LastKnownZone}, URL: {enhancedPlayer.VideoUrlLink}";
        }
        return "No EnhancedVideoPlayer found";
    }
}