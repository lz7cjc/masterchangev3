using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;


public class favourites : MonoBehaviour
{
     string getFavourites = "https://masterchange.today/php_scripts/userfavess.php";
  //  string getFavourites = "http://localhost/php_scripts/userfavess.php";

    // Start is called before the first frame update
    private int dbuserid;
    public Text errormessage;
    public Text URL;
    // public Text header;


    private void Start()
    {
        dbuserid = PlayerPrefs.GetInt("dbuserid");
        Debug.Log("userid: " + dbuserid); 
        if (dbuserid == 0)
        {
            errormessage.text = "You will need to register if you want to save and view favourites. If you want to do this, choose the button below then remove your headset";
        }
        else
        {
            StartCoroutine(getResults());
        }
    }

    IEnumerator getResults()
    {
        //how maany tracked days
        WWWForm forma = new WWWForm();

        forma.AddField("user", dbuserid);

        UnityWebRequest wwwa = UnityWebRequest.Post(getFavourites, forma); // The file location for where my .php file is.
        yield return wwwa.SendWebRequest();
        if (wwwa.isNetworkError || wwwa.isHttpError)
        {

            errormessage.text = "Sorry, something went wrong. Not sure where, not sure when";
        }
        else
        {

            string json = wwwa.downloadHandler.text;
            Debug.Log("json from db: " + json);
            //char[] charstotrim = { '[', ']' };
            //json = json.Trim(charstotrim);
            //Debug.Log("jsontrimmed from db: " + json);

            if (json != "")
            {

                FavouritesJSON loadedPlayerData = JsonUtility.FromJson<FavouritesJSON>(json);


                for (int i = 0; i < loadedPlayerData.data.Count; i++)
                { 
                URL.text = loadedPlayerData.data[i].URL;
                    string isUrl = loadedPlayerData.data[i].URL;
                    URL.text = isUrl;
                    //string isHeader = loadedPlayerData.data[i].title;
                    //Debug.Log(" URL.text " + isUrl + " number: " + i + "Title is; " + isHeader);
                    //header.text = isHeader;
                    Debug.Log(" URL.text " + isUrl + " number: " + i);

                }

            }
        }
    }


        ///////////////////////////////////////////////////////////////////
        ///

    

    public class Favourites
    {
        public string URL;
        public string title;
    }
   
    [Serializable]
    public class FavouritesJSON
    {
        public List<Favourites> data;
    }
}
