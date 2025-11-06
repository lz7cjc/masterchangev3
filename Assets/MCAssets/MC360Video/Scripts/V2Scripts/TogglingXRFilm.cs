using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

/// <summary>
/// OPTIMIZED: XR management with faster switching and better error handling
/// - Graceful editor mode handling (no XR errors)
/// - Faster VR/360 switching
/// - Better loading screen coordination
/// - Smoother transitions
/// - Canvas event camera switching
/// </summary>
public class togglingXRFilm : MonoBehaviour
{
    public bool switchVRon;

    [Header("VR Components")]
    public GameObject vrReticleObject;

    [Header("Camera References")]
    public GameObject mainCamera360;
    public GameObject mainCameraVR;

    [Header("Canvas Reference")]
    public Canvas hudCanvas;

    [Header("Debug")]
    public bool enableDebugLogging = true;

    private VRReticlePointer reticlePointer360;
    private VRReticlePointer reticlePointerVR;
    private CameraController cameraController;
    private PlayerInput playerInput360;
    private PlayerInput playerInputVR;
    private EventSystem eventSystem;
    private bool isXRActive = false;
    private bool isInEditorMode = false;

    void Start()
    {
        Debug.Log("[VRLOAD] === togglingXR START ===");

        // Check if we're in editor
#if UNITY_EDITOR
        isInEditorMode = true;
        Debug.Log("[VRLOAD] Running in Unity Editor - XR will be simulated");
#endif

        // Get EventSystem reference
        eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            Debug.LogWarning("[VRLOAD] EventSystem not found!");
        }

        // Get camera references and components
        SetupCameraReferences();

        if (!isInEditorMode)
        {
            LogSystemInfo();
        }

        Debug.Log("[VRLOAD] Waiting for StartUp to set initial mode...");
    }

    /// <summary>
    /// Setup camera references and their components
    /// </summary>
    private void SetupCameraReferences()
    {
        if (mainCamera360 != null)
        {
            playerInput360 = mainCamera360.GetComponent<PlayerInput>();
            reticlePointer360 = mainCamera360.GetComponent<VRReticlePointer>();

            if (reticlePointer360 == null)
            {
                reticlePointer360 = mainCamera360.AddComponent<VRReticlePointer>();
                Debug.Log("[VRLOAD] Added VRReticlePointer to 360 camera");
            }
        }
        else
        {
            Debug.LogError("[VRLOAD] Main Camera360 not assigned!");
        }

        if (mainCameraVR != null)
        {
            playerInputVR = mainCameraVR.GetComponent<PlayerInput>();
            reticlePointerVR = mainCameraVR.GetComponent<VRReticlePointer>();

            if (reticlePointerVR == null)
            {
                reticlePointerVR = mainCameraVR.AddComponent<VRReticlePointer>();
                Debug.Log("[VRLOAD] Added VRReticlePointer to VR camera");
            }
        }
        else
        {
            Debug.LogError("[VRLOAD] Main CameraVR not assigned!");
        }

        // Legacy support
        if (vrReticleObject != null)
        {
            cameraController = vrReticleObject.GetComponent<CameraController>();
        }
    }

    /// <summary>
    /// Set VR mode directly (called by StartUp.cs)
    /// </summary>
    public void SetVRMode(bool enableVR)
    {
        Debug.Log($"[VRLOAD] === SetVRMode({enableVR}) ===");

        if (enableVR && !isXRActive)
        {
            Debug.Log("[VRLOAD] Starting VR mode...");
            StartCoroutine(StartXR());
        }
        else if (!enableVR && isXRActive)
        {
            Debug.Log("[VRLOAD] Stopping VR mode...");
            StopXR();
        }
        else if (!enableVR && !isXRActive)
        {
            Debug.Log("[VRLOAD] Setting 360 mode...");
            Set360Mode();
            EnableTouchControls();
        }
    }

    /// <summary>
    /// Toggle between VR and 360 modes
    /// </summary>
    public void SwitchingVR()
    {
        Debug.Log($"[VRLOAD] === SwitchingVR === Current: {(isXRActive ? "VR" : "360")}");

        VRLoadingManager loadingManager = VRLoadingManager.Instance;

        if (!isXRActive)
        {
            Debug.Log("[VRLOAD] Switching TO VR");
            if (loadingManager != null)
            {
                loadingManager.ShowSwitchToVR();
            }
            StartCoroutine(SwitchToVRWithLoading());
        }
        else
        {
            Debug.Log("[VRLOAD] Switching TO 360");
            if (loadingManager != null)
            {
                loadingManager.ShowSwitchTo360();
            }
            StartCoroutine(SwitchTo360WithLoading());
        }
    }

    /// <summary>
    /// Switch to VR with loading screen coordination
    /// </summary>
    private IEnumerator SwitchToVRWithLoading()
    {
        VRLoadingManager loadingManager = VRLoadingManager.Instance;

        if (loadingManager != null)
        {
            loadingManager.UpdateProgress(0.3f);
        }

        yield return StartXR();

        if (loadingManager != null)
        {
            loadingManager.UpdateProgress(1f);
            yield return new WaitForSeconds(0.3f);
            loadingManager.HideLoading();
        }
    }

    /// <summary>
    /// Switch to 360 with loading screen coordination
    /// </summary>
    private IEnumerator SwitchTo360WithLoading()
    {
        VRLoadingManager loadingManager = VRLoadingManager.Instance;

        if (loadingManager != null)
        {
            loadingManager.UpdateProgress(0.3f);
        }

        StopXR();
        yield return new WaitForEndOfFrame();

        if (loadingManager != null)
        {
            loadingManager.UpdateProgress(1f);
            yield return new WaitForSeconds(0.3f);
            loadingManager.HideLoading();
        }
    }

    /// <summary>
    /// OPTIMIZED: Start XR subsystems with editor mode handling
    /// </summary>
    public IEnumerator StartXR()
    {
        Debug.Log("[VRLOAD] ========== START XR ==========");

        if (isXRActive)
        {
            Debug.LogWarning("[VRLOAD] XR already active - skipping");
            yield break;
        }

        // Editor mode: Just switch cameras without XR
        if (isInEditorMode)
        {
            Debug.Log("[VRLOAD] Editor mode - switching cameras without XR initialization");
            SwitchToVRCamera();
            EnableGyroscope();
            DisableTouchControls();
            SetVRModeOnReticle();
            isXRActive = true;
            Debug.Log("[VRLOAD] ========== XR START COMPLETE (EDITOR MODE) ✓ ==========");
            yield break;
        }

        // Check XR settings
        if (XRGeneralSettings.Instance == null)
        {
            Debug.LogError("[VRLOAD] XRGeneralSettings.Instance is NULL!");
            Debug.LogError("[VRLOAD] Enable XR Plugin Management in Project Settings");
            yield break;
        }

        if (XRGeneralSettings.Instance.Manager == null)
        {
            Debug.LogError("[VRLOAD] XR Manager is NULL!");
            yield break;
        }

        // Initialize loader if needed
        if (XRGeneralSettings.Instance.Manager.activeLoader == null)
        {
            Debug.Log("[VRLOAD] Initializing XR loader...");
            yield return XRGeneralSettings.Instance.Manager.InitializeLoader();

            if (XRGeneralSettings.Instance.Manager.activeLoader == null)
            {
                Debug.LogError("[VRLOAD] Failed to initialize XR loader!");
                yield break;
            }

            Debug.Log($"[VRLOAD] ✓ Loader initialized: {XRGeneralSettings.Instance.Manager.activeLoader.GetType().Name}");
        }

        // Start subsystems
        Debug.Log("[VRLOAD] Starting XR subsystems...");
        XRGeneralSettings.Instance.Manager.StartSubsystems();

        // Brief delay for subsystems
        yield return new WaitForSeconds(0.1f);

        // Switch cameras
        SwitchToVRCamera();

        // Setup VR mode
        EnableGyroscope();
        DisableTouchControls();
        SetVRModeOnReticle();

        isXRActive = true;

        if (enableDebugLogging)
        {
            LogXRStatus();
        }

        Debug.Log("[VRLOAD] ========== XR START COMPLETE ✓ ==========");
    }

    /// <summary>
    /// OPTIMIZED: Stop XR subsystems
    /// </summary>
    public void StopXR()
    {
        Debug.Log("[VRLOAD] ========== STOP XR ==========");

        if (!isXRActive)
        {
            Debug.LogWarning("[VRLOAD] XR not active - skipping");
            return;
        }

        // Stop XR subsystems (only if not in editor)
        if (!isInEditorMode && XRGeneralSettings.Instance?.Manager?.activeLoader != null)
        {
            Debug.Log("[VRLOAD] Stopping XR subsystems...");
            XRGeneralSettings.Instance.Manager.StopSubsystems();
            Debug.Log("[VRLOAD] ✓ XR stopped");
        }

        // Switch cameras
        SwitchTo360Camera();

        EnableTouchControls();
        Set360Mode();

        isXRActive = false;
        Debug.Log("[VRLOAD] ========== XR STOP COMPLETE ✓ ==========");
    }

    /// <summary>
    /// Switch to VR camera and update canvas event camera
    /// </summary>
    private void SwitchToVRCamera()
    {
        if (mainCamera360 != null)
        {
            mainCamera360.SetActive(false);
            // Disable 360 reticle
            if (reticlePointer360 != null)
                reticlePointer360.enabled = false;
        }

        if (mainCameraVR != null)
        {
            mainCameraVR.SetActive(true);
            // Enable VR reticle
            if (reticlePointerVR != null)
                reticlePointerVR.enabled = true;
        }

        if (hudCanvas != null && mainCameraVR != null)
        {
            hudCanvas.worldCamera = mainCameraVR.GetComponent<Camera>();
            Debug.Log("[VRLOAD] ✓ Canvas event camera set to VR");
        }

        Debug.Log("[VRLOAD] ✓ Switched to VR camera");
    }

    /// <summary>
    /// Switch to 360 camera and update canvas event camera
    /// </summary>
    private void SwitchTo360Camera()
    {
        if (mainCameraVR != null)
        {
            mainCameraVR.SetActive(false);
            // Disable VR reticle
            if (reticlePointerVR != null)
                reticlePointerVR.enabled = false;
        }

        if (mainCamera360 != null)
        {
            mainCamera360.SetActive(true);
            // Enable 360 reticle
            if (reticlePointer360 != null)
                reticlePointer360.enabled = true;
        }

        if (hudCanvas != null && mainCamera360 != null)
        {
            hudCanvas.worldCamera = mainCamera360.GetComponent<Camera>();
            Debug.Log("[VRLOAD] ✓ Canvas event camera set to 360");
        }

        Debug.Log("[VRLOAD] ✓ Switched to 360 camera");
    }

    private void SetVRModeOnReticle()
    {
        if (reticlePointerVR != null)
        {
            reticlePointerVR.SetMode(VRReticlePointer.ViewMode.ModeVR);
            Debug.Log("[VRLOAD] ✓ VR Camera Reticle: ModeVR");
        }
        else if (reticlePointer360 != null)
        {
            reticlePointer360.SetMode(VRReticlePointer.ViewMode.ModeVR);
            Debug.Log("[VRLOAD] ✓ 360 Camera Reticle (fallback): ModeVR");
        }

        // Legacy support
        if (cameraController != null)
        {
            try
            {
                var method = cameraController.GetType().GetMethod("SetMode");
                if (method != null)
                {
                    method.Invoke(cameraController, new object[] { 2 });
                    Debug.Log("[VRLOAD] ✓ CameraController: ModeVR (legacy)");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[VRLOAD] Could not set mode on CameraController: {e.Message}");
            }
        }
    }

    private void Set360Mode()
    {
        if (reticlePointer360 != null)
        {
            reticlePointer360.SetMode(VRReticlePointer.ViewMode.Mode360);
            Debug.Log("[VRLOAD] ✓ 360 Camera Reticle: Mode360");
        }

        // Legacy support
        if (cameraController != null)
        {
            try
            {
                var method = cameraController.GetType().GetMethod("SetMode");
                if (method != null)
                {
                    method.Invoke(cameraController, new object[] { 1 });
                    Debug.Log("[VRLOAD] ✓ CameraController: Mode360 (legacy)");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[VRLOAD] Could not set mode on CameraController: {e.Message}");
            }
        }
    }

    private void DisableTouchControls()
    {
        if (playerInput360 != null)
        {
            playerInput360.enabled = false;
        }

        if (playerInputVR != null)
        {
            playerInputVR.enabled = false;
        }

        if (eventSystem != null)
        {
            eventSystem.enabled = false;
        }

        Debug.Log("[VRLOAD] ✓ Touch controls disabled");
    }

    public void EnableTouchControls()
    {
        if (playerInput360 != null)
        {
            playerInput360.enabled = true;
        }

        if (playerInputVR != null)
        {
            playerInputVR.enabled = false;
        }

        if (eventSystem != null)
        {
            eventSystem.enabled = true;
        }

        Debug.Log("[VRLOAD] ✓ Touch controls enabled");
    }

    private void EnableGyroscope()
    {
        if (SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;
            Debug.Log("[VRLOAD] ✓ Gyroscope enabled");
        }
        else if (!isInEditorMode)
        {
            Debug.LogWarning("[VRLOAD] Device does not support gyroscope");
        }
    }

    private void LogSystemInfo()
    {
        if (!enableDebugLogging) return;

        Debug.Log("[VRLOAD] === SYSTEM INFO ===");
        Debug.Log($"[VRLOAD] Device: {SystemInfo.deviceModel}");
        Debug.Log($"[VRLOAD] OS: {SystemInfo.operatingSystem}");
        Debug.Log($"[VRLOAD] Gyroscope: {SystemInfo.supportsGyroscope}");

        if (XRGeneralSettings.Instance != null && XRGeneralSettings.Instance.Manager != null)
        {
            if (XRGeneralSettings.Instance.Manager.activeLoader != null)
            {
                Debug.Log($"[VRLOAD] ✓ Active XR Loader: {XRGeneralSettings.Instance.Manager.activeLoader.GetType().Name}");
            }
            else
            {
                Debug.Log("[VRLOAD] No active XR loader");
            }
        }
    }

    private void LogXRStatus()
    {
        if (!enableDebugLogging || isInEditorMode) return;

        Debug.Log("[VRLOAD] === XR STATUS ===");

        var displays = new List<XRDisplaySubsystem>();
        SubsystemManager.GetSubsystems(displays);
        Debug.Log($"[VRLOAD] Display Subsystems: {displays.Count}");

        var devices = new List<UnityEngine.XR.InputDevice>();
        UnityEngine.XR.InputDevices.GetDevices(devices);
        Debug.Log($"[VRLOAD] Input Devices: {devices.Count}");

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            Debug.Log($"[VRLOAD] Main Camera: {mainCam.gameObject.name}");
            Debug.Log($"[VRLOAD] Stereo enabled: {mainCam.stereoEnabled}");
        }
    }

    /// <summary>
    /// Check if currently in VR mode
    /// </summary>
    public bool IsVRActive()
    {
        return isXRActive;
    }

    void OnDestroy()
    {
        Debug.Log("[VRLOAD] togglingXR destroyed");
        if (isXRActive && !isInEditorMode)
        {
            StopXR();
        }
    }
}