using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Globalization;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;

//do this when submitting the results from the second habits page
public class dbhabitsinsertsandpit : MonoBehaviour
{
    //remote
    string posturl = "https://masterchange.today/php_scripts/habit1insert.php";
    string gethabits = "https://masterchange.today/php_scripts/gethabits.php";
    //local
    // readonly string posturl = "http://localhost/php_scripts/habit1insert.php";


    //public string errorMessage;
    //private bool doesExisthabits;
    private int dbuserid;
    public Slider CigValue;
    public Slider QuitsValue;
    public Slider smokeyears;

    private int formCigs;
    private int formQuits;
    private int formYears;

    public Text cigs;
        public Text years;
    public Text quits;
    private void Start()
    {
        dbuserid = PlayerPrefs.GetInt("dbuserid");
        StartCoroutine(getHabits());
        Debug.Log("it is running");
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
            var myData = JsonUtility.FromJson<UserHabits>("{\"data\":" + json + "}");
            // Debug.Log(json);
            foreach (var habit in myData.data)
            {
             
                if (habit.Habit_ID == 16)
                {
                    CigValue.value = habit.amount;
                 
                }
                if (habit.Habit_ID == 17)
                {
                    QuitsValue.value = habit.amount;
                
                }
                if (habit.Habit_ID == 18)
                {
                    smokeyears.value = habit.amount;
                }
            }


        }
        }

    //public void CallRegisterCoroutine()
    //{


    //        


    //}
    public void Cigsday(float value)
    {
        Debug.Log("New cig per day Value " + CigValue.value);
        cigs.text = CigValue.value.ToString();

    }
    public void Quitattempts(float value)
    {
          Debug.Log("New attempts Value " + QuitsValue.value);
        quits.text = QuitsValue.value.ToString();

    }
    public void YearsSmoked(float value)
    {
          Debug.Log("New years Value " + smokeyears.value);
        years.text = smokeyears.value.ToString();

    }

    public void sethabits()
    {
         formYears = Convert.ToInt32(smokeyears.value);
       formCigs = Convert.ToInt32(CigValue.value);
        formQuits = Convert.ToInt32(QuitsValue.value);
         StartCoroutine(pushhabits());
    }



    IEnumerator pushhabits()
    {
        Debug.Log("in the habits function");
        WWWForm form = new WWWForm();
        form.AddField("user", dbuserid);
        form.AddField("cigsPerDay", formCigs);
        form.AddField("yearsSmoked", formYears);
        form.AddField("previousQuits", formQuits);

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
