using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ignoreFirstVisit : MonoBehaviour
{
    
    public Toggle hideScreen;
    int HideStartScreen;
   
    // Start is called before the first frame update
    void Start()
    {
        //PlayerPrefs.SetInt("firstScreenHide", 0);
        HideStartScreen = PlayerPrefs.GetInt("IntroScreen");
        Debug.Log("gethideScreen in start ()" + HideStartScreen);

        if (HideStartScreen == 1) 
        {
            SceneManager.LoadScene("dashboard");
        }
        

    }

    public void changeToggle()
    {

 //       Debug.Log("in update hidescreen value" + hideScreen);
        if (hideScreen.isOn)
        {
            HideStartScreen = 1;

        }

        else
        {
            HideStartScreen = 0;
        }

 //       Debug.Log("HideStartScreen" + HideStartScreen);

        PlayerPrefs.SetInt("IntroScreen", HideStartScreen);
    }

// Update is called once per frame
 
}
