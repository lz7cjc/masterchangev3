using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.XR;

/// <summary>
/// UPDATED: Event System coordinator for GazeReticlePointer
/// Changed VRReticlePointer to GazeReticlePointer
/// </summary>
public class EventSystemCoordinator : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private EventSystem eventSystem;
    [SerializeField] private InputSystemUIInputModule uiInputModule;
    [SerializeField] private GazeReticlePointer gazeReticlePointer;  // CHANGED: was VRReticlePointer

    [Header("VR Settings")]
    [SerializeField] private Camera vrCamera;
    [SerializeField] private Camera regularCamera;

    private void Awake()
    {
        // Find components if not assigned
        if (eventSystem == null)
            eventSystem = FindFirstObjectByType<EventSystem>();

        if (uiInputModule == null)
            uiInputModule = FindFirstObjectByType<InputSystemUIInputModule>();

        if (gazeReticlePointer == null)
            gazeReticlePointer = FindFirstObjectByType<GazeReticlePointer>();  // CHANGED

        if (eventSystem == null)
        {
            Debug.LogError("[EventSystemCoordinator] No EventSystem found in scene!");
        }
    }

    private void Start()
    {
        ConfigureEventSystemForCurrentMode();
    }

    private void ConfigureEventSystemForCurrentMode()
    {
        int vrState = PlayerPrefs.GetInt("toggleToVR", 0);
        bool isVRMode = vrState == 1 && XRSettings.enabled;

        Debug.Log($"[EventSystemCoordinator] Configuring for VR mode: {isVRMode}");

        if (uiInputModule != null)
        {
            // Configure pointer behavior based on mode
            if (isVRMode)
            {
                // In VR, allow multi-touch and tracking but single mouse/pen
                uiInputModule.pointerBehavior = UIPointerBehavior.SingleMouseOrPenButMultiTouchAndTrack;

                // Set cursor lock behavior for VR
                uiInputModule.cursorLockBehavior = InputSystemUIInputModule.CursorLockBehavior.ScreenCenter;
            }
            else
            {
                // In 360 mode, use single unified pointer
                uiInputModule.pointerBehavior = UIPointerBehavior.SingleUnifiedPointer;

                // Set cursor lock behavior for 360 mode
                uiInputModule.cursorLockBehavior = InputSystemUIInputModule.CursorLockBehavior.OutsideScreen;
            }

            Debug.Log($"[EventSystemCoordinator] UI Module pointer behavior: {uiInputModule.pointerBehavior}");
        }

        // Update the gaze reticle pointer mode
        if (gazeReticlePointer != null)
        {
            if (isVRMode)
            {
                gazeReticlePointer.SetMode(GazeReticlePointer.ViewMode.ModeVR);
            }
            else
            {
                gazeReticlePointer.SetMode(GazeReticlePointer.ViewMode.Mode360);
            }
        }
    }

    // Call this method when switching between VR and 360 modes
    public void OnVRModeChanged(bool isVRMode)
    {
        Debug.Log($"[EventSystemCoordinator] VR mode changed to: {isVRMode}");

        // Small delay to let XR system stabilize
        Invoke(nameof(ConfigureEventSystemForCurrentMode), 0.2f);
    }

    // Method to be called by other systems when VR state changes
    public void RefreshConfiguration()
    {
        ConfigureEventSystemForCurrentMode();
    }

    private void OnValidate()
    {
        // Auto-assign components in editor
        if (eventSystem == null)
            eventSystem = FindFirstObjectByType<EventSystem>();

        if (uiInputModule == null)
            uiInputModule = FindFirstObjectByType<InputSystemUIInputModule>();

        if (gazeReticlePointer == null)
            gazeReticlePointer = FindFirstObjectByType<GazeReticlePointer>();
    }
}