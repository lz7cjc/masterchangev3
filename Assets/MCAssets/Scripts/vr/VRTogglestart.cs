using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Management;

public class VRTogglestart : MonoBehaviour
{

    void Start()
    {
        if (XRGeneralSettings.Instance.Manager.activeLoader != null)
        {
            XRGeneralSettings.Instance.Manager.activeLoader.Stop();
            XRGeneralSettings.Instance.Manager.activeLoader.Deinitialize();
            Debug.Log("XR stopped completely.");
        }
        StartXR();
    }

    public void StartXR()
    {

        XRGeneralSettings.Instance.Manager.InitializeLoaderSync();
        XRGeneralSettings.Instance.Manager.StartSubsystems();
    }

    //public void StopXR()
    //{

    //    if (XRGeneralSettings.Instance.Manager.isInitializationComplete)
    //    {
    //        XRGeneralSettings.Instance.Manager.StopSubsystems();
    //    }
    //}
    /**
    private void Start()
    {
        StartCoroutine(StartVR());   
    }

    IEnumerator StartVR()
    {
        yield return XRGeneralSettings.Instance.Manager.InitializeLoader();

        if (XRGeneralSettings.Instance.Manager.activeLoader == null)
        {
            Debug.LogError("Initializing XR Failed. Check Editor or Player log for details.");
        }
        else
        {
            Debug.Log("Starting XR...");
            XRGeneralSettings.Instance.Manager.StartSubsystems();
        }
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
}
    **/
}