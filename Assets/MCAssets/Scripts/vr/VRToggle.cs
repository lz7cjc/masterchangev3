using System.Collections;

using UnityEngine;
using UnityEngine.XR.Management;


public class VRToggle : MonoBehaviour
{

    public bool vrOn;

    public void Start()
    {
        if (vrOn)
        {
            StartCoroutine(VRStart());

        }
        else
        {
            VRStop();
        }
    }

    private IEnumerator VRStart()
    {
        Debug.Log("Initializing XR...");
        /*if (!XRGeneralSettings.Instance.Manager.isInitializationComplete)*/ yield return XRGeneralSettings.Instance.Manager.InitializeLoader();
        yield return 0;

        if (XRGeneralSettings.Instance.Manager.activeLoader == null)
        {
            Debug.LogError("Initializing XR Failed.");
        }
        else
        {
            Debug.Log("Starting XR...");
            XRGeneralSettings.Instance.Manager.StartSubsystems();
        }

    }
        //public IEnumerator VRStart()
      // {
        //Debug.Log("start vr");

        //var xrManager = XRGeneralSettings.Instance.Manager;
        //if (!xrManager.isInitializationComplete)
        //{
        //    yield return xrManager.InitializeLoader();
        //    xrManager.StartSubsystems();
        //    Screen.sleepTimeout = SleepTimeout.NeverSleep;

        //}

   

    private void VRStop()
        {
            Debug.Log("Stopping XR...");
            if (XRGeneralSettings.Instance != null) XRGeneralSettings.Instance.Manager.StopSubsystems();
            Camera.main.ResetAspect();
        }
        //{
        //Debug.Log("stopping vr");
        //var xrManager = XRGeneralSettings.Instance.Manager;
        //if (xrManager.isInitializationComplete)
        //{
        //    xrManager.StopSubsystems();
        //    xrManager.DeinitializeLoader();

        //    //cam.ResetAspect();
        //    //cam.fieldOfView = defaultFov;
        //    //cam.ResetProjectionMatrix();
        //    //cam.ResetWorldToCameraMatrix();
        //    Screen.sleepTimeout = SleepTimeout.SystemSetting;
        //}

   //}
}
