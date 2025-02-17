using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class justSetRiros : MonoBehaviour
{
    private int DBuser;
    public bool rirosEarnt;
    public bool rirosBought;
    public bool rirosSpent;
    public string description;
     public int rirosValue;
    private string riroType;

    string posturl = "https://masterchange.today/php_scripts/setriros.php";
    private string Switchscene ;

    private justGetRiros justGetRiros;

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
     //      Debug.Log("type of payout == earnt");
            riroType = "Earnt";
        }
        if (rirosSpent)
        {
      //      Debug.Log("type of payout == Spent");
            riroType = "Spent";
        }
        if (rirosBought)
        {
    //        Debug.Log("type of payout == Bought");
            riroType = "Bought";
        }
        //Debug.Log("what is the riro value" + rirosValue);
        //Debug.LogWarning("what is the type" + riroType);
             StartCoroutine(SetRirosDB());
      }

    public void toBuy(int amount)
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
    //    Debug.Log("rirotype is; " + riroType);
   //     Debug.Log("rirosValue is; " + rirosValue);
    //    Debug.Log("description is; " + description);
        WWWForm form = new WWWForm();
        form.AddField("riroType", riroType);
        form.AddField("rirosValue", rirosValue);
        form.AddField("description", description);

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
            justGetRiros = FindObjectOfType<justGetRiros>();
            justGetRiros.getRiros();

            SceneManager.LoadScene(Switchscene);
            // errormessage.text = "Your details have been updated. Thank you for your support";
            //    Debug.Log("from the pho" + www.downloadHandler.text);


        }
    }

    }
      

