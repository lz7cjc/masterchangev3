using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.XR.Management;

/// <summary>
/// OPTIMAL VERSION: Conditionally initializes XR based on user's chosen mode
/// UPDATED: Converted from Rigidbody to CharacterController for better VR teleportation
/// </summary>
public class StartUp : MonoBehaviour
{
    [Header("Player Reference")]
    [SerializeField] private CharacterController player; // CHANGED: From Rigidbody to CharacterController

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
    public togglingXR togglingXRScript;
    public GameObject loadingPanel; // Fallback if VRLoadingManager not found

    // Reference to the loading manager
    private VRLoadingManager loadingManager;

    // Track if we initialized XR in this session
    private bool xrInitializedByUs = false;

    void Start()
    {
        // Get loading manager reference
        loadingManager = VRLoadingManager.Instance;

        // CRITICAL: Check which mode the user chose
        // This determines if we need to initialize XR or not
        bool startInVR = PlayerPrefs.GetInt("toggleToVR", 0) == 1;

        Debug.Log($"[StartUp] ========== SCENE START ==========");
        Debug.Log($"[StartUp] User chose mode: {(startInVR ? "VR" : "360")}");
        Debug.Log($"[StartUp] PlayerPrefs VRMode: {PlayerPrefs.GetInt("VRMode", 0)}");
        Debug.Log($"[StartUp] PlayerPrefs toggleToVR: {PlayerPrefs.GetInt("toggleToVR", 0)}");

        if (startInVR)
        {
            // Show loading screen immediately
            if (loadingManager != null)
            {
                loadingManager.ShowInitialLoading();
            }
            else if (loadingPanel != null)
            {
                loadingPanel.SetActive(true);
            }

            // Initialize scene in VR mode (with XR initialization)
            StartCoroutine(InitializeVRMode());
        }
        else
        {
            // Initialize scene in 360 mode (no XR needed)
            StartCoroutine(Initialize360Mode());
        }
    }

    /// <summary>
    /// OPTIMAL: Initialize scene in VR mode
    /// This handles XR initialization, addressables, and scene setup
    /// </summary>
    IEnumerator InitializeVRMode()
    {
        Debug.Log("[StartUp] === INITIALIZING VR MODE ===");

        // Step 1: Wait for addressables first
        if (loadingManager != null)
            loadingManager.ShowAssetLoading();

        yield return WaitForAddressablesWithProgress();

        // Step 2: Check if XR needs to be initialized
        bool needsXRInit = !IsXRAlreadyInitialized();

        if (needsXRInit)
        {
            Debug.Log("[StartUp] XR not initialized - initializing now...");

            if (loadingManager != null)
            {
                loadingManager.ShowInitialLoading();
                loadingManager.UpdateProgress(0.3f);
            }

            // Initialize XR system
            yield return InitializeXRSystem();

            if (loadingManager != null)
                loadingManager.UpdateProgress(0.5f);
        }
        else
        {
            Debug.Log("[StartUp] XR already initialized - skipping initialization");
            if (loadingManager != null)
                loadingManager.UpdateProgress(0.5f);
        }

        // Step 3: Start XR subsystems and setup VR camera
        if (loadingManager != null)
            loadingManager.ShowInitialLoading();

        Debug.Log("[StartUp] Starting XR subsystems...");
        yield return togglingXRScript.StartXR();

        if (loadingManager != null)
            loadingManager.UpdateProgress(0.8f);

        // Step 4: Wait for camera to be ready
        yield return WaitForVRCameraReady();

        if (loadingManager != null)
            loadingManager.UpdateProgress(0.95f);

        // Step 5: Extra safety buffer
        yield return new WaitForSeconds(0.5f);

        // Step 6: Setup scene
        yield return WaitForAddressablesAndSetupScene();

        // Step 7: Hide loading screen
        if (loadingManager != null)
        {
            loadingManager.UpdateProgress(1f);
            yield return new WaitForSeconds(0.2f);
            loadingManager.HideLoading();
        }
        else if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }

        Debug.Log("[StartUp] === VR MODE INITIALIZATION COMPLETE ===");
    }

    /// <summary>
    /// OPTIMAL: Initialize scene in 360 mode
    /// No XR initialization needed, just load assets and setup scene
    /// </summary>
    IEnumerator Initialize360Mode()
    {
        Debug.Log("[StartUp] === INITIALIZING 360 MODE ===");

        // Show loading for addressables
        if (loadingManager != null)
            loadingManager.ShowAssetLoading();

        yield return WaitForAddressablesWithProgress();

        // Setup 360 mode (no XR)
        if (togglingXRScript != null)
        {
            togglingXRScript.SetVRMode(false);
        }

        // Setup scene
        yield return WaitForAddressablesAndSetupScene();

        // Hide loading
        if (loadingManager != null)
        {
            loadingManager.HideLoading();
        }

        Debug.Log("[StartUp] === 360 MODE INITIALIZATION COMPLETE ===");
    }

    /// <summary>
    /// NEW: Check if XR system is already initialized
    /// Handles scene reloads where XR might already be active
    /// </summary>
    bool IsXRAlreadyInitialized()
    {
        if (XRGeneralSettings.Instance == null)
        {
            Debug.Log("[StartUp] XRGeneralSettings.Instance is NULL - XR not initialized");
            return false;
        }

        if (XRGeneralSettings.Instance.Manager == null)
        {
            Debug.Log("[StartUp] XR Manager is NULL - XR not initialized");
            return false;
        }

        if (XRGeneralSettings.Instance.Manager.activeLoader != null)
        {
            Debug.Log($"[StartUp] ✓ XR already initialized with loader: {XRGeneralSettings.Instance.Manager.activeLoader.GetType().Name}");
            return true;
        }

        Debug.Log("[StartUp] No active XR loader - XR not initialized");
        return false;
    }

    /// <summary>
    /// NEW: Initialize the XR system manually
    /// Only called when needed (VR mode and not already initialized)
    /// </summary>
    IEnumerator InitializeXRSystem()
    {
        Debug.Log("[StartUp] Initializing XR system...");

        if (XRGeneralSettings.Instance == null || XRGeneralSettings.Instance.Manager == null)
        {
            Debug.LogError("[StartUp] Cannot initialize XR - XRGeneralSettings not configured!");
            yield break;
        }

        yield return XRGeneralSettings.Instance.Manager.InitializeLoader();

        if (XRGeneralSettings.Instance.Manager.activeLoader == null)
        {
            Debug.LogError("[StartUp] XR initialization failed - no loader found!");
            yield break;
        }

        xrInitializedByUs = true;
        Debug.Log($"[StartUp] ✓ XR initialized successfully: {XRGeneralSettings.Instance.Manager.activeLoader.GetType().Name}");
    }

    IEnumerator WaitForAddressablesWithProgress()
    {
        Debug.Log("[StartUp] Waiting for addressables...");
        float elapsedTime = 0f;
        float timeout = 5f;

        while (elapsedTime < timeout)
        {
            if (loadingManager != null)
                loadingManager.UpdateProgress(0.1f + (elapsedTime / timeout) * 0.2f);

            yield return null;
            elapsedTime += Time.deltaTime;
        }

        Debug.Log("[StartUp] Addressables ready");
    }

    IEnumerator WaitForVRCameraReady()
    {
        Debug.Log("[StartUp] Waiting for VR camera to be ready...");
        float elapsedTime = 0f;
        float timeout = 3f;

        while (elapsedTime < timeout)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null && mainCamera.gameObject.activeInHierarchy)
            {
                Debug.Log("[StartUp] ✓ VR camera is ready");
                yield break;
            }

            yield return null;
            elapsedTime += Time.deltaTime;
        }

        Debug.LogWarning("[StartUp] VR camera not ready after timeout - continuing anyway");
    }

    IEnumerator WaitForAddressablesAndSetupScene()
    {
        Debug.Log("[StartUp] Setting up scene...");

        yield return new WaitForSeconds(0.2f);

        ResetScene();

        Debug.Log("[StartUp] ✓ Scene setup complete");
    }

    void OnDestroy()
    {
        if (xrInitializedByUs)
        {
            Debug.Log("[StartUp] OnDestroy - Cleaning up XR system");
            // Note: togglingXR will handle this in its OnDestroy
            // We just track that we initialized it
        }
    }

    // === ALL METHODS BELOW THIS LINE ARE UPDATED FOR CHARACTER CONTROLLER ===

    public void ResetScene()
    {
        Debug.Log("[StartUp] Marker: ResetScene");

        stage = PlayerPrefs.GetInt("stage", 0);

        bool hasLastKnownZone = PlayerPrefs.HasKey("lastknownzone");
        lastKnownZone = PlayerPrefs.GetString("lastknownzone", "Home");
        currentZone = lastKnownZone;

        Debug.Log($"[StartUp] Has lastknownzone PlayerPref: {hasLastKnownZone}");
        Debug.Log($"[StartUp] lastKnownZone value: '{lastKnownZone}'");
        Debug.Log($"[StartUp] currentZone: '{currentZone}'");

        bool isNewPlayer = !hasLastKnownZone;

        InitializeScene();
        HandleZoneNavigation();

        if (isNewPlayer)
        {
            PlayerPrefs.SetString("lastknownzone", "Home");
            Debug.Log("[StartUp] Set default lastknownzone to 'Home' for new player");
        }
    }

    private void InitializeScene()
    {
        Debug.Log("[StartUp] Marker: InitializeScene");

        togglingXR = FindFirstObjectByType<togglingXR>();

        if (togglingXR != null)
        {
            int vrMode = PlayerPrefs.GetInt("toggleToVR", 0);
            Debug.Log($"[StartUp] toggleToVR PlayerPref = {vrMode}");

            if (vrMode == 1)
            {
                Debug.Log("[StartUp] Setting VR mode ON (from PlayerPrefs)");
                togglingXR.SetVRMode(true);
            }
            else
            {
                Debug.Log("[StartUp] Setting VR mode OFF (360 mode from PlayerPrefs)");
                togglingXR.SetVRMode(false);
            }
        }
        else
        {
            Debug.LogError("[StartUp] togglingXR component not found!");
        }

        if (closeAllHuds != null)
        {
            closeAllHuds.CloseTheHuds();
        }
        else
        {
            Debug.LogError("[StartUp] Marker: closeAllHuds is not assigned!");
        }

        toggler = false;
        MovePlayerToCurrentZone();
    }

    private void HandleZoneNavigation()
    {
        Debug.Log("[StartUp] Marker: HandleZoneNavigation");

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
        Debug.Log("[StartUp] Marker: IsBCTZone");
        return zone switch
        {
            "Smoking" or "Alcohol" or "Mindfulness" => true,
            _ => false
        };
    }

    private void SetPlayerToTarget(GameObject target)
    {
        Debug.Log("[StartUp] Marker: SetPlayerToTarget");
        if (target != null && player != null)
        {
            // CHANGED: Simple CharacterController teleportation
            player.enabled = false; // Disable CharacterController for teleport

            player.transform.SetParent(null);
            player.transform.position = Vector3.zero;
            player.transform.rotation = Quaternion.identity;

            player.transform.SetParent(target.transform);
            player.transform.localPosition = Vector3.zero;
            player.transform.localRotation = Quaternion.identity;
            player.transform.localScale = Vector3.one;

            player.enabled = true; // Re-enable CharacterController
        }
        else
        {
            Debug.LogError($"[StartUp] Marker: Missing reference - Target: {target}, Player: {player}");
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

    private void MovePlayerToCurrentZone()
    {
        Debug.Log($"[StartUp] Marker: MovePlayerToCurrentZone -> currentZone: {currentZone}");

        ZoneManager zoneManager = FindFirstObjectByType<ZoneManager>();
        if (zoneManager != null)
        {
            GameObject targetObject = GetTargetForZone(currentZone);
            if (targetObject != null)
            {
                Debug.Log($"[StartUp] Using ZoneManager with target: {targetObject.name}");
                zoneManager.MoveToZoneByGameObject(targetObject);
            }
            else
            {
                Debug.LogWarning($"[StartUp] No target found for zone: {currentZone}");
            }
        }
        else
        {
            Debug.Log("[StartUp] ZoneManager not found, using fallback");
            GameObject targetObject = GetTargetForZone(currentZone);
            SetPlayerToTarget(targetObject);
        }
    }

    private void NavigateToBCTZone()
    {
        Debug.Log($"[StartUp] Marker: Navigating to BCT zone: {currentZone}");

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
                Debug.LogError($"[StartUp] Marker: Unhandled BCT zone: {currentZone}");
                NavigateToRegularZone();
                break;
        }
    }

    private void NavigateToRegularZone()
    {
        Debug.Log("[StartUp] Marker: NavigateToRegularZone");
        GameObject targetObject = GetTargetForZone(currentZone);
        SetPlayerToTarget(targetObject);
    }

    private void HandleAlcoholTreatment()
    {
        Debug.Log("[StartUp] Marker: HandleAlcoholTreatment");
        SetPlayerToTarget(targetAlcohol);
        stage = PlayerPrefs.GetInt("stageAlcohol");

        alcoholintro.SetActive(stage == 0);
        alcoholfollow.SetActive(stage == 1);
        alcoholsolution.SetActive(stage == 2);
        alcoholfinish.SetActive(stage == 3);
    }

    private void HandleSmokingTreatment()
    {
        Debug.Log("[StartUp] Marker: HandleSmokingTreatment");
        SetPlayerToTarget(targetSmoking);
        stage = PlayerPrefs.GetInt("stageSmoking");

        welcomeSmoking.SetActive(false);
        InitialConsultation.SetActive(false);
        CTScanDelay.SetActive(false);
        CTresults.SetActive(false);
        smokingDone.SetActive(false);

        switch (stage)
        {
            case 1:
                welcomeSmoking.SetActive(true);
                break;
            case 2:
                InitialConsultation.SetActive(true);
                break;
            case 3:
                PlayerPrefs.DeleteKey("CTstartpoint");
                PlayerPrefs.DeleteKey("delaynotification");
                break;
            case 4:
                CTresults.SetActive(true);
                break;
            case 5:
                InitialConsultation.SetActive(true);
                break;
            default:
                SetPlayerToTarget(targetHome);
                break;
        }

        player.transform.localPosition = Vector3.zero;
    }
}