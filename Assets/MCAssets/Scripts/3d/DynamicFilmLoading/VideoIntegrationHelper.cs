using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.EventSystems;

/*
 Main Purpose: Bridges the zone system with Unity's Event System for VR and mouse interactions.
Key Features:

Event Handling: Manages hover, click, and VR pointer interactions
Progress Indicators: Shows visual feedback during hover delays
PlayerPrefs Integration: Stores video information for scene transitions
VR Compatibility: Works with VR reticle pointers and gaze-based selection
Customizable Events: Allows other scripts to respond to video interactions

What it does: Handles all the user interaction logic - what happens when someone looks at, hovers over, or clicks on a video in VR or with a mouse.*/

// Helper component that bridges the zone system with your existing Event Trigger approach
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

    private VideoZonePrefab zonePrefab;
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
        // Get required components
        zonePrefab = GetComponent<VideoZonePrefab>();
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
        if (zonePrefab != null)
        {
            currentZone = zonePrefab.zoneName;

            // Sync hover time with zone prefab
            if (zonePrefab.hoverTimeRequired != hoverTimeRequired)
            {
                zonePrefab.hoverTimeRequired = hoverTimeRequired;
            }

            UpdateUIElements();
            CountVideosInZone();
        }
    }

    private void Update()
    {
        // Handle hover timing (matching DynamicLoadVideo1 pattern)
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

    // Event Trigger Callbacks (matching your existing pattern)
    public void OnPointerEnterEvent()
    {
        isHovering = true;
        hoverTimer = 0f;

        if (progressIndicator != null)
        {
            progressIndicator.SetActive(true);
        }

        videoEvents.OnVideoHoverStart?.Invoke();

        Debug.Log($"Pointer enter on video: {zonePrefab?.videoTitle ?? gameObject.name}");
    }

    public void OnPointerExitEvent()
    {
        isHovering = false;
        hoverTimer = 0f;

        if (progressIndicator != null)
        {
            progressIndicator.SetActive(false);
        }

        videoEvents.OnVideoHoverEnd?.Invoke();

        Debug.Log($"Pointer exit on video: {zonePrefab?.videoTitle ?? gameObject.name}");
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

        // Update PlayerPrefs with zone information (matching DynamicLoadVideo1)
        if (updatePlayerPrefsOnPlay && !string.IsNullOrEmpty(currentZone))
        {
            PlayerPrefs.SetString("lastknownzone", currentZone);

            if (zonePrefab != null)
            {
                PlayerPrefs.SetString("nextscene", zonePrefab.nextScene);
                PlayerPrefs.SetString("returntoscene", zonePrefab.returnScene);
                PlayerPrefs.SetInt("stage", zonePrefab.stage);
                PlayerPrefs.SetString("behaviour", zonePrefab.behaviour);
                PlayerPrefs.SetString("VideoUrl", zonePrefab.videoUrl);

                if (!string.IsNullOrEmpty(zonePrefab.videoTitle))
                {
                    PlayerPrefs.SetString("videoTitle", zonePrefab.videoTitle);
                }

                if (!string.IsNullOrEmpty(zonePrefab.videoDescription))
                {
                    PlayerPrefs.SetString("videoDescription", zonePrefab.videoDescription);
                }
            }

            PlayerPrefs.Save();
        }

        // Trigger events
        videoEvents.OnVideoPlay?.Invoke();

        Debug.Log($"Video play triggered in zone: {currentZone}");

        // Load the scene (matching your existing pattern)
        UnityEngine.SceneManagement.SceneManager.LoadScene("360VideoApp", UnityEngine.SceneManagement.LoadSceneMode.Single);
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

        VideoZonePrefab[] allVideos = FindObjectsOfType<VideoZonePrefab>();
        videosInCurrentZone = 0;

        foreach (var video in allVideos)
        {
            if (video.zoneName.Equals(currentZone, System.StringComparison.OrdinalIgnoreCase))
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

            if (zonePrefab != null)
            {
                zonePrefab.zoneName = newZone;
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

// Compatibility component for working with your VR system
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