using System.Collections;
using UnityEngine;
using UnityEngine.XR.Management;

public class togglingXR : MonoBehaviour
{
    public bool switchVRon;
    private int vrSetOn;

    public void Start()
    {
        Debug.Log($"Start method called. switchVRon: {switchVRon}");

        // Add debug log to check XRGeneralSettings.Instance at the start
        if (XRGeneralSettings.Instance == null)
        {
            Debug.LogWarning("XRGeneralSettings.Instance is null at Start. Ensure XR Plugin Management is configured.");
        }

        if (switchVRon)
        {
            StartCoroutine(StartXR());
        }
        else
        {
            StopXR();
        }
    }

    public void SwitchingVR()
    {
        vrSetOn = PlayerPrefs.GetInt("toggleToVR");
        Debug.Log($"[togglingXR] SwitchingVR called. toggleToVR: {vrSetOn}");

        if (vrSetOn == 1)
        {
            Debug.Log("[togglingXR] Starting XR mode.");
            StartCoroutine(StartXR());
        }
        else
        {
            Debug.Log("[togglingXR] Stopping XR mode. Enforcing 2D mode.");
            StopXR();
        }
    }

    public IEnumerator StartXR()
    {
        Debug.Log("[togglingXR] Attempting to start XR...");
        var startTime = Time.realtimeSinceStartup;

        ShowLoadingScreen();

        yield return new WaitForSeconds(0.5f);

        if (XRGeneralSettings.Instance == null)
        {
            Debug.LogWarning("[togglingXR] XRGeneralSettings.Instance is null!");
            HideLoadingScreen();
            yield break;
        }

        if (XRGeneralSettings.Instance.Manager == null)
        {
            Debug.LogWarning("[togglingXR] XRGeneralSettings.Instance.Manager is null!");
            HideLoadingScreen();
            yield break;
        }

        yield return XRGeneralSettings.Instance.Manager.InitializeLoader();

        if (XRGeneralSettings.Instance.Manager.activeLoader == null)
        {
            Debug.LogError("[togglingXR] Initializing XR Failed.");
        }
        else
        {
            XRGeneralSettings.Instance.Manager.StartSubsystems();
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Debug.Log("[togglingXR] XR started successfully!");
        }

        HideLoadingScreen();
        Debug.Log($"[togglingXR] StartXR completed in {Time.realtimeSinceStartup - startTime} seconds.");
    }

    public void StopXR()
    {
        Debug.Log("Attempting to stop XR...");

        // Add debug log to check XRGeneralSettings.Instance before stopping XR
        if (XRGeneralSettings.Instance == null)
        {
            Debug.LogWarning("Cannot stop XR - XRGeneralSettings.Instance is null. Ensure XR Plugin Management is configured.");
            return;
        }

        if (XRGeneralSettings.Instance.Manager == null)
        {
            Debug.LogWarning("Cannot stop XR - XRGeneralSettings.Instance.Manager is null");
            return;
        }

        if (XRGeneralSettings.Instance.Manager.activeLoader != null)
        {
            Debug.Log("Stopping XR subsystems...");
            // Stop XR subsystems and deinitialize the loader
            XRGeneralSettings.Instance.Manager.StopSubsystems();
            XRGeneralSettings.Instance.Manager.DeinitializeLoader();
            Debug.Log("XR stopped successfully!");
        }
        else
        {
            Debug.Log("No active XR loader to stop");
        }
    }

    // Add methods to handle loading screen visibility
    private void ShowLoadingScreen()
    {
        Debug.Log("Showing loading screen...");
        // Implement logic to display a loading screen or fallback UI
    }

    private void HideLoadingScreen()
    {
        Debug.Log("Hiding loading screen...");
        // Implement logic to hide the loading screen or fallback UI
    }

    private void Initialize2DScreen()
    {
        Debug.Log("Initializing 2D screen...");
        // Implement logic to ensure the 2D screen is properly displayed
    }
}