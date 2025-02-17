using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using System.Net;
//using UnityEngine.UI;
using TMPro;

public class hudCountdown : MonoBehaviour
{

    public TMP_Text countdownHud1;
 
    float reverseCounter;
    private int hideCountdown;
    // Start is called before the first frame update
        private void Start()
        {
            countdownHud1.SetText("");
      
        }

        public void SetCountdown(float waitFor, float counter)
        {
           

            hideCountdown = PlayerPrefs.GetInt("hidecountdown");
          
            if (hideCountdown != 1)
            {


                reverseCounter = waitFor + 1 - counter;

                reverseCounter = Mathf.Floor(reverseCounter);
                countdownHud1.SetText(reverseCounter.ToString());
         //   countdownHud1.color = new Color32(1, 116, 85, 255);
            //countdownHud1.color = New Color(64, 116, 85);

        }
        }

    // Update is called once per frame
    public void resetCountdown()
    {
       
            reverseCounter = 0;
        countdownHud1.SetText("");
   
    }

}