using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Globalization;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;


public class DBInsertHabits1 : MonoBehaviour
{
    //remote
   string posturl = "https://masterchange.today/php_scripts/habit1insert.php";
    string gethabits = "https://masterchange.today/php_scripts/gethabits.php";
    //local
    // readonly string posturl = "http://localhost/php_scripts/habit1insert.php";

    //public string errorMessage;
    private int dbuserid;
    public Toggle ttfcless;
    public Toggle ttfcmore;
    public Toggle nrtYes;
    public Toggle nrtNo;
    public Toggle prequit;
    public Toggle quitting;
    public Toggle nodate;
    private int formnodate;
    private int formprequit;
    private int formquitting;
    private int formNRTYes;
    private int formNRTNo;
    private int formTTFCless;
    private int formTTFCmore;
    private void Start()
    {


        //Debug.Log("user int at top of insert habits1" + userInt);
        //get the user habit information
        dbuserid = PlayerPrefs.GetInt("dbuserid");
        StartCoroutine(getHabits());
        Debug.Log("it is running");
    }

    IEnumerator getHabits()
    {
        WWWForm form = new WWWForm();

        form.AddField("dbuserid", dbuserid);
        UnityWebRequest www1 = UnityWebRequest.Post(gethabits, form); // The file location for where my .php file is.
        yield return www1.SendWebRequest();
        if (www1.isNetworkError || www1.isHttpError)
        {
            Debug.Log(www1.error);
            // errorMessage = www.error;
        }
        else
        {

            Debug.Log("Got the user's habits");

            Debug.Log("this comes back" + www1.downloadHandler.text);
            string json = www1.downloadHandler.text;
            var myData = JsonUtility.FromJson<UserHabits>("{\"data\":" + json + "}");
            // Debug.Log(json);
            foreach (var habit in myData.data)
            {

                if (habit.Habit_ID == 19)
                {
                    if (habit.yesorno)
                    {
                        nodate.isOn = true;
                    }
                    else
                    {
                        nodate.isOn = false;
                    }
                }
                if (habit.Habit_ID == 20)
                {
                    if (habit.yesorno)
                    {
                        prequit.isOn = true;
                    }
                    else
                    {
                        prequit.isOn = false;
                    }

                }
                if (habit.Habit_ID == 21)
                {
                    if (habit.yesorno)
                    {
                        quitting.isOn = true;
                    }
                    else
                    {
                        quitting.isOn = false;
                    }

                }
            }
        }
    }   
       
    
    public void OnChangeNRT()
    {
        formNRTNo = Convert.ToInt32(nrtNo.isOn);
        formNRTYes = Convert.ToInt32(nrtYes.isOn);
    }

    public void QuitStage()
            {
                formnodate = Convert.ToInt32(nodate.isOn);
                formprequit = Convert.ToInt32(prequit.isOn);
                formquitting = Convert.ToInt32(quitting.isOn);
            }

    public void Timetetofirstyes()
    {
        formTTFCmore = Convert.ToInt32(ttfcmore.isOn);
        formTTFCless = Convert.ToInt32(ttfcless.isOn);
    }

            public void sethabits()
            {
              
               






                StartCoroutine(pushhabits());
            }

            IEnumerator pushhabits()
            {
                Debug.Log("in the habits function");
                WWWForm form = new WWWForm();
                    form.AddField("screen", "2");
                    form.AddField("user", dbuserid);
                    form.AddField("prequit", formprequit);
                    form.AddField("quitting", formprequit);
                    form.AddField("nodate", formnodate);
                    form.AddField("ttfcless", formTTFCless);
                    form.AddField("nrt", formNRTYes);


        UnityWebRequest www1 = UnityWebRequest.Post(posturl, form); // The file location for where my .php file is.
                yield return www1.SendWebRequest();
                if (www1.isNetworkError || www1.isHttpError)
                {
                    Debug.Log(www1.error);
                    // errorMessage = www.error;
                }
                else
                {

                    Debug.Log("Form Upload Complete!");

                    Debug.Log("this comes back" + www1.downloadHandler.text);


                }


                UnityWebRequest www = UnityWebRequest.Post(posturl, form); // The file location for where my .php file is.
                yield return www.SendWebRequest();
                if (www.isNetworkError || www.isHttpError)
                {
                    Debug.Log(www.error);
                    // errorMessage = www.error;
                }
                else
                {

                    Debug.Log("Form Upload Complete!");

                    Debug.Log("this comes back" + www.downloadHandler.text);
                    SceneManager.LoadScene("habits1db");
                }

            }


    public class UserHabits
    {
        public List<habitinfo> data;
    }
    [System.Serializable]

    public class habitinfo
    {
        public int Habit_ID;
        public int label;
        public int amount;
        public Text yesorno;

    }


}
