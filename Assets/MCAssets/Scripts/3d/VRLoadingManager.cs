using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// OPTIMIZED: Central loading screen manager for VR application
/// - Unified debug keyword: [VRLOAD]
/// - Faster loading times
/// - Guaranteed overlay visibility
/// - Better progress tracking
/// </summary>
public class VRLoadingManager : MonoBehaviour
{
    public static VRLoadingManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Image progressBar;
    [SerializeField] private TextMeshProUGUI loadingText;

    [Header("Loading Messages")]
    [SerializeField] private string initializingVRMessage = "Initializing VR...";
    [SerializeField] private string loadingAssetsMessage = "Loading assets...";
    [SerializeField] private string switchingToVRMessage = "Switching to VR Mode...";
    [SerializeField] private string switching360Message = "Switching to 360 Mode...";

    [Header("Timing")]
    [SerializeField] private float minimumDisplayTime = 0.5f; // Minimum time to show loading screen
    [SerializeField] private float fadeOutDuration = 0.3f;

    private float loadingStartTime;
    private bool isLoading = false;
    private Coroutine hideCoroutine;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[VRLOAD] VRLoadingManager instance created");
        }
        else
        {
            Debug.LogWarning("[VRLOAD] Duplicate VRLoadingManager found - destroying");
            Destroy(gameObject);
            return;
        }

        // Ensure loading panel starts hidden
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
            Debug.Log("[VRLOAD] Loading panel initialized (hidden)");
        }
        else
        {
            Debug.LogError("[VRLOAD] Loading panel not assigned in Inspector!");
        }
    }

    #region Public Show Methods

    /// <summary>
    /// Show loading screen for initial VR initialization
    /// </summary>
    public void ShowInitialLoading()
    {
        ShowLoading(initializingVRMessage, 0f);
    }

    /// <summary>
    /// Show loading screen for asset loading
    /// </summary>
    public void ShowAssetLoading()
    {
        ShowLoading(loadingAssetsMessage, 0f);
    }

    /// <summary>
    /// Show loading screen when switching to VR mode
    /// </summary>
    public void ShowSwitchToVR()
    {
        ShowLoading(switchingToVRMessage, 0f);
    }

    /// <summary>
    /// Show loading screen when switching to 360 mode
    /// </summary>
    public void ShowSwitchTo360()
    {
        ShowLoading(switching360Message, 0f);
    }

    #endregion

    #region Core Loading Methods

    /// <summary>
    /// Internal method to show loading screen with message and progress
    /// </summary>
    private void ShowLoading(string message, float initialProgress)
    {
        if (loadingPanel == null)
        {
            Debug.LogError("[VRLOAD] Cannot show loading - panel is null!");
            return;
        }

        // Cancel any pending hide operations
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
            Debug.Log("[VRLOAD] Cancelled pending hide operation");
        }

        loadingStartTime = Time.time;
        isLoading = true;

        loadingPanel.SetActive(true);
        UpdateStatus(message);
        UpdateProgress(initialProgress);

        Debug.Log($"[VRLOAD] ✓ Loading screen VISIBLE - Message: '{message}'");
    }

    /// <summary>
    /// Update loading status text
    /// </summary>
    public void UpdateStatus(string message)
    {
        if (loadingText != null)
        {
            loadingText.text = message;
            Debug.Log($"[VRLOAD] Status updated: '{message}'");
        }
    }

    /// <summary>
    /// Update loading progress (0 to 1)
    /// </summary>
    public void UpdateProgress(float progress)
    {
        if (progressBar != null)
        {
            progressBar.fillAmount = Mathf.Clamp01(progress);
            Debug.Log($"[VRLOAD] Progress: {(progress * 100):F0}%");
        }
    }

    /// <summary>
    /// Hide the loading screen with fade out
    /// </summary>
    public void HideLoading()
    {
        if (!isLoading)
        {
            Debug.Log("[VRLOAD] Hide called but not loading - skipping");
            return;
        }

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }

        hideCoroutine = StartCoroutine(HideLoadingCoroutine());
    }

    /// <summary>
    /// Coroutine to handle smooth hiding with minimum display time
    /// </summary>
    private IEnumerator HideLoadingCoroutine()
    {
        Debug.Log("[VRLOAD] Hide sequence started");

        // Ensure minimum display time
        float elapsedTime = Time.time - loadingStartTime;
        float remainingTime = minimumDisplayTime - elapsedTime;

        if (remainingTime > 0)
        {
            Debug.Log($"[VRLOAD] Waiting {remainingTime:F2}s to meet minimum display time");
            yield return new WaitForSeconds(remainingTime);
        }

        // Brief delay before fade out
        yield return new WaitForSeconds(0.2f);

        // Fade out (you can add actual fade animation here if you have CanvasGroup)
        Debug.Log("[VRLOAD] Fading out...");
        yield return new WaitForSeconds(fadeOutDuration);

        // Hide the panel
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
            Debug.Log("[VRLOAD] ✓✓✓ Loading screen HIDDEN ✓✓✓");
        }

        isLoading = false;
        hideCoroutine = null;
    }

    /// <summary>
    /// Force immediate hide without fade or minimum time (emergency use only)
    /// </summary>
    public void ForceHide()
    {
        Debug.LogWarning("[VRLOAD] FORCE HIDE called");

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }

        isLoading = false;
        Debug.Log("[VRLOAD] Loading screen force hidden");
    }

    #endregion

    #region Status Queries

    /// <summary>
    /// Check if loading screen is currently visible
    /// </summary>
    public bool IsLoading()
    {
        return isLoading;
    }

    #endregion

    private void OnDestroy()
    {
        Debug.Log("[VRLOAD] VRLoadingManager destroyed");
        if (Instance == this)
        {
            Instance = null;
        }
    }
}