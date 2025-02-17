using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;

//do this when submitting the results from the second habits page
public class dbbio : MonoBehaviour
{

    //remote
    string inserthabits = "https://masterchange.today/php_scripts/habitvaluesjson.php";
    string gethabits = "https://masterchange.today/php_scripts/gethabits.php";
    //local
    // readonly string posturl = "http://localhost/php_scripts/habit1insert.php";


    //public string errorMessage;
    //private bool doesExisthabits;
    private int dbuserid;
    public Slider Age;
    public Slider Weight;
    public Slider Height;
    private string Switchscene;
  

    private int formAge;
    private int formHeight;
    private int formWeight;

    public Text txtAge;
    public Text txtWeight;
    public Text txtHeight;
    public Text TextBMI;
    public Text errormessage;
    private int ageid = 26;
    private int weightid = 27;
    private int heightid = 28;

    private justSetRiros justSetRiros;
    private float BMI;


    //  public globalvariables globaltest;
    private void Start()
    {
        dbuserid = PlayerPrefs.GetInt("dbuserid");
        StartCoroutine(getHabits());

    }

   
    IEnumerator getHabits()
    {
        WWWForm forma = new WWWForm();

        forma.AddField("dbuserid", dbuserid);
        UnityWebRequest wwwa = UnityWebRequest.Post(gethabits, forma); // The file location for where my .php file is.
        yield return wwwa.SendWebRequest();
        if (wwwa.isNetworkError || wwwa.isHttpError)
        {
            
        }
        else
        {

             string json = wwwa.downloadHandler.text;
            UserHabits loadedPlayerData = JsonUtility.FromJson<UserHabits>(json);
            for (int i = 0; i < loadedPlayerData.data.Count; i++)
            {
                if (loadedPlayerData.data[i].Habit_ID == 26)
                {
                    Age.value = loadedPlayerData.data[i].amount;
                }
                if (loadedPlayerData.data[i].Habit_ID == 27)
                {
                    Weight.value = loadedPlayerData.data[i].amount;
                }
                if (loadedPlayerData.data[i].Habit_ID == 28)
                {
                    Height.value = loadedPlayerData.data[i].amount;
                }

            }


        }
    }


    public void changeAge()
    {
         txtAge.text = Age.value.ToString();

    }
    public void changeWeight()
    {
         txtWeight.text = Weight.value.ToString();
        BMIcalc();
    }
    public void changeHeight()
    {
        txtHeight.text = Height.value.ToString();
        BMIcalc();
    }
    void BMIcalc()
    {

        BMI = (Weight.value / Mathf.Pow(Height.value / 100, 2));
        TextBMI.text = BMI.ToString();
    }

    public void sethabits(string nextScene)
    {
       
        formAge = Convert.ToInt32(Age.value);
        formHeight = Convert.ToInt32(Height.value);
        formWeight = Convert.ToInt32(Weight.value);
        //set the the global variable for next scene
        globalvariables.Instance.nextScene = nextScene;
        Switchscene = globalvariables.Instance.nextScene;
        StartCoroutine(pushhabits());
    }



    IEnumerator pushhabits()
    {
        WWWForm formb = new WWWForm();
        UserHabitsPut obj = new UserHabitsPut();

        obj.data1.Add(new habitinfoput()
        {
            Habit_ID = ageid,
            amount = formAge,
            label = "value"


        });

        obj.data1.Add(new habitinfoput()
        {
            Habit_ID = heightid,
            amount = formHeight,
            label = "value"

        });

        obj.data1.Add(new habitinfoput()
        {
            Habit_ID = weightid,
            amount = formWeight,
            label = "value"

        });

        string json = JsonUtility.ToJson(obj);

        //send the habits as JSON to php
         formb.AddField("user", dbuserid);
        formb.AddField("cthearray", json);
         //insert the habits and return whether new and therefore needs payment or replacement and therefore no payment
        UnityWebRequest www = UnityWebRequest.Post(inserthabits, formb); // The file location for where my .php file is.
        yield return www.SendWebRequest();
        if (www.isNetworkError || www.isHttpError)
        {
            // Debug.Log("qqqq" + www.error);
            errormessage.text = "Oops, sorry but that didn't work. Can you email us at bugs@masterchange.today; tell us what you were trying to do and the error message that follows and if we can replicate you will receive 1000 riros: <b>" + www.error + "</b>";
        }
        else
        {
            string dowepay = www.downloadHandler.text;
            newInfo newInfo = JsonUtility.FromJson<newInfo>(dowepay);
            
            //do we need to pay for this information? 
             string dopay = newInfo.reward;
             if (dopay == "1")
            {
                 timeToPay();
            }
            //Debug.Log("shall we pay:" + shallwepay);       
             else
            {
                SceneManager.LoadScene(Switchscene);
            }
        }
}
        public void timeToPay()
        {
        Debug.LogWarning("time tp pay function" + Switchscene);
            justSetRiros = FindObjectOfType<justSetRiros>();
        justSetRiros.toPayOut();

    }
}

    public class newInfo
    {
        public string reward;
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




