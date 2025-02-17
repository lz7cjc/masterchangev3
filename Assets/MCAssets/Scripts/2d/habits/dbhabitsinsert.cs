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
public class dbhabitsinsert : MonoBehaviour
{
    //remote
    string posturl = "https://masterchange.today/php_scripts/habit1insert.php";
    //local
    // readonly string posturl = "http://localhost/php_scripts/habit1insert.php";


    //public string errorMessage;
    //private bool doesExisthabits;
    private int userInt;
    private bool ttfcless;
    //private Text ttfcmore;
    private bool nrtYes;
    //private Text nrtNo;
    private bool prequit;
    private bool quitting;
    private bool nodate;
    private int CigValue;
    private int QuitsValue;
    private int smokeyears;
    private bool existsUser;
    private string habits;
      
    public void CallRegisterCoroutine()
    {
        Debug.Log("in the db function");
        habits = Application.persistentDataPath + "/habit1.json";
       // doesExisthabits = File.Exists(habits);

        existsUser = PlayerPrefs.HasKey("dbuserid");
        if (existsUser)
        {
            userInt = PlayerPrefs.GetInt("dbuserid");
            Debug.Log("user id at top of insert habits" + userInt);
            //get the user habit information
            
            string json = File.ReadAllText(habits);
            // Debug.Log(json);
            PlayerData loadedPlayerData1 = JsonUtility.FromJson<PlayerData>(json);

            //set the variables for the form
            CigValue = loadedPlayerData1.JSONcigsPerDay;
            smokeyears = loadedPlayerData1.JSONyearsSmoked;
            QuitsValue = loadedPlayerData1.JSONpreviousQuits;
            Debug.Log("ttfcless is set to" + ttfcless);
            ttfcless = loadedPlayerData1.JSONttfcless;
            Debug.Log("nrtYes" + nrtYes);
            nrtYes = loadedPlayerData1.JSONnrt;
            Debug.Log("prequit" + prequit);
            prequit = loadedPlayerData1.JSONprequit;
            Debug.Log("quitting" + quitting);
            quitting = loadedPlayerData1.JSONquitting;
            Debug.Log("nodate" + nrtYes);
            nodate = loadedPlayerData1.JSONnodate;
            StartCoroutine(Habits());

        }
      
        
    }

    IEnumerator Habits()
{
        Debug.Log("in the habits function");
        WWWForm form = new WWWForm();

        Debug.Log("tostringvalue of bool" + quitting.ToString());
        form.AddField("cigsPerDay", CigValue);
        form.AddField("yearsSmoked", smokeyears);
        form.AddField("previousQuits", QuitsValue);

        Debug.Log("quit value bool to string" + prequit.ToString());
        Debug.Log("quit value bool to int" + Convert.ToInt32(prequit));
        form.AddField("prequit", Convert.ToInt32(prequit));
        form.AddField("quitting", Convert.ToInt32(quitting));
        form.AddField("nodate", Convert.ToInt32(nodate));
        form.AddField("ttfcless", Convert.ToInt32(ttfcless));
        form.AddField("nrt", Convert.ToInt32(nrtYes));
        form.AddField("user", userInt);
        Debug.Log("going to the db: prequit " + prequit);
        Debug.Log("going to the db: quitting" + quitting);
        Debug.Log("going to the db: nodate" + nodate);
        Debug.Log("going to the db: ttfcless" + ttfcless);
        Debug.Log("going to the db: nrt" + nrtYes);

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
    


    private class PlayerData
    {
        public int JSONcigsPerDay;
        public int JSONpreviousQuits;
        public int JSONyearsSmoked;

        public Text JSONttfcless;
        //public bool JSONttfcmore;
        public bool JSONnrt;
        public bool JSONprequit;
        public bool JSONquitting;
        public bool JSONnodate;
    }


   
}
