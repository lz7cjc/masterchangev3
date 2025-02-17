using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Networking;

public class DynamicLoadVideo1 : MonoBehaviour
{
    public bool mousehover = false;
    public float counter = 0;
    //public int Film_button;
   // private string Switchscenename;
   // private object Scenename;
    public string VideoUrlLinkGood;
    public string VideoUrlLinkBad;
    private float Jeopardy;
    private int randomInt;
    private float JCMultiplier;
    public int stage;
    public string returnScene;
    public string nextScene;
    public string behaviour;
    //public GameObject hospital;
    //public GameObject filmstuff;
    //public GameObject terrain;
    //public GameObject main;
    //public GameObject training;
    //public GameObject voting;
    //public GameObject player;
    //public GameObject target;
    private StartUp StartUp;

    private floorceilingmove floorceilingmove;
    private void Start()

    {

        Jeopardy = PlayerPrefs.GetFloat("jcp");
        // debug.log("jC us: " + Jeopardy);
        randomInt = Random.Range(5, 25);
        // debug.log("Random number" + randomInt);
        JCMultiplier = randomInt*(Jeopardy/100);
        // debug.log("Multiplier: " + JCMultiplier);
         }


   
    // Update is called once per frame
    void Update()
    {
        if (mousehover)
        {
            //floorceilingmove = FindFirstObjectByType<floorceilingmove>();
            //floorceilingmove.stopTheCamera();
            counter += Time.deltaTime;
            if (counter >= 3)
            {
                mousehover = false;
                counter = 0;
                PlayerPrefs.SetString("nextscene", nextScene);
                PlayerPrefs.SetString("returntoscene", returnScene);
                PlayerPrefs.SetInt("stagesmoking", stage);
                PlayerPrefs.SetString("behaviour", behaviour);

                if (JCMultiplier >= 4.5)
                {

                    
                    PlayerPrefs.SetString("videourl", VideoUrlLinkBad);

                   // // debug.log("Play bad video");
                //    showhide3d = FindObjectOfType<showhide3d>();
                //    showhide3d.ResetScene();
                //
                }
                else
                {
                    
                    PlayerPrefs.SetString("videourl", VideoUrlLinkGood);
                    
                    //// debug.log("Play good video");

                }

                //     print("---->>>" + PlayerPrefs.GetString("VideoUrl"));
                StartUp = FindFirstObjectByType<StartUp>();
                StartUp.ResetScene();
                //   
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


}