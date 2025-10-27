using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

/// <summary>
/// OPTIMIZED: XR management with unified debug keyword [VRLOAD]
/// - Cleaner debug logging
/// - Better coordination with loading screens
/// - Faster initialization
/// </summary>
public class togglingXR : MonoBehaviour
{
    public bool switchVRon;

    [Header("VR Components")]
    public GameObject vrReticleObject;

    [Header("Camera References")]
    public GameObject mainCamera360;
    public GameObject mainCameraVR;

    [Header("Debug")]
    public bool enableDebugLogging = true;

    private CustomReticlePointer reticlePointer;
    private CameraController cameraController;
    private PlayerInput playerInput360;
    private PlayerInput playerInputVR;
    private EventSystem eventSystem;
    private bool isXRActive = false;

    void Start()
    {
        Debug.Log("[VRLOAD] === togglingXR START ===");
        Debug.Log($"[VRLOAD] Initial switchVRon: {switchVRon}");

        // Get EventSystem reference
        eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            Debug.LogWarning("[VRLOAD] EventSystem not found!");
        }

        // Get camera references
        if (mainCamera360 != null)
        {
            playerInput360 = mainCamera360.GetComponent<PlayerInput>();
            if (playerInput360 != null)
            {
                Debug.Log("[VRLOAD] ✓ PlayerInput found on 360 camera");
            }
        }
        else
        {
            Debug.LogError("[VRLOAD] Main Camera360 not assigned!");
        }

        if (mainCameraVR != null)
        {
            playerInputVR = mainCameraVR.GetComponent<PlayerInput>();
        }
        else
        {
            Debug.LogError("[VRLOAD] Main CameraVR not assigned!");
        }

        // Get VR reticle components
        if (vrReticleObject != null)
        {
            reticlePointer = vrReticleObject.GetComponent<CustomReticlePointer>();
            cameraController = vrReticleObject.GetComponent<CameraController>();

            if (reticlePointer == null)
            {
                Debug.LogError("[VRLOAD] CustomReticlePointer not found on VR Reticle!");
            }
        }
        else
        {
            Debug.LogError("[VRLOAD] VR Reticle Object not assigned!");
        }

        LogSystemInfo();
        Debug.Log("[VRLOAD] Waiting for StartUp to set initial mode...");
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
        else
        {
            Debug.Log($"[VRLOAD] Already in desired mode");
        }
    }

    /// <summary>
    /// Toggle between VR and 360 modes
    /// </summary>
    public void SwitchingVR()
    {
        Debug.Log($"[VRLOAD] === SwitchingVR === Current: {(isXRActive ? "VR" : "360")}");

        if (!isXRActive)
        {
            Debug.Log("[VRLOAD] Switching TO VR");
            StartCoroutine(StartXR());
        }
        else
        {
            Debug.Log("[VRLOAD] Switching TO 360");
            StopXR();
        }
    }

    /// <summary>
    /// OPTIMIZED: Start XR subsystems with better logging
    /// </summary>
    public IEnumerator StartXR()
    {
        Debug.Log("[VRLOAD] ========== START XR ==========");

        if (isXRActive)
        {
            Debug.LogWarning("[VRLOAD] XR already active - skipping");
            yield break;
        }

        // Check XR settings
        if (XRGeneralSettings.Instance == null)
        {
            Debug.LogError("[VRLOAD] XRGeneralSettings.Instance is NULL!");
            Debug.LogError("[VRLOAD] Enable XR Plugin Management in Project Settings");
            yield break;
        }

        Debug.Log("[VRLOAD] ✓ XRGeneralSettings exists");

        if (XRGeneralSettings.Instance.Manager == null)
        {
            Debug.LogError("[VRLOAD] XR Manager is NULL!");
            yield break;
        }

        Debug.Log("[VRLOAD] ✓ XR Manager exists");

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
        else
        {
            Debug.Log($"[VRLOAD] ✓ Loader already active: {XRGeneralSettings.Instance.Manager.activeLoader.GetType().Name}");
        }

        // Start subsystems
        Debug.Log("[VRLOAD] Starting XR subsystems...");
        XRGeneralSettings.Instance.Manager.StartSubsystems();

        // Brief delay for subsystems to start
        yield return new WaitForSeconds(0.2f);

        // Setup VR mode
        EnableGyroscope();
        DisableTouchControls();
        SetVRMode();

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

        if (XRGeneralSettings.Instance?.Manager?.activeLoader != null)
        {
            Debug.Log("[VRLOAD] Stopping XR subsystems...");
            XRGeneralSettings.Instance.Manager.StopSubsystems();

            Debug.Log("[VRLOAD] Deinitializing XR loader...");
            XRGeneralSettings.Instance.Manager.DeinitializeLoader();

            Debug.Log("[VRLOAD] ✓ XR stopped");
        }

        EnableTouchControls();
        Set360Mode();

        isXRActive = false;
        Debug.Log("[VRLOAD] ========== XR STOP COMPLETE ✓ ==========");
    }

    private void SetVRMode()
    {
        if (reticlePointer != null)
        {
            reticlePointer.SetMode(CustomReticlePointer.ViewMode.ModeVR);
            Debug.Log("[VRLOAD] ✓ Reticle: ModeVR");
        }
        else if (cameraController != null)
        {
            cameraController.SetMode(CustomReticlePointer.ViewMode.ModeVR);
            Debug.Log("[VRLOAD] ✓ CameraController: ModeVR");
        }
    }

    private void Set360Mode()
    {
        if (reticlePointer != null)
        {
            reticlePointer.SetMode(CustomReticlePointer.ViewMode.Mode360);
            Debug.Log("[VRLOAD] ✓ Reticle: Mode360");
        }
        else if (cameraController != null)
        {
            cameraController.SetMode(CustomReticlePointer.ViewMode.Mode360);
            Debug.Log("[VRLOAD] ✓ CameraController: Mode360");
        }
    }

    private void DisableTouchControls()
    {
        Debug.Log("[VRLOAD] Disabling touch controls...");

        if (playerInput360 != null)
        {
            playerInput360.enabled = false;
            Debug.Log("[VRLOAD] ✓ 360 camera input disabled");
        }

        if (playerInputVR != null)
        {
            playerInputVR.enabled = false;
            Debug.Log("[VRLOAD] ✓ VR camera input disabled");
        }

        if (eventSystem != null)
        {
            eventSystem.enabled = false;
            Debug.Log("[VRLOAD] ✓ EventSystem disabled");
        }

        Debug.Log("[VRLOAD] ✓✓✓ Touch controls disabled ✓✓✓");
    }

    public void EnableTouchControls()
    {
        Debug.Log("[VRLOAD] Enabling touch controls...");

        if (playerInput360 != null)
        {
            playerInput360.enabled = true;
            Debug.Log("[VRLOAD] ✓ 360 camera input enabled");
        }

        if (playerInputVR != null)
        {
            playerInputVR.enabled = false;
        }

        if (eventSystem != null)
        {
            eventSystem.enabled = true;
            Debug.Log("[VRLOAD] ✓ EventSystem enabled");
        }

        Debug.Log("[VRLOAD] ✓✓✓ Touch controls enabled ✓✓✓");
    }

    private void EnableGyroscope()
    {
        if (SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;
            Debug.Log("[VRLOAD] ✓ Gyroscope enabled");
        }
        else
        {
            Debug.LogError("[VRLOAD] Device does NOT support gyroscope!");
        }
    }

    private void LogSystemInfo()
    {
        if (!enableDebugLogging) return;

        Debug.Log("[VRLOAD] === SYSTEM INFO ===");
        Debug.Log($"[VRLOAD] Device: {SystemInfo.deviceModel}");
        Debug.Log($"[VRLOAD] OS: {SystemInfo.operatingSystem}");
        Debug.Log($"[VRLOAD] Gyroscope: {SystemInfo.supportsGyroscope}");
        Debug.Log($"[VRLOAD] Graphics: {SystemInfo.graphicsDeviceType}");

        Debug.Log("[VRLOAD] === XR CONFIGURATION ===");
        if (XRGeneralSettings.Instance != null)
        {
            Debug.Log("[VRLOAD] ✓ XRGeneralSettings exists");
            if (XRGeneralSettings.Instance.Manager != null)
            {
                Debug.Log("[VRLOAD] ✓ XR Manager exists");
                if (XRGeneralSettings.Instance.Manager.activeLoader != null)
                {
                    Debug.Log($"[VRLOAD] ✓ Active Loader: {XRGeneralSettings.Instance.Manager.activeLoader.GetType().Name}");
                }
                else
                {
                    Debug.LogWarning("[VRLOAD] ⚠ No active loader yet");
                }
            }
            else
            {
                Debug.LogError("[VRLOAD] ✗ XR Manager NULL - Configure in Project Settings!");
            }
        }
        else
        {
            Debug.LogError("[VRLOAD] ✗ XRGeneralSettings NULL - Enable XR Plugin Management!");
        }
    }

    private void LogXRStatus()
    {
        if (!enableDebugLogging) return;

        Debug.Log("[VRLOAD] === XR STATUS ===");

        var displays = new List<XRDisplaySubsystem>();
        SubsystemManager.GetSubsystems(displays);
        Debug.Log($"[VRLOAD] Display Subsystems: {displays.Count}");
        foreach (var display in displays)
        {
            Debug.Log($"[VRLOAD]   - Running: {display.running}");
        }

        var devices = new List<UnityEngine.XR.InputDevice>();
        UnityEngine.XR.InputDevices.GetDevices(devices);
        Debug.Log($"[VRLOAD] Input Devices: {devices.Count}");

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            Debug.Log($"[VRLOAD] Main Camera: {mainCam.gameObject.name}");
            Debug.Log($"[VRLOAD] Stereo enabled: {mainCam.stereoEnabled}");
        }

        if (SystemInfo.supportsGyroscope)
        {
            Debug.Log($"[VRLOAD] Gyroscope enabled: {Input.gyro.enabled}");
        }
    }

    void OnDestroy()
    {
        Debug.Log("[VRLOAD] togglingXR destroyed");
        if (isXRActive)
        {
            StopXR();
        }
    }
}