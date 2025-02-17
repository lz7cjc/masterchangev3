using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Networking;

/// <summary>
/// /////////////////////////////////////////////////////////
/// 
/// old using json files
/// //////////////////////////////////////////////////////
/// </summary>


public class updateDB : MonoBehaviour
{

    //public bool mousehover = false;
    // public float counter = 0;
    private int userInt;
    //remote
     string posturl = "https://masterchange.today/php_scripts/bioDB.php";
    //local
   // readonly string posturl = "http://localhost/php_scripts/bioDB.php";
    private bool existsUser;
    private int age;
    private int weight;
     private int height;
    public void writeToDB()
    {
        //get the user id
        existsUser = PlayerPrefs.HasKey("dbuserid");
        if (existsUser)
        {
            userInt = PlayerPrefs.GetInt("dbuserid");
            Debug.Log("iser id" + userInt);
            //get the user habit information

            string json = File.ReadAllText(Application.persistentDataPath + "/Bio.json");
            Debug.Log(Application.persistentDataPath + "/Bio.json");
            PlayerData loadedPlayerData = JsonUtility.FromJson<PlayerData>(json);


            //set the variables for the form
            age = loadedPlayerData.age;
            weight = loadedPlayerData.weight;
            height = loadedPlayerData.height;
            StartCoroutine(BioC0()); 
        }
        }

    IEnumerator BioC0()
    {
        Debug.Log("xxx"); 
        WWWForm form = new WWWForm();
        form.AddField("age", age);
        Debug.Log("XXX age" + age);
        form.AddField("height", height);
        Debug.Log("XXX height" + height);
        form.AddField("weight", weight);
        Debug.Log("XXX weight" + weight);
        form.AddField("user", userInt);
        Debug.Log("XXX userInt" + userInt);

        UnityWebRequest www = UnityWebRequest.Post(posturl, form); // The file location for where my .php file is.
        yield return www.SendWebRequest();
        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
            // errorMessage = www.error;
        }
        else
        {
            Debug.Log("Form Upload Complete!");
            Debug.Log(www.downloadHandler.text);
        }
    }

    // Update is called once per frame


    private class PlayerData
    {
        //Health factors
        public int age;
        public int weight;
        public int height;
       
    }
}

