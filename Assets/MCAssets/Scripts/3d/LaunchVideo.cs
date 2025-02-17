using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Networking;

public class LaunchVideo : MonoBehaviour
{
    public bool mousehover = false;
    public float counter = 0;
    //public int Film_button;
   // private string Switchscenename;
   // private object Scenename;
  
    public int stage;
    public string returnScene;
 //   public GameObject returnToZone_tgt;
    private string videoUrl;
  //  public GameObject player;
  
   
    // Update is called once per frame
    void Update()
    {
        if (mousehover)
        {
           
            counter += Time.deltaTime;
            if (counter >= 3)
            {
                mousehover = false;
                counter = 0;
                PlayerPrefs.SetString("nextscene", "videoplayer");
              //  PlayerPrefs.SetString("returntoscene", returnToZone_tgt.name);
                PlayerPrefs.SetInt("stagesmoking", stage);
                //PlayerPrefs.SetString("behaviour", behaviour);

                PlayerPrefs.SetString("videourl", videoUrl);
                SceneManager.LoadScene("360VideoApp");


            }
        }
    }

    // mouse Enter event
    public void MouseHoverChangeScene()
    {
        
     //   // debug.log("setting scenename");
         mousehover = true;
    }

    // mouse Exit Event
    public void MouseExit()
    {
       // // debug.log("cancelling scene change");
        mousehover = false;
        counter = 0;
    }


    public void LaunchSmoking()
    {
        
        PlayerPrefs.GetString("videourl");
        SceneManager.LoadScene("360VideoApp");
    }
    public void LaunchAlcohol()
    {
        PlayerPrefs.SetString("videourl", "alcoholwelcome");
        SceneManager.LoadScene("360VideoApp");
    }

}