using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;



public class SetSkipVRSwitchScene : MonoBehaviour
{
    private int SkipLearningScreenInt;
    private int SkipSwitchScreenInt;
    public Toggle unityDontShowVRScreenDisplay;
    private updateuserdb updateuserdb;
    private int using2d;

    public void Start()
    {
        SkipSwitchScreenInt = PlayerPrefs.GetInt("SwitchtoVR");
        using2d = PlayerPrefs.GetInt("toggleToVR");


        if ((PlayerPrefs.HasKey("switchtovr")) || (using2d == 0))
        {
            if (PlayerPrefs.GetInt("SwitchtoVR") == 0)
            {
                Debug.Log("redirect as skipping switchtovr");
                SceneManager.LoadScene("everything");
            }
        }
        else
        {

            unityDontShowVRScreenDisplay.isOn = false;
        }
    }

    /// <summary>
    /// This will skip the switchvr screen is checked the don't sow this screen again box
    /// 
    /// </summary>

    public void onChangeShowVRScreenDisplay(bool value)
    {
        Debug.Log("unityDontShowVRScreenDisplay " + unityDontShowVRScreenDisplay.isOn);
        if (unityDontShowVRScreenDisplay.isOn)
        {
            PlayerPrefs.SetInt("SwitchtoVR", 0);
            Debug.Log("hide the screen");
        }
        else
        {
            PlayerPrefs.SetInt("SwitchtoVR", 1);
            Debug.Log("show the screen");

        }

        if (PlayerPrefs.HasKey("dbuserid"))
        { 
        updateuserdb = FindObjectOfType<updateuserdb>();
        updateuserdb.callToUpdate();
        }
    }

    public void goToMasterChange()
    {
        SkipLearningScreenInt = PlayerPrefs.GetInt("trainingDone");
        Debug.Log("SkipLearningScreen " + SkipLearningScreenInt);

       
            SceneManager.LoadScene("everything");
       

    }

    public void goDashboard()
    {
      SceneManager.LoadScene("dashboard");


    }

}
    

