using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Networking;
using System;

public class Coefficient : MonoBehaviour

{
    private int Cigsperday;
    private int YearsSmoked;
    private int QuitAttempts;
    private int Prequit;
    private int Quitting;
    private int NotStarted;
    private int NRT;
    private int TTFCLess;
    private int TTFCMore;
    private int JC;
    // private string JCFinal;
    private bool doesExisthabits;
    //  public Text JCfinal;
    public Text JCtext;
    private int dbuserid; 
    string gethabits = "https://masterchange.today/php_scripts/gethabits.php";
    private syncfromdb syncfromdb;
 

    private void Start()
    {
        dbuserid = PlayerPrefs.GetInt("dbuserid");
         if (PlayerPrefs.HasKey("habitsdone"))
        {
            if (PlayerPrefs.GetInt("habitsdone") == 1)
            {
                StartCoroutine(getHabits());
            }
            else if (PlayerPrefs.GetInt("habitsdone") == 0)
            {
                PlayerPrefs.SetFloat("JCP", 0);

                //trying to make the redirect more intelligent; if they are going back to hospital and have not set habits send to habits
                ///////////////////////////
                ///
                if ((PlayerPrefs.GetString("returntoscene") == "hospital") && (PlayerPrefs.GetString("behaviour") == "smoking"))
                {
                    SceneManager.LoadScene("habitsdb");
                }
                else
                { 
                //////////////////////////////
                JCtext.text = "TBC";
                }
            }
        }
         else
        {
            PlayerPrefs.SetFloat("JCP", 0);
            JCtext.text = "TBC";
        }            

    }

    IEnumerator getHabits()
    {
        WWWForm forma = new WWWForm();

        forma.AddField("dbuserid", dbuserid);
        UnityWebRequest wwwa = UnityWebRequest.Post(gethabits, forma); // The file location for where my .php file is.
        yield return wwwa.SendWebRequest();
        if (wwwa.isNetworkError || wwwa.isHttpError)
        {
            JCtext.text = "oops";
        }
        else
        {

            string json = wwwa.downloadHandler.text;
            UserHabits loadedPlayerData = JsonUtility.FromJson<UserHabits>(json);

            for (int i = 0; i < loadedPlayerData.data.Count; i++)
            {
                //Debug.Log("4444. should be each ID of habits: " + loadedPlayerData.data[i].Habit_ID);
                if (loadedPlayerData.data[i].Habit_ID == 16)
                {
                    Cigsperday = loadedPlayerData.data[i].amount;
                }
                if (loadedPlayerData.data[i].Habit_ID == 17)
                {
                    QuitAttempts = loadedPlayerData.data[i].amount;
                }
                if (loadedPlayerData.data[i].Habit_ID == 18)
                {
                    YearsSmoked = loadedPlayerData.data[i].amount;
                }
                if (loadedPlayerData.data[i].Habit_ID == 19)
                {
                    Quitting = loadedPlayerData.data[i].yesorno;
                }
                if (loadedPlayerData.data[i].Habit_ID == 20)
                {
                    Prequit = loadedPlayerData.data[i].yesorno;
                }
                if (loadedPlayerData.data[i].Habit_ID == 21)
                {
                    NotStarted = loadedPlayerData.data[i].yesorno;
                }
                if (loadedPlayerData.data[i].Habit_ID == 22)
                {
                    NRT = loadedPlayerData.data[i].yesorno;
                }

                if (loadedPlayerData.data[i].Habit_ID == 24)
                {
                    TTFCLess = loadedPlayerData.data[i].yesorno;
                }

                if (loadedPlayerData.data[i].Habit_ID == 25)
                {
                    TTFCMore = loadedPlayerData.data[i].yesorno;
                }
/* Want to include all the factors but need a better way to scroll through
                if (loadedPlayerData.data[i].Habit_ID == 20)
                {
                    Prequit = loadedPlayerData.data[i].yesorno;
                }
                if (loadedPlayerData.data[i].Habit_ID == 20)
                {
                    Prequit = loadedPlayerData.data[i].yesorno;
                }
                if (loadedPlayerData.data[i].Habit_ID == 20)
                {
                    Prequit = loadedPlayerData.data[i].yesorno;
                }
                if (loadedPlayerData.data[i].Habit_ID == 20)
                {
                    Prequit = loadedPlayerData.data[i].yesorno;
                }*/





            }

        calculateCoefficient();
        }

    }

        void calculateCoefficient()
        {
            //taking NRT
            if (NRT==0)
            {
                JC = JC + 10;
            }
            //Debug.Log("JeopardyCoefficent after nrt" + JC);

            //cigs per day - max 10
            if (Cigsperday < 5)
            {
                JC = JC + 1;
            }

            else if (Cigsperday >= 5 && Cigsperday < 10)
            {
                JC = JC + 3;
            }

            else if (Cigsperday >= 10 && Cigsperday < 20)
            {
                JC = JC + 4;
            }

            else if (Cigsperday >= 20 && Cigsperday < 40)
            {
                JC = JC + 6;
            }

            else if (Cigsperday >= 40)
            {
                JC = JC + 10;
            }
            //Debug.Log("JeopardyCoefficent Cigsperday" + JC);

            //years smoked - max 10
            if (YearsSmoked < 1)
            {
                JC = JC + 1;
            }

            else if (YearsSmoked >= 1 && YearsSmoked < 3)
            {
                JC = JC + 2;
            }

            else if (YearsSmoked >= 3 && YearsSmoked < 8)
            {
                JC = JC + 4;
            }

            else if (YearsSmoked >= 8 && YearsSmoked < 15)
            {
                JC = JC + 6;
            }

            else if (YearsSmoked >= 15)
            {
                JC = JC + 10;
            }
            //Debug.Log("JeopardyCoefficent YearsSmoked" + JC);
            //ttfc - max 5
            if (TTFCLess == 1)
            {
                JC = JC + 5;
            }
            else
            {
                JC = JC + 3;
            }
            //Debug.Log("JeopardyCoefficent TTFC" + JC);
            //take away number of quit attempts
            JC = JC - (QuitAttempts / 2);
            Debug.Log("JeopardyCoefficent quitattempts last one" + JC);


            float JCpercent = (float)JC;

            JCpercent = Mathf.Round(JCpercent / 35 * 100);
            //Max score 35
            // float JCFinal = (float)JC;
            // JCFinal = (JC / 35 * 100);
            //  Debug.Log("JC final" + JCFinal);
            // Debug.Log("Final JeopardyCoefficent" + JCFinal);

            PlayerPrefs.SetFloat("JCP", JCpercent);
            JCtext.text = JCpercent.ToString();
        syncfromdb = FindObjectOfType<syncfromdb>();
        syncfromdb.firstFunction();

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
        public int yesorno;

    }

    //Jeopardy Coefficient
    public float jeopardyCoefficient;
    }




