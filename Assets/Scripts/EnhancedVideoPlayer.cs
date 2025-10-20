using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

/// <summary>
/// Simplified EnhancedVideoPlayer - Just handles video launching
/// Zone management and positioning handled by FilmZoneManager
/// Compatibility shim added for older API names (zoneName, nextscene, returntoscene, MouseHoverChangeScene, MouseExit, SetVideoUrl, category)
/// </summary>
[RequireComponent(typeof(Collider))]
public class EnhancedVideoPlayer : MonoBehaviour
{
    [Header("Video Data")]
    public string VideoUrlLink;
    public string title;
    public string description;  
    public string prefabType; // For tracking which prefab variant this uses

    // Compatibility fields (older code expects these names)
    public string zoneName;
    public string nextscene;
    public string returntoscene;
    public string category;

    [Header("Zone")]
    public string LastKnownZone = "Home";

    [Header("Interaction")]
    public float hoverTimeRequired = 3.0f;
    private bool isHovering = false;
    private float hoverTimer = 0f;

    [Header("Scene Transitions")]
    public string videoSceneName = "360VideoApp";
    public string returnSceneName = "mainVR";

    [Header("Visual Feedback (Optional)")]
    public GameObject hoverIndicator;
    public TMP_Text titleText;
    public TMP_Text descriptionText;

    void Start()
    {
        // Ensure collider is trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // Update text if references exist
        UpdateDisplayText();
    }

    void Update()
    {
        // Handle hover timer
        if (isHovering)
        {
            hoverTimer += Time.deltaTime;

            // Show progress if indicator exists
            if (hoverIndicator != null)
            {
                float progress = Mathf.Clamp01(hoverTimer / hoverTimeRequired);
                hoverIndicator.transform.localScale = Vector3.one * progress;
            }

            // Launch video when timer completes
            if (hoverTimer >= hoverTimeRequired)
            {
                LaunchVideo();
                ResetHover();
            }
        }
    }

    /// <summary>
    /// Called when pointer/cursor enters the collider
    /// </summary>
    void OnMouseEnter()
    {
        StartHover();
    }

    /// <summary>
    /// Called when pointer/cursor exits the collider
    /// </summary>
    void OnMouseExit()
    {
        ResetHover();
    }

    /// <summary>
    /// Called when clicked (immediate launch)
    /// </summary>
    void OnMouseDown()
    {
        LaunchVideo();
    }

    /// <summary>
    /// Start hover interaction
    /// </summary>
    public void StartHover()
    {
        isHovering = true;
        hoverTimer = 0f;

        if (hoverIndicator != null)
        {
            hoverIndicator.SetActive(true);
        }

        Debug.Log($"Started hovering on: {title}");
    }

    /// <summary>
    /// Reset hover state
    /// </summary>
    public void ResetHover()
    {
        isHovering = false;
        hoverTimer = 0f;

        if (hoverIndicator != null)
        {
            hoverIndicator.SetActive(false);
            hoverIndicator.transform.localScale = Vector3.zero;
        }
    }

    /// <summary>
    /// Launch the video
    /// </summary>
    public void LaunchVideo()
    {
        if (string.IsNullOrEmpty(VideoUrlLink))
        {
            Debug.LogError($"Cannot launch video - URL is empty for {gameObject.name}");
            return;
        }

        // Ensure compatibility: prefer explicit compatibility fields if set
        if (!string.IsNullOrEmpty(nextscene))
            videoSceneName = nextscene;
        if (!string.IsNullOrEmpty(returntoscene))
            returnSceneName = returntoscene;

        // Save video data to PlayerPrefs for the video player scene
        PlayerPrefs.SetString("VideoUrl", VideoUrlLink);
        PlayerPrefs.SetString("videoTitle", title);
        PlayerPrefs.SetString("videoDescription", description);
        PlayerPrefs.SetString("lastknownzone", LastKnownZone);
        PlayerPrefs.SetString("returntoscene", returnSceneName);
        PlayerPrefs.Save();

        Debug.Log($"Launching video: {title} ({VideoUrlLink})");

        // Load video scene
        SceneManager.LoadScene(videoSceneName);
    }

    /// <summary>
    /// Update display text components
    /// </summary>
    public void UpdateDisplayText()
    {
        if (titleText != null)
        {
            titleText.text = title;
        }

        if (descriptionText != null)
        {
            descriptionText.text = description;
        }
    }

    /// <summary>
    /// Set all video data at once
    /// </summary>
    public void SetVideoData(string url, string videoTitle, string videoDescription, string zone)
    {
        VideoUrlLink = url;
        title = videoTitle;
        description = videoDescription;
        LastKnownZone = zone;

        UpdateDisplayText();
    }

    // ===== VR COMPATIBILITY METHODS =====

    /// <summary>
    /// For VR pointer systems that need to call methods directly
    /// </summary>
    public void OnPointerEnter()
    {
        StartHover();
    }

    public void OnPointerExit()
    {
        ResetHover();
    }

    public void OnPointerClick()
    {
        LaunchVideo();
    }

    // ===== Backwards-compatible API expected by other code in the project =====

    // Old name used in many places -> forward to StartHover
    public void MouseHoverChangeScene()
    {
        StartHover();
    }

    // Old name used in many places -> forward to ResetHover
    public void MouseExit()
    {
        ResetHover();
    }

    // Old name used in many places -> forward to LaunchVideo
    public void SetVideoUrl()
    {
        LaunchVideo();
    }
}