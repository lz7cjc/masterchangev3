using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Universal XR mode manager - works across ALL scenes
/// Renamed from togglingXRFilm for clarity
/// Manages VR/360 mode switching with GazeReticlePointer system
/// </summary>
public class togglingXR : MonoBehaviour
{
    [Header("Camera References - REQUIRED")]
    [Tooltip("Your 360 degree camera")]
    public GameObject mainCamera360;
    
    [Tooltip("Your VR camera")]
    public GameObject mainCameraVR;

    [Header("Canvas Reference - OPTIONAL")]
    [Tooltip("Optional: Your HUD Canvas (can leave empty if using 3D UI)")]
    public Canvas hudCanvas;

    [Header("Debug")]
    public bool enableDebugLogging = true;

    // Internal references
    private GazeReticlePointer reticlePointer360;
    private GazeReticlePointer reticlePointerVR;
    private bool isXRActive = false;
    private bool isInEditorMode = false;

    void Start()
    {
        if (enableDebugLogging) Debug.Log("[togglingXR] Starting initialization");

        // Check if in editor
        #if UNITY_EDITOR
            isInEditorMode = true;
            if (enableDebugLogging) Debug.Log("[togglingXR] Running in Unity Editor");
        #endif

        // Validate required references
        if (!ValidateRequiredReferences())
        {
            Debug.LogError("[togglingXR] Missing required camera references! Check Inspector.");
            return;
        }

        // Canvas is optional now (for 3D UI)
        if (hudCanvas == null && enableDebugLogging)
        {
            Debug.Log("[togglingXR] No HUD Canvas assigned (optional - OK for 3D UI)");
        }

        // Get GazeReticlePointer components from cameras
        if (mainCamera360 != null)
        {
            reticlePointer360 = mainCamera360.GetComponent<GazeReticlePointer>();
            if (reticlePointer360 == null && enableDebugLogging)
            {
                Debug.LogWarning("[togglingXR] 360 camera missing GazeReticlePointer component");
            }
        }

        if (mainCameraVR != null)
        {
            reticlePointerVR = mainCameraVR.GetComponent<GazeReticlePointer>();
            if (reticlePointerVR == null && enableDebugLogging)
            {
                Debug.LogWarning("[togglingXR] VR camera missing GazeReticlePointer component");
            }
        }

        if (enableDebugLogging) Debug.Log("[togglingXR] Initialization complete");
    }

    /// <summary>
    /// Validate that required references are assigned
    /// Canvas is now optional (for 3D UI support)
    /// </summary>
    private bool ValidateRequiredReferences()
    {
        bool valid = true;

        if (mainCamera360 == null)
        {
            Debug.LogError("[togglingXR] Main Camera 360 is not assigned!");
            valid = false;
        }

        if (mainCameraVR == null)
        {
            Debug.LogError("[togglingXR] Main Camera VR is not assigned!");
            valid = false;
        }

        return valid;
    }

    /// <summary>
    /// Set VR mode - called by StartUp.cs
    /// </summary>
    /// <param name="enableVR">True for VR mode, False for 360 mode</param>
    public void SetVRMode(bool enableVR)
    {
        if (enableDebugLogging) Debug.Log($"[togglingXR] SetVRMode called: {enableVR}");

        if (enableVR)
        {
            StartCoroutine(StartXR());
        }
        else
        {
            StopXR();
        }
    }

    /// <summary>
    /// Start XR mode (VR)
    /// </summary>
    public IEnumerator StartXR()
    {
        if (enableDebugLogging) Debug.Log("[togglingXR] Starting XR mode");

        // Don't initialize XR in editor
        if (isInEditorMode)
        {
            if (enableDebugLogging) Debug.Log("[togglingXR] Skipping XR initialization in editor");
        }
        else
        {
            // Initialize XR on device
            if (XRGeneralSettings.Instance != null && XRGeneralSettings.Instance.Manager != null)
            {
                yield return XRGeneralSettings.Instance.Manager.InitializeLoader();

                if (XRGeneralSettings.Instance.Manager.activeLoader != null)
                {
                    XRGeneralSettings.Instance.Manager.StartSubsystems();
                    isXRActive = true;
                    if (enableDebugLogging) Debug.Log("[togglingXR] XR started successfully");
                }
                else
                {
                    Debug.LogError("[togglingXR] Failed to initialize XR loader");
                }
            }
        }

        // Switch to VR camera
        SetupVRMode();
        yield return null;
    }

    /// <summary>
    /// Stop XR mode (back to 360)
    /// </summary>
    public void StopXR()
    {
        if (enableDebugLogging) Debug.Log("[togglingXR] Stopping XR mode");

        if (!isInEditorMode && isXRActive)
        {
            if (XRGeneralSettings.Instance != null && XRGeneralSettings.Instance.Manager != null)
            {
                XRGeneralSettings.Instance.Manager.StopSubsystems();
                XRGeneralSettings.Instance.Manager.DeinitializeLoader();
                isXRActive = false;
                if (enableDebugLogging) Debug.Log("[togglingXR] XR stopped");
            }
        }

        // Switch to 360 camera
        Setup360Mode();
    }

    /// <summary>
    /// Setup VR mode - activate VR camera
    /// </summary>
    private void SetupVRMode()
    {
        if (mainCamera360 != null) mainCamera360.SetActive(false);
        if (mainCameraVR != null) mainCameraVR.SetActive(true);

        // Set reticle pointer mode
        if (reticlePointerVR != null)
        {
            reticlePointerVR.SetMode(GazeReticlePointer.ViewMode.ModeVR);
        }

        // Update canvas camera (optional - only if canvas exists)
        if (hudCanvas != null)
        {
            UpdateCanvasCamera(mainCameraVR);
        }

        if (enableDebugLogging) Debug.Log("[togglingXR] VR mode active");
    }

    /// <summary>
    /// Setup 360 mode - activate 360 camera
    /// </summary>
    private void Setup360Mode()
    {
        if (enableDebugLogging) Debug.Log("[togglingXR] Setup360Mode called");

        if (mainCameraVR != null) 
        {
            mainCameraVR.SetActive(false);
            if (enableDebugLogging) Debug.Log("[togglingXR] VR camera deactivated");
        }
        
        if (mainCamera360 != null) 
        {
            mainCamera360.SetActive(true);
            if (enableDebugLogging) Debug.Log("[togglingXR] 360 camera activated");
        }

        // Set reticle pointer mode
        if (reticlePointer360 != null)
        {
            if (enableDebugLogging) Debug.Log($"[togglingXR] Setting reticle pointer to Mode360, current mode: {reticlePointer360.currentMode}");
            reticlePointer360.SetMode(GazeReticlePointer.ViewMode.Mode360);
            if (enableDebugLogging) Debug.Log($"[togglingXR] Reticle pointer mode set, new mode: {reticlePointer360.currentMode}");
        }
        else
        {
            if (enableDebugLogging) Debug.LogWarning("[togglingXR] reticlePointer360 is NULL! Cannot set mode.");
        }

        // Update canvas camera (optional - only if canvas exists)
        if (hudCanvas != null)
        {
            UpdateCanvasCamera(mainCamera360);
        }

        // CRITICAL: Update InputSystemUIInputModule for 360 mode
        UpdateInputModuleFor360Mode();

        if (enableDebugLogging) Debug.Log("[togglingXR] 360 mode active");
    }

    /// <summary>
    /// Update canvas to render to the correct camera (optional - only if using Canvas-based UI)
    /// </summary>
    private void UpdateCanvasCamera(GameObject cameraObject)
    {
        if (hudCanvas != null && cameraObject != null)
        {
            Camera cam = cameraObject.GetComponent<Camera>();
            if (cam != null)
            {
                hudCanvas.worldCamera = cam;
                if (enableDebugLogging) Debug.Log($"[togglingXR] Canvas updated to camera: {cameraObject.name}");
            }
        }
    }

    /// <summary>
    /// Check if currently in VR mode
    /// </summary>
    public bool IsVRMode()
    {
        return mainCameraVR != null && mainCameraVR.activeSelf;
    }

    /// <summary>
    /// Check if currently in 360 mode
    /// </summary>
    public bool Is360Mode()
    {
        return mainCamera360 != null && mainCamera360.activeSelf;
    }

    /// <summary>
    /// Update InputSystemUIInputModule for 360 mode (mouse/touch controls)
    /// This is critical for making 360 mode work at startup
    /// </summary>
    private void UpdateInputModuleFor360Mode()
    {
        // Find the EventSystem in the scene
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            if (enableDebugLogging) Debug.LogWarning("[togglingXR] No EventSystem found in scene");
            return;
        }

        // Get the InputSystemUIInputModule
        InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
        if (inputModule == null)
        {
            if (enableDebugLogging) Debug.LogWarning("[togglingXR] No InputSystemUIInputModule found on EventSystem");
            return;
        }

        // Enable mouse and touch controls for 360 mode
        // The pointer behavior should be "Single Mouse Or Pen But Multi Touch"
        if (enableDebugLogging) 
        {
            Debug.Log("[togglingXR] Configuring InputSystemUIInputModule for 360 mode (mouse/touch)");
        }

        // The InputSystemUIInputModule should already be configured correctly by your PlayerControls.inputactions
        // But we need to make sure it's enabled and active
        inputModule.enabled = false; // Force refresh
        inputModule.enabled = true;

        if (enableDebugLogging)
        {
            Debug.Log($"[togglingXR] Input module refreshed - Pointer Behavior: {inputModule.pointerBehavior}");
        }
    }

    /// <summary>
    /// Get the currently active camera
    /// </summary>
    public GameObject GetActiveCamera()
    {
        if (IsVRMode()) return mainCameraVR;
        if (Is360Mode()) return mainCamera360;
        return null;
    }
}
