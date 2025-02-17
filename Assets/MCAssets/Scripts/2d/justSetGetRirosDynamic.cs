using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

/// <summary>
/// This is a copy of the original justSetGetRiros code but adapted to take dynamic values initially for the quiz - should be a way to consolidate into a single script
/// 2903023
/// 
/// </summary>
public class justSetGetRirosDynamic : MonoBehaviour
{
    private int DBuser;
    private bool rirosEarnt;
    private bool rirosBought;
    private bool rirosSpent;
    private string riroType;

    string posturl = "https://masterchange.today/php_scripts/setgetriros.php";
 //   string posturl = "http://localhost/php_scripts/setgetriros.php";
    private string Switchscene ;

    /// <summary>
    /// check record count for sceneid and user in habits
    /// do they get riros for filling out the form
    /// </summary>


    public void toPayOut(string typeOfRiros, int postValue)
        {
        Debug.Log("###value coming from gazequiz" + typeOfRiros + "amount" + postValue);
        Switchscene = globalvariables.Instance.nextScene;
        StartCoroutine(SetRirosDB(typeOfRiros, postValue));
      }

    private IEnumerator SetRirosDB(string riroType, int valuePost)
    {
        Debug.Log("###rirotype is " + riroType + "amount2" + valuePost);
       // Debug.Log("###rirosValue is; " + rirosValue);
      //  Debug.Log("###description is; " + description);
        WWWForm form = new WWWForm();
        form.AddField("riroType", riroType);
        form.AddField("rirosValue", valuePost);
        form.AddField("description", "Quiz");
        DBuser = PlayerPrefs.GetInt("dbuserid");
        form.AddField("userid", DBuser);

       
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
            Debug.Log("###returning riros srting: " + json);
            riros riros = JsonUtility.FromJson<riros>(json); 
          int rirosEarntOut = riros.Earnt;
            int rirosBoughtOut = riros.Bought;
            int rirosSpentOut = riros.Spent;
            Debug.LogWarning("###riros earnt" + rirosEarntOut);
            Debug.LogWarning("###riros rirosBought" + rirosBought);
            Debug.LogWarning("###riros rirosSpent" + rirosSpentOut);

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
