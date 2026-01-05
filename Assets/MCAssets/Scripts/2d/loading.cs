using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

#if ADDRESSABLES_ENABLED
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

/// <summary>
/// EVERYTHING SCENE - Loading orchestrator
/// Handles async loading from everything → mainVR with progress tracking
/// Works with VRLoadingManager on same GameObject
/// </summary>
public class loading : MonoBehaviour
{
    [Header("UI References (Auto-find if empty)")]
    [SerializeField] private Image progressBar;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI percentageText;

    [Header("Settings")]
    [SerializeField] private float minimumLoadTime = 1.5f;
    [SerializeField] private string targetScene = "mainVR";

    [Header("Addressables (Optional)")]
    [SerializeField] private bool useAddressables = false;
    [SerializeField] private TextMeshProUGUI assetCountText;
    [SerializeField] private TextMeshProUGUI assetSizeText;

    private float loadStartTime;
    private VRLoadingManager vrLoadingManager;

    void Awake()
    {
        Debug.Log("[LOADING] ==========================================");
        Debug.Log("[LOADING] EVERYTHING SCENE - Awake");
        Debug.Log($"[LOADING] Time: {Time.realtimeSinceStartup:F3}s");
        Debug.Log("[LOADING] ==========================================");

        // Auto-find VRLoadingManager on same GameObject
        vrLoadingManager = GetComponent<VRLoadingManager>();
        if (vrLoadingManager == null)
        {
            Debug.LogWarning("[LOADING] VRLoadingManager not found on same GameObject!");
        }

        // Auto-find UI components if not assigned
        if (progressBar == null)
            progressBar = GetComponentInChildren<Image>();
        
        if (statusText == null || percentageText == null)
        {
            TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var text in texts)
            {
                if (text.name.Contains("Progress") || text.name.Contains("Status"))
                    statusText = text;
                else if (text.name.Contains("Percent"))
                    percentageText = text;
            }
        }
    }

    void Start()
    {
        loadStartTime = Time.realtimeSinceStartup;

        Debug.Log("[LOADING] ==========================================");
        Debug.Log("[LOADING] EVERYTHING SCENE - Loading Start");
        Debug.Log($"[LOADING] Target scene: {targetScene}");
        Debug.Log($"[LOADING] Addressables: {(useAddressables ? "ENABLED" : "DISABLED")}");
        Debug.Log("[LOADING] ==========================================");

        // Initialize UI
        UpdateProgress(0f, "Initializing...");

        // Start loading sequence
        StartCoroutine(LoadMainVRSequence());
    }

    /// <summary>
    /// Main loading sequence - handles addressables (optional) then scene load
    /// </summary>
    IEnumerator LoadMainVRSequence()
    {
        Debug.Log("[LOADING] === LOAD SEQUENCE START ===");

        // PHASE 1: Initial setup (0-5%)
        UpdateProgress(0.05f, "Preparing...");
        yield return new WaitForSeconds(0.2f);

#if ADDRESSABLES_ENABLED
        // PHASE 2: Load addressables if enabled (5-40%)
        if (useAddressables)
        {
            yield return StartCoroutine(LoadAddressablesAssets());
        }
        else
        {
            UpdateProgress(0.1f, "Ready to load scene...");
            yield return new WaitForSeconds(0.1f);
        }
#else
        if (useAddressables)
        {
            Debug.LogWarning("[LOADING] Addressables enabled but ADDRESSABLES_ENABLED not defined!");
            Debug.LogWarning("[LOADING] Add 'ADDRESSABLES_ENABLED' to Scripting Define Symbols");
        }
        UpdateProgress(0.1f, "Ready to load scene...");
        yield return new WaitForSeconds(0.1f);
#endif

        // PHASE 3: Load mainVR scene (40-100%)
        yield return StartCoroutine(LoadMainVRScene());

        Debug.Log("[LOADING] === LOAD SEQUENCE COMPLETE ===");
    }

#if ADDRESSABLES_ENABLED
    /// <summary>
    /// Load addressable assets with progress tracking
    /// </summary>
    IEnumerator LoadAddressablesAssets()
    {
        Debug.Log("[LOADING] === ADDRESSABLES LOADING START ===");
        UpdateProgress(0.1f, "Loading assets...");

        // Example: Load assets by label
        // Replace "MainSceneAssets" with your actual label
        AsyncOperationHandle<IList<GameObject>> handle = 
            Addressables.LoadAssetsAsync<GameObject>("MainSceneAssets", 
                (obj) => {
                    // Called for each loaded asset
                    Debug.Log($"[LOADING] Loaded asset: {obj.name}");
                });

        // Track progress
        while (!handle.IsDone)
        {
            float progress = handle.PercentComplete;
            float displayProgress = 0.1f + (progress * 0.3f); // Map to 10-40%
            
            UpdateProgress(displayProgress, "Loading assets...");

            // Update asset info if UI elements available
            if (assetCountText != null || assetSizeText != null)
            {
                var downloadStatus = handle.GetDownloadStatus();
                
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
            }

            yield return null;
        }

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            Debug.Log($"[LOADING] ✓ Addressables loaded: {handle.Result.Count} assets");
            UpdateProgress(0.4f, "Assets ready!");
        }
        else
        {
            Debug.LogError("[LOADING] Addressables load failed!");
            UpdateProgress(0.4f, "Asset load failed - continuing...");
        }

        yield return new WaitForSeconds(0.3f);
        Debug.Log("[LOADING] === ADDRESSABLES LOADING COMPLETE ===");
    }
#endif

    /// <summary>
    /// Load mainVR scene asynchronously with progress tracking
    /// </summary>
    IEnumerator LoadMainVRScene()
    {
        Debug.Log("[LOADING] === SCENE LOAD START ===");

        // Phase 1: Start async load (40-50%)
        UpdateProgress(0.4f, "Loading VR environment...");
        yield return new WaitForSeconds(0.2f);

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetScene);
        if (asyncLoad == null)
        {
            Debug.LogError($"[LOADING] Failed to start loading scene: {targetScene}");
            UpdateProgress(1f, "Error loading scene!");
            yield break;
        }

        // Prevent scene from activating until we're ready
        asyncLoad.allowSceneActivation = false;
        Debug.Log("[LOADING] Async load started");

        // Phase 2: Track loading progress (50-90%)
        while (asyncLoad.progress < 0.9f)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            float displayProgress = 0.5f + (progress * 0.4f); // Map to 50-90%

            UpdateProgress(displayProgress, "Loading VR environment...");
            yield return null;
        }

        Debug.Log("[LOADING] ✓ Scene loaded to 90%");
        UpdateProgress(0.9f, "Preparing VR...");

        // Phase 3: Ensure minimum load time (smooth UX)
        float elapsedTime = Time.realtimeSinceStartup - loadStartTime;
        float remainingTime = minimumLoadTime - elapsedTime;

        if (remainingTime > 0)
        {
            Debug.Log($"[LOADING] Waiting {remainingTime:F2}s for smooth transition");
            float waitStart = Time.realtimeSinceStartup;

            while (Time.realtimeSinceStartup - waitStart < remainingTime)
            {
                float waitProgress = (Time.realtimeSinceStartup - waitStart) / remainingTime;
                float displayProgress = 0.9f + (waitProgress * 0.05f); // 90-95%

                UpdateProgress(displayProgress, "Preparing VR...");
                yield return null;
            }
        }

        // Phase 4: Final preparation (95-100%)
        UpdateProgress(0.95f, "Almost ready...");
        yield return new WaitForSeconds(0.3f);

        UpdateProgress(1f, "Ready!");
        yield return new WaitForSeconds(0.2f);

        // Phase 5: Activate scene
        Debug.Log("[LOADING] Activating mainVR scene...");
        asyncLoad.allowSceneActivation = true;

        float totalTime = Time.realtimeSinceStartup - loadStartTime;
        Debug.Log("[LOADING] ==========================================");
        Debug.Log($"[LOADING] SCENE LOAD COMPLETE - Total: {totalTime:F2}s");
        Debug.Log("[LOADING] ==========================================");

        // Note: Loading UI will be hidden by StartUp.cs in mainVR scene
    }

    /// <summary>
    /// Update progress UI and VRLoadingManager
    /// </summary>
    void UpdateProgress(float progress, string status)
    {
        // Update local UI
        if (progressBar != null)
        {
            progressBar.fillAmount = progress;
        }

        if (statusText != null)
        {
            statusText.text = status;
        }

        if (percentageText != null)
        {
            percentageText.text = $"{(progress * 100):F0}%";
        }

        // Update VRLoadingManager (if present)
        if (vrLoadingManager != null)
        {
            vrLoadingManager.UpdateProgress(progress);
            vrLoadingManager.UpdateStatus(status);
        }

        Debug.Log($"[LOADING] Progress: {(progress * 100):F0}% - {status}");
    }
}
