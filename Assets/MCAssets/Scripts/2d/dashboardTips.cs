using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Globalization;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;

public class dashboardTips : MonoBehaviour
{
    public Text ContentBody;
    //public Text ContentBody1;
    //public Text ContentBody2;
    //remote
    string posturl = "masterchange.today/php_scripts/dashboardtips.php";

    //readonly string posturl = "http://localhost/php_scripts/dashboardtips.php";
    //private string userInt;

    static int RandomiserTip(int max)
    {
        //Debug.Log("toppick :" + toppick);
        int whichrecord = UnityEngine.Random.Range(0, max);
        Debug.Log("each record" + whichrecord);
        return whichrecord;

    }

    // Start is called before the first frame update
    void Start()
    {
        int returnVisitor = PlayerPrefs.GetInt("ReturnVisitor");
        Debug.Log("return visitor:" + returnVisitor);
        int screenCount = PlayerPrefs.GetInt("screenCounter");
        Debug.Log("screenCount:" + screenCount);
        if (returnVisitor == 1)
        { 
       // string userpath = File.ReadAllText(Application.persistentDataPath + "/userinfo.json");
       // Debug.Log("userid" + userpath);

        //UserData loadedUserData = JsonUtility.FromJson<UserData>(userpath);
       // Debug.Log("userData" + loadedUserData);
       //  userInt = loadedUserData.User_id;
        CallRegisterCoroutine();
        
        screenCount = screenCount + 1;
            Debug.Log("screencount = " + screenCount);
        PlayerPrefs.SetInt("screenCounter", screenCount);
       }
        else
        {
            PlayerPrefs.SetInt("ReturnVisitor", 1);
            screenCount = 1;
            PlayerPrefs.SetInt("screenCounter", screenCount);
        }
    
    }


    // Update is called once per frame
    public void CallRegisterCoroutine()
    {
         Debug.Log("in the call register coroutine");
            StartCoroutine(GetUserTips());
      
    }
    IEnumerator GetUserTips()
    {
        Debug.Log("in the IEnumerator");

        WWWForm form = new WWWForm();
       // form.AddField("userid", userInt);


        UnityWebRequest www = UnityWebRequest.Post(posturl, form); // The file location for where my .php file is.
        yield return www.SendWebRequest();
        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
             //errorMessage = www.error;
        }
        else
        {
            string json = www.downloadHandler.text;
           // json = json.Remove(json.Length - 4);
          //  json = json.Remove(9,3);
             //char[] trimchars = { '[', ']' };
            // json = json.Trim(trimchars);
             Debug.Log("json" + json);
            Debug.Log("Form Upload Complete!");
            File.WriteAllText(Application.persistentDataPath + "/dashboardtips.json", json);

            string readJson = File.ReadAllText(Application.persistentDataPath + "/dashboardtips.json");
             Debug.Log("tips json file" + readJson);
            PrintUserTips();
        }
    }
    public void PrintUserTips()
    {
        string json = File.ReadAllText(Application.persistentDataPath + "/dashboardtips.json");
        Debug.Log("json for tips: " + json);
        PlayerTipsJSON loadedPlayerData = JsonUtility.FromJson<PlayerTipsJSON>(json);
        int toppick = loadedPlayerData.data.Count;
        int randompick = RandomiserTip(toppick);

        for (int i = 0; i < loadedPlayerData.data.Count; i++)
        {
            //Debug.Log(loadedPlayerData.data[i].User_ID + "\n");
            Debug.Log(loadedPlayerData.data[i].ContentTitle + "\n");
            Debug.Log(loadedPlayerData.data[i].ContentBody + "\n");
            //    Debug.Log(loadedPlayerData.data[i].value + "\n");
            //    Debug.Log(loadedPlayerData.data[i].boolean + "\n");
            //
        }
        // Debug.Log("this is a single record - record 4!" + loadedPlayerData.data[randompick].ContentBody + "\n");
        ContentBody.text = loadedPlayerData.data[RandomiserTip(toppick)].ContentBody;
        //ContentBody1.text = loadedPlayerData.data[RandomiserTip(toppick)].ContentBody;
        //ContentBody2.text = loadedPlayerData.data[RandomiserTip(toppick)].ContentBody;

    }

    private class UserData
    {
        public string User_id;


    }

    [Serializable]
    public class PlayerData
    {
       // public string fromJSONusername;
        public string ContentTitle;
        public string ContentBody;
      //  public int User_ID;
      //  public int value;
      //  public bool boolean;
    }

    [Serializable]
    public class PlayerTipsJSON
    {
       public List<PlayerData> data;
    }
}
