using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Toggles between Pause and Play states on a single button.
/// When paused, shows Play icon and triggers Play action.
/// When playing, shows Pause icon and triggers Pause action.
/// Attach to the Pause/Play button GameObject.
/// </summary>
public class VRPausePlayToggle : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image buttonImage;
    [SerializeField] private VRHUDButtonUI hudButtonUI;

    [Header("Pause State (Video Playing)")]
    [SerializeField] private Sprite pauseSprite;
    [SerializeField] private Sprite pauseHoverSprite;
    [SerializeField] private string pauseActionName = "Pause";

    [Header("Play State (Video Paused)")]
    [SerializeField] private Sprite playSprite;
    [SerializeField] private Sprite playHoverSprite;
    [SerializeField] private string playActionName = "Play";

    [Header("Video Controller Reference")]
    [SerializeField] private VRVideoController videoController;
    [SerializeField] private bool autoFindVideoController = true;

    [Header("Current State")]
    [SerializeField] private bool isVideoPlaying = true; // Start as playing (so shows Pause icon)

    private void Start()
    {
        // Auto-find components if not assigned
        if (buttonImage == null)
            buttonImage = GetComponent<Image>();

        if (hudButtonUI == null)
            hudButtonUI = GetComponent<VRHUDButtonUI>();

        if (autoFindVideoController && videoController == null)
            videoController = FindObjectOfType<VRVideoController>();

        // Set initial state
        UpdateButtonState();

        // Subscribe to button trigger event
        if (hudButtonUI != null)
        {
            hudButtonUI.OnButtonTriggered.AddListener(OnButtonTriggered);
        }

        Debug.Log($"[VRPausePlayToggle] Initialized - Video playing: {isVideoPlaying}");
    }

    private void OnButtonTriggered()
    {
        if (isVideoPlaying)
        {
            // Video is playing, so pause it
            PauseVideo();
        }
        else
        {
            // Video is paused, so play it
            PlayVideo();
        }

        // Toggle state
        isVideoPlaying = !isVideoPlaying;

        // Update button appearance
        UpdateButtonState();
    }

    private void UpdateButtonState()
    {
        if (buttonImage == null) return;

        if (isVideoPlaying)
        {
            // Video is playing → Show PAUSE button
            if (pauseSprite != null)
                buttonImage.sprite = pauseSprite;

            Debug.Log("[VRPausePlayToggle] State: Playing → Showing PAUSE button");
        }
        else
        {
            // Video is paused → Show PLAY button
            if (playSprite != null)
                buttonImage.sprite = playSprite;

            Debug.Log("[VRPausePlayToggle] State: Paused → Showing PLAY button");
        }
    }

    private void PauseVideo()
    {
        if (videoController != null)
        {
            videoController.Pause();
            Debug.Log("[VRPausePlayToggle] ⏸ Video paused");
        }
        else
        {
            Debug.LogWarning("[VRPausePlayToggle] Video controller not found!");
        }
    }

    private void PlayVideo()
    {
        if (videoController != null)
        {
            videoController.Play();
            Debug.Log("[VRPausePlayToggle] ▶ Video playing");
        }
        else
        {
            Debug.LogWarning("[VRPausePlayToggle] Video controller not found!");
        }
    }

    /// <summary>
    /// Manually set the state (useful for external control)
    /// </summary>
    public void SetPlayingState(bool playing)
    {
        isVideoPlaying = playing;
        UpdateButtonState();
        Debug.Log($"[VRPausePlayToggle] State manually set to: {(playing ? "Playing" : "Paused")}");
    }

    /// <summary>
    /// Force button to show Pause icon
    /// </summary>
    [ContextMenu("Show Pause Icon")]
    public void ShowPauseIcon()
    {
        isVideoPlaying = true;
        UpdateButtonState();
    }

    /// <summary>
    /// Force button to show Play icon
    /// </summary>
    [ContextMenu("Show Play Icon")]
    public void ShowPlayIcon()
    {
        isVideoPlaying = false;
        UpdateButtonState();
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (hudButtonUI != null)
        {
            hudButtonUI.OnButtonTriggered.RemoveListener(OnButtonTriggered);
        }
    }
}