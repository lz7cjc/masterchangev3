using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Networking;

public class hasRegistered2d : MonoBehaviour
{

    //public bool mousehover = false;
   // public float counter = 0;
    private string Switchscene;
    private int skipvrscreen;
    public Text label;

    private bool registered;

    public void Start()
    {
        registered = PlayerPrefs.HasKey("dbuserid");
            if (registered)
        {
              Switchscene = "earn riros";
        }
        else 
        {
            Switchscene = "preregister";

        }
    }

    // Update is called once per frame
    public void ChangeSceneNow()
    {
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