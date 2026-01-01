using UnityEngine;

/// <summary>
/// ModeBasedUIController - Shows/hides UI elements based on VR/360 mode
/// Attach this to a GameObject in your scene (e.g., on your HUD or a manager object)
/// 
/// Usage:
/// 1. Assign rotation buttons to vrOnlyElements array
/// 2. Optionally assign any 360-only elements to mode360OnlyElements array
/// 3. Script automatically shows/hides based on current mode
/// </summary>
public class ModeBasedUIController : MonoBehaviour
{
    [Header("VR Mode Elements")]
    [Tooltip("UI elements that should ONLY show in VR mode (e.g., rotation buttons)")]
    public GameObject[] vrOnlyElements;

    [Header("360 Mode Elements")]
    [Tooltip("UI elements that should ONLY show in 360 mode (optional)")]
    public GameObject[] mode360OnlyElements;

    [Header("Settings")]
    [Tooltip("Check mode every frame (disable if you only switch modes at startup)")]
    public bool updateEveryFrame = false;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private bool lastKnownVRState = false;
    private bool hasInitialized = false;

    void Start()
    {
        // Initial setup
        UpdateUIVisibility();
        hasInitialized = true;
    }

    void Update()
    {
        if (!updateEveryFrame && hasInitialized) return;

        bool currentVRState = IsVRMode();
        
        // Only update if mode changed (optimization)
        if (currentVRState != lastKnownVRState || !hasInitialized)
        {
            UpdateUIVisibility();
            lastKnownVRState = currentVRState;
        }
    }

    /// <summary>
    /// Manually trigger UI update (call this if you switch modes at runtime)
    /// </summary>
    public void RefreshUI()
    {
        UpdateUIVisibility();
    }

    private void UpdateUIVisibility()
    {
        bool isVR = IsVRMode();

        if (showDebugLogs)
        {
            Debug.Log($"[ModeBasedUIController] Current mode: {(isVR ? "VR" : "360")}");
        }

        // Show/hide VR-only elements (rotation buttons, etc.)
        if (vrOnlyElements != null)
        {
            foreach (GameObject element in vrOnlyElements)
            {
                if (element != null)
                {
                    element.SetActive(isVR);
                    if (showDebugLogs)
                    {
                        Debug.Log($"[ModeBasedUIController] VR element '{element.name}' set to: {isVR}");
                    }
                }
            }
        }

        // Show/hide 360-only elements
        if (mode360OnlyElements != null)
        {
            foreach (GameObject element in mode360OnlyElements)
            {
                if (element != null)
                {
                    element.SetActive(!isVR);
                    if (showDebugLogs)
                    {
                        Debug.Log($"[ModeBasedUIController] 360 element '{element.name}' set to: {!isVR}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Check current mode from PlayerPrefs
    /// Returns true if VR mode, false if 360 mode
    /// </summary>
    private bool IsVRMode()
    {
        return PlayerPrefs.GetInt("toggleToVR", 0) == 1;
    }

    /// <summary>
    /// Public method to check current mode
    /// </summary>
    public bool GetIsVRMode()
    {
        return IsVRMode();
    }

    /// <summary>
    /// Alternative: Get mode from active camera's GazeReticlePointer
    /// More reliable if mode can change without PlayerPrefs update
    /// </summary>
    private bool IsVRModeFromCamera()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) return false;

        GazeReticlePointer pointer = mainCam.GetComponent<GazeReticlePointer>();
        if (pointer == null) return false;

        return pointer.currentMode == GazeReticlePointer.ViewMode.ModeVR;
    }
}
