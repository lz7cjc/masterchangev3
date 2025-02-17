using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;


public class getfavourites : MonoBehaviour
{
     string FavouritesURLs = "https://masterchange.today/php_scripts/userfavess.php";
   // string FavouritesURLs = "http://localhost/php_scripts/userfavess.php";

    private int dbuserid;
    public string URL;
    public GameObject heart;


    private void Start()
    {     
            callFavourites();       
    }

    public void callFavourites()
    {
        dbuserid = PlayerPrefs.GetInt("dbuserid");

        if (PlayerPrefs.HasKey("dbuserid"))
        {
            StartCoroutine(getResults());
        }
    }
    IEnumerator getResults()
    {
        //how maany tracked days
        WWWForm forma = new WWWForm();

        forma.AddField("user", dbuserid);
        forma.AddField("url", URL);

        UnityWebRequest wwwa = UnityWebRequest.Post(FavouritesURLs, forma); // The file location for where my .php file is.
        yield return wwwa.SendWebRequest();
       

            string json = wwwa.downloadHandler.text;
      
        if (json == "1")
        {

            heart.SetActive(true);
            Debug.Log("url" + URL + "json is: " + json);

        }
        else
        {
            heart.SetActive(false);
            Debug.Log("url" + URL + "json is: " + json);

        }



    }
   }

