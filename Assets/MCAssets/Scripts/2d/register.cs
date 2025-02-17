using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Globalization;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;


public class register : MonoBehaviour
{
    //remote
    string posturl = "https://masterchange.today/php_scripts/register2.php";
    //local
 //readonly string posturl = "http://localhost/php_scripts/register2.php";

    public InputField usernameField;
    public InputField passwordField;
    public InputField dob;
    public InputField fname;
    public InputField lname;
    public InputField email;
    public Toggle receiveEmail;
    //public Toggle TsandCs;
    public Button acceptSubmissionButton;
    public Text errorMessage;
    private int credsgiven;
    private int IntroScreen;
    private int SwitchtoVR;
    private int SkipLearningScreenInt;
    private string behaviour;
    private string nextscene;
    private string returnToScene;
    private int emailvalid;
    private string myAge;
    private int userYear;
    private string thisyearS;
    private int thisyear;
    private int age;
    private updateRiros updateRiros;
    public string Switchscene;
    private bool isItNumber;
    private int prerirosEarnt;
    private int prerirosSpent;
    private int prerirosBalance;
    public int riroValue;
    private setRegRiros setRegRiros;
    public Text earnRirosText;
    public string description;
    private int stage;

    public void Start()
    {
        behaviour = PlayerPrefs.GetString("behaviour");
        nextscene = PlayerPrefs.GetString("nextscene");
        prerirosEarnt = PlayerPrefs.GetInt("rirosEarnt");
        prerirosSpent = PlayerPrefs.GetInt("rirosSpent");
        prerirosBalance = PlayerPrefs.GetInt("rirosBalance");
        returnToScene = PlayerPrefs.GetString("returntoscene");

        //usernameField.text = "ff88fgdafh";
        //passwordField.text = "34534535dg";
        //dob.text = "1934";
        //email.text = "nik@beriro.co.uk";

        Debug.Log("iii behaviour and return to scene: " + behaviour + returnToScene);
#if UNITY_ANDROID || UNITY_EDITOR

        earnRirosText.text = "Earn 10,000 Riros when you register";
        riroValue = 10000;


#endif

#if UNITY_IOS
        earnRirosText.text = "Register for a more personalised experience";
        riroValue = 0;

#endif
        IntroScreen = PlayerPrefs.GetInt("IntroScreen");
        if (IntroScreen != 1)
        {
            IntroScreen = 0;
        }
        stage = PlayerPrefs.GetInt("stage");
        if (stage != 1)
        {
            stage = 0;
        }

        SwitchtoVR = PlayerPrefs.GetInt("SwitchtoVR");
        if (SwitchtoVR !=1)
        {
            SwitchtoVR = 0;
        }
        SkipLearningScreenInt = PlayerPrefs.GetInt("trainingDone");
        if (SkipLearningScreenInt != 1)
        {
            SkipLearningScreenInt = 0;
        }

        
        if (PlayerPrefs.HasKey("returnToScene"))
        {
        returnToScene = PlayerPrefs.GetString("returntoscene");
           
        }
      
        //usernameField.text = "masterchange";
     
        //passwordField.text = "password";
        //dob.text = "2000";
        //email.text = "ryr@";

        if (PlayerPrefs.HasKey("dbuserid"))

        {
            SceneManager.LoadScene("earn riros");
        }
        //Debug.Log("in start");
   
    }

    public void CallRegisterCoroutine(string switchscne)
    { 
        globalvariables.Instance.nextScene = switchscne;
        StartCoroutine(Register());
        onChangeEmail();
        Debug.Log("in CallRegisterCoroutine");
    }


    public void onChangeEmail()
    {
        Debug.Log("opt in value" + receiveEmail.isOn);
    }

    IEnumerator Register()
    {
        Debug.Log("Register");

        WWWForm form = new WWWForm();
        form.AddField("c_username", usernameField.text);
        form.AddField("c_password", passwordField.text);
        form.AddField("c_dob", dob.text);
        form.AddField("c_fname", fname.text);
        form.AddField("c_lname", lname.text);
        form.AddField("c_email", email.text);

        form.AddField("creditgiven", credsgiven);
        form.AddField("introscreen", IntroScreen);
        form.AddField("switchtoVr", SwitchtoVR);
        form.AddField("SkipLearning", SkipLearningScreenInt);
        form.AddField("description", description);
        form.AddField("behaviour", behaviour);
        form.AddField("stage", stage);

        form.AddField("rirosEarnt", prerirosEarnt + riroValue);
        form.AddField("rirosSpent", prerirosSpent);
        //form.AddField("rirosBalance", prerirosBalance);
        //form.AddField("rirosBought", prerirosBalance);

        if (PlayerPrefs.HasKey("returnToScene"))
        {
            form.AddField("returnscene", returnToScene);
        }
        else
        {
            form.AddField("returnscene", "0");
        }


        if (receiveEmail.isOn)
        {
            form.AddField("c_optin", "1");
        }
        else
        {
            form.AddField("c_optin", "0");
        }


        UnityWebRequest www = UnityWebRequest.Post(posturl, form); // The file location for where my .php file is.
        yield return www.SendWebRequest();
        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log("we got an error" + www.error);
            errorMessage.text = www.error;

        }
        else
        {
            string json = www.downloadHandler.text;
            //json = json.Trim('[', ']');

            Debug.Log("what is downloaded" + www.downloadHandler.text);
            Debug.Log("json strnig" + json);
            Debug.Log("Form Upload Complete!");

            if (json == "3: name already exists")
            {
                errorMessage.text = "That username already exists. Please choose a different one";
            }
            else if (json == "2:name check query failed")
            {
                errorMessage.text = "Sorry, we failed to check if that username is in use. Error Code - 2. Please try again";
            }
            else if (json == "0: DB Connection failed")
            {
                errorMessage.text = "Sorry, we failed to connect to the database. Error Code - 002. Please try again";
            }
            else if (json == "Registration insert record failed")
            {
                errorMessage.text = "Sorry, we failed to connect. Error Code - 4 . Please try again";
            }
            else if (json == "5: User Settings failed to update")
            {
                errorMessage.text = "Your account has been created with errors. Please email useraccounts@masterchange.today with the error code 4001";
            }
            else if (json == "6: Riro entry failed")
            {
                errorMessage.text = "Your account has been created but your R$ didn't update. This will be resolved the next time you earn, spend or buy riros";
            }


            else

            {
                Debug.Log("json returned after data entry" + json);
                UserData UserData = JsonUtility.FromJson<UserData>(json);

                string userid = UserData.UserID;
                Debug.Log("userid" + userid);

                string Username = UserData.Username;
                Debug.Log("Username" + Username);

                //  string Fname = loadUserData.Fname;
                PlayerPrefs.SetInt("dbuserid", Convert.ToInt32(userid));
                PlayerPrefs.SetString("username", Username);
                // PlayerPrefs.SetString("fname", Fname);
                //  updateRiros = FindObjectOfType<updateRiros>();
                //  updateRiros.firstFunction();





                /*   setRegRiros = FindObjectOfType<setRegRiros>();
                   setRegRiros.toPayOut();
               */  
                Switchscene = globalvariables.Instance.nextScene;
                if ((behaviour == "smoking") && (returnToScene == "hospital"))
                    {
                    SceneManager.LoadScene("habitsdb");

                    }
                else
                {
                    SceneManager.LoadScene(Switchscene);
                }
               
            }
        }
    }
    


    public void Validation()
    {
        emailvalid = email.text.IndexOf('@');
       
      //Calculating Age
        //get the age from year entered
        //what is the date today
        DateTime now = DateTime.Today;
        Debug.Log("now" + now);

        //get the year from today
       thisyearS = (now.ToString("yyyy"));
        Debug.Log("thisyear" + thisyearS);
        thisyear = Convert.ToInt32(thisyearS);
        Debug.Log("integer Year" + thisyear);

        //what year did they enter in the app
        myAge = dob.text;
        Debug.Log("dob myAge" + myAge);

       Debug.Log("what is my age type" + myAge.GetType());
         if (myAge !="")
                {
                 userYear = Convert.ToInt32(myAge);
                }
        // Debug.Log("dob converted" + userYear);

        //calculate their rough age
        age = thisyear - userYear;
        Debug.Log("their age in years" + age);
         //acceptSubmissionButton.interactable = true;
        if ((usernameField.text.Length >= 6) && (passwordField.text.Length >= 8) && (age >= 18) && (emailvalid >= 1))
        {
            Debug.Log("should be usable");
            acceptSubmissionButton.interactable = true;
        }
             else
        {
            Debug.Log("should not be usable");
            acceptSubmissionButton.interactable = false;
        }
    }



    private class UserData
    {
        public string UserID;
        public string Username;
        //public string Fname;
       // public int riros;

    }


}