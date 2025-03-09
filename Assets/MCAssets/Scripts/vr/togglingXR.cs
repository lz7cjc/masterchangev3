using System.Collections;
using UnityEngine;
using UnityEngine.XR.Management;

public class togglingXR : MonoBehaviour
{
    public bool switchVRon;
    private int vrSetOn;

    public void Start()
    {
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
        if (vrSetOn == 1)
        {
            StartCoroutine(StartXR());
        }
        else
        {
            StopXR();
        }

    }


    public IEnumerator StartXR()
    {
        Debug.Log("Attempting to start XR...");

        // Wait for XR system to be ready
        yield return new WaitForSeconds(0.5f);

        if (XRGeneralSettings.Instance == null)
        {
            Debug.LogWarning("XRGeneralSettings.Instance is null! Make sure XR Plugin Management is properly set up in Project Settings.");
            switchVRon = false;
            yield break;
        }

        if (XRGeneralSettings.Instance.Manager == null)
        {
            Debug.LogWarning("XRGeneralSettings.Instance.Manager is null! Make sure you have at least one XR Plugin installed and enabled.");
            switchVRon = false;
            yield break;
        }

        // Initialize the XR loader
        yield return XRGeneralSettings.Instance.Manager.InitializeLoader();

        if (XRGeneralSettings.Instance.Manager.activeLoader == null)
        {
            // Initialization failed
            Debug.LogError("Initializing XR Failed. Check Editor or Player log for details.");
            switchVRon = false;
        }
        else
        {
            // Start XR subsystems
            XRGeneralSettings.Instance.Manager.StartSubsystems();
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Debug.Log("XR started successfully!");
        }
    }

    public void StopXR()
    {
        Debug.Log("Attempting to stop XR...");

        if (XRGeneralSettings.Instance == null)
        {
            Debug.LogWarning("Cannot stop XR - XRGeneralSettings.Instance is null");
            return;
        }

        if (XRGeneralSettings.Instance.Manager == null)
        {
            Debug.LogWarning("Cannot stop XR - XRGeneralSettings.Instance.Manager is null");
            return;
        }

        if (XRGeneralSettings.Instance.Manager.activeLoader != null)
        {
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
}