using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if ADDRESSABLES_ENABLED
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

/// <summary>
/// VR Loading Manager - Lives in "everything" scene
/// Manages loading UI and progress tracking
/// Does NOT use DontDestroyOnLoad (stays in everything scene)
/// </summary>
public class VRLoadingManager : MonoBehaviour
{
    public static VRLoadingManager Instance;

    [Header("UI References")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI percentageText;
    [SerializeField] private Image progressBar;

    [Header("Addressables Info (Optional)")]
    [SerializeField] private TextMeshProUGUI assetCountText;
    [SerializeField] private TextMeshProUGUI assetSizeText;
    [SerializeField] private TextMeshProUGUI timeRemainingText;
    [SerializeField] private bool showAddressablesInfo = false;

    private float currentProgress = 0f;
    private float targetProgress = 0f;
    private float progressSmoothSpeed = 2f;

#if ADDRESSABLES_ENABLED
    private AsyncOperationHandle currentAddressableOp;
    private bool trackingAddressables = false;
#endif

    void Awake()
    {
        Debug.Log("[VRLoadingManager] ==========================================");
        Debug.Log("[VRLoadingManager] Awake - everything scene");
        Debug.Log($"[VRLoadingManager] Time: {Time.realtimeSinceStartup:F3}s");
        Debug.Log("[VRLoadingManager] ==========================================");

        // Simple singleton (no DontDestroyOnLoad - we stay in this scene)
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[VRLoadingManager] ✓ Instance created");
        }
        else
        {
            Debug.LogWarning($"[VRLoadingManager] Duplicate found - destroying {gameObject.name}");
            Destroy(gameObject);
            return;
        }

        // Ensure loading panel is visible
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
            Debug.Log("[VRLoadingManager] Loading panel activated");
        }
        else
        {
            Debug.LogError("[VRLoadingManager] Loading panel not assigned!");
        }
    }

    void Update()
    {
        // Smooth progress bar animation
        if (Mathf.Abs(currentProgress - targetProgress) > 0.01f)
        {
            currentProgress = Mathf.Lerp(currentProgress, targetProgress, Time.deltaTime * progressSmoothSpeed);
            UpdateUI();
        }

#if ADDRESSABLES_ENABLED
        if (trackingAddressables && currentAddressableOp.IsValid())
        {
            UpdateAddressablesProgress();
        }
#endif
    }

    /// <summary>
    /// Show loading screen with message
    /// </summary>
    public void ShowLoading(string message, float initialProgress = 0f)
    {
        Debug.Log($"[VRLoadingManager] ShowLoading: {message} ({initialProgress * 100:F0}%)");

        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }

        currentProgress = initialProgress;
        targetProgress = initialProgress;

        if (statusText != null)
        {
            statusText.text = message;
        }

        UpdateUI();
    }

    /// <summary>
    /// Hide loading screen
    /// </summary>
    public void HideLoading()
    {
        Debug.Log("[VRLoadingManager] HideLoading called");

        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }

        currentProgress = 0f;
        targetProgress = 0f;

#if ADDRESSABLES_ENABLED
        trackingAddressables = false;
#endif
    }

    /// <summary>
    /// Update progress value (0-1)
    /// </summary>
    public void UpdateProgress(float progress)
    {
        targetProgress = Mathf.Clamp01(progress);
    }

    /// <summary>
    /// Update status message
    /// </summary>
    public void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    /// <summary>
    /// Update UI elements
    /// </summary>
    private void UpdateUI()
    {
        if (percentageText != null)
        {
            percentageText.text = $"{Mathf.RoundToInt(currentProgress * 100)}%";
        }

        if (progressBar != null)
        {
            progressBar.fillAmount = currentProgress;
        }
    }

    /// <summary>
    /// Quick helpers for mode switching
    /// </summary>
    public void ShowSwitchToVR()
    {
        ShowLoading("Switching to VR mode...", 0f);
    }

    public void ShowSwitchTo360()
    {
        ShowLoading("Switching to 360 mode...", 0f);
    }

#if ADDRESSABLES_ENABLED
    // ==========================================
    // ADDRESSABLES SUPPORT (Enable with scripting define)
    // ==========================================

    /// <summary>
    /// Track addressables loading operation
    /// </summary>
    public void TrackAddressablesLoad(AsyncOperationHandle operation, string assetName = "Assets")
    {
        currentAddressableOp = operation;
        trackingAddressables = true;
        ShowLoading($"Loading {assetName}...", 0f);
        Debug.Log($"[VRLoadingManager] Tracking addressables: {assetName}");
    }

    /// <summary>
    /// Update addressables progress with detailed info
    /// </summary>
    void UpdateAddressablesProgress()
    {
        if (!currentAddressableOp.IsValid())
        {
            trackingAddressables = false;
            return;
        }

        float progress = currentAddressableOp.PercentComplete;
        UpdateProgress(progress);

        if (showAddressablesInfo)
        {
            // Get download status
            var downloadStatus = currentAddressableOp.GetDownloadStatus();
            
            if (assetCountText != null)
            {
                assetCountText.text = $"Loading assets...";
            }

            if (assetSizeText != null)
            {
                float downloadedMB = downloadStatus.DownloadedBytes / (1024f * 1024f);
                float totalMB = downloadStatus.TotalBytes / (1024f * 1024f);
                
                if (totalMB > 0)
                {
                    assetSizeText.text = $"{downloadedMB:F1} / {totalMB:F1} MB";
                }
            }

            if (timeRemainingText != null && progress > 0.01f)
            {
                // Simple estimate based on current progress
                float remainingProgress = 1f - progress;
                float estimatedRemaining = (Time.time / progress) * remainingProgress;
                timeRemainingText.text = $"~{Mathf.CeilToInt(estimatedRemaining)}s";
            }
        }

        if (currentAddressableOp.IsDone)
        {
            Debug.Log("[VRLoadingManager] Addressables load complete");
            trackingAddressables = false;
        }
    }

    /// <summary>
    /// Stop tracking addressables
    /// </summary>
    public void StopTrackingAddressables()
    {
        trackingAddressables = false;
        Debug.Log("[VRLoadingManager] Stopped tracking addressables");
    }
#endif

    /// <summary>
    /// Force show loading with detailed progress info
    /// (For manual asset counting without addressables)
    /// </summary>
    public void ShowLoadingWithDetails(string message, int loadedCount, int totalCount, float sizeMB, float totalSizeMB)
    {
        ShowLoading(message, (float)loadedCount / totalCount);

        if (assetCountText != null)
        {
            assetCountText.text = $"{loadedCount} / {totalCount} assets";
        }

        if (assetSizeText != null)
        {
            assetSizeText.text = $"{sizeMB:F1} / {totalSizeMB:F1} MB";
        }
    }
}
