using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using UnityEngine.UI;
using System;

public class Food : MonoBehaviour
{
    public Toggle omnivore;
    public Toggle pescador;
    public Toggle vegetarian;
    public Toggle vegan;
    public Toggle takeaway;
    public Toggle microwave;
    public Toggle fried;
    public Toggle grilled;
    public Toggle steam;
    public Toggle grain;
    public Toggle fruit;
    public Toggle meat;
    public Toggle processed;
    public Slider portions;

    private int dbuserid;

    private int formomnivore;
    private int formpescador;
    private int formvegetarian;
    private int formvegan;
    private int formtakeaway;
    private int formmicrowave;
    private int formfried;
    private int formgrilled;
    private int formsteam;
    private int formgrain;
    private int formfruit;
    private int formmeat;
    private int formprocessed;
    private int formportions;

    private int omnivoreID = 50;
    private int pescadorID = 51;
    private int vegetarianID = 52;
    private int veganID = 53;
    private int takeawayID = 58;
    private int microwaveID = 54;
    private int friedID = 55;
    private int grilledID = 56;
    private int steamID = 57;
    private int grainID = 59;
    private int fruitID = 60;
    private int meatID = 61;
    private int processedID = 62;
    private int portionsID = 63;


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
                if (loadedPlayerData.data[i].Habit_ID == omnivoreID)
                {
                    omnivore.isOn = loadedPlayerData.data[i].yesorno;
                }
                if (loadedPlayerData.data[i].Habit_ID == pescadorID)
                {
                     pescador.isOn = loadedPlayerData.data[i].yesorno;
                }
                if (loadedPlayerData.data[i].Habit_ID == vegetarianID)
                {
                    vegetarian.isOn = loadedPlayerData.data[i].yesorno;
                }
                if (loadedPlayerData.data[i].Habit_ID == veganID)
                {
                    vegan.isOn = loadedPlayerData.data[i].yesorno;
                }
                if (loadedPlayerData.data[i].Habit_ID == takeawayID)
                {
                    takeaway.isOn = loadedPlayerData.data[i].yesorno;
                }
                if (loadedPlayerData.data[i].Habit_ID == microwaveID)
                {
                    microwave.isOn = loadedPlayerData.data[i].yesorno;
                }
                if (loadedPlayerData.data[i].Habit_ID == friedID)
                {
                    fried.isOn = loadedPlayerData.data[i].yesorno;
                }
                if (loadedPlayerData.data[i].Habit_ID == grilledID)
                {
                    grilled.isOn = loadedPlayerData.data[i].yesorno;
                }
                if (loadedPlayerData.data[i].Habit_ID == steamID)
                {
                    steam.isOn = loadedPlayerData.data[i].yesorno;

                }
                if (loadedPlayerData.data[i].Habit_ID == grainID)
                {
                    grain.isOn = loadedPlayerData.data[i].yesorno;

                }
                if (loadedPlayerData.data[i].Habit_ID == fruitID)
                {
                    fruit.isOn = loadedPlayerData.data[i].yesorno;

                }
                if (loadedPlayerData.data[i].Habit_ID == meatID)
                {
                    meat.isOn = loadedPlayerData.data[i].yesorno;

                }
                if (loadedPlayerData.data[i].Habit_ID == processedID)
                {
                    processed.isOn = loadedPlayerData.data[i].yesorno;

                }
                if (loadedPlayerData.data[i].Habit_ID == portionsID)
                {
                    portions.value = loadedPlayerData.data[i].amount;

                }

            }
        }

    }



    public void OnChangeOmn()
    {

    }
    public void OnChangeFoodPesc()
    {

    }
    public void OnChangeFoodVeg()
    {

    }
    public void OnChangeFoodVegan()
    {

    }

    public void OnChangeTakeaway()
    {
        Debug.Log("takeaway " + takeaway.isOn);

    }

    public void OnChangeMicrowave()
    {
        Debug.Log("microwave: " + microwave.isOn);

    }
    public void OnChangeFried()
    {
        Debug.Log("fried " + fried.isOn);

    }
    public void OnChangeGrilled()
    {
        Debug.Log("grilled: " + grilled.isOn);

    }

    public void OnChangeSteam()
    {
        Debug.Log("steam: " + steam.isOn);

    }

    public void OnChangeGrain()
    {
        Debug.Log("grain: " + grain.isOn);

    }

    public void OnChangeFruit()
    {
        Debug.Log("fruit: " + fruit.isOn);

    }

    public void OnChangeMeat()
    {
        Debug.Log("meat: " + meat.isOn);

    }
    public void OnChangeProcessed()
    {
        Debug.Log("processed: " + processed.isOn);

    }
    public void OnChangePortions()
    {
        Debug.Log("portions: " + portions.value);
   
    }



    public void sethabits()
    {
        formomnivore = Convert.ToInt32(omnivore.isOn);
        formpescador = Convert.ToInt32(pescador.isOn);
        formvegetarian = Convert.ToInt32(vegetarian.isOn);
        formvegan = Convert.ToInt32(vegan.isOn);
        formtakeaway = Convert.ToInt32(takeaway.isOn);
        formmicrowave = Convert.ToInt32(microwave.isOn);
        formfried = Convert.ToInt32(fried.isOn);
        formgrilled = Convert.ToInt32(grilled.isOn);
        formsteam = Convert.ToInt32(steam.isOn);
        formgrain = Convert.ToInt32(grain.isOn);
        formfruit = Convert.ToInt32(fruit.isOn);
        formmeat = Convert.ToInt32(meat.isOn);
        formprocessed = Convert.ToInt32(processed.isOn);
        formportions = Convert.ToInt32(portions.value);
        StartCoroutine(pushhabits());
    }



    IEnumerator pushhabits()
    {
        WWWForm form = new WWWForm();

        UserHabitsPut obj = new UserHabitsPut();

        obj.data1.Add(new habitinfoput()
        {
            Habit_ID = omnivoreID,
            amount = formomnivore,
            label = "binary"
        });

        obj.data1.Add(new habitinfoput()
        {
            Habit_ID = pescadorID,
            amount = formpescador,
            label = "binary"

        });

        obj.data1.Add(new habitinfoput()
        {
            Habit_ID = vegetarianID,
            amount = formvegetarian,
            label = "binary"
        });

        obj.data1.Add(new habitinfoput()
        {
            Habit_ID = veganID,
            amount = formvegan,
            label = "binary"
        });

        obj.data1.Add(new habitinfoput()
        {
            Habit_ID = takeawayID,
            amount = formtakeaway,
            label = "binary"
        });

        obj.data1.Add(new habitinfoput()
        {
            Habit_ID = microwaveID,
            amount = formmicrowave,
            label = "binary"
        });

        obj.data1.Add(new habitinfoput()
        {
            Habit_ID = friedID,
            amount = formfried,
            label = "binary"
        });

        obj.data1.Add(new habitinfoput()
        {
            Habit_ID = grilledID,
            amount = formgrilled,
            label = "binary"
        });
        obj.data1.Add(new habitinfoput()
        {
            Habit_ID = steamID,
            amount = formsteam,
            label = "binary"
        });
        obj.data1.Add(new habitinfoput()
        {
            Habit_ID = grainID,
            amount = formgrain,
            label = "binary"
        });
        obj.data1.Add(new habitinfoput()
        {
            Habit_ID = fruitID,
            amount = formfruit,
            label = "binary"
        });
        obj.data1.Add(new habitinfoput()
        {
            Habit_ID = meatID,
            amount = formmeat,
            label = "binary"
        });
        obj.data1.Add(new habitinfoput()
        {
            Habit_ID = processedID,
            amount = formprocessed,
            label = "binary"
        });
        obj.data1.Add(new habitinfoput()
        {
            Habit_ID = portionsID,
            amount = formportions,
            label = "value"
        });
        string json = JsonUtility.ToJson(obj);


        Debug.Log("this is the json i created" + json);
        form.AddField("user", dbuserid);
        form.AddField("cthearray", json);

        //UnityWebRequest www1 = UnityWebRequest.Post(posturl, form); // The file location for where my .php file is.
        //yield return www1.SendWebRequest();
        //if (www1.isNetworkError || www1.isHttpError)
        //{
        //    Debug.Log(www1.error);
        //    // errorMessage = www.error;
        //}
        //else
        //{

        //    Debug.Log("Form Upload Complete!");

        //    Debug.Log("this comes back" + www1.downloadHandler.text);


        //}


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
            //ChangesceneInc = FindObjectOfType<ChangesceneInc>();
            //ChangesceneInc.ChangeSceneNow(Switchscene);

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