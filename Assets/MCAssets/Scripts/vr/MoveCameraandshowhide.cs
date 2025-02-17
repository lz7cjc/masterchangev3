using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MoveCameraandshowhide : MonoBehaviour
{
   //camera work
    public GameObject player; //what are we moving
    public GameObject TargetObject; // where is the camera going
    public GameObject startMainSmall; //where to move player  to in main level
    public GameObject startTrainingPos; // where to move the player in the training level
    public bool childCamera; //do we need to move the player inside an object
  
    //gazing stuff
    public bool mousehover = false;
    public float Counter = 0;
     private int isTraining; 

    //what is to show/hide
    public GameObject TrainingLevel; //sets the training level
    public GameObject SmallSigns; //sets the main level 
     public Boolean showTraining; // do you want to set the training level
    //public GameObject MainCamera; 
    void Update()
    {

        if (mousehover)
        {

            Counter += Time.deltaTime;
            if (Counter >= 3)
            {
                mousehover = false;
                Counter = 0;
                if (showTraining)
                {

                    PlayerPrefs.SetInt("trainingDone", 0);
                }



                else if (!showTraining)
                {

                    PlayerPrefs.SetInt("trainingDone", 1);
                }
                player.transform.position = TargetObject.transform.position;
                TargetObject.SetActive(true);
                if (childCamera)
                {
                    player.transform.SetParent(TargetObject.transform);
                }//transform.root to put it back to the top
                showhideassets();

            }
        }
    }

    // mouse Enter event
    public void MouseHoverChangeScene()
    {
        Debug.Log("setting walk");
        // Markername = ObjectName;
        mousehover = true;
          TargetObject.SetActive(true); 
       
    
    }

    // mouse Exit Event
    public void MouseExit()
    {
        Debug.Log("cancelling walk");
       // Markername = "";
        mousehover = false;
        Counter = 0;
    }

    public void showhideassets()
    {


        isTraining = PlayerPrefs.GetInt("trainingDone");
        Debug.Log("isTraining" + isTraining);
        if ((isTraining == 1) )
        {
            Debug.Log("in smallsigns");
            SmallSigns.SetActive(true);
            TrainingLevel.SetActive(false);
            player.transform.position = startMainSmall.transform.position;


        }
       
        else if (isTraining == 0)
        {

            Debug.Log("in learning");
            SmallSigns.SetActive(false);
            TrainingLevel.SetActive(true);
            player.transform.position = startTrainingPos.transform.position;

        }

    }


}
