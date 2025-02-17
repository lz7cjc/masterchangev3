using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class justGetRiros : MonoBehaviour
{
    /// <summary>
    /// Single script to get riros
    /// </summary>
    /// 

    private int DBuser;
    private int rirosEarnt;
    private int rirosSpent;
    private int rirosBought;
    public Text rirosValue;
 //   public Text rirostxt;
  //  public GameObject notloggedon;
 //   public GameObject loggedon;



    //how much do you earn

    string posturl = "https://masterchange.today/php_scripts/getriros.php";
    //local
 //    readonly string posturl = "http://localhost/php_scripts/getriros.php";


    // Start is called before the first frame update
    void Start()
    {
        fromPPorDB();
    }

    public void fromPPorDB()
    {
        if (!PlayerPrefs.HasKey("dbuserid"))
        {
            rirosValue.text = PlayerPrefs.GetString("rirosBalance");

        }
        else
        {
            getRiros();
        }

    }
    public void getRiros()
    {
   
            DBuser = PlayerPrefs.GetInt("dbuserid");
            StartCoroutine(getRirosDB());

 
    }
    // Update is called once per frame


    IEnumerator getRirosDB()
    {

        WWWForm form = new WWWForm();

        Debug.Log("the tttt: " + DBuser);
        form.AddField("userid", DBuser);
     

        UnityWebRequest www = UnityWebRequest.Post(posturl, form); // The file location for where my .php file is.
        yield return www.SendWebRequest();
        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log("there was a yyy" + www.error);
            //errorMessage.text = "We hit a problem: (" + www.error + "). Please skip this screen until we have fixed it";

        }
        else
        {
            //update the player prefs
            string json = www.downloadHandler.text;
            riros riros = JsonUtility.FromJson<riros>(json);
             Debug.Log("ocming back: " + json);
            // riros loadedresults = JsonUtility.FromJson<riros>(json);
           
            rirosEarnt = riros.Earnt;
            rirosBought = riros.Bought;
            rirosSpent = riros.Spent;
            Debug.LogWarning("riros earnt" + rirosEarnt);
            Debug.LogWarning("riros rirosBought" + rirosBought);
            Debug.LogWarning("riros rirosSpent" + rirosSpent);

            PlayerPrefs.SetInt("rirosEarnt", rirosEarnt);
            PlayerPrefs.SetInt("rirosBought", rirosBought);
            PlayerPrefs.SetInt("rirosSpent", rirosSpent);
            PlayerPrefs.SetInt("rirosBalance", rirosBought + rirosEarnt - rirosSpent);
            rirosValue.text = PlayerPrefs.GetInt("rirosBalance").ToString();

        }
    }
    //public class payout
    //{
    //    public int paythem;
    //}
    private class riros
    {
        public int Bought;
        public int Spent;
        public int Earnt;


    }
}
