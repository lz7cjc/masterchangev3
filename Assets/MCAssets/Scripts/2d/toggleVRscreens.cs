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
    private int  switchTo2DOption;


    /// <summary>
    /// Choose between headset and 2d screens
    /// 
    /// </summary>
    /// 
    //Detect if a click occurs
    public void OnPointerClick(PointerEventData pointerEventData)
    {
        //Output to console the clicked GameObject's name and the following message. You can replace this with your own actions for when clicking the GameObject.
        Debug.Log(name + " Game Object Clicked!");
    }
    public void switchVR(int switchTo2DOption)
    {
        Debug.Log("hit script");
        if (switchTo2DOption == 1)
        {
            PlayerPrefs.SetInt("toggleToVR", 1);
            SceneManager.LoadScene("FirstVisit");

        }
        else if (switchTo2DOption == 0)
        {
            PlayerPrefs.SetInt("toggleToVR", 0);
            SceneManager.LoadScene("FirstVisit2d");
        }

    }

      
      

  
  
   
    
}