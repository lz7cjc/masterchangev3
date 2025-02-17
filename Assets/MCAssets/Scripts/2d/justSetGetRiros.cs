using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class justSetGetRiros : MonoBehaviour
{
    private int DBuser;
    public bool rirosEarnt;
    public bool rirosBought;
    public bool rirosSpent;
    public string description;
     public int rirosValue;
    private string riroType;

    string posturl = "https://masterchange.today/php_scripts/setgetriros.php";
    private string Switchscene ;

    /// <summary>
    /// check record count for sceneid and user in habits
    /// do they get riros for filling out the form
    /// </summary>

    public void toPayOut()
        {
        Switchscene = globalvariables.Instance.nextScene;
      //  Debug.Log("scenename in just set riros: " + Switchscene);
        
        
        if (rirosEarnt)
        {
        //   Debug.Log("type of payout == earnt");
            riroType = "Earnt";
        }
        if (rirosSpent)
        {
     //       Debug.Log("type of payout == Spent");
            riroType = "Spent";
        }
        if (rirosBought)
        {
      //      Debug.Log("type of payout == Bought");
            riroType = "Bought";
        }
        //Debug.Log("what is the riro value" + rirosValue);
        //Debug.LogWarning("what is the type" + riroType);
             StartCoroutine(SetRirosDB());
      }

    public void toBuy()
    {
        riroType = "Bought";
        StartCoroutine(SetRirosDB());

    }

    public void toSpend()
    {
        riroType = "Spent";
        StartCoroutine(SetRirosDB());


    }
    public void toEarn()
    {
        riroType = "Earnt";
        StartCoroutine(SetRirosDB());

    }




    private IEnumerator SetRirosDB()
    {
   //     Debug.Log("rirotype is; " + riroType);
    //    Debug.Log("rirosValue is; " + rirosValue);
    //    Debug.Log("description is; " + description);
        WWWForm form = new WWWForm();
        form.AddField("riroType", riroType);
        if (description == "Registration")
        {
            rirosValue = rirosValue + PlayerPrefs.GetInt("rirosEarnt");
        }
 //       Debug.Log("rirosValue is with reg; " + rirosValue);

        form.AddField("rirosValue", rirosValue);
        form.AddField("description", description);
        if (PlayerPrefs.HasKey("dbuserid"))
        {
           DBuser = PlayerPrefs.GetInt("dbuserid");
          form.AddField("userid", DBuser);

        }
       
        UnityWebRequest www = UnityWebRequest.Post(posturl, form); // The file location for where my .php file is.
        yield return www.SendWebRequest();
        if (www.isNetworkError || www.isHttpError)
        {
    //        Debug.Log(www.error);
            // errormessage.text = "We hit a problem: (" + www.error + "). Please send the receipt of your payment to purchases@masterchange.today and we will sort it out";

        }
        else
        {      
                 string json = www.downloadHandler.text;
      //      Debug.Log("returning riros srting: " + json);
            riros riros = JsonUtility.FromJson<riros>(json); 
          int rirosEarntOut = riros.Earnt;
            int rirosBoughtOut = riros.Bought;
            int rirosSpentOut = riros.Spent;
            Debug.LogWarning("riros earnt" + rirosEarntOut);
            Debug.LogWarning("riros rirosBought" + rirosBought);
            Debug.LogWarning("riros rirosSpent" + rirosSpentOut);

            PlayerPrefs.SetInt("rirosEarnt", rirosEarntOut);
            PlayerPrefs.SetInt("rirosBought", rirosBoughtOut);
            PlayerPrefs.SetInt("rirosSpent", rirosSpentOut);
            PlayerPrefs.SetInt("rirosBalance", rirosBoughtOut + rirosEarntOut - rirosSpentOut);

            // rirostxt.text = PlayerPrefs.GetInt("rirosBalance").ToString();
     //       Debug.Log("scenename" + Switchscene);
            bool result = string.IsNullOrEmpty(Switchscene as string);
     //      Debug.Log("result" + result);
            if (!result)
            { 
                SceneManager.LoadScene(Switchscene);
             }
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