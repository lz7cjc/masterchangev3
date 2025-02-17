using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Globalization;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;
using JetBrains.Annotations;


public class getfav : MonoBehaviour
{
    string posturl = "https://masterchange.today/php_scripts/alluserfavess.php";
// string FavouritesURLs = "http://localhost/php_scripts/alluserfavess.php";

// Start is called before the first frame update
private int dbuserid;
    //  public Text errormessage;
    // public string url;
    // public GameObject heart;
    // public Text header;

    //private static globalvariables faves;

    private matchfav matchfav;
    public void Start()
{
    favReset();

}

public void favReset()
{
    if (PlayerPrefs.HasKey("dbuserid"))
    {
        dbuserid = PlayerPrefs.GetInt("dbuserid");
     //   Debug.Log("dbuserid = " + dbuserid);
        CallRegisterCoroutine();

    }
}
public void CallRegisterCoroutine()
{
//    Debug.Log("in the call register coroutine");

    StartCoroutine(getResults());

}
    IEnumerator getResults()
    {
  //      Debug.Log("in getresults");
        WWWForm form = new WWWForm();
        form.AddField("userid", dbuserid);

        UnityWebRequest www = UnityWebRequest.Post(posturl, form); // The file location for where my .php file is.
        yield return www.SendWebRequest();
  //      Debug.Log("www finished");
        if (www.isNetworkError || www.isHttpError)
        {
    //        Debug.Log("error in coroutine");
        }
        else
        {
    //        Debug.Log("www success");
            string json = www.downloadHandler.text;
     //       Debug.Log("json from db: " + json);
            char[] charstotrim = { '[', ']' };
            json = json.Trim(charstotrim);
      //      Debug.Log("jsontrimmed from db: " + json);

            File.WriteAllText(Application.persistentDataPath + "/favourites.json", json);
            matchfav = FindObjectOfType<matchfav>();
            matchfav.MyFaves();
        }
    }

    }
