using System.Collections;
using UnityEngine;
using UnityEngine.XR.Management;

public class togglingXR : MonoBehaviour
{
    public bool switchVRon;
    private int vrSetOn;

    [Header("VR Components")]
    [SerializeField] private GameObject vrReticleObject; // Your GameObject with VRReticlePointer script

    private VRReticlePointer vrReticlePointer;
    private bool isInitializing = false;

    public void Start()
    {
        Debug.Log($"[togglingXR] Start method called. switchVRon: {switchVRon}");

        // Get the VRReticlePointer component
        if (vrReticleObject != null)
        {
            vrReticlePointer = vrReticleObject.GetComponent<VRReticlePointer>();
            if (vrReticlePointer == null)
            {
                Debug.LogWarning("[togglingXR] VRReticlePointer component not found on assigned GameObject!");
            }
        }
        else
        {
            Debug.LogWarning("[togglingXR] VR Reticle Object not assigned in Inspector!");
        }

        // Check PlayerPrefs for VR state
        int vrState = PlayerPrefs.GetInt("toggleToVR", 0);
        Debug.Log($"[togglingXR] PlayerPrefs toggleToVR at Start: {vrState}");

        if (vrState == 1)
        {
            Debug.Log("[togglingXR] Starting XR from Start method based on PlayerPrefs");
            StartCoroutine(StartXR());
        }
        else
        {
            Debug.Log("[togglingXR] Not starting XR - staying in 2D/360 mode");
            // Ensure VR is fully stopped
            if (XRGeneralSettings.Instance?.Manager?.activeLoader != null)
            {
                StopXR();
            }

            // Don't hide the reticle object - just set it to Mode360
            // This preserves your existing 360 degree navigation
            if (vrReticlePointer != null)
            {
                vrReticlePointer.SetMode(VRReticlePointer.ViewMode.Mode360);
                Debug.Log("[togglingXR] VRReticlePointer set to 360 mode");
            }
        }
    }

    public void SwitchingVR()
    {
        vrSetOn = PlayerPrefs.GetInt("toggleToVR");
        Debug.Log($"[togglingXR] SwitchingVR called. toggleToVR: {vrSetOn}");

        if (isInitializing)
        {
            Debug.LogWarning("[togglingXR] Already initializing/deinitializing XR, ignoring request");
            return;
        }

        if (vrSetOn == 1)
        {
            Debug.Log("[togglingXR] Starting XR mode.");
            StartCoroutine(StartXR());
        }
        else
        {
            Debug.Log("[togglingXR] Stopping XR mode. Going back to 360 mode.");
            StopXR();
        }
    }

    public IEnumerator StartXR()
    {
        if (isInitializing)
        {
            Debug.LogWarning("[togglingXR] StartXR already in progress");
            yield break;
        }

        isInitializing = true;
        Debug.Log("[togglingXR] Attempting to start XR...");
        var startTime = Time.realtimeSinceStartup;

        ShowLoadingScreen();

        // Wait a frame to ensure everything is ready
        yield return null;

        // Check if XRGeneralSettings exists
        if (XRGeneralSettings.Instance == null)
        {
            Debug.LogError("[togglingXR] XRGeneralSettings.Instance is null! XR Plugin Management may not be configured.");
            Debug.LogError("[togglingXR] Please ensure XR Plugin Management is installed and configured for Android:");
            Debug.LogError("[togglingXR] Edit > Project Settings > XR Plug-in Management > Android tab > Enable your XR plugin");
            HideLoadingScreen();
            isInitializing = false;
            yield break;
        }

        if (XRGeneralSettings.Instance.Manager == null)
        {
            Debug.LogError("[togglingXR] XRGeneralSettings.Instance.Manager is null!");
            Debug.LogError("[togglingXR] XR Loader may not be properly set up.");
            HideLoadingScreen();
            isInitializing = false;
            yield break;
        }

        // Check if XR is already running
        if (XRGeneralSettings.Instance.Manager.activeLoader != null)
        {
            Debug.Log("[togglingXR] XR is already running");

            // Set to VR mode
            if (vrReticlePointer != null)
            {
                vrReticlePointer.SetMode(VRReticlePointer.ViewMode.ModeVR);
                Debug.Log("[togglingXR] VRReticlePointer set to VR mode");
            }

            HideLoadingScreen();
            isInitializing = false;
            yield break;
        }

        Debug.Log("[togglingXR] Initializing XR Loader...");
        yield return XRGeneralSettings.Instance.Manager.InitializeLoader();

        if (XRGeneralSettings.Instance.Manager.activeLoader == null)
        {
            Debug.LogError("[togglingXR] Initializing XR Failed. Check that:");
            Debug.LogError("[togglingXR] 1. XR Plugin is enabled in Project Settings > XR Plug-in Management > Android");
            Debug.LogError("[togglingXR] 2. The correct XR plugin package is installed (e.g., Google Cardboard)");
            Debug.LogError("[togglingXR] 3. Build target is set to Android");
            Debug.LogError("[togglingXR] 4. Minimum API level is set correctly (Android 7.0+)");
        }
        else
        {
            Debug.Log("[togglingXR] Starting XR Subsystems...");
            XRGeneralSettings.Instance.Manager.StartSubsystems();

            // Prevent screen from sleeping in VR mode
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            // Set VRReticlePointer to VR mode
            if (vrReticlePointer != null)
            {
                vrReticlePointer.SetMode(VRReticlePointer.ViewMode.ModeVR);
                Debug.Log("[togglingXR] VRReticlePointer set to VR mode");
            }
            else
            {
                Debug.LogWarning("[togglingXR] VRReticlePointer not found! Assign vrReticleObject in Inspector.");
            }

            Debug.Log("[togglingXR] XR started successfully!");

            // Log active XR display
            var displays = new System.Collections.Generic.List<UnityEngine.XR.XRDisplaySubsystem>();
            SubsystemManager.GetSubsystems(displays);
            if (displays.Count > 0)
            {
                Debug.Log($"[togglingXR] XR Display found: {displays[0].running}");
            }
        }

        HideLoadingScreen();
        isInitializing = false;
        Debug.Log($"[togglingXR] StartXR completed in {Time.realtimeSinceStartup - startTime} seconds.");
    }

    public void StopXR()
    {
        if (isInitializing)
        {
            Debug.LogWarning("[togglingXR] Cannot stop XR while initializing");
            return;
        }

        isInitializing = true;
        Debug.Log("[togglingXR] Attempting to stop XR...");

        // Return to 360 mode (preserves your tap and drag navigation)
        if (vrReticlePointer != null)
        {
            vrReticlePointer.SetMode(VRReticlePointer.ViewMode.Mode360);
            Debug.Log("[togglingXR] VRReticlePointer set back to 360 mode");
        }

        // Check if XRGeneralSettings exists
        if (XRGeneralSettings.Instance == null)
        {
            Debug.LogWarning("[togglingXR] Cannot stop XR - XRGeneralSettings.Instance is null");
            isInitializing = false;
            return;
        }

        if (XRGeneralSettings.Instance.Manager == null)
        {
            Debug.LogWarning("[togglingXR] Cannot stop XR - XRGeneralSettings.Instance.Manager is null");
            isInitializing = false;
            return;
        }

        if (XRGeneralSettings.Instance.Manager.activeLoader != null)
        {
            Debug.Log("[togglingXR] Stopping XR subsystems...");
            XRGeneralSettings.Instance.Manager.StopSubsystems();
            Debug.Log("[togglingXR] Deinitializing XR loader...");
            XRGeneralSettings.Instance.Manager.DeinitializeLoader();
            Debug.Log("[togglingXR] XR stopped successfully!");
        }
        else
        {
            Debug.Log("[togglingXR] No active XR loader to stop");
        }

        // Reset to 360 mode settings
        Initialize360Mode();

        isInitializing = false;
    }

    private void ShowLoadingScreen()
    {
        Debug.Log("[togglingXR] Showing loading screen...");
        // TODO: Implement loading screen display if you have one
        // Example: LoadingScreenUI.SetActive(true);
    }

    private void HideLoadingScreen()
    {
        Debug.Log("[togglingXR] Hiding loading screen...");
        // TODO: Implement loading screen hide if you have one
        // Example: LoadingScreenUI.SetActive(false);
    }

    private void Initialize360Mode()
    {
        Debug.Log("[togglingXR] Initializing 360 mode...");
        // Reset screen sleep timeout to system default
        Screen.sleepTimeout = SleepTimeout.SystemSetting;

        // Allow auto-rotation for 360 mode
        Screen.orientation = ScreenOrientation.AutoRotation;
    }

    private void OnDestroy()
    {
        // Clean up XR when this GameObject is destroyed
        if (XRGeneralSettings.Instance?.Manager?.activeLoader != null)
        {
            StopXR();
        }
    }
}