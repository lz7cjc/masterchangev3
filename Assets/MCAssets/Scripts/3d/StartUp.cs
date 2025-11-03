using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.XR.Management;

/// <summary>
/// FINAL VERSION: Loading screen coordination with optional addressables
/// - Ensures loading screen shows from scene start
/// - Makes addressables optional
/// - Works whether coming from "everything" scene or direct load
/// - Handles both VR and 360 modes
/// </summary>
public class StartUp : MonoBehaviour
{
    [Header("Player Reference")]
    [SerializeField] private CharacterController player;

    [Header("BCT Zone Targets")]
    [SerializeField] private GameObject targetAlcohol;
    [SerializeField] private GameObject targetSmoking;
    [SerializeField] private GameObject targetMfn;
    [SerializeField] private GameObject targetHeights;

    [Header("Regular World Targets")]
    [SerializeField] private GameObject targetTravel;
    [SerializeField] private GameObject targetBeaches;
    [SerializeField] private GameObject targetSport;
    [SerializeField] private GameObject targetHome;

    [Header("UI References")]
    [SerializeField] private closeAllHuds closeAllHuds;
    [SerializeField] private SetVrState setVrState;
    [SerializeField] private togglingXR togglingXR;

    [Header("Stage Management")]
    private int stage;
    private string currentZone;
    private string lastKnownZone;
    [SerializeField] private bool toggler;

    [Header("Alcohol Treatment")]
    [SerializeField] private GameObject alcoholintro;
    [SerializeField] private GameObject alcoholfollow;
    [SerializeField] private GameObject alcoholsolution;
    [SerializeField] private GameObject alcoholfinish;

    [Header("Smoking Treatment")]
    [SerializeField] private GameObject welcomeSmoking;
    [SerializeField] private GameObject InitialConsultation;
    [SerializeField] private GameObject CTScanDelay;
    [SerializeField] private GameObject CTresults;
    [SerializeField] private GameObject smokingDone;
    [SerializeField] private int stopFilm;
    [SerializeField] private GameObject hud;

    [Header("Loading Configuration")]
    public togglingXR togglingXRScript;
    public GameObject loadingPanel;
    [Tooltip("Enable if using Addressables system")]
    public bool useAddressables = false;

    private VRLoadingManager loadingManager;
    private bool xrInitializedByUs = false;
    private bool isInitializing = false;

    void Awake()
    {
        // CRITICAL: Get loading manager reference FIRST
        loadingManager = VRLoadingManager.Instance;

        if (loadingManager == null)
        {
            Debug.LogWarning("[StartUp] VRLoadingManager not found! Loading screen won't work.");
        }
    }

    void Start()
    {
        bool startInVR = PlayerPrefs.GetInt("toggleToVR", 0) == 1;

        Debug.Log($"[StartUp] ========== SCENE START ==========");
        Debug.Log($"[StartUp] User chose mode: {(startInVR ? "VR" : "360")}");
        Debug.Log($"[StartUp] Addressables enabled: {useAddressables}");

        // Show loading screen IMMEDIATELY
        if (startInVR)
        {
            ShowLoadingScreen("Initializing VR...");
            StartCoroutine(InitializeVRMode());
        }
        else
        {
            ShowLoadingScreen("Loading...");
            StartCoroutine(Initialize360Mode());
        }
    }

    /// <summary>
    /// OPTIMIZED: Initialize VR mode with proper loading coordination
    /// </summary>
    IEnumerator InitializeVRMode()
    {
        if (isInitializing)
        {
            Debug.LogWarning("[StartUp] Already initializing - skipping duplicate call");
            yield break;
        }

        isInitializing = true;
        Debug.Log("[StartUp] === INITIALIZING VR MODE ===");
        float progress = 0f;

        // Step 1: Wait for addressables if enabled (0-30%)
        if (useAddressables)
        {
            UpdateLoadingScreen(0f, "Loading assets...");
            yield return WaitForAddressablesWithProgress((p) => {
                UpdateLoadingScreen(0f + (p * 0.3f), "Loading assets...");
            });
            progress = 0.3f;
        }
        else
        {
            // Skip addressables, go straight to XR init
            UpdateLoadingScreen(0f, "Initializing...");
            yield return new WaitForSeconds(0.2f);
            progress = 0.3f;
        }

        // Step 2: Initialize XR if needed (30-50%)
        bool needsXRInit = !IsXRAlreadyInitialized();
        if (needsXRInit)
        {
            UpdateLoadingScreen(progress, "Initializing XR...");
            yield return InitializeXRSystem();
            progress = 0.5f;
        }
        else
        {
            Debug.Log("[StartUp] XR already initialized");
            progress = 0.5f;
        }

        // Step 3: Start XR subsystems (50-65%)
        UpdateLoadingScreen(progress, "Starting VR...");
        yield return togglingXRScript.StartXR();
        progress = 0.65f;

        // Step 4: Wait for VR camera to be ready (65-75%)
        UpdateLoadingScreen(progress, "Preparing VR camera...");
        yield return WaitForVRCameraReady();
        progress = 0.75f;

        // Step 5: Setup scene configuration (75-90%)
        UpdateLoadingScreen(progress, "Configuring scene...");
        ResetScene(); // This sets up zones and moves player
        yield return new WaitForEndOfFrame(); // Let physics settle
        progress = 0.9f;

        // Step 6: Final checks (90-100%)
        UpdateLoadingScreen(progress, "Almost ready...");
        yield return new WaitForSeconds(0.3f);
        UpdateLoadingScreen(1f, "Ready!");

        // Step 7: Hide loading screen
        yield return new WaitForSeconds(0.2f);
        HideLoadingScreen();

        isInitializing = false;
        Debug.Log("[StartUp] === VR MODE INITIALIZATION COMPLETE ===");
    }

    /// <summary>
    /// OPTIMIZED: Initialize 360 mode with proper loading coordination
    /// </summary>
    IEnumerator Initialize360Mode()
    {
        if (isInitializing)
        {
            Debug.LogWarning("[StartUp] Already initializing - skipping duplicate call");
            yield break;
        }

        isInitializing = true;
        Debug.Log("[StartUp] === INITIALIZING 360 MODE ===");
        float progress = 0f;

        // Step 1: Wait for addressables if enabled (0-50%)
        if (useAddressables)
        {
            UpdateLoadingScreen(0f, "Loading assets...");
            yield return WaitForAddressablesWithProgress((p) => {
                UpdateLoadingScreen(0f + (p * 0.5f), "Loading assets...");
            });
            progress = 0.5f;
        }
        else
        {
            // Skip addressables
            UpdateLoadingScreen(0f, "Initializing...");
            yield return new WaitForSeconds(0.2f);
            progress = 0.5f;
        }

        // Step 2: Setup 360 mode (50-70%)
        UpdateLoadingScreen(progress, "Setting up 360 mode...");
        if (togglingXRScript != null)
        {
            togglingXRScript.SetVRMode(false);
        }
        yield return new WaitForEndOfFrame();
        progress = 0.7f;

        // Step 3: Setup scene configuration (70-90%)
        UpdateLoadingScreen(progress, "Configuring scene...");
        ResetScene();
        yield return new WaitForEndOfFrame();
        progress = 0.9f;

        // Step 4: Final checks (90-100%)
        UpdateLoadingScreen(progress, "Almost ready...");
        yield return new WaitForSeconds(0.3f);
        UpdateLoadingScreen(1f, "Ready!");

        // Step 5: Hide loading screen
        yield return new WaitForSeconds(0.2f);
        HideLoadingScreen();

        isInitializing = false;
        Debug.Log("[StartUp] === 360 MODE INITIALIZATION COMPLETE ===");
    }

    /// <summary>
    /// Wait for addressables with progress callback (OPTIONAL)
    /// </summary>
    IEnumerator WaitForAddressablesWithProgress(System.Action<float> onProgress)
    {
        if (!useAddressables)
        {
            Debug.Log("[StartUp] Addressables disabled - skipping");
            onProgress?.Invoke(1f);
            yield break;
        }

        LoadingManager addressablesLoader = FindFirstObjectByType<LoadingManager>();

        if (addressablesLoader == null)
        {
            Debug.Log("[StartUp] No LoadingManager found - skipping addressables wait");
            onProgress?.Invoke(1f);
            yield break;
        }

        Debug.Log("[StartUp] Waiting for addressables...");
        float timeout = 10f;
        float elapsed = 0f;

        while (addressablesLoader != null && elapsed < timeout)
        {
            // Estimate progress based on time (smooth animation)
            float estimatedProgress = Mathf.Clamp01(elapsed / 3f);
            onProgress?.Invoke(estimatedProgress);

            elapsed += Time.deltaTime;
            yield return null;
        }

        onProgress?.Invoke(1f);
        Debug.Log("[StartUp] Addressables ready");
    }

    /// <summary>
    /// Check if XR is already initialized
    /// </summary>
    bool IsXRAlreadyInitialized()
    {
        // In editor, XR might not be available - this is OK
#if UNITY_EDITOR
        if (XRGeneralSettings.Instance == null)
        {
            Debug.Log("[StartUp] Editor mode - XR not available (expected)");
            return false;
        }
#endif

        if (XRGeneralSettings.Instance == null) return false;
        if (XRGeneralSettings.Instance.Manager == null) return false;

        bool hasLoader = XRGeneralSettings.Instance.Manager.activeLoader != null;
        if (hasLoader)
        {
            Debug.Log($"[StartUp] ✓ XR already initialized: {XRGeneralSettings.Instance.Manager.activeLoader.GetType().Name}");
        }
        return hasLoader;
    }

    /// <summary>
    /// Initialize XR system
    /// </summary>
    IEnumerator InitializeXRSystem()
    {
#if UNITY_EDITOR
        Debug.LogWarning("[StartUp] Editor mode - XR initialization will fail (expected)");
        yield break;
#endif

        if (XRGeneralSettings.Instance == null || XRGeneralSettings.Instance.Manager == null)
        {
            Debug.LogError("[StartUp] Cannot initialize XR - XRGeneralSettings not configured!");
            yield break;
        }

        if (XRGeneralSettings.Instance.Manager.activeLoader == null)
        {
            Debug.Log("[StartUp] Initializing XR loader...");
            yield return XRGeneralSettings.Instance.Manager.InitializeLoader();

            if (XRGeneralSettings.Instance.Manager.activeLoader == null)
            {
                Debug.LogError("[StartUp] Failed to initialize XR loader!");
                yield break;
            }

            Debug.Log($"[StartUp] ✓ XR loader initialized: {XRGeneralSettings.Instance.Manager.activeLoader.GetType().Name}");
            xrInitializedByUs = true;
        }
    }

    /// <summary>
    /// Wait for VR camera to be ready
    /// </summary>
    IEnumerator WaitForVRCameraReady()
    {
        Debug.Log("[StartUp] Waiting for VR camera...");

        // Wait a few frames for camera to activate
        yield return new WaitForSeconds(0.2f);

        // Check if main camera is active
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            Debug.Log($"[StartUp] ✓ Main camera ready: {mainCam.gameObject.name}");
        }
        else
        {
            Debug.LogWarning("[StartUp] Main camera not found!");
        }
    }

    /// <summary>
    /// Reset scene configuration - CALLED AFTER CAMERA IS READY
    /// </summary>
    public void ResetScene()
    {
        Debug.Log("[StartUp] === RESET SCENE START ===");

        stage = PlayerPrefs.GetInt("stage", 0);
        lastKnownZone = PlayerPrefs.GetString("lastknownzone", "Home");
        currentZone = lastKnownZone;

        Debug.Log($"[StartUp] Zone: {currentZone}, Stage: {stage}");

        InitializeScene();
        HandleZoneNavigation();

        Debug.Log("[StartUp] === RESET SCENE COMPLETE ===");
    }

    /// <summary>
    /// Initialize scene - sets VR mode and positions player
    /// </summary>
    private void InitializeScene()
    {
        Debug.Log("[StartUp] InitializeScene");

        // Close HUDs
        if (closeAllHuds != null)
        {
            closeAllHuds.CloseTheHuds();
        }

        toggler = false;

        // Move player to correct location
        MovePlayerToCurrentZone();
    }

    /// <summary>
    /// Handle zone navigation based on current zone
    /// </summary>
    private void HandleZoneNavigation()
    {
        if (IsBCTZone(currentZone))
        {
            NavigateToBCTZone();
        }
        else
        {
            NavigateToRegularZone();
        }
    }

    private bool IsBCTZone(string zone)
    {
        return zone switch
        {
            "Smoking" or "Alcohol" or "Mindfulness" => true,
            _ => false
        };
    }

    /// <summary>
    /// OPTIMIZED: Teleport player using CharacterController
    /// </summary>
    private void SetPlayerToTarget(GameObject target)
    {
        if (target != null && player != null)
        {
            Debug.Log($"[StartUp] Teleporting player to: {target.name}");

            player.enabled = false;

            player.transform.SetParent(null);
            player.transform.position = Vector3.zero;
            player.transform.rotation = Quaternion.identity;

            player.transform.SetParent(target.transform);
            player.transform.localPosition = Vector3.zero;
            player.transform.localRotation = Quaternion.identity;
            player.transform.localScale = Vector3.one;

            player.enabled = true;

            Debug.Log($"[StartUp] ✓ Player positioned at: {player.transform.position}");
        }
        else
        {
            Debug.LogError($"[StartUp] Cannot position player - Target: {target}, Player: {player}");
        }
    }

    private GameObject GetTargetForZone(string zoneName)
    {
        return zoneName switch
        {
            "Alcohol" => targetAlcohol,
            "Smoking" => targetSmoking,
            "Mindfulness" => targetMfn,
            "Travel" => targetTravel,
            "Beaches" => targetBeaches,
            "Sport" => targetSport,
            "Heights" => targetHeights,
            _ => targetHome
        };
    }

    /// <summary>
    /// Move player to current zone
    /// </summary>
    private void MovePlayerToCurrentZone()
    {
        Debug.Log($"[StartUp] Moving player to zone: {currentZone}");

        ZoneManager zoneManager = FindFirstObjectByType<ZoneManager>();
        GameObject targetObject = GetTargetForZone(currentZone);

        if (targetObject == null)
        {
            Debug.LogWarning($"[StartUp] No target found for zone: {currentZone}");
            return;
        }

        if (zoneManager != null)
        {
            Debug.Log($"[StartUp] Using ZoneManager with target: {targetObject.name}");
            zoneManager.MoveToZoneByGameObject(targetObject);
        }
        else
        {
            Debug.Log("[StartUp] ZoneManager not found, using direct positioning");
            SetPlayerToTarget(targetObject);
        }
    }

    private void NavigateToBCTZone()
    {
        Debug.Log($"[StartUp] Navigating to BCT zone: {currentZone}");

        switch (currentZone)
        {
            case "Alcohol":
                HandleAlcoholTreatment();
                break;

            case "Smoking":
                HandleSmokingTreatment();
                break;

            case "Mindfulness":
                SetPlayerToTarget(targetMfn);
                break;

            default:
                Debug.LogError($"[StartUp] Unhandled BCT zone: {currentZone}");
                NavigateToRegularZone();
                break;
        }
    }

    private void NavigateToRegularZone()
    {
        GameObject targetObject = GetTargetForZone(currentZone);
        SetPlayerToTarget(targetObject);
    }

    private void HandleAlcoholTreatment()
    {
        SetPlayerToTarget(targetAlcohol);
        stage = PlayerPrefs.GetInt("stageAlcohol");

        alcoholintro.SetActive(stage == 0);
        alcoholfollow.SetActive(stage == 1);
        alcoholsolution.SetActive(stage == 2);
        alcoholfinish.SetActive(stage == 3);
    }

    private void HandleSmokingTreatment()
    {
        SetPlayerToTarget(targetSmoking);
        stage = PlayerPrefs.GetInt("stageSmoking");

        welcomeSmoking.SetActive(false);
        InitialConsultation.SetActive(false);
        CTScanDelay.SetActive(false);
        CTresults.SetActive(false);
        smokingDone.SetActive(false);

        switch (stage)
        {
            case 1: welcomeSmoking.SetActive(true); break;
            case 2: InitialConsultation.SetActive(true); break;
            case 3:
                PlayerPrefs.DeleteKey("CTstartpoint");
                PlayerPrefs.DeleteKey("delaynotification");
                break;
            case 4: CTresults.SetActive(true); break;
            case 5: InitialConsultation.SetActive(true); break;
            default: SetPlayerToTarget(targetHome); break;
        }

        player.transform.localPosition = Vector3.zero;
    }

    // === LOADING SCREEN HELPERS ===

    private void ShowLoadingScreen(string message)
    {
        if (loadingManager != null)
        {
            loadingManager.ShowLoading(message, 0f);
        }
        else if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[StartUp] No loading screen available!");
        }
    }

    private void UpdateLoadingScreen(float progress, string message)
    {
        if (loadingManager != null)
        {
            loadingManager.UpdateProgress(progress);
            loadingManager.UpdateStatus(message);
        }
    }

    private void HideLoadingScreen()
    {
        if (loadingManager != null)
        {
            loadingManager.HideLoading();
        }
        else if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (xrInitializedByUs && XRGeneralSettings.Instance?.Manager != null)
        {
            Debug.Log("[StartUp] Cleaning up XR initialization");
        }
    }
}