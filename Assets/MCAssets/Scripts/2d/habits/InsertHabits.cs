using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Globalization;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;


public class InsertHabits : MonoBehaviour
{
    //remote
 
    public Slider smokes;
    public Slider years;
    public Slider attempts;
    //public string errorMessage;
    private bool doesExisthabits;
    private int userInt;
    private Text ttfcless;
    //private Text ttfcmore;
    private Text nrtYes;
    //private Text nrtNo;
    private Text prequit;
    private Text quitting;
    private Text nodate;
    public Text textCigValue;
    public Text textQuitsValue;
    public Text textYearsSmoked;
    //private bool existsUser;
    public void Start()
    {
        string habits = Application.persistentDataPath + "/habit1.json";
            doesExisthabits = File.Exists(habits);
        if (doesExisthabits)
        {
           
            string json = File.ReadAllText(habits);
           // Debug.Log(json);
            PlayerData loadedPlayerData1 = JsonUtility.FromJson<PlayerData>(json);

            //set the variables for the form
            smokes.value = loadedPlayerData1.JSONcigsPerDay;
            years.value = loadedPlayerData1.JSONyearsSmoked;
            attempts.value = loadedPlayerData1.JSONpreviousQuits;
            ttfcless = loadedPlayerData1.JSONttfcless;
            nrtYes = loadedPlayerData1.JSONnrt;
            prequit = loadedPlayerData1.JSONprequit;
            quitting = loadedPlayerData1.JSONquitting;
            nodate = loadedPlayerData1.JSONnodate;
          //  Health factors
        }
        else
        {
            smokes.value = 15;
            years.value = 10;
            attempts.value = 3;
            //ttfcless.isOn = ttfcless.isOn;
            //nrt.isOn = nrt.isOn;
            //prequit.isOn = nrt.isOn;
            //quitting.isOn = nrt.isOn;
            //nodate.isOn = nrt.isOn;

        }
    }
    public void Cigsday(float value)
    {
       // Debug.Log("New cig per day Value " + smokes.value);

    }
    public void Quitattempts(float value)
    {
      //  Debug.Log("New attempts Value " + attempts.value);

    }
    public void YearsSmoked(float value)
    {
      //  Debug.Log("New years Value " + years.value);

    }
    //public void CallRegisterCoroutine()
    //{
    //    if (existsUser)
    //    {
    //        StartCoroutine(Habits());
    //        updateJsonFile();
    //    }
    //    else
    //    {
    //        updateJsonFile();
    //    }
    //  //  Debug.Log("in the call register coroutine");
        
    //}

//    IEnumerator Habits()
//{
//        Debug.Log("in the habits function");
//        WWWForm form = new WWWForm();
//        form.AddField("cigsPerDay", smokes.value.ToString());
//        form.AddField("yearsSmoked", years.value.ToString());
//        form.AddField("previousQuits", attempts.value.ToString());
//        form.AddField("user", userInt);
//        Debug.Log("user id in IEnumerator Habits()" + userInt);

//        Debug.Log("form values");
//        Debug.Log("cigsPerDay" + smokes.value.ToString());
//        Debug.Log("yearsSmoked" + years.value.ToString());
//        Debug.Log("previousQuits" + attempts.value.ToString());




//        UnityWebRequest www = UnityWebRequest.Post(posturl, form); // The file location for where my .php file is.
//        yield return www.SendWebRequest();
//        if (www.isNetworkError || www.isHttpError)
//        {
//            Debug.Log(www.error);
//           // errorMessage = www.error;

//        }
//        else
//        {

//            Debug.Log("Form Upload Complete!");
            
            
            
//        }
//    }
    void Update()
    {
        // moved this content to updateJsonFile() via coroutine function in case i can call once on button press
        textCigValue.text = smokes.value.ToString();
        textQuitsValue.text = attempts.value.ToString();
        textYearsSmoked.text = years.value.ToString();
        
      }

    public void updateJsonFile()
    {
        PlayerData playerData = new PlayerData
        {
            JSONttfcless = ttfcless,
            JSONnrt = nrtYes,
            JSONprequit = prequit,
            JSONquitting = quitting,
            JSONnodate = nodate,
            JSONcigsPerDay = smokes.value,
            JSONpreviousQuits = attempts.value,
            JSONyearsSmoked = years.value
        };

        string json = JsonUtility.ToJson(playerData);
       File.WriteAllText(Application.persistentDataPath + "/habit1.json", json);
        //File.AppendAllText(Application.persistentDataPath + "/habit1.json", json);
        SceneManager.LoadScene("habits1db");
    }
    private class PlayerData
    {
        public float JSONcigsPerDay;
        public float JSONpreviousQuits;
        public float JSONyearsSmoked;

        public Text JSONttfcless;
        //public bool JSONttfcmore;
        public Text JSONnrt;
        public Text JSONprequit;
        public Text JSONquitting;
        public Text JSONnodate;
    }


    //private class UserData
    //{
    //    public string User_id;
    //    public string Username;
    //    public string Fname;
    //    public float JSONJC;
    //    public int JSONRiros;

    //}
}
