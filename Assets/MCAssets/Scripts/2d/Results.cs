using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Globalization;
using UnityEngine.UI;
using System;

public class Results : MonoBehaviour
{
    private int dbuserid;

    public Text textCigValue;
    public Text textQuitsValue;
    public Text textYearsSmoked;
    public Text textNrt;
    public Text textTTFC;
    public Text textQuitType;

    string gethabits = "https://masterchange.today/php_scripts/gethabits.php";

    public void Start()
    {

        dbuserid = PlayerPrefs.GetInt("dbuserid");
        StartCoroutine(getHabits());
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

            UserHabits loadedPlayerData = JsonUtility.FromJson<UserHabits>(json);
            Debug.Log("record count: " + loadedPlayerData.data.Count);
            for (int i = 0; i < loadedPlayerData.data.Count; i++)
            {
                //Debug.Log(loadedPlayerData.data[i].User_ID + "\n");
                //Debug.Log("hhhh" + loadedPlayerData.data[i].label + "\n");
                // Debug.Log("hhhh" + loadedPlayerData.data[i].amount + "\n");
                Debug.Log("hhhh yesorno" + loadedPlayerData.data[i].yesorno + "\n");
                if (loadedPlayerData.data[i].Habit_ID == 16)
                {
                    textCigValue.text = loadedPlayerData.data[i].amount.ToString();
                }
                if (loadedPlayerData.data[i].Habit_ID == 17)
                {
                    textQuitsValue.text = loadedPlayerData.data[i].amount.ToString();
                }
                if (loadedPlayerData.data[i].Habit_ID == 18)
                {
                    textYearsSmoked.text = loadedPlayerData.data[i].amount.ToString();
                }
                if (loadedPlayerData.data[i].Habit_ID == 19)
                {
                    if (loadedPlayerData.data[i].yesorno)
                    {
                        textQuitType.text = "No Quit Date Set";
                    }
                }
                if (loadedPlayerData.data[i].Habit_ID == 20)
                {
                    if (loadedPlayerData.data[i].yesorno)
                    {
                        textQuitType.text = "Quit Date Set";
                    }
                }
                if (loadedPlayerData.data[i].Habit_ID == 21)
                {
                    if (loadedPlayerData.data[i].yesorno)
                    {
                        textQuitType.text = "Quitting Now";
                    }
                }
                if (loadedPlayerData.data[i].Habit_ID == 22)
                {
                    if (loadedPlayerData.data[i].yesorno)
                    {
                        textNrt.text = "Yes";
                    }
                    else
                    {
                      
                    textNrt.text = "No";
                    }
                }
                if (loadedPlayerData.data[i].Habit_ID == 24)
                {
                    if (loadedPlayerData.data[i].yesorno)
                    {
                        textTTFC.text = "Yes";
                    }
                    else
                    {
                        textTTFC.text = "No";
                    }
                }


            }
        }


        //set the variables for the form

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




}