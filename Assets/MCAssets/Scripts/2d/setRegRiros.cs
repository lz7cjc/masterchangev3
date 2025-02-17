using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class setRegRiros : MonoBehaviour
{
    private int DBuser;
    public string description;
    private int rirosSpent;
    private int rirosEarnt;
    private int rirosBought;
    public int rirosReg;
    string posturl = "https://masterchange.today/php_scripts/setregriros.php";
    private string Switchscene ;

    /// <summary>
    /// check record count for sceneid and user in habits
    /// do they get riros for filling out the form
    /// </summary>

    public void toPayOut()
        {
        Switchscene = globalvariables.Instance.nextScene;
        Debug.Log("scenename in just set riros: " + Switchscene);
        StartCoroutine(SetRirosDB());
      }

     private IEnumerator SetRirosDB()
    {
          WWWForm form = new WWWForm();
#if UNITY_ANDROID

                 rirosEarnt = rirosReg + PlayerPrefs.GetInt("rirosEarnt");

#endif

#if UNITY_IOS
        rirosEarnt = PlayerPrefs.GetInt("rirosEarnt");

#endif

        rirosBought = PlayerPrefs.GetInt("rirosBought");
        rirosSpent = PlayerPrefs.GetInt("rirosSpent");
         form.AddField("rirosEarnt", rirosEarnt);
        form.AddField("rirosSpent", rirosSpent);
        form.AddField("rirosBought", rirosBought);
        form.AddField("description", description);
        DBuser = PlayerPrefs.GetInt("dbuserid");
        form.AddField("userid", DBuser);
     
        UnityWebRequest www = UnityWebRequest.Post(posturl, form); // The file location for where my .php file is.
        yield return www.SendWebRequest();
        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
            // errormessage.text = "We hit a problem: (" + www.error + "). Please send the receipt of your payment to purchases@masterchange.today and we will sort it out";

        }
        else
        {
            Debug.Log("rirosEarnt: " + rirosEarnt);
            Debug.Log("rirosBalance: " + (rirosEarnt - rirosSpent));
            PlayerPrefs.SetInt("rirosEarnt", rirosEarnt);
            PlayerPrefs.SetInt("rirosSpent", rirosSpent);
            PlayerPrefs.SetInt("rirosBought", rirosBought);
            PlayerPrefs.SetInt("rirosBalance", rirosEarnt  - rirosSpent);
        
       SceneManager.LoadScene(Switchscene);
        }
    }

}