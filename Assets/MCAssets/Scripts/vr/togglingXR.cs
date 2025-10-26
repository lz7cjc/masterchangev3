using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class togglingXR : MonoBehaviour
{
    public bool switchVRon;

    [Header("VR Components")]
    public GameObject vrReticleObject;

    [Header("Camera References")]
    public GameObject mainCamera360; // 360 mode camera (with touch controls)
    public GameObject mainCameraVR;  // VR mode camera (with head tracking)

    [Header("Debug")]
    public bool enableDebugLogging = true;

    private CustomReticlePointer reticlePointer;
    private CameraController cameraController;
    private PlayerInput playerInput360; // Touch controls for 360 mode
    private PlayerInput playerInputVR;  // Should be disabled in VR mode
    private EventSystem eventSystem;
    private bool isXRActive = false;

    void Start()
    {
        Debug.Log("=== [togglingXR] START ===");
        Debug.Log($"[togglingXR] Initial switchVRon: {switchVRon}");

        // Get EventSystem reference
        eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            Debug.LogWarning("[togglingXR] EventSystem not found!");
        }

        // Get camera references and their PlayerInput components
        if (mainCamera360 != null)
        {
            playerInput360 = mainCamera360.GetComponent<PlayerInput>();
            if (playerInput360 != null)
            {
                Debug.Log($"[togglingXR] Found PlayerInput on Main Camera360 (360 camera)");
            }
            else
            {
                Debug.LogWarning("[togglingXR] Main Camera360 does NOT have PlayerInput component!");
            }
        }
        else
        {
            Debug.LogError("[togglingXR] Main Camera360 not assigned in Inspector!");
        }

        if (mainCameraVR != null)
        {
            playerInputVR = mainCameraVR.GetComponent<PlayerInput>();
            if (playerInputVR != null)
            {
                Debug.Log($"[togglingXR] Found PlayerInput on Main CameraVR (VR camera)");
            }
        }
        else
        {
            Debug.LogError("[togglingXR] Main CameraVR not assigned in Inspector!");
        }

        // Validate VR Reticle Object
        if (vrReticleObject == null)
        {
            Debug.LogError("[togglingXR] VR Reticle Object not assigned!");
        }
        else
        {
            Debug.Log($"[togglingXR] VR Reticle Object assigned: {vrReticleObject.name}");

            reticlePointer = vrReticleObject.GetComponent<CustomReticlePointer>();
            if (reticlePointer == null)
            {
                Debug.LogError($"[togglingXR] VR Reticle Object '{vrReticleObject.name}' does NOT have CustomReticlePointer component!");
            }
            else
            {
                Debug.Log("[togglingXR] ✓ CustomReticlePointer found on VR Reticle Object");
            }

            cameraController = vrReticleObject.GetComponent<CameraController>();
            if (cameraController != null)
            {
                Debug.Log("[togglingXR] ✓ CameraController found on VR Reticle Object");
            }
        }

        LogSystemInfo();

        // Don't auto-start here - let StartUp.cs call SetVRMode()
        Debug.Log("[togglingXR] Waiting for StartUp to set initial mode...");
    }

    /// <summary>
    /// NEW METHOD: Set VR mode directly (called by StartUp.cs)
    /// </summary>
    public void SetVRMode(bool enableVR)
    {
        Debug.Log($"[togglingXR] === SetVRMode called with enableVR={enableVR} ===");

        if (enableVR && !isXRActive)
        {
            Debug.Log("[togglingXR] Starting VR mode...");
            StartCoroutine(StartXR());
        }
        else if (!enableVR && isXRActive)
        {
            Debug.Log("[togglingXR] Stopping VR mode...");
            StopXR();
        }
        else if (!enableVR && !isXRActive)
        {
            Debug.Log("[togglingXR] Setting 360 mode...");
            Set360Mode();
            EnableTouchControls();
        }
        else
        {
            Debug.Log($"[togglingXR] Already in desired mode (isXRActive={isXRActive})");
        }
    }

    /// <summary>
    /// Toggle between VR and 360 modes (called by UI button)
    /// </summary>
    public void SwitchingVR()
    {
        Debug.Log($"[togglingXR] === SwitchingVR called === Current state: {(isXRActive ? "VR" : "360")}");

        if (!isXRActive)
        {
            Debug.Log("[togglingXR] Switching TO VR mode");
            StartCoroutine(StartXR());
        }
        else
        {
            Debug.Log("[togglingXR] Switching TO 360 mode");
            StopXR();
        }
    }

    public IEnumerator StartXR()
    {
        Debug.Log("[togglingXR] ========== START XR SEQUENCE ==========");

        if (isXRActive)
        {
            Debug.LogWarning("[togglingXR] XR already active! Skipping StartXR");
            yield break;
        }

        if (XRGeneralSettings.Instance == null)
        {
            Debug.LogError("[togglingXR] XRGeneralSettings.Instance is NULL!");
            yield break;
        }

        Debug.Log("[togglingXR] ✓ XRGeneralSettings.Instance exists");

        if (XRGeneralSettings.Instance.Manager == null)
        {
            Debug.LogError("[togglingXR] XRGeneralSettings.Instance.Manager is NULL!");
            yield break;
        }

        Debug.Log("[togglingXR] ✓ XR Manager exists");

        Debug.Log("[togglingXR] Calling InitializeLoader()...");
        yield return XRGeneralSettings.Instance.Manager.InitializeLoader();

        if (XRGeneralSettings.Instance.Manager.activeLoader == null)
        {
            Debug.LogError("[togglingXR] Failed to initialize XR Loader!");
            yield break;
        }

        Debug.Log($"[togglingXR] ✓ XR Loader initialized: {XRGeneralSettings.Instance.Manager.activeLoader.GetType().Name}");

        Debug.Log("[togglingXR] Starting XR subsystems...");
        XRGeneralSettings.Instance.Manager.StartSubsystems();

        yield return new WaitForSeconds(0.3f);

        LogXRStatus();
        EnableGyroscope();
        DisableTouchControls();
        SetVRMode();

        isXRActive = true;
        Debug.Log("[togglingXR] ========== XR START COMPLETE ==========");
    }

    public void StopXR()
    {
        Debug.Log("[togglingXR] ========== STOP XR SEQUENCE ==========");

        if (!isXRActive)
        {
            Debug.LogWarning("[togglingXR] XR not active! Skipping StopXR");
            return;
        }

        if (XRGeneralSettings.Instance?.Manager?.activeLoader != null)
        {
            Debug.Log("[togglingXR] Stopping XR subsystems...");
            XRGeneralSettings.Instance.Manager.StopSubsystems();

            Debug.Log("[togglingXR] Deinitializing XR loader...");
            XRGeneralSettings.Instance.Manager.DeinitializeLoader();

            Debug.Log("[togglingXR] ✓ XR stopped and deinitialized");
        }

        EnableTouchControls();
        Set360Mode();

        isXRActive = false;
        Debug.Log("[togglingXR] ========== XR STOP COMPLETE ==========");
    }

    private void SetVRMode()
    {
        Debug.Log("[togglingXR] Setting VR mode on reticle pointer...");

        if (reticlePointer != null)
        {
            reticlePointer.SetMode(CustomReticlePointer.ViewMode.ModeVR);
            Debug.Log("[togglingXR] ✓ Reticle set to ModeVR");
        }
        else if (cameraController != null)
        {
            cameraController.SetMode(CustomReticlePointer.ViewMode.ModeVR);
            Debug.Log("[togglingXR] ✓ CameraController set to ModeVR");
        }
    }

    private void Set360Mode()
    {
        Debug.Log("[togglingXR] Setting 360 mode on reticle pointer...");

        if (reticlePointer != null)
        {
            reticlePointer.SetMode(CustomReticlePointer.ViewMode.Mode360);
            Debug.Log("[togglingXR] ✓ Reticle set to Mode360");
        }
        else if (cameraController != null)
        {
            cameraController.SetMode(CustomReticlePointer.ViewMode.Mode360);
            Debug.Log("[togglingXR] ✓ CameraController set to Mode360");
        }
    }

    private void DisableTouchControls()
    {
        Debug.Log("[togglingXR] === Disabling touch controls for VR mode ===");

        // Disable PlayerInput on both cameras
        if (playerInput360 != null)
        {
            playerInput360.enabled = false;
            Debug.Log("[togglingXR] ✓ PlayerInput on Main Camera360 DISABLED");
        }

        if (playerInputVR != null)
        {
            playerInputVR.enabled = false;
            Debug.Log("[togglingXR] ✓ PlayerInput on Main CameraVR DISABLED");
        }

        // CRITICAL: Disable EventSystem to completely block touch input
        if (eventSystem != null)
        {
            eventSystem.enabled = false;
            Debug.Log("[togglingXR] ✓ EventSystem DISABLED - touch input completely blocked");
        }

        Debug.Log("[togglingXR] ✓✓✓ ALL TOUCH CONTROLS DISABLED - VR mode active ✓✓✓");
    }

    private void EnableTouchControls()
    {
        Debug.Log("[togglingXR] === Enabling touch controls for 360 mode ===");

        // Enable PlayerInput on 360 camera
        if (playerInput360 != null)
        {
            playerInput360.enabled = true;
            Debug.Log("[togglingXR] ✓ PlayerInput on Main Camera360 ENABLED");
        }

        // Keep PlayerInput on VR camera disabled
        if (playerInputVR != null)
        {
            playerInputVR.enabled = false;
            Debug.Log("[togglingXR] PlayerInput on Main CameraVR remains DISABLED");
        }

        // CRITICAL: Re-enable EventSystem for touch input
        if (eventSystem != null)
        {
            eventSystem.enabled = true;
            Debug.Log("[togglingXR] ✓ EventSystem ENABLED - touch input restored");
        }

        Debug.Log("[togglingXR] ✓✓✓ TOUCH CONTROLS ENABLED for 360 mode ✓✓✓");
    }

    private void EnableGyroscope()
    {
        if (SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;
            Debug.Log("[togglingXR] ✓ Gyroscope ENABLED");
        }
        else
        {
            Debug.LogError("[togglingXR] Device does NOT support gyroscope!");
        }
    }

    private void LogSystemInfo()
    {
        if (!enableDebugLogging) return;

        Debug.Log("=== [togglingXR] SYSTEM INFO ===");
        Debug.Log($"Device: {SystemInfo.deviceModel}");
        Debug.Log($"OS: {SystemInfo.operatingSystem}");
        Debug.Log($"Gyroscope supported: {SystemInfo.supportsGyroscope}");
        Debug.Log($"Graphics API: {SystemInfo.graphicsDeviceType}");
    }

    private void LogXRStatus()
    {
        if (!enableDebugLogging) return;

        Debug.Log("=== [togglingXR] XR STATUS ===");

        var displays = new List<XRDisplaySubsystem>();
        SubsystemManager.GetSubsystems(displays);
        Debug.Log($"XR Display Subsystems: {displays.Count}");
        foreach (var display in displays)
        {
            Debug.Log($"  - Display running: {display.running}");
        }

        var devices = new List<UnityEngine.XR.InputDevice>();
        UnityEngine.XR.InputDevices.GetDevices(devices);
        Debug.Log($"XR Input Devices: {devices.Count}");

        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            Debug.Log($"Main Camera: {mainCam.gameObject.name}");
            Debug.Log($"  Stereo enabled: {mainCam.stereoEnabled}");
        }

        if (SystemInfo.supportsGyroscope)
        {
            Debug.Log($"Gyroscope enabled: {Input.gyro.enabled}");
        }
    }

    void OnDestroy()
    {
        Debug.Log("[togglingXR] OnDestroy called");
        if (isXRActive)
        {
            StopXR();
        }
    }
}