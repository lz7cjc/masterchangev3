using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;

//do this when submitting the results from the second habits page
public class drinks : MonoBehaviour
{

    //remote
    string inserthabits = "https://masterchange.today/php_scripts/drinktracker.php";
    //   string gethabits = "https://masterchange.today/php_scripts/gethabits.php";

    private int dbuserid;
    private int drinkID_U;
    private int DrinkValue_U;
     public Text errormessage;
    public Dropdown dateofdrink;
    public Button adddrinks;

    private string json;

    private int whichDay;
    private string whichDay2;

    public void chngDDL()
    {
         whichDay = dateofdrink.value;
         if (whichDay == 0)
        {
            whichDay2 = "Today";
            adddrinks.interactable = true;
        }
        else if (whichDay == 1)
        {
            whichDay2 = "Yesterday";
            adddrinks.interactable = true;
        }
        else if (whichDay == 2)
        {
            whichDay2 = "Day before yesterday";
            adddrinks.interactable = true;
        }
        else if (whichDay == 3)
        {
            whichDay2 = "Three days ago";
            adddrinks.interactable = true;
        }
        else if (whichDay == 4)
        {
            whichDay2 = "Four days ago";
            adddrinks.interactable = true;
        }
        else if (whichDay == 5)
        {
            whichDay2 = "Five days ago";
            adddrinks.interactable = true;
        }
        else if (whichDay == 6)
        {
            whichDay2 = "Six days ago";
            adddrinks.interactable = true;
        }
        else if (whichDay == 7)
        {
            whichDay2 = "Seven days ago";
            adddrinks.interactable = true;
        }
    }
    public void setDrinks()
    {
       
       
        dbuserid = PlayerPrefs.GetInt("dbuserid");

        var inputboxvalue = GameObject.FindObjectsOfType<InputField>();

        UserDrinksPut obj = new UserDrinksPut();
        foreach (var inputbox in inputboxvalue)
        {
            var drinkValue = inputbox.text;
                 
            if (drinkValue =="")
            {
                DrinkValue_U = 0;
            }
            else
            {
                DrinkValue_U = Convert.ToInt32(drinkValue);
            }
                   var drinkID = inputbox.name;
                   drinkID_U = Convert.ToInt32(drinkID);

            //add to JSON
            obj.data1.Add(new drinkinfoput()
            {
                Drink_ID = drinkID_U,
                amount = DrinkValue_U,

            });

        }
        json = JsonUtility.ToJson(obj);
        Debug.Log("json is: " + json);

        StartCoroutine(pushhabits());
    }

    IEnumerator pushhabits()
    {
         Debug.Log("thectarrya: " + json);
       Debug.Log("instide posting");

        Debug.Log("what value comes out of the ddl: " + whichDay);

        WWWForm form = new WWWForm();
        form.AddField("user", dbuserid);
        form.AddField("daysago", whichDay);
        form.AddField("drinkArray", json);
        UnityWebRequest www = UnityWebRequest.Post(inserthabits, form); // The file location for where my .php file is.
   
          yield return www.SendWebRequest();
        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
             errormessage.text = "Oops, sorry but that didn't work. Please can you email us at bugs@masterchange.today; tell us what you were trying to do and the error message that follows and if we can replicate you will receive 1000 riros: <b>" + www.error + "</b>";
        }
        else
        {
            errormessage.text = "You told us how much you drank " + whichDay2 + ". Thanks. The more days you record, the better insights you will get. Do a week and it is helpful, record for a month and even better";
            
        }
    }





    [Serializable]
    public class drinkinfoput
    {
        public int Drink_ID;
         public int amount;
    

    }

    [Serializable]
    public class UserDrinksPut
    {
        public List<drinkinfoput> data1 = new List<drinkinfoput>();
    }


}

