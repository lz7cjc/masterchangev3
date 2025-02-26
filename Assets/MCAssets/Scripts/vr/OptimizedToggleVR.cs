using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.SceneManagement;

public class OptimizedToggleVR : MonoBehaviour
{
    public bool enableVR = false; // Expose VR toggle in the Inspector
    private bool isVRMode = false; // Track if VR is active
    private XRManagerSettings xrManager;
    private Camera mainCamera;
    private const float SAFETY_DELAY = 0.5f;

    private void Start()
    {
        // Check if XR Plugin Management is set up
        if (XRGeneralSettings.Instance == null)
        {
            Debug.LogError("XRGeneralSettings is missing. Ensure XR Plugin Management is set up.");
            return;
        }

        xrManager = XRGeneralSettings.Instance.Manager;
        if (xrManager == null)
        {
            Debug.LogError("XRManager is missing. Ensure XR Plugin Management is set up.");
            return;
        }

        mainCamera = Camera.main;

        // Check PlayerPrefs to determine if VR should be enabled
        int vrState = PlayerPrefs.GetInt("toggletovr", enableVR ? 1 : 0); // Default to Inspector value if not set
        ToggleVRMode(vrState == 1);
    }

    public void ToggleVRMode(bool enableVR)
    {
        if (enableVR)
        {
            StartCoroutine(StartXR());
        }
        else
        {
            StopXR();
        }

        // Save the state to PlayerPrefs
        PlayerPrefs.SetInt("toggletovr", enableVR ? 1 : 0);
    }

    public void StartVR()
    {
        StartCoroutine(StartXR());
    }

    public void StopVR()
    {
        StopXR();
    }

    private IEnumerator StartXR()
    {
        if (xrManager.activeLoader != null)
        {
            Debug.Log("XR is already initialized.");
            yield break;
        }

        Debug.Log("Initializing XR...");
        yield return xrManager.InitializeLoader();

        if (xrManager.activeLoader == null)
        {
            Debug.LogError("XR Loader failed to initialize.");
            yield break;
        }

        xrManager.StartSubsystems();
        isVRMode = true;
        Debug.Log("XR Started Successfully!");

        // Update camera settings for VR
        if (mainCamera != null)
        {
            mainCamera.stereoTargetEye = StereoTargetEyeMask.Both;
            mainCamera.ResetStereoProjectionMatrices();
        }

        // Optional: Redirect to a new scene after starting XR
        // SceneManager.LoadScene("YourSceneName");
    }

    private void StopXR()
    {
        if (!isVRMode)
        {
            Debug.Log("XR is not running, no need to stop.");
            return;
        }

        Debug.Log("Stopping XR...");
        xrManager.StopSubsystems();
        xrManager.DeinitializeLoader();
        isVRMode = false;
        Debug.Log("XR Stopped.");

        // Reset camera settings
        if (mainCamera != null)
        {
            mainCamera.stereoTargetEye = StereoTargetEyeMask.None;
            mainCamera.ResetProjectionMatrix();
            mainCamera.fieldOfView = 90f;
        }
    }
}
