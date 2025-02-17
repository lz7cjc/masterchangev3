using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;


public class drinkReport : MonoBehaviour
{
    string getdays = "https://masterchange.today/php_scripts/trackeddays.php";
    string getunitsTotal = "https://masterchange.today/php_scripts/totalunits.php";
    string getworstdrinkday = "https://masterchange.today/php_scripts/worstdrinkday.php";

    // Start is called before the first frame update
    private int dbuserid;
    private string Switchscene;
    public Text errormessage;
    public Text daysTracked;
    public Text totalUnitstxt;
    public Text worstDrink;
    public Text worstDay;
    public Text averageUnitstxt;
    private int averageUnits;
   private int totalDays;
    private int totalUnits;
    

    private void Start()
    {
        dbuserid = PlayerPrefs.GetInt("dbuserid");
        StartCoroutine(getResults());
    }

    IEnumerator getResults()
    {
        //how maany tracked days
        WWWForm forma = new WWWForm();

        forma.AddField("user", dbuserid);
   
        UnityWebRequest wwwa = UnityWebRequest.Post(getdays, forma); // The file location for where my .php file is.
        yield return wwwa.SendWebRequest();
        if (wwwa.isNetworkError || wwwa.isHttpError)
        {
            daysTracked.text = "N/A";
            errormessage.text = "Sorry, something went wrong. Not sure where, not sure when";
        }
        else
        {

            string json = wwwa.downloadHandler.text;
            Debug.Log("json from db: " + json);
            char[] charstotrim = {'[',']'};
            json = json.Trim(charstotrim);
            Debug.Log("jsontrimmed from db: " + json);

            if (json != "")
            {
                trackeddaysCL loadedPlayerData = JsonUtility.FromJson<trackeddaysCL>(json);
                daysTracked.text = loadedPlayerData.trackeddays.ToString();
                totalDays = loadedPlayerData.trackeddays;
                Debug.Log("tracked days" + totalDays);
             
            }
        }

        ///////////////////////////////////////////////////////////////////
        ///

        //most units
        WWWForm formb = new WWWForm();

        formb.AddField("user", dbuserid);


        UnityWebRequest wwwb = UnityWebRequest.Post(getunitsTotal, formb); // The file location for where my .php file is.
        yield return wwwb.SendWebRequest();
        if (wwwb.isNetworkError || wwwb.isHttpError)
        {
            totalUnitstxt.text = "N/A";
            errormessage.text = "Sorry, something went wrong. Not sure where, not sure when";

        }
        else
        {

            string json1 = wwwb.downloadHandler.text;
            Debug.Log("json from db: " + json1);
            char[] charstotrim = { '[', ']' };
            json1 = json1.Trim(charstotrim);

            if (json1 != "")
            {
                units loadedPlayerData = JsonUtility.FromJson<units>(json1);
                totalUnitstxt.text = loadedPlayerData.totalunits.ToString();
                totalUnits = loadedPlayerData.totalunits;
            }
        }
        averageUnits = totalUnits / totalDays;
        averageUnitstxt.text = averageUnits.ToString();
        ///////////////////////////////////////////////////////////
        ///
        //worst drink day
        WWWForm formc = new WWWForm();

        formc.AddField("user", dbuserid);

        UnityWebRequest wwwc = UnityWebRequest.Post(getworstdrinkday, formc); // The file location for where my .php file is.
    yield return wwwc.SendWebRequest();
        if (wwwc.isNetworkError || wwwc.isHttpError)
        {
            worstDay.text = "N/A";
            worstDrink.text = "N/A";
            errormessage.text = "Sorry, something went wrong. Not sure where, not sure when";

        }
        else
{

    string json2 = wwwc.downloadHandler.text;
    Debug.Log("json from db: " + json2);
            char[] charstotrim = { '[', ']' };
            json2 = json2.Trim(charstotrim);

            if (json2 != "")
    {
                worstdrink loadedPlayerData = JsonUtility.FromJson<worstdrink>(json2);
                worstDay.text = loadedPlayerData.dateofdrink;
                worstDrink.text = loadedPlayerData.drink;
            }
        }
     
    }

    

    public class trackeddaysCL
    {
        public int trackeddays;
    }
    public class units
    {
        public int totalunits;
    }
    public class worstdrink
    {
        public string dateofdrink;
        public string drink;
    }
}
