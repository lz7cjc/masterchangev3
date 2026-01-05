using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;

#if ADDRESSABLES_ENABLED
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

/// <summary>
/// OPTIMIZED StartUp - handles mainVR scene initialization
/// - Faster VR/360 mode setup
/// - Proper loading screen management
/// - Addressables support (commented out, ready to enable)
/// </summary>
public class StartUp : MonoBehaviour
{
    [Header("Player Reference")]
    public GameObject Player;

    [Header("BCT Zone Targets")]
    public GameObject TargetAlcohol;
    public GameObject TargetSmoking;
    public GameObject TargetMfn;
    public GameObject TargetHeights;

    [Header("Regular World Targets")]
    public GameObject TargetTravel;
    public GameObject TargetBeaches;
    public GameObject TargetSport;
    public GameObject TargetHome;
    public GameObject Toggler;

    [Header("Alcohol Treatment")]
    public GameObject AlcoholIntro;
    public GameObject AlcoholFollow;
    public GameObject AlcoholSolution;
    public GameObject AlcoholFinish;

    [Header("Smoking Treatment")]
    public GameObject WelcomeSmoking;
    public GameObject InitialConsultation;
    public GameObject CTScanDelay;
    public GameObject CTResults;
    public GameObject SmokingDone;
    public GameObject StopFilm;
    public GameObject Hud;

    [Header("Loading Configuration")]
    public GameObject TogglingXRScript;
    public GameObject LoadingPanel;

    [Header("Addressables (Optional - Enable with scripting define)")]
    public bool useAddressables = false;
    public float addressablesTimeout = 5f;

    private VRLoadingManager loadingManager;
    private bool xrInitializedByUs = false;
    private float sceneLoadStartTime;

    void Awake()
    {
        sceneLoadStartTime = Time.realtimeSinceStartup;
        
        Debug.Log("[StartUp] ==========================================");
        Debug.Log($"[StartUp] AWAKE - mainVR scene loaded at {sceneLoadStartTime:F3}s");
        Debug.Log($"[StartUp] Frame: {Time.frameCount}");
        Debug.Log("[StartUp] ==========================================");

        // Find VRLoadingManager (should persist from Dashboard)
        loadingManager = VRLoadingManager.Instance;

        if (loadingManager == null)
        {
            Debug.LogWarning("[StartUp] VRLoadingManager not found! Loading screen won't work.");
        }
        else
        {
            Debug.Log($"[StartUp] ✓ VRLoadingManager found: {loadingManager.gameObject.name}");
            // Keep loading screen visible during initialization
        }
    }

    void Start()
    {
        Debug.Log($"[StartUp] === START BEGIN (Frame: {Time.frameCount}) ===");
        
        int toggleToVR = PlayerPrefs.GetInt("toggleToVR", 0);
        string mode = toggleToVR == 1 ? "VR" : "360";
        
        Debug.Log($"[StartUp] Mode: {mode}");
        Debug.Log($"[StartUp] Addressables: {(useAddressables ? "ENABLED" : "DISABLED")}");

        if (toggleToVR == 1)
        {
            StartCoroutine(InitializeVRMode());
        }
        else
        {
            StartCoroutine(Initialize360Mode());
        }
    }

    /// <summary>
    /// Initialize VR mode with XR system
    /// </summary>
    IEnumerator InitializeVRMode()
    {
        Debug.Log("[StartUp] === VR MODE INITIALIZATION START ===");
        
        if (loadingManager != null)
        {
            loadingManager.UpdateStatus("Initializing VR...");
            loadingManager.UpdateProgress(0.1f);
        }

        yield return new WaitForEndOfFrame();

        // ADDRESSABLES: Wait for assets to load (if enabled)
#if ADDRESSABLES_ENABLED
        if (useAddressables)
        {
            yield return StartCoroutine(WaitForAddressables());
        }
#else
        if (useAddressables)
        {
            Debug.LogWarning("[StartUp] Addressables enabled but ADDRESSABLES_ENABLED not defined!");
            Debug.LogWarning("[StartUp] Add 'ADDRESSABLES_ENABLED' to Scripting Define Symbols");
        }
#endif

        if (loadingManager != null)
        {
            loadingManager.UpdateStatus("Starting XR...");
            loadingManager.UpdateProgress(0.3f);
        }

        // Initialize XR if needed
        if (!IsXRAlreadyInitialized())
        {
            yield return StartCoroutine(InitializeXRSystem());
        }

        if (loadingManager != null)
        {
            loadingManager.UpdateStatus("Setting up VR camera...");
            loadingManager.UpdateProgress(0.6f);
        }

        // Wait for VR camera to be ready
        yield return StartCoroutine(WaitForVRCameraReady());

        if (loadingManager != null)
        {
            loadingManager.UpdateStatus("Finalizing...");
            loadingManager.UpdateProgress(0.8f);
        }

        // Activate toggling script
        if (TogglingXRScript != null)
        {
            TogglingXRScript.SetActive(true);
        }

        // Position player and setup scene
        ResetScene();

        if (loadingManager != null)
        {
            loadingManager.UpdateProgress(1f);
            loadingManager.UpdateStatus("Ready!");
        }

        yield return new WaitForSeconds(0.3f);

        // HIDE LOADING SCREEN
        HideLoadingScreen();

        float totalTime = Time.realtimeSinceStartup - sceneLoadStartTime;
        Debug.Log("[StartUp] ==========================================");
        Debug.Log($"[StartUp] VR MODE READY - Total time: {totalTime:F2}s");
        Debug.Log("[StartUp] ==========================================");
    }

    /// <summary>
    /// Initialize 360 mode (no XR)
    /// </summary>
    IEnumerator Initialize360Mode()
    {
        Debug.Log("[StartUp] === 360 MODE INITIALIZATION START ===");

        if (loadingManager != null)
        {
            loadingManager.UpdateStatus("Initializing 360°...");
            loadingManager.UpdateProgress(0.2f);
        }

        yield return new WaitForEndOfFrame();

        // ADDRESSABLES: Wait for assets to load (if enabled)
#if ADDRESSABLES_ENABLED
        if (useAddressables)
        {
            yield return StartCoroutine(WaitForAddressables());
        }
#endif

        if (loadingManager != null)
        {
            loadingManager.UpdateStatus("Setting up scene...");
            loadingManager.UpdateProgress(0.6f);
        }

        // Activate toggling script
        if (TogglingXRScript != null)
        {
            TogglingXRScript.SetActive(true);
        }

        // Position player and setup scene
        ResetScene();

        if (loadingManager != null)
        {
            loadingManager.UpdateProgress(1f);
            loadingManager.UpdateStatus("Ready!");
        }

        yield return new WaitForSeconds(0.3f);

        // HIDE LOADING SCREEN
        HideLoadingScreen();

        float totalTime = Time.realtimeSinceStartup - sceneLoadStartTime;
        Debug.Log("[StartUp] ==========================================");
        Debug.Log($"[StartUp] 360 MODE READY - Total time: {totalTime:F2}s");
        Debug.Log("[StartUp] ==========================================");
    }

#if ADDRESSABLES_ENABLED
    // ==========================================
    // ADDRESSABLES SUPPORT
    // ==========================================

    /// <summary>
    /// Wait for addressables to download/load
    /// Shows detailed progress with asset count and size
    /// </summary>
    IEnumerator WaitForAddressables()
    {
        Debug.Log("[StartUp] === ADDRESSABLES LOADING START ===");

        if (loadingManager != null)
        {
            loadingManager.UpdateStatus("Loading assets...");
        }

        // Example: Load a label or specific assets
        // Replace "MainSceneAssets" with your actual addressables label
        AsyncOperationHandle<IList<GameObject>> handle = 
            Addressables.LoadAssetsAsync<GameObject>("MainSceneAssets", null);

        if (loadingManager != null)
        {
            loadingManager.TrackAddressablesLoad(handle, "Scene Assets");
        }

        // Wait for completion
        yield return handle;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            Debug.Log($"[StartUp] ✓ Addressables loaded: {handle.Result.Count} assets");
        }
        else
        {
            Debug.LogError("[StartUp] Addressables load failed!");
        }

        Debug.Log("[StartUp] === ADDRESSABLES LOADING COMPLETE ===");
    }
#else
    // Fallback when addressables not enabled
    IEnumerator WaitForAddressables()
    {
        Debug.LogWarning("[StartUp] Addressables requested but not enabled - skipping");
        yield return null;
    }
#endif

    /// <summary>
    /// Check if XR is already initialized
    /// </summary>
    bool IsXRAlreadyInitialized()
    {
        if (XRGeneralSettings.Instance == null || XRGeneralSettings.Instance.Manager == null)
        {
            return false;
        }

        bool hasActiveLoader = XRGeneralSettings.Instance.Manager.activeLoader != null;
        Debug.Log($"[StartUp] XR already initialized: {hasActiveLoader}");
        return hasActiveLoader;
    }

    /// <summary>
    /// Initialize XR system
    /// </summary>
    IEnumerator InitializeXRSystem()
    {
        if (XRGeneralSettings.Instance == null || XRGeneralSettings.Instance.Manager == null)
        {
            Debug.LogError("[StartUp] XRGeneralSettings not available!");
            yield break;
        }

        Debug.Log("[StartUp] Initializing XR loader...");
        yield return XRGeneralSettings.Instance.Manager.InitializeLoader();

        if (XRGeneralSettings.Instance.Manager.activeLoader == null)
        {
            Debug.LogError("[StartUp] Failed to initialize XR loader!");
        }
        else
        {
            Debug.Log($"[StartUp] ✓ XR initialized: {XRGeneralSettings.Instance.Manager.activeLoader.GetType().Name}");
            xrInitializedByUs = true;
        }
    }

    /// <summary>
    /// Wait for VR camera to be ready (stereo mode enabled)
    /// </summary>
    IEnumerator WaitForVRCameraReady()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("[StartUp] No main camera found");
            yield break;
        }

        int waitFrames = 0;
        while (!mainCamera.stereoEnabled && waitFrames < 60)
        {
            waitFrames++;
            yield return null;
        }

        if (mainCamera.stereoEnabled)
        {
            Debug.Log($"[StartUp] ✓ VR camera ready (waited {waitFrames} frames)");
        }
        else
        {
            Debug.LogWarning("[StartUp] Main camera not in stereo mode after 60 frames");
        }
    }

    /// <summary>
    /// PUBLIC: Reset scene to current zone/stage
    /// Called by other scripts (PickBCT.cs, drinkoutcome.cs, etc.)
    /// </summary>
    public void ResetScene()
    {
        Debug.Log("[StartUp] === RESET SCENE START ===");

        string currentZone = PlayerPrefs.GetString("currentZone", "Home");
        int currentStage = PlayerPrefs.GetInt("currentStage", 0);

        Debug.Log($"[StartUp] Zone: {currentZone}, Stage: {currentStage}");

        InitializeScene();
        HandleZoneNavigation();

        Debug.Log("[StartUp] === RESET SCENE COMPLETE ===");
    }

    /// <summary>
    /// Initialize scene objects
    /// </summary>
    void InitializeScene()
    {
        if (Hud != null)
        {
            Hud.SetActive(true);
        }

        MovePlayerToCurrentZone();
    }

    /// <summary>
    /// Navigate to appropriate zone based on PlayerPrefs
    /// </summary>
    void HandleZoneNavigation()
    {
        string currentZone = PlayerPrefs.GetString("currentZone", "Home");
        int currentStage = PlayerPrefs.GetInt("currentStage", 0);

        if (currentZone == "Alcohol")
        {
            NavigateToAlcoholZone(currentStage);
        }
        else if (currentZone == "Smoking")
        {
            NavigateToSmokingZone(currentStage);
        }
        else
        {
            NavigateToRegularZone(currentZone);
        }
    }

    /// <summary>
    /// Teleport player to target position
    /// </summary>
    void SetPlayerToTarget(GameObject target)
    {
        if (target == null)
        {
            Debug.LogWarning("[StartUp] Target is null!");
            return;
        }

        Debug.Log($"[StartUp] Teleporting player to: {target.name}");

        if (Player != null)
        {
            Player.transform.position = target.transform.position;
            Player.transform.rotation = target.transform.rotation;
            Debug.Log($"[StartUp] ✓ Player at: {target.transform.position}");
        }
        else
        {
            Debug.LogWarning("[StartUp] Player reference is null!");
        }
    }

    /// <summary>
    /// Navigate to alcohol treatment stage
    /// </summary>
    void NavigateToAlcoholZone(int stage)
    {
        switch (stage)
        {
            case 0: SetPlayerToTarget(AlcoholIntro); break;
            case 1: SetPlayerToTarget(AlcoholFollow); break;
            case 2: SetPlayerToTarget(AlcoholSolution); break;
            case 3: SetPlayerToTarget(AlcoholFinish); break;
            default: SetPlayerToTarget(TargetAlcohol); break;
        }
    }

    /// <summary>
    /// Navigate to smoking treatment stage
    /// </summary>
    void NavigateToSmokingZone(int stage)
    {
        switch (stage)
        {
            case 0: SetPlayerToTarget(WelcomeSmoking); break;
            case 1: SetPlayerToTarget(InitialConsultation); break;
            case 2: SetPlayerToTarget(CTScanDelay); break;
            case 3: SetPlayerToTarget(CTResults); break;
            case 4: SetPlayerToTarget(SmokingDone); break;
            default: SetPlayerToTarget(TargetSmoking); break;
        }
    }

    /// <summary>
    /// Navigate to regular world zone
    /// </summary>
    void NavigateToRegularZone(string zoneName)
    {
        GameObject target = GetZoneTarget(zoneName);
        if (target != null)
        {
            SetPlayerToTarget(target);
        }
    }

    /// <summary>
    /// Move player using ZoneManager if available, otherwise direct positioning
    /// </summary>
    void MovePlayerToCurrentZone()
    {
        string currentZone = PlayerPrefs.GetString("currentZone", "Home");
        GameObject targetZone = GetZoneTarget(currentZone);

        if (targetZone != null)
        {
            Debug.Log($"[StartUp] Moving player to zone: {currentZone}");
            
            ZoneManager zoneManager = FindObjectOfType<ZoneManager>();
            if (zoneManager != null)
            {
                Debug.Log($"[StartUp] Using ZoneManager with target: {targetZone.name}");
                zoneManager.MoveToZoneByGameObject(targetZone);
            }
            else
            {
                Debug.Log("[StartUp] No ZoneManager - using direct positioning");
                SetPlayerToTarget(targetZone);
            }
        }
    }

    /// <summary>
    /// Get zone target GameObject by name
    /// </summary>
    GameObject GetZoneTarget(string zoneName)
    {
        switch (zoneName)
        {
            case "Alcohol": return TargetAlcohol;
            case "Smoking": return TargetSmoking;
            case "MFN": return TargetMfn;
            case "Heights": return TargetHeights;
            case "Travel": return TargetTravel;
            case "Beaches": return TargetBeaches;
            case "Sport": return TargetSport;
            case "Home": return TargetHome;
            default: return TargetHome;
        }
    }

    /// <summary>
    /// Hide loading screen (called after initialization complete)
    /// </summary>
    void HideLoadingScreen()
    {
        Debug.Log("[StartUp] Hiding loading screen...");

        if (loadingManager != null)
        {
            loadingManager.HideLoading();
            Debug.Log("[StartUp] ✓ VRLoadingManager hidden");
        }
        else if (LoadingPanel != null)
        {
            LoadingPanel.SetActive(false);
            Debug.Log("[StartUp] ✓ Local loading panel hidden");
        }
        else
        {
            Debug.LogWarning("[StartUp] No loading screen to hide!");
        }
    }

    /// <summary>
    /// Cleanup on destroy
    /// </summary>
    void OnDestroy()
    {
        if (xrInitializedByUs && XRGeneralSettings.Instance != null && XRGeneralSettings.Instance.Manager != null)
        {
            Debug.Log("[StartUp] Deinitializing XR on destroy");
            XRGeneralSettings.Instance.Manager.DeinitializeLoader();
        }
    }
}
