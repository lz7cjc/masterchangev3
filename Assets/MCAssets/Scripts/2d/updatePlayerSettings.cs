using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Networking;

public class updatePlayerSettings : MonoBehaviour
{
  
    public Toggle IntroScreen;
    public Toggle SwitchtoVR;
    public Toggle Training;
    private int IntroScreenset;
    private int SwitchtoVRset;
    private int Trainingset;
    private int signsmall;



    public void Start()
    {
       
        IntroScreenset = PlayerPrefs.GetInt("IntroScreen");
        SwitchtoVRset = PlayerPrefs.GetInt("SwitchtoVR");
        Trainingset = PlayerPrefs.GetInt("trainingDone");
        signsmall = PlayerPrefs.GetInt("EyesGood");


        //check if hiding first screen
        if (PlayerPrefs.HasKey("IntroScreen"))

        {
            if (IntroScreenset == 1)
            {
                IntroScreen.isOn = true;
            }
            else if (IntroScreenset == 0)
            {
                IntroScreen.isOn = false;
            }
        }

        //check if hiding VR switch screen
        if (PlayerPrefs.HasKey("SwitchtoVR"))

        {
            if (SwitchtoVRset == 0)
            {
                SwitchtoVR.isOn = true;
            }
            else if (SwitchtoVRset == 1)
            {
                SwitchtoVR.isOn = false;
            }

        }

        //check if hiding training level
        if (PlayerPrefs.HasKey("trainingDone"))

        {

            if (Trainingset == 1)
            {
                Training.isOn = true;
            }
            else if (Trainingset == 0)
            {
                Training.isOn = false;
            }

        }

        

    }

    public void OnChangeFirstScreen()
    {
        IntroScreenset = System.Convert.ToInt32(IntroScreen.isOn);
        Debug.Log("IntroScreen: " + IntroScreen.isOn);
        Debug.Log("IntroScreenset: " + IntroScreenset);
        PlayerPrefs.SetInt("IntroScreen", IntroScreenset);
  

    }

    public void OnChangeSwitchVR()
    {
        Debug.Log("OnChangeSwitchVR: " + SwitchtoVR.isOn);
        SwitchtoVRset = System.Convert.ToInt32(SwitchtoVR.isOn);
        Debug.Log("SwitchtoVRset: " + SwitchtoVRset);
        if (SwitchtoVR.isOn)
        {
            PlayerPrefs.SetInt("SwitchtoVR", 0);
        }
        else if (!SwitchtoVR.isOn)
        {
            PlayerPrefs.SetInt("SwitchtoVR", 1);
        }
    }
    public void OnChangeTraining()
    {
        Debug.Log("Training " + Training.isOn);
        Trainingset = System.Convert.ToInt32(Training.isOn);
        Debug.Log("Trainingset " + Trainingset);
        PlayerPrefs.SetInt("trainingDone", Trainingset);

    }
  


}