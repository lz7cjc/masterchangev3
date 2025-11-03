using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// FINAL VERSION: Loading screen manager that works when parent canvas is inactive
/// - Activates canvas when showing
/// - Properly handles inactive parent
/// - Smooth fading
/// - Works from any scene transition
/// </summary>
public class VRLoadingManager : MonoBehaviour
{
    public static VRLoadingManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject loadingPanel;  // This GameObject (LoadingPanel)
    [SerializeField] private Image progressBar;
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private CanvasGroup canvasGroup; // Optional for fade effects

    [Header("Loading Messages")]
    [SerializeField] private string initializingVRMessage = "Initializing VR...";
    [SerializeField] private string loadingAssetsMessage = "Loading assets...";
    [SerializeField] private string switchingToVRMessage = "Switching to VR Mode...";
    [SerializeField] private string switching360Message = "Switching to 360 Mode...";

    [Header("Timing")]
    [SerializeField] private float minimumDisplayTime = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.3f;

    private float loadingStartTime;
    private bool isLoading = false;
    private Coroutine hideCoroutine;
    private Canvas parentCanvas;

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

        // Get parent canvas
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            Debug.LogError("[VRLOAD] No parent Canvas found! Loading screen won't work.");
        }
        else
        {
            Debug.Log($"[VRLOAD] Found parent canvas: {parentCanvas.gameObject.name}");

            // Ensure canvas is set up correctly
            if (parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                Debug.LogWarning("[VRLOAD] Canvas is not Screen Space Overlay - may not cover everything!");
            }
        }

        // Try to get or add CanvasGroup for fade effects
        if (canvasGroup == null)
        {
            canvasGroup = loadingPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = loadingPanel.AddComponent<CanvasGroup>();
                Debug.Log("[VRLOAD] Added CanvasGroup for fade effects");
            }
        }

        // Ensure LoadingPanel starts inactive (parent canvas may be inactive too)
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
            Debug.Log("[VRLOAD] Loading panel initialized (inactive)");
        }
    }

    #region Public Show Methods

    /// <summary>
    /// Show loading screen with custom message and initial progress
    /// </summary>
    public void ShowLoading(string message, float initialProgress)
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

        // CRITICAL: Activate parent canvas if it's inactive
        if (parentCanvas != null && !parentCanvas.gameObject.activeSelf)
        {
            parentCanvas.gameObject.SetActive(true);
            Debug.Log("[VRLOAD] ✓ Activated parent canvas");
        }

        // Reset canvas group alpha
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        // Activate loading panel
        loadingPanel.SetActive(true);
        UpdateStatus(message);
        UpdateProgress(initialProgress);

        Debug.Log($"[VRLOAD] ✓✓✓ Loading screen VISIBLE ✓✓✓ - Message: '{message}'");
    }

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

    #region Update Methods

    /// <summary>
    /// Update loading status text
    /// </summary>
    public void UpdateStatus(string message)
    {
        if (loadingText != null)
        {
            loadingText.text = message;
        }
    }

    /// <summary>
    /// Update loading progress (0 to 1) with smooth animation
    /// </summary>
    public void UpdateProgress(float progress)
    {
        if (progressBar != null)
        {
            float targetProgress = Mathf.Clamp01(progress);

            // Smooth transition
            if (progressBar.fillAmount < targetProgress)
            {
                progressBar.fillAmount = Mathf.Lerp(progressBar.fillAmount, targetProgress, Time.deltaTime * 5f);
            }
            else
            {
                progressBar.fillAmount = targetProgress;
            }
        }
    }

    #endregion

    #region Hide Methods

    /// <summary>
    /// Hide the loading screen with smooth fade out
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
        Debug.Log("[VRLOAD] === HIDE SEQUENCE START ===");

        // Ensure minimum display time
        float elapsedTime = Time.time - loadingStartTime;
        float remainingTime = minimumDisplayTime - elapsedTime;

        if (remainingTime > 0)
        {
            Debug.Log($"[VRLOAD] Waiting {remainingTime:F2}s for minimum display time");
            yield return new WaitForSeconds(remainingTime);
        }

        // Ensure progress bar is at 100%
        if (progressBar != null)
        {
            progressBar.fillAmount = 1f;
        }

        // Brief pause at 100%
        yield return new WaitForSeconds(0.2f);

        // Fade out
        Debug.Log("[VRLOAD] Fading out...");
        if (canvasGroup != null)
        {
            float fadeStart = Time.time;
            while (Time.time - fadeStart < fadeOutDuration)
            {
                float alpha = 1f - ((Time.time - fadeStart) / fadeOutDuration);
                canvasGroup.alpha = alpha;
                yield return null;
            }
            canvasGroup.alpha = 0f;
        }
        else
        {
            // No canvas group, just wait
            yield return new WaitForSeconds(fadeOutDuration);
        }

        // Hide the panel
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
            Debug.Log("[VRLOAD] ✓ Loading panel hidden");
        }

        // Optionally deactivate parent canvas to save performance
        // (You can enable this if you want)
        // if (parentCanvas != null)
        // {
        //     parentCanvas.gameObject.SetActive(false);
        //     Debug.Log("[VRLOAD] ✓ Parent canvas deactivated");
        // }

        isLoading = false;
        hideCoroutine = null;

        Debug.Log("[VRLOAD] ✓✓✓ Loading screen HIDDEN ✓✓✓");
        Debug.Log("[VRLOAD] === HIDE SEQUENCE COMPLETE ===");
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

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
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