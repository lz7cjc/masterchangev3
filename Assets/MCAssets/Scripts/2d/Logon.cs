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

public class Logon : MonoBehaviour
{
    //remote
   string posturl = "https://masterchange.today/php_scripts/login.php";
    //local
   //readonly string posturl = "http://localhost/php_scripts/login.php";
   // readonly string geturl = "http://localhost/php_scripts/login.php";
    public InputField nameField;
    public InputField passwordField;
    public Button acceptSubmissionButton;
    private bool doesExistUserInfo;
    private bool registered;
    private string SwitchScene;
    public Text failedlogin;

    public void Start()
    {
        loggedin();
     
    }
     private void loggedin()
    {
        registered = PlayerPrefs.HasKey("dbuserid");
        if (registered)
        {
            SceneManager.LoadScene("earn riros");
        }


    }
    public void CallLogInCoroutine()
    {
        StartCoroutine(Login());
        
    }


    IEnumerator Login()
    {

        WWWForm form = new WWWForm();
        form.AddField("c_username", nameField.text);
        Debug.Log("c_username" + nameField.text);
        form.AddField("c_password", passwordField.text);
        Debug.Log("c_password" + passwordField.text);



        UnityWebRequest www = UnityWebRequest.Post(posturl, form); // The file location for where my .php file is.
        yield return www.SendWebRequest();
        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
        }
        else
        {
                   Debug.Log("Form Upload Complete!");

            string json = www.downloadHandler.text;
            Debug.Log("downloaded text: " + json);
        
            //Debug.Log("json returned: " + loadedPlayerData);
            if (json == "Your username or password is incorrect. Please try again")
            {
                failedlogin.text = "Your username or password is incorrect. Please try again";
            }
            else
            {
                UserPreferences loadedPlayerData = JsonUtility.FromJson<UserPreferences>(json);
                Debug.Log("dbuserid" + Convert.ToInt32(loadedPlayerData.data[0].dbuserid));
              
                PlayerPrefs.SetInt("dbuserid", Convert.ToInt32(loadedPlayerData.data[0].dbuserid));
                PlayerPrefs.SetInt("IntroScreen", Convert.ToInt32(loadedPlayerData.data[0].IntroScreen));
                PlayerPrefs.SetInt("SwitchtoVR", Convert.ToInt32(loadedPlayerData.data[0].SwitchtoVR));
                PlayerPrefs.SetInt("trainingDone", Convert.ToInt32(loadedPlayerData.data[0].SkipLearningScreenInt));
                PlayerPrefs.SetInt("creditsgiven", Convert.ToInt32(loadedPlayerData.data[0].creditsgiven));
               if (!PlayerPrefs.HasKey("returnToScene"))
                { 
                PlayerPrefs.SetString("returnToScene", loadedPlayerData.data[0].returnToScene);
                }
                PlayerPrefs.SetInt("stage", Convert.ToInt32(loadedPlayerData.data[0].stage));
                
                //PlayerPrefs.SetFloat("CTstartpoint", Convert.ToSingle(loadedPlayerData.data[0].CTstartpoint));
                
                if (!PlayerPrefs.HasKey("behaviour"))
                    {
                    PlayerPrefs.SetString("behaviour", loadedPlayerData.data[0].behaviour);
                    }
                PlayerPrefs.SetInt("habitsdone", Convert.ToInt32(loadedPlayerData.data[0].habitsdone));
            
             SceneManager.LoadScene("dashboard");
           }
        }
    }

    public void Validation()
    {
          
        acceptSubmissionButton.interactable = nameField.text.Length >= 7 && passwordField.text.Length >= 8;
    }



    public class UserPreferences
    {
        public List<preferenceinfo> data;
    }
    [System.Serializable]

    public class preferenceinfo
    {
        public string success;
        public int dbuserid;
        public int IntroScreen;
        public int SwitchtoVR;
        public int SkipLearningScreenInt;
        public int creditsgiven;
        public string returnToScene;
        public int stage;
        public float CTstartpoint;
        public string behaviour;
        public int habitsdone;
   
    }

}
