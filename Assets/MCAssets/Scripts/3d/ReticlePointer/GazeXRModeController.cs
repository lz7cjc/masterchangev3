using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

/// <summary>
/// VR/360 mode switching controller integrated with GazeReticlePointer system
/// Handles XR subsystem management, camera switching, and input configuration
/// Compatible with Google Cardboard and Unity's new Input System
/// </summary>
public class GazeXRModeController : MonoBehaviour
{
    public bool switchVRon;

    [Header("Camera References")]
    public GameObject mainCamera360;
    public GameObject mainCameraVR;

    [Header("Canvas Reference (Optional)")]
    [Tooltip("If using Unity UI Canvas, assign it here for automatic event camera switching")]
    public Canvas hudCanvas;

    [Header("Debug")]
    public bool enableDebugLogging = true;

    private GazeReticlePointer reticlePointer360;
    private GazeReticlePointer reticlePointerVR;
    private PlayerInput playerInput360;
    private PlayerInput playerInputVR;
    private EventSystem eventSystem;
    private bool isXRActive = false;
    private bool isInEditorMode = false;

    void Start()
    {
        Debug.Log("[GazeXR] === GazeXRModeController START ===");

        // Check if in editor
#if UNITY_EDITOR
        isInEditorMode = true;
        Debug.Log("[GazeXR] Running in Unity Editor - XR will be simulated");
#endif

        // Get EventSystem
        eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            Debug.LogWarning("[GazeXR] EventSystem not found!");
        }

        // Setup camera references
        SetupCameraReferences();

        if (!isInEditorMode)
        {
            LogSystemInfo();
        }

        Debug.Log("[GazeXR] Waiting for StartUp to set initial mode...");
    }

    /// <summary>
    /// Setup camera references and components
    /// </summary>
    private void SetupCameraReferences()
    {
        if (mainCamera360 != null)
        {
            playerInput360 = mainCamera360.GetComponent<PlayerInput>();
            reticlePointer360 = mainCamera360.GetComponent<GazeReticlePointer>();

            if (reticlePointer360 == null)
            {
                reticlePointer360 = mainCamera360.AddComponent<GazeReticlePointer>();
                Debug.Log("[GazeXR] Added GazeReticlePointer to 360 camera");
            }
        }
        else
        {
            Debug.LogError("[GazeXR] Main Camera360 not assigned!");
        }

        if (mainCameraVR != null)
        {
            playerInputVR = mainCameraVR.GetComponent<PlayerInput>();
            reticlePointerVR = mainCameraVR.GetComponent<GazeReticlePointer>();

            if (reticlePointerVR == null)
            {
                reticlePointerVR = mainCameraVR.AddComponent<GazeReticlePointer>();
                Debug.Log("[GazeXR] Added GazeReticlePointer to VR camera");
            }
        }
        else
        {
            Debug.LogError("[GazeXR] Main CameraVR not assigned!");
        }
    }

    /// <summary>
    /// Set VR mode directly (called by StartUp.cs)
    /// </summary>
    public void SetVRMode(bool enableVR)
    {
        Debug.Log($"[GazeXR] === SetVRMode({enableVR}) ===");

        if (enableVR && !isXRActive)
        {
            Debug.Log("[GazeXR] Starting VR mode...");
            StartCoroutine(StartXR());
        }
        else if (!enableVR && isXRActive)
        {
            Debug.Log("[GazeXR] Stopping VR mode...");
            StopXR();
        }
        else if (!enableVR && !isXRActive)
        {
            Debug.Log("[GazeXR] Setting 360 mode...");
            Set360Mode();
            EnableTouchControls();
        }
    }

    /// <summary>
    /// Toggle between VR and 360 modes
    /// </summary>
    public void SwitchingVR()
    {
        Debug.Log($"[GazeXR] === SwitchingVR === Current: {(isXRActive ? "VR" : "360")}");

        if (!isXRActive)
        {
            Debug.Log("[GazeXR] Switching TO VR");
            StartCoroutine(SwitchToVRWithLoading());
        }
        else
        {
            Debug.Log("[GazeXR] Switching TO 360");
            StartCoroutine(SwitchTo360WithLoading());
        }
    }

    /// <summary>
    /// Switch to VR with loading screen coordination
    /// </summary>
    private IEnumerator SwitchToVRWithLoading()
    {
        // Optional: Show loading screen here
        // VRLoadingManager.Instance?.ShowSwitchToVR();

        yield return StartXR();

        // Optional: Hide loading screen here
        // VRLoadingManager.Instance?.HideLoading();
    }

    /// <summary>
    /// Switch to 360 with loading screen coordination
    /// </summary>
    private IEnumerator SwitchTo360WithLoading()
    {
        // Optional: Show loading screen here
        // VRLoadingManager.Instance?.ShowSwitchTo360();

        StopXR();
        yield return new WaitForEndOfFrame();

        // Optional: Hide loading screen here
        // VRLoadingManager.Instance?.HideLoading();
    }

    /// <summary>
    /// Start XR subsystems with editor mode handling
    /// </summary>
    public IEnumerator StartXR()
    {
        Debug.Log("[GazeXR] ========== START XR ==========");

        if (isXRActive)
        {
            Debug.LogWarning("[GazeXR] XR already active - skipping");
            yield break;
        }

        // Editor mode: Just switch cameras without XR
        if (isInEditorMode)
        {
            Debug.Log("[GazeXR] Editor mode - switching cameras without XR initialization");
            SwitchToVRCamera();
            EnableGyroscope();
            DisableTouchControls();
            SetVRModeOnReticle();
            isXRActive = true;
            Debug.Log("[GazeXR] ========== XR START COMPLETE (EDITOR MODE) ✓ ==========");
            yield break;
        }

        // Check XR settings
        if (XRGeneralSettings.Instance == null)
        {
            Debug.LogError("[GazeXR] XRGeneralSettings.Instance is NULL!");
            Debug.LogError("[GazeXR] Enable XR Plugin Management in Project Settings");
            yield break;
        }

        if (XRGeneralSettings.Instance.Manager == null)
        {
            Debug.LogError("[GazeXR] XR Manager is NULL!");
            yield break;
        }

        // Initialize loader if needed
        if (XRGeneralSettings.Instance.Manager.activeLoader == null)
        {
            Debug.Log("[GazeXR] Initializing XR loader...");
            yield return XRGeneralSettings.Instance.Manager.InitializeLoader();

            if (XRGeneralSettings.Instance.Manager.activeLoader == null)
            {
                Debug.LogError("[GazeXR] Failed to initialize XR loader!");
                yield break;
            }

            Debug.Log($"[GazeXR] ✓ Loader initialized: {XRGeneralSettings.Instance.Manager.activeLoader.GetType().Name}");
        }

        // Start subsystems
        Debug.Log("[GazeXR] Starting XR subsystems...");
        XRGeneralSettings.Instance.Manager.StartSubsystems();

        // Small delay for subsystems to stabilize
        yield return new WaitForSeconds(0.2f);

        // Switch cameras
        SwitchToVRCamera();

        EnableGyroscope();
        DisableTouchControls();
        SetVRModeOnReticle();

        isXRActive = true;

        if (enableDebugLogging)
        {
            LogXRStatus();
        }

        Debug.Log("[GazeXR] ========== XR START COMPLETE ✓ ==========");
    }

    /// <summary>
    /// Stop XR subsystems
    /// </summary>
    public void StopXR()
    {
        Debug.Log("[GazeXR] ========== STOP XR ==========");

        if (!isXRActive)
        {
            Debug.LogWarning("[GazeXR] XR not active - skipping");
            return;
        }

        // Stop XR subsystems (only if not in editor)
        if (!isInEditorMode && XRGeneralSettings.Instance?.Manager?.activeLoader != null)
        {
            Debug.Log("[GazeXR] Stopping XR subsystems...");
            XRGeneralSettings.Instance.Manager.StopSubsystems();
            Debug.Log("[GazeXR] ✓ XR stopped");
        }

        // Switch cameras
        SwitchTo360Camera();

        EnableTouchControls();
        Set360Mode();

        isXRActive = false;
        Debug.Log("[GazeXR] ========== XR STOP COMPLETE ✓ ==========");
    }

    /// <summary>
    /// Switch to VR camera and update references
    /// </summary>
    private void SwitchToVRCamera()
    {
        if (mainCamera360 != null)
        {
            mainCamera360.SetActive(false);
            if (reticlePointer360 != null)
                reticlePointer360.enabled = false;
        }

        if (mainCameraVR != null)
        {
            mainCameraVR.SetActive(true);
            if (reticlePointerVR != null)
                reticlePointerVR.enabled = true;
        }

        // Update Canvas event camera if using Unity UI
        if (hudCanvas != null && mainCameraVR != null)
        {
            hudCanvas.worldCamera = mainCameraVR.GetComponent<Camera>();
            Debug.Log("[GazeXR] ✓ Canvas event camera set to VR");
        }

        Debug.Log("[GazeXR] ✓ Switched to VR camera");
    }

    /// <summary>
    /// Switch to 360 camera and update references
    /// </summary>
    private void SwitchTo360Camera()
    {
        if (mainCameraVR != null)
        {
            mainCameraVR.SetActive(false);
            if (reticlePointerVR != null)
                reticlePointerVR.enabled = false;
        }

        if (mainCamera360 != null)
        {
            mainCamera360.SetActive(true);
            if (reticlePointer360 != null)
                reticlePointer360.enabled = true;
        }

        // Update Canvas event camera if using Unity UI
        if (hudCanvas != null && mainCamera360 != null)
        {
            hudCanvas.worldCamera = mainCamera360.GetComponent<Camera>();
            Debug.Log("[GazeXR] ✓ Canvas event camera set to 360");
        }

        Debug.Log("[GazeXR] ✓ Switched to 360 camera");
    }

    /// <summary>
    /// Set VR mode on GazeReticlePointer
    /// </summary>
    private void SetVRModeOnReticle()
    {
        if (reticlePointerVR != null)
        {
            reticlePointerVR.SetMode(GazeReticlePointer.ViewMode.ModeVR);
            Debug.Log("[GazeXR] ✓ VR Camera Reticle: ModeVR");
        }
        else if (reticlePointer360 != null)
        {
            reticlePointer360.SetMode(GazeReticlePointer.ViewMode.ModeVR);
            Debug.Log("[GazeXR] ✓ 360 Camera Reticle (fallback): ModeVR");
        }
    }

    /// <summary>
    /// Set 360 mode on GazeReticlePointer
    /// </summary>
    private void Set360Mode()
    {
        if (reticlePointer360 != null)
        {
            reticlePointer360.SetMode(GazeReticlePointer.ViewMode.Mode360);
            Debug.Log("[GazeXR] ✓ 360 Camera Reticle: Mode360");
        }
    }

    /// <summary>
    /// Disable touch controls (VR mode)
    /// </summary>
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

        Debug.Log("[GazeXR] ✓ Touch controls disabled");
    }

    /// <summary>
    /// Enable touch controls (360 mode)
    /// </summary>
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

        Debug.Log("[GazeXR] ✓ Touch controls enabled");
    }

    /// <summary>
    /// Enable gyroscope
    /// </summary>
    private void EnableGyroscope()
    {
        if (SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;
            Debug.Log("[GazeXR] ✓ Gyroscope enabled");
        }
        else if (!isInEditorMode)
        {
            Debug.LogWarning("[GazeXR] Device does not support gyroscope");
        }
    }

    /// <summary>
    /// Log system info
    /// </summary>
    private void LogSystemInfo()
    {
        if (!enableDebugLogging) return;

        Debug.Log("[GazeXR] === SYSTEM INFO ===");
        Debug.Log($"[GazeXR] Device: {SystemInfo.deviceModel}");
        Debug.Log($"[GazeXR] OS: {SystemInfo.operatingSystem}");
        Debug.Log($"[GazeXR] Gyroscope: {SystemInfo.supportsGyroscope}");

        if (XRGeneralSettings.Instance != null && XRGeneralSettings.Instance.Manager != null)
        {
            if (XRGeneralSettings.Instance.Manager.activeLoader != null)
            {
                Debug.Log($"[GazeXR] ✓ Active XR Loader: {XRGeneralSettings.Instance.Manager.activeLoader.GetType().Name}");
            }
            else
            {
                Debug.Log("[GazeXR] No active XR loader");
            }
        }
    }

    /// <summary>
    /// Log XR status
    /// </summary>
    private void LogXRStatus()
    {
        if (!enableDebugLogging || isInEditorMode) return;

        Debug.Log("[GazeXR] === XR STATUS ===");

        var displays = new List<XRDisplaySubsystem>();
        SubsystemManager.GetSubsystems(displays);
        Debug.Log($"[GazeXR] Display Subsystems: {displays.Count}");

        var devices = new List<UnityEngine.XR.InputDevice>();
        UnityEngine.XR.InputDevices.GetDevices(devices);
        Debug.Log($"[GazeXR] Input Devices: {devices.Count}");

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            Debug.Log($"[GazeXR] Main Camera: {mainCam.gameObject.name}");
            Debug.Log($"[GazeXR] Stereo enabled: {mainCam.stereoEnabled}");
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
        Debug.Log("[GazeXR] GazeXRModeController destroyed");
        if (isXRActive && !isInEditorMode)
        {
            StopXR();
        }
    }
}
