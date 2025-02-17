using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Management;

public class VRTogglemouseover : MonoBehaviour
{
        
   
    void Start()
    {
        //if (XRGeneralSettings.Instance.Manager.activeLoader != null)
        //{
        //    XRGeneralSettings.Instance.Manager.activeLoader.Stop();
        //    XRGeneralSettings.Instance.Manager.activeLoader.Deinitialize();
        //    Debug.Log("XR stopped completely.");
        //}
        StopXR();
    }

   public void StopXR()
    {
        var xrManager = XRGeneralSettings.Instance.Manager;
        if (!xrManager.isInitializationComplete)
            return; // Safety check
        xrManager.StopSubsystems();
      //  xrManager.DeinitializeLoader();
    }
}
   
