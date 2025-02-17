using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;


public class getfavbatch : MonoBehaviour
{
    string FavouritesURLs = "https://masterchange.today/php_scripts/userfavess.php";
    // string FavouritesURLs = "http://localhost/php_scripts/userfavess.php";

    // Start is called before the first frame update
    private int dbuserid;
    //  public Text errormessage;
    //public string URL;
    // public GameObject heart;
    // public Text header;
    private Transform signs; 

    private void Start()
    {

        if (PlayerPrefs.HasKey("dbuserid"))
        {

            var signs = GameObject.FindGameObjectsWithTag("signpost");
            foreach (var sign in signs)
            {
                //var URL = sign.GetComponent<Script(URL)>;
                string url = sign.GetComponent<ToggleShowHideVideo>().VideoUrlLink;
                Debug.Log("url is: " + url);
                StartCoroutine(getResults(url));

                Transform[] allSignChildren = sign.transform.GetComponentsInChildren<Transform>();
                Transform foundNode = null;


                //get all children of sign
                //check all children of sign for the correct node
                foreach (Transform child in allSignChildren)
                {
                   
                    //get ready to store the found node
                  

                    if (transform.name == "node_id34")
                    {
                        foundNode = transform;
                        break;
                    }
                }

                //check if we found it
                if (foundNode != null)
                {
                    //disable it if we found the right node
                    foundNode.gameObject.SetActive(false);
                }
            }
        }
    }


IEnumerator getResults(string URL)
{
          WWWForm forma = new WWWForm();

        forma.AddField("user", dbuserid);
        forma.AddField("url", URL);

        UnityWebRequest wwwa = UnityWebRequest.Post(FavouritesURLs, forma); // The file location for where my .php file is.
        yield return wwwa.SendWebRequest();
        //if (wwwa.isNetworkError || wwwa.isHttpError)

        //sign.GetComponent<ToggleShowHideVideo>()
            string json = wwwa.downloadHandler.text;
      //      Debug.Log("json from db: " + json);
            char[] charstotrim = { '[', ']' };
            json = json.Trim(charstotrim);
    //        Debug.Log("jsontrimmed from db: " + json);

            if (json == "1")
            {
       //     Debug.Log("favourite");
               }
            else
            {
       //     Debug.Log("not favourite");
             }
            //Favourites loadedPlayerData = JsonUtility.FromJson<Favourites>(json);


            //for (int i = 0; i < loadedPlayerData.data.Count; i++)
            //{ 
            //URL.text = loadedPlayerData.data[i].URL;
         
        }

    }


