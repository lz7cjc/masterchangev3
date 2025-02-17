using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Globalization;
using UnityEngine.UI;
using System;

public class InsertHabits1 : MonoBehaviour
{
    public string Switchscene;

    public Toggle quitnow;
    public Toggle undecided;
    public Toggle prequit;
    public Toggle NRT;
    public Toggle TTFCunder;
    public Toggle TTFCover;
    public Toggle NRTno;

    private int dbuserid;

    private int formquitnow;
    private int formundecided;
    private int formprequit;
    private int formNRT;
    private int formTTFCunder;
    private int formTTFCover;
    private int formNRTno;

    private int quitnowID = 21;
    private int undecidedID = 19;
    private int prequitID = 20;
    private int NRTID = 22;
    private int TTFCunderID = 24;
    private int TTFCoverID = 25;
    private int NRTnoIDs = 23;

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
                Debug.Log("hhhh Habit_ID" + loadedPlayerData.data[i].Habit_ID + "\n");
                    Debug.Log("hhhh yesorno" + loadedPlayerData.data[i].yesorno + "\n");
                if (loadedPlayerData.data[i].Habit_ID == 21)
                {
                    quitnow.isOn = loadedPlayerData.data[i].yesorno;
                    Debug.Log("quitnow: " + loadedPlayerData.data[i].yesorno);
                }
                if (loadedPlayerData.data[i].Habit_ID == 19)
                {
                    undecided.isOn = loadedPlayerData.data[i].yesorno;
                    Debug.Log("undecided: " + loadedPlayerData.data[i].yesorno);
                }
                if (loadedPlayerData.data[i].Habit_ID == 20)
                {
                    prequit.isOn = loadedPlayerData.data[i].yesorno;
                    Debug.Log("prequit: " + loadedPlayerData.data[i].yesorno);
                }
                if (loadedPlayerData.data[i].Habit_ID == 22)
                {
                    NRT.isOn = loadedPlayerData.data[i].yesorno;
                    Debug.Log("NRT: " + loadedPlayerData.data[i].yesorno);
                }
                if (loadedPlayerData.data[i].Habit_ID == 23)
                {
                    NRTno.isOn = loadedPlayerData.data[i].yesorno;
                    Debug.Log("NRTno: " + loadedPlayerData.data[i].yesorno);
                }
                if (loadedPlayerData.data[i].Habit_ID == 24)
                {
                    TTFCunder.isOn = loadedPlayerData.data[i].yesorno;
                    Debug.Log("ttfc-: " + loadedPlayerData.data[i].yesorno);
                }
                if (loadedPlayerData.data[i].Habit_ID == 25)
                {
                    TTFCover.isOn = loadedPlayerData.data[i].yesorno;
                    Debug.Log("ttfc+: " + loadedPlayerData.data[i].yesorno);
                }

            }

    

        }
    }

  
    public void OnChangeQuitNow()
    {

    }



    public void OnChangeundecided()
    {
       

    }

    public void OnChangeprequit()
    {
      

    }
    public void OnChangeNRT()
    {
       

    }
    public void OnChangeNoNRT()
    {
       

    }

    public void OnChangettfcunder()
    {


    }
    public void OnChangettfcover()
    {


    }

       public void sethabits()
    {
        formquitnow = Convert.ToInt32(quitnow.isOn);
        formundecided = Convert.ToInt32(undecided.isOn); ;
        formprequit = Convert.ToInt32(prequit.isOn); ;
        formNRT = Convert.ToInt32(NRT.isOn); ;
        formTTFCunder = Convert.ToInt32(TTFCunder.isOn); ;
        formTTFCover = Convert.ToInt32(TTFCover.isOn); ;
        formNRTno = Convert.ToInt32(NRTno.isOn); ;
        StartCoroutine(pushhabits());
    }



    IEnumerator pushhabits()
    {
        WWWForm form = new WWWForm();

        UserHabitsPut obj = new UserHabitsPut();

        obj.data1.Add(new habitinfoput()
        {
            Habit_ID = quitnowID,
            amount = formquitnow,
            label = "binary"
        });


        obj.data1.Add(new habitinfoput()
        {
            Habit_ID = undecidedID,
            amount = formundecided,
            label = "binary"

        });

        obj.data1.Add(new habitinfoput()
        {
            Habit_ID = prequitID,
            amount = formprequit,
            label = "binary"
        });

        obj.data1.Add(new habitinfoput()
        {
            Habit_ID = NRTID,
            amount = formNRT,
            label = "binary"
        });

        obj.data1.Add(new habitinfoput()
        {
            Habit_ID = TTFCunderID,
            amount = formTTFCunder,
            label = "binary"
        });

        obj.data1.Add(new habitinfoput()
        {
            Habit_ID = TTFCoverID,
            amount = formTTFCover,
            label = "binary"
        });

        obj.data1.Add(new habitinfoput()
        {
            Habit_ID = NRTnoIDs,
            amount = formNRTno,
            label = "binary"
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
        //  public int amount;
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


































//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.Networking;
//using System.IO;
//using System.Globalization;
//using UnityEngine.UI;
//using System;
//using UnityEngine.SceneManagement;


//public class InsertHabits1 : MonoBehaviour
//{
//    //remote
//   string posturl = "https://masterchange.today/php_scripts/habit1insert.php";
//    //local
//    // readonly string posturl = "http://localhost/php_scripts/habit1insert.php";

//    //public string errorMessage;
//    private bool doesExisthabits;
//    private int userInt;
//    public Toggle ttfcless;
//    public Toggle ttfcmore;
//    public Toggle nrtYes;
//    public Toggle nrtNo;
//    public Toggle prequit;
//    public Toggle quitting;
//    public Toggle nodate;
//    //public float cigsPerDay;
//    //public float previousQuits;
//    //public float yearsSmoked;
//    private float CigValue;
//    private float QuitsValue;
//    private float YearsSmoked;
//    private bool existsUser;
//    public void Start()
//    {


//        //Debug.Log("user int at top of insert habits1" + userInt);
//       //get the user habit information
//        string habits = Application.persistentDataPath + "/habit1.json";
//        doesExisthabits = File.Exists(habits);


//        if (doesExisthabits)
//         {
//            string json = File.ReadAllText(habits);
//            Debug.Log(json);
//            PlayerData loadedPlayerData = JsonUtility.FromJson<PlayerData>(json);

//            //set the variables for the form
//            //smokes.value = loadedPlayerData1.JSONcigsPerDay;
//            //years.value = loadedPlayerData1.JSONyearsSmoked;
//            //attempts.value = loadedPlayerData1.JSONpreviousQuits;
//            ttfcless.isOn = loadedPlayerData.JSONttfcless;
//            ttfcmore.isOn = !loadedPlayerData.JSONttfcless;
//            nrtYes.isOn = loadedPlayerData.JSONnrt;
//            nrtNo.isOn = !loadedPlayerData.JSONnrt;
//            prequit.isOn = loadedPlayerData.JSONprequit;
//            quitting.isOn = loadedPlayerData.JSONquitting;
//            nodate.isOn = loadedPlayerData.JSONnodate;
//            CigValue = loadedPlayerData.JSONcigsPerDay;
//            QuitsValue = loadedPlayerData.JSONpreviousQuits;
//            YearsSmoked = loadedPlayerData.JSONyearsSmoked;

//         }

//    }
//    public void OnChangeNRTYes()
//    {

//    }

//    public void QuitStage()
//    {

//    }

//    public void Timetetofirst()
//    {

//    }




//    public void updateJsonFile()
//    {
//        PlayerData playerData = new PlayerData();

//        playerData.JSONnrt = nrtYes.isOn;
//        Debug.Log("playerData.JSONnrt" + playerData.JSONnrt);
//        Debug.Log("nrtYes.isOn" + nrtYes.isOn);
//        playerData.JSONprequit = prequit.isOn;
//        playerData.JSONquitting = quitting.isOn;
//        playerData.JSONnodate = nodate.isOn;
//        playerData.JSONttfcless = ttfcless.isOn;
//        playerData.JSONcigsPerDay = CigValue;
//        playerData.JSONpreviousQuits = QuitsValue;
//        playerData.JSONyearsSmoked = YearsSmoked;
//        Debug.Log("cigs per day in write to json" + CigValue);
//        string json = JsonUtility.ToJson(playerData);
//        File.WriteAllText(Application.persistentDataPath + "/habit1.json", json);

//        SceneManager.LoadScene("habits results");
//    }
//    private class PlayerData
//    {
//        public float JSONcigsPerDay;
//        public float JSONpreviousQuits;
//        public float JSONyearsSmoked;

//        public bool JSONttfcless;
//        //public bool JSONttfcmore;
//        public bool JSONnrt;
//        public bool JSONprequit;
//        public bool JSONquitting;
//        public bool JSONnodate;

//    }


//}
