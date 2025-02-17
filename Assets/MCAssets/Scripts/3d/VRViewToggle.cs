using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using System.Collections; // Add this to resolve IEnumerator
public class VRViewToggle : MonoBehaviour
{
    private int vrMode;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        vrMode = PlayerPrefs.GetInt("vrstate");

        if (vrMode == 1)
        {
            StartCoroutine(EnableVRMode());
        }
        else
        {
            Disable360Mode();
        }
    }

    public void changeVRState(int vrOn)
    {
        Debug.Log("Script fired");
        if (vrOn == 1)
        {
            PlayerPrefs.SetInt("vrstate", 1);
            StartCoroutine(EnableVRMode());
        }
        else if (vrOn == 0)
        {
            PlayerPrefs.SetInt("vrstate", 0);
            Disable360Mode();
        }
    }

    public IEnumerator EnableVRMode()
    {
        var xrManager = XRGeneralSettings.Instance.Manager;

        // Initialize the XR loader
        yield return xrManager.InitializeLoader();

        if (xrManager.activeLoader != null)
        {
            xrManager.StartSubsystems();

            if (mainCamera != null)
            {
                mainCamera.stereoTargetEye = StereoTargetEyeMask.Both;
                mainCamera.ResetStereoProjectionMatrices();
            }
        }
    }

    public void Disable360Mode()
    {
        var xrManager = XRGeneralSettings.Instance.Manager;
        if (xrManager.activeLoader != null)
        {
            xrManager.StopSubsystems();
            xrManager.DeinitializeLoader();
        }

        if (mainCamera != null)
        {
            mainCamera.stereoTargetEye = StereoTargetEyeMask.None;
            mainCamera.ResetProjectionMatrix();
            mainCamera.fieldOfView = 90f;
        }
    }

    void OnDestroy()
    {
        var xrManager = XRGeneralSettings.Instance.Manager;
        if (xrManager != null && xrManager.activeLoader != null)
        {
            xrManager.StopSubsystems();
            xrManager.DeinitializeLoader();
        }
    }
}
