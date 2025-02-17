using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class justGetRirosForTips : MonoBehaviour
{
    private int DBuser;
    private int rirosEarnt;
    private int rirosSpent;
    private int rirosBought;




    //how much do you earn

    string posturl = "https://masterchange.today/php_scripts/getriros.php";
    //local
    // readonly string posturl = "http://localhost/php_scripts/getriros.php";


    // Start is called before the first frame update
    void Start()
    {

        getRirosDB();

    }


    // Update is called once per frame

    IEnumerator getRirosDB()
    {

        WWWForm form = new WWWForm();


        form.AddField("userid", DBuser);
        Debug.Log("userid" + DBuser);


        UnityWebRequest www = UnityWebRequest.Post(posturl, form); // The file location for where my .php file is.
        yield return www.SendWebRequest();
        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
            //errorMessage.text = "We hit a problem: (" + www.error + "). Please skip this screen until we have fixed it";

        }
        else
        {
            string json = www.downloadHandler.text;
            json = json.Trim('[', ']');
            riros loadedresults = JsonUtility.FromJson<riros>(json);
            rirosEarnt = loadedresults.rirosEarnt;
            rirosBought = loadedresults.rirosBought;
            rirosSpent = loadedresults.rirosSpent;

            PlayerPrefs.SetInt("rirosEarnt", rirosEarnt);
            PlayerPrefs.SetInt("rirosBought", rirosBought);
            PlayerPrefs.SetInt("rirosSpent", rirosSpent);
            PlayerPrefs.SetInt("rirosBalance", rirosBought + rirosEarnt - rirosSpent);

            Debug.Log("from the pho" + json);
        }
    }

    private class riros
    {
        public int rirosBought;
        public int rirosSpent;
        public int rirosEarnt;


    }
}
