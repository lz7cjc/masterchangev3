using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Networking;

public class ScreenDisplayLearning : MonoBehaviour
{
    public Toggle unityDontShowLearning;
     private int SkipLearningScreenInt;
    private int SkipSwitchScreenInt;
    private int healthcheck;
    private string whichchange;

    /// <summary>
    /// This is setting and checking the checkbox and reading/writing the screendisplaylearning.json file
    /// 
    /// </summary>
    public void Start()
    {
  

        SkipLearningScreenInt = PlayerPrefs.GetInt("trainingDone");
        SkipSwitchScreenInt = PlayerPrefs.GetInt("SwitchtoVR");
        whichchange = PlayerPrefs.GetString("nextscene"); 
        Debug.Log("///SkipLearningScreen in start from toggle//// " + SkipLearningScreenInt);
        Debug.Log("///SkipSwitchScreenInt in start from toggle//// " + SkipLearningScreenInt);

        //if (whichchange == "welcome")
        //{
        //    SceneManager.LoadScene("welcome");
        //    PlayerPrefs.DeleteKey("returnToScene");
        //}
        //else
        //{

        
        if (SkipLearningScreenInt == 1)
        { unityDontShowLearning.isOn = true;
        }
        else
        {

            unityDontShowLearning.isOn = false;
        }


        }
    

public void onChangeDontShowLearning()
    {
        Debug.Log("trainingDone " + unityDontShowLearning.isOn);
        if (unityDontShowLearning.isOn)
        {
            PlayerPrefs.SetInt("trainingDone", 1);
            Debug.Log("the value in the if toggle function from player prefs is: " + PlayerPrefs.GetInt("trainingDone"));
        }
        else if (!unityDontShowLearning.isOn)
        {
            PlayerPrefs.SetInt("trainingDone", 0);
            Debug.Log("the value in the if not toggle function from player prefs is: " + PlayerPrefs.GetInt("trainingDone"));
        }
        Debug.Log("the value in the toggle function from player prefs is: " + PlayerPrefs.GetInt("trainingDone"));
    }

    public void toMasterChange()
    {
        SkipLearningScreenInt = PlayerPrefs.GetInt("trainingDone");
            Debug.Log("which button showSwitchScreenInt " + SkipSwitchScreenInt);

        Debug.Log("which button showTrainingSceneInt " + SkipLearningScreenInt);

        //  SkipSwitchScreenInt = PlayerPrefs.GetInt("SkipSwitchScreenInt");
        // SkipLearningScreenInt = PlayerPrefs.GetInt("trainingDone");
        Debug.Log("what is learning set to?" + SkipLearningScreenInt);
        Debug.Log("what is SkipSwitchScreenInt set to?" + SkipSwitchScreenInt);
       if (SkipSwitchScreenInt == 0)
        {
            SceneManager.LoadScene("switchtoVR");
        }
        
        else
        {
            SceneManager.LoadScene("everything");
        }



    }

  
   
    
}