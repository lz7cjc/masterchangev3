using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main manager for Film HUD - coordinates all film control buttons and HUD visibility.
/// Singleton pattern for easy access from buttons.
/// 
/// ATTACH TO: HUDPivot GameObject (or create a FilmHUDManager GameObject)
/// 
/// FEATURES:
/// - Toggle HUD visibility
/// - Coordinate button states
/// - Connect to VRVideoController
/// - Auto-hide functionality (optional)
/// </summary>
public class FilmHUDManager : MonoBehaviour
{
    public static FilmHUDManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private VRVideoController videoController;
    [SerializeField] private GameObject hudRootObject; // The main HUD canvas/panel
    [SerializeField] private FilmHUDFollower hudFollower;

    [Header("HUD Panels")]
    [SerializeField] private GameObject level1Panel; // Open/Close button
    [SerializeField] private GameObject controlBar; // Main controls
    [SerializeField] private GameObject level2Panel; // Additional controls

    [Header("Settings")]
    [SerializeField] private bool startHUDClosed = true;
    [SerializeField] private bool enableAutoHide = false;
    [SerializeField] private float autoHideDelay = 5f;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    // State
    private bool isHUDOpen = false;
    private float timeSinceLastInteraction = 0f;

    // Public property for buttons to access
    public bool ShowDebugInfo => showDebugInfo;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("[FilmHUDManager] Multiple instances detected! Destroying duplicate.");
            Destroy(this);
            return;
        }

        // Auto-find video controller if not assigned
        if (videoController == null)
        {
            videoController = FindObjectOfType<VRVideoController>();
        }

        // Auto-find follower if not assigned
        if (hudFollower == null)
        {
            hudFollower = GetComponent<FilmHUDFollower>();
        }

        if (showDebugInfo)
        {
            Debug.Log("[FilmHUDManager] Initialized");
        }
    }

    private void Start()
    {
        // Set initial HUD state
        if (startHUDClosed)
        {
            CloseHUD();
        }
        else
        {
            OpenHUD();
        }
    }

    private void Update()
    {
        // Handle auto-hide
        if (enableAutoHide && isHUDOpen)
        {
            timeSinceLastInteraction += Time.deltaTime;

            if (timeSinceLastInteraction >= autoHideDelay)
            {
                CloseHUD();
            }
        }
    }

    #region HUD Control Methods

    /// <summary>
    /// Toggle HUD open/closed
    /// </summary>
    public void ToggleHUD()
    {
        if (isHUDOpen)
        {
            CloseHUD();
        }
        else
        {
            OpenHUD();
        }

        if (showDebugInfo)
        {
            Debug.Log($"[FilmHUDManager] HUD toggled: {(isHUDOpen ? "OPEN" : "CLOSED")}");
        }
    }

    /// <summary>
    /// Open the HUD
    /// </summary>
    public void OpenHUD()
    {
        isHUDOpen = true;
        timeSinceLastInteraction = 0f;

        // Show control panels
        if (controlBar != null)
            controlBar.SetActive(true);

        if (level2Panel != null)
            level2Panel.SetActive(true);

        // Snap HUD to camera position when opening
        if (hudFollower != null)
        {
            hudFollower.SnapToCamera();
        }

        if (showDebugInfo)
        {
            Debug.Log("<color=green>[FilmHUDManager] ✓ HUD Opened</color>");
        }
    }

    /// <summary>
    /// Close the HUD
    /// </summary>
    public void CloseHUD()
    {
        isHUDOpen = false;

        // Hide control panels (keep Level1 open/close button visible)
        if (controlBar != null)
            controlBar.SetActive(false);

        if (level2Panel != null)
            level2Panel.SetActive(false);

        if (showDebugInfo)
        {
            Debug.Log("[FilmHUDManager] HUD Closed");
        }
    }

    /// <summary>
    /// Called when user interacts with any button
    /// </summary>
    public void OnButtonInteraction()
    {
        timeSinceLastInteraction = 0f;
    }

    #endregion

    #region Video Control Methods (called by FilmHUDButtons)

    public void PlayVideo()
    {
        if (videoController != null)
        {
            videoController.Play();
            OnButtonInteraction();
        }
        else
        {
            Debug.LogWarning("[FilmHUDManager] Video controller not assigned!");
        }
    }

    public void PauseVideo()
    {
        if (videoController != null)
        {
            videoController.Pause();
            OnButtonInteraction();
        }
    }

    public void StopVideo()
    {
        if (videoController != null)
        {
            videoController.StopVideo();
            OnButtonInteraction();
        }
    }

    public void RewindVideo()
    {
        if (videoController != null)
        {
            videoController.Rewind();
            OnButtonInteraction();
        }
    }

    public void FastForwardVideo()
    {
        if (videoController != null)
        {
            videoController.FastForward();
            OnButtonInteraction();
        }
    }

    public void VeryFastForwardVideo()
    {
        if (videoController != null)
        {
            videoController.VeryFastForward();
            OnButtonInteraction();
        }
    }

    public void NormalSpeedVideo()
    {
        if (videoController != null)
        {
            videoController.NormalSpeed();
            OnButtonInteraction();
        }
    }

    public void RestartVideo()
    {
        if (videoController != null)
        {
            videoController.RestartVideo();
            OnButtonInteraction();
        }
    }

    public void SkipForward()
    {
        if (videoController != null)
        {
            videoController.SkipForward(10f);
            OnButtonInteraction();
        }
    }

    public void SkipBackward()
    {
        if (videoController != null)
        {
            videoController.SkipBackward(10f);
            OnButtonInteraction();
        }
    }

    #endregion

    #region Public Properties & Getters

    public bool IsHUDOpen => isHUDOpen;

    public VRVideoController VideoController => videoController;

    #endregion

    #region Inspector Helpers

    [ContextMenu("Open HUD")]
    private void ContextOpenHUD()
    {
        OpenHUD();
    }

    [ContextMenu("Close HUD")]
    private void ContextCloseHUD()
    {
        CloseHUD();
    }

    [ContextMenu("Toggle HUD")]
    private void ContextToggleHUD()
    {
        ToggleHUD();
    }

    [ContextMenu("Show Status")]
    private void ShowStatus()
    {
        Debug.Log($"[FilmHUDManager] Status:\n" +
                  $"  HUD Open: {isHUDOpen}\n" +
                  $"  Video Controller: {(videoController != null ? "✓" : "✗")}\n" +
                  $"  HUD Follower: {(hudFollower != null ? "✓" : "✗")}\n" +
                  $"  Auto-Hide: {(enableAutoHide ? $"Yes ({autoHideDelay}s)" : "No")}\n" +
                  $"  Time Since Interaction: {timeSinceLastInteraction:F1}s");
    }

    #endregion
}