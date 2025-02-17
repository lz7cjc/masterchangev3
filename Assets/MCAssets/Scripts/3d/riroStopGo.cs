using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


public class riroStopGo : MonoBehaviour
{
    private int riroAmount;
    public TextMeshPro errorMessageTxt; 
   // public TextMeshPro hintTxt;


    public GameObject getRirosBtn;
    public GameObject registerBtn;
    public GameObject habitsBtn;
    public GameObject activitiesBtn;
    public TextMeshPro removeHeadsetlbl;
    private string behaviour;
    private int dbUserId;
    private int habitsDone;
    public TextMeshPro tenkRiros;

    // public GameObject registerCanvas;
    //  public Text registertext;
    // Start is called before the first frame update

    public void doNotPass(int whichType)
    {
        riroAmount = PlayerPrefs.GetInt("rirosbalance");
       // behaviour = PlayerPrefs.GetString("behaviour");
        habitsDone = PlayerPrefs.GetInt("habitsdone");
        // debug.log("vvv riro balance" + riroAmount);
        //smoking value 1
        if (whichType == 1)
        {
            smokingMessages();
            PlayerPrefs.SetInt("stage", 0);
        }
     //alcohol value 2
        if (whichType == 2)
        {
            alcoholMessages();
        }

       if (whichType == 3)
        {
            videoFailed();
        }


        //generic films 0
        else if (whichType == 0)

        {
            genericMessages();
            
        }
        // debug.log("redirect to do not pass");

        //PlayerPrefs.DeleteKey("stopfilm");

    }

    private void smokingMessages()
    {



        if (PlayerPrefs.HasKey("dbuserid"))

        {
            registeredNoHabits();

        }

        else
        {
            registerAndHabits();
        }
    }

    private void alcoholMessages()
    {
        if (PlayerPrefs.HasKey("dbuserid"))

        {
            needMoreRiros();

        }

        else
        {
            needtoregister();
        }
    }

    private void genericMessages()
    {
        if (PlayerPrefs.HasKey("dbuserid"))

        {
            needMoreRiros();

        }

        else
        {
            needtoregister();
        }
    }

    private void videoFailed()
    {
        errorMessageTxt.text = "Something went wrong, trying to play that film. The details of the problem have been sent to our tech team and we will get it fixed as soon as possible. This is a category 1 error";
        removeHeadsetlbl.text = "";
        getRirosBtn.SetActive(false);
        registerBtn.SetActive(false);
        habitsBtn.SetActive(false);
        activitiesBtn.SetActive(true);
    }
    private void registeredNoHabits()
    {
                // debug.log("vvv no habits done  registered");
                //  PlayerPrefs.SetString("nextscene", "register");
                errorMessageTxt.text = "To start the quit smoking experience, we need to know about your smoking habits. Please fill in the short questionnaire to continue";
        // registertext.text = "Earn Riros";
        getRirosBtn.SetActive(false);
        registerBtn.SetActive(false);
        habitsBtn.SetActive(true);
        activitiesBtn.SetActive(false);
        //hintTxt.enabled = false;
    }
           
    private void needMoreRiros()
            {
                // debug.log("vvv no riros  registered");
                errorMessageTxt.text = "R$: " + riroAmount + " ... You have insufficient funds to launch that experience. You can earn or buy riros to continue";
        // registertext.text = "Earn Riros";
        getRirosBtn.SetActive(true);
        registerBtn.SetActive(false);
        habitsBtn.SetActive(false);
        activitiesBtn.SetActive(false);
        //  hintTxt.enabled = false;
    }

   private void registerAndHabits()
            {
                // debug.log("vvv no habits done");
                //  PlayerPrefs.SetString("nextscene", "register");
                errorMessageTxt.text = "To start the quit smoking experience, we need to know about your smoking habits. Please register and then fill in the short questionnaire to continue";
        // registertext.text = "Earn Riros";
        getRirosBtn.SetActive(false);
        registerBtn.SetActive(true);
        habitsBtn.SetActive(false);
        activitiesBtn.SetActive(false);

#if UNITY_ANDROID

        tenkRiros.text = "Special bonus for Android Users: Register and get R$10,000";

#endif


        ///////
        ///need to auto move to habits after register
        ///


       // hintTxt.enabled = true;
            }
    private void needtoregister()
            {
                // debug.log("no riros no register");
                errorMessageTxt.text = "R$: " + riroAmount + " ... You have insufficient funds to launch that experience. Please register to buy or earn more Riros";
        // registertext.text = "Earn Riros";
        getRirosBtn.SetActive(false);
        registerBtn.SetActive(true);
        habitsBtn.SetActive(false);
        activitiesBtn.SetActive(false);
#if UNITY_ANDROID

        tenkRiros.text = "Special bonus for Android Users: Register and get R$10,000";

#endif
        //   hintTxt.enabled = true;
    }


}
  

