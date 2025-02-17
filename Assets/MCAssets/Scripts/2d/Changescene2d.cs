using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Networking;

public class Changescene2d : MonoBehaviour
{

    //public bool mousehover = false;
   // public float counter = 0;
    public string Switchscene;
    private int skipvrscreen;

   

    // Update is called once per frame
    public void ChangeSceneNow()
    {
        Debug.Log("in teh function ");
        if (PlayerPrefs.HasKey("SwitchtoVR"))
        {
            skipvrscreen = PlayerPrefs.GetInt("SwitchtoVR");
        }

        if (Switchscene == "switchtovr")
        {
            if (skipvrscreen == 0)
            {
                SceneManager.LoadScene("everything");

            }
            else
            {
                SceneManager.LoadScene(Switchscene);
            }
        }
        else
        {
            SceneManager.LoadScene(Switchscene);
        }
       
        
       

    }
    

   
}