using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;

public class drinkstodb : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject drinkstats;
   string inserthabits = "https://masterchange.today/php_scripts/drinktracker.php";
      string gethabits = "https://masterchange.today/php_scripts/getdrinks.php";

 //   string gethabits = "http://localhost/php_scripts/getdrinks.php";
  //  string inserthabits = "http://localhost/php_scripts/drinktracker.php";

    private int dbuserid;
    private string Switchscene;
    private int habitID_U;
    private int habitValue_U;
      public Text errormessage;
    public Text userDays;
    public Text userDate;
    private DateTime dateIs;
    private int days;
    private justSetGetRiros justSetGetRiros;
    private string json;
    private DateTime displayDate;
    public Dropdown pickDay;

    private int recordDay;
    void Start()
    {
        recordDay = PlayerPrefs.GetInt("whichday");
        pickDay.value = recordDay;       
        userDays.text = recordDay.ToString() + " days ago";
        userDate.text = "Date to track: " + displayDate.ToString("dd/MM/yy");
        dbuserid = PlayerPrefs.GetInt("dbuserid");
        StartCoroutine(getDrinks());
    }
    public void ddlchange()
    {
        recordDay = pickDay.value;
        Debug.Log("ddl: " + recordDay);
        if (recordDay == 0)
        {
            
            PlayerPrefs.SetInt("whichday", 0);
            userDays.text = recordDay.ToString() + " days ago";
            displayDate = DateTime.Now.Date.AddDays(0);
            userDate.text = "Date to track: " + displayDate.ToString("dd/MM/yy");

        }
        if (recordDay == 1)
        {
            PlayerPrefs.SetInt("whichday", 1);
            userDays.text = recordDay.ToString() + " days ago";
            displayDate = DateTime.Now.Date.AddDays(-1);
            userDate.text = "Date to track: " + displayDate.ToString("dd/MM/yy");

           

        }
        if (recordDay == 2)
        {
   
            PlayerPrefs.SetInt("whichday", 2);
            userDays.text = recordDay.ToString() + " days ago";
            displayDate = DateTime.Now.Date.AddDays(-2);
            userDate.text = "Date to track: " + displayDate.ToString("dd/MM/yy");


        }
        if (recordDay == 3)
        {
           
            PlayerPrefs.SetInt("whichday", 3);
            userDays.text = recordDay.ToString() + " days ago";
            displayDate = DateTime.Now.Date.AddDays(-3);
            userDate.text = "Date to track: " + displayDate.ToString("dd/MM/yy");

            
              }
        if (recordDay == 4)
        {
            PlayerPrefs.SetInt("whichday", 4);
            userDays.text = recordDay.ToString() + " days ago";
            displayDate = DateTime.Now.Date.AddDays(-4);
            userDate.text = "Date to track: " + displayDate.ToString("dd/MM/yy");

   

    }
        userDate.text = "Date to track: " + displayDate.ToString("dd/MM/yy");

       StartCoroutine(getDrinks());
    }

    IEnumerator getDrinks()
    {
        WWWForm forma = new WWWForm();

        forma.AddField("dbuserid", dbuserid);
        forma.AddField("whichday", recordDay);
        Debug.Log("which day is being sent to server: " + recordDay);

        UnityWebRequest wwwa = UnityWebRequest.Post(gethabits, forma); // The file location for where my .php file is.
        yield return wwwa.SendWebRequest();
        if (wwwa.isNetworkError || wwwa.isHttpError)
        {

        }
        else
        {

            string json = wwwa.downloadHandler.text;
            Debug.Log("json from db: " + json);
         
            if (json != "")
            { 
                UserHabits loadedPlayerData = JsonUtility.FromJson<UserHabits>(json);
                var drinks = drinkstats.GetComponentsInChildren<Text>();
                Debug.Log("111. record count: " + loadedPlayerData.data.Count);

          //   bool jsonValid = json.
            //sliders
                foreach (var drinkis in drinks)
                {
                    var nameofdrink = drinkis.name;
                    var drinkValue = drinkis.text;
                    Debug.Log("name of drink: " + nameofdrink);
                    Debug.Log("drinkValue: " + drinkValue);

                    int numbernameofdrink = Convert.ToInt32(nameofdrink);

                        for (int i = 0; i < loadedPlayerData.data.Count; i++)
                        {
                            Debug.Log("4444. should be each ID of habits: " + loadedPlayerData.data[i].drinkid);
                            if (loadedPlayerData.data[i].drinkid == numbernameofdrink)
                            {
                                Debug.Log("5555 value of slider: " + drinkis.text);
                                drinkis.text = loadedPlayerData.data[i].amount.ToString();
                            }
                        }
                }
            }
            else 
            {
            var drinks = drinkstats.GetComponentsInChildren<Text>();
          
                //   bool jsonValid = json.
                //sliders
                foreach (var drinkis in drinks)
                {
                      drinkis.text = "0";
                  
              
                    
                }

            }
        }
    }

        public void setDrinks(string nextScene)
        {
            globalvariables.Instance.nextScene = nextScene;
            Switchscene = globalvariables.Instance.nextScene;

        var drinksIn = drinkstats.GetComponentsInChildren<Text>();

        //get the values of each slider
        //get the label for each slider
        //set the obj to loop until count is finished
        //set the the global variable for next scene
        UserHabitsPut obj = new UserHabitsPut();
            foreach (var drinkIs2 in drinksIn)
            {
                var drinkValue = drinkIs2.text;
             //   habitValue_U = Convert.ToInt32(habitValue);
                var drinkID = drinkIs2.name;
            //    habitID_U = Convert.ToInt32(habitID);
                Debug.Log("aaaaa. is it on drinkValue" + drinkValue);
                Debug.Log("bbbb. name of  drinkID:" + drinkID);

                //add to JSON
                obj.data1.Add(new drinkoutput()
                {
                    drinkName = Convert.ToInt32(drinkID),
                    drinkAmount = drinkValue,
                    label = "drink"
                });

            }

            json = JsonUtility.ToJson(obj);
            Debug.Log("json is: " + json);
            StartCoroutine(pushhabits());
        }

        IEnumerator pushhabits()
        {
            Debug.Log("thectarrya: " + json);
        Debug.Log("user" + dbuserid);
        Debug.Log("whichday" + recordDay);
        WWWForm form = new WWWForm();
            form.AddField("user", dbuserid);
            form.AddField("drinkArray", json);
              form.AddField("daysago", recordDay);

        UnityWebRequest www = UnityWebRequest.Post(inserthabits, form); // The file location for where my .php file is.

            yield return www.SendWebRequest();
            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);
                errormessage.text = "Oops, sorry but that didn't work. Please can you email us at bugs@masterchange.today; tell us what you were trying to do and the error message that follows and if we can replicate you will receive 1000 riros: <b>" + www.error + "</b>";
            }
            else
            {
                string dowepay = www.downloadHandler.text;
                Debug.Log("do we pay" + dowepay);
                newInfo newInfo = JsonUtility.FromJson<newInfo>(dowepay);
                string dopay = newInfo.reward;
                Debug.LogWarning("dopay is: " + dopay);
                if (dopay == "1")
                {
                    timeToPay();
                }

                 errormessage.text = "Congratulations; you have recorded your drinking. Choose another day from the drop down list, view your stats or explore the rest of MasterChange";
         
            }
        }


    public void timeToPay()
    {
        Debug.LogWarning("time tp pay function" + Switchscene);
        justSetGetRiros = FindObjectOfType<justSetGetRiros>();
        justSetGetRiros.toPayOut();

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
        public int drinkid;
        public int label;
        public int amount;
   
    }


    [Serializable]
    public class drinkoutput
    {
        public int drinkName;
        public string drinkAmount;
        public string label;


    }

    [Serializable]
    public class UserHabitsPut
    {
        public List<drinkoutput> data1 = new List<drinkoutput>();
    }


}


