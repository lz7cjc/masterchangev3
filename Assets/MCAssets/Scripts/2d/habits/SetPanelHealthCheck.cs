using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Networking;

public class SetPanelHealthCheck : MonoBehaviour
{

     public Text TextStopgo;
    private bool doesExisthabits;


    void Start()
    {
        //(PlayerPrefs.HasKey("habitsdone")) &&
        if ((PlayerPrefs.GetInt("habitsdone") == 1))
        {
            TextStopgo.text = "You have unlocked this area of the World of MasterChange. Congratulations";
            TextStopgo.color = Color.green;

        }
        else
        {
            if (PlayerPrefs.HasKey("dbuserid"))
            {
                TextStopgo.text = "You will need to take off your headset to complete a basic consultation before you go for your health check. You will be asked 6 questions then you can enter";
                PlayerPrefs.SetInt("habitsdone", 0);

            }
            else
            {
                TextStopgo.text = "You will first need to take off your headset to register, then complete a basic consultation before you go for your health check";
                PlayerPrefs.SetInt("habitsdone", 0);
                TextStopgo.color = Color.white;
            }
            //PlayerPrefs.SetString("behaviour", "smoking");
            //PlayerPrefs.SetInt("stage", 0);
        }
    }
}