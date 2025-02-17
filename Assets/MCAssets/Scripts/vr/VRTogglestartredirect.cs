using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.SceneManagement;

public class VRTogglestartredirect : MonoBehaviour
{

    private void Start()
    {
        StartCoroutine(StartVR());   
    }

    IEnumerator StartVR()
    {
        var xrManager = XRGeneralSettings.Instance.Manager;
        if (!xrManager.isInitializationComplete)
         yield return xrManager.InitializeLoader();
        xrManager.StartSubsystems();
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        SceneManager.LoadScene("everything");
    }
}