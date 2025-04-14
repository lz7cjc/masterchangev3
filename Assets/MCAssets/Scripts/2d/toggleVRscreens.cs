using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.EventSystems;

public class toggleVRscreens : MonoBehaviour
{
    private int switchTo2DOption;
    private int showHidePickFormat;

    /// <summary>
    /// Choose between headset and 2d screens
    /// 
    /// </summary>
    /// 
    //Detect if a click occurs
    //public void OnPointerClick(PointerEventData pointerEventData)
    //{
    //    //Output to console the clicked GameObject's name and the following message. You can replace this with your own actions for when clicking the GameObject.
    //    Debug.Log(name + " Game Object Clicked!");
    //}
    public void Start()
    {
        Debug.Log("[toggleVRscreens] Start method called.");
        var startTime = Time.realtimeSinceStartup;

        switchTo2DOption = PlayerPrefs.GetInt("toggleToVR");
        showHidePickFormat = PlayerPrefs.GetInt("hidePickFormat");

        Debug.Log($"[toggleVRscreens] toggleToVR: {switchTo2DOption}, hidePickFormat: {showHidePickFormat}");

        if (switchTo2DOption == 1 && showHidePickFormat == 1)
        {
            Debug.Log("[toggleVRscreens] Loading VR scene: FirstVisit");
            SceneManager.LoadScene("FirstVisit");
        }
        else if (switchTo2DOption == 0 && showHidePickFormat == 1)
        {
            Debug.Log("[toggleVRscreens] Loading 2D scene: FirstVisit2d");
            SceneManager.LoadScene("FirstVisit2d");
        }
        else
        {
            Debug.LogWarning("[toggleVRscreens] No valid condition met. Defaulting to 2D scene.");
            SceneManager.LoadScene("FirstVisit2d");
        }

        Debug.Log($"[toggleVRscreens] Initialization completed in {Time.realtimeSinceStartup - startTime} seconds.");
    }

    public void Set2dorVR(int VR)
    {
        if (VR == 1)
        {
            PlayerPrefs.SetInt("toggleToVR", 1);
            PlayerPrefs.Save();
            Debug.Log("set to VR");
            SceneManager.LoadScene("FirstVisit");
        }
        else if (VR == 0)
        {
            PlayerPrefs.SetInt("toggleToVR", 0);
            PlayerPrefs.Save();
            Debug.Log("set to 2D");
            SceneManager.LoadScene("FirstVisit2d");
        }
    }
}