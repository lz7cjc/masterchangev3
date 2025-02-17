using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Globalization;
using UnityEngine.UI;
using System;

public class habits1 : MonoBehaviour
{
    public string Switchscene;

    public Slider cigsperday;
    public Slider previousAttempts;
    public Slider yearsSmoked;

    public Text cigsforform;
    public Text attemptsforform;
    public Text yearsforform;

    //public Toggle partner;
    //public Toggle friends;
    //public Toggle parents;
    //public Toggle coworkers;

    private int dbuserid;

    private int formcigs;
    private int formattempts;
    private int formyears;
    //private int formpartner;
    //private int formalone;
    //private int formwithnonsmokers;
    //private int formwithsmokers;

    private int cigarettesID = 16;
    private int attemptsID = 17;
    private int yearsID = 18;
    //private int partnerID = 32;
    //private int aloneID = 29;
    //private int withnonsmokersID = 30;
    //private int withsmokerIDs = 31;

    private ChangesceneInc ChangesceneInc;

    //remote
    string posturl = "https://masterchange.today/php_scripts/habitvaluesjson.php";
    string gethabits = "https://masterchange.today/php_scripts/gethabits.php";
    //local
    // readonly string posturl = "http://localhost/php_scripts/habitvaluesjson.php";
    public void Start()
    {
        dbuserid = PlayerPrefs.GetInt("dbuserid");
        StartCoroutine(getHabits());

    }

        IEnumerator getHabits()
        {
            Debug.Log("in the habits function");
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

            UserHabits loadedPlayerData = JsonUtility.FromJson<UserHabits>(json);
            Debug.Log("record count: " + loadedPlayerData.data.Count);
            for (int i = 0; i < loadedPlayerData.data.Count; i++)
            {
                  if (loadedPlayerData.data[i].Habit_ID == 16)
                {
                    cigsperday.value = loadedPlayerData.data[i].amount;
                }
                if (loadedPlayerData.data[i].Habit_ID == 17)
                {
                    previousAttempts.value = loadedPlayerData.data[i].amount;
                }
                if (loadedPlayerData.data[i].Habit_ID == 18)
                {
                    yearsSmoked.value = loadedPlayerData.data[i].amount;
                }

            }
        }
    }

       


    public void OnChangeCigs()
    {
        cigsforform.text = cigsperday.value.ToString(); 
       }



    public void OnChangeAttempts()
    {
        attemptsforform.text = previousAttempts.value.ToString();

    }

    public void OnChangeYears()
    {
        yearsforform.text = yearsSmoked.value.ToString();


    }

    public void sethabits()
    {
        formcigs = (int)cigsperday.value;
        formattempts = (int)previousAttempts.value ;
        formyears = (int)yearsSmoked.value ;
        StartCoroutine(pushhabits());
    }



    IEnumerator pushhabits()
    {
        WWWForm form = new WWWForm();

        UserHabitsPut obj = new UserHabitsPut();

        obj.data1.Add(new habitinfoput()
        {
            Habit_ID = cigarettesID,
            amount = formcigs,
            label = "value"
        });
        Debug.Log("check habit id: " + cigarettesID);
        Debug.Log("amount: " + formcigs);
        Debug.Log("label: " + "value");

        obj.data1.Add(new habitinfoput()
        {
            Habit_ID = attemptsID,
            amount = formattempts,
            label = "value"

        });

        obj.data1.Add(new habitinfoput()
        {
            Habit_ID = yearsID,
            amount = formyears,
            label = "value"

        });


        string json = JsonUtility.ToJson(obj);


        Debug.Log("this is the json i created" + json);
        form.AddField("user", dbuserid);
        form.AddField("cthearray", json);

      
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
  

        }

    }

    [Serializable]
    public class UserHabits
    {
        public List<habitinfo> data;
    }
    [System.Serializable]

    public class habitinfo
    {
        public int Habit_ID;
      //  public string label;
       public int amount;
        public bool yesorno;

    }

[Serializable]
    public class habitinfoput
    {
        public int Habit_ID;
        public string label;
        public int amount;


    }

    [Serializable]
    public class UserHabitsPut
    {
        public List<habitinfoput> data1 = new List<habitinfoput>();
    }


}


