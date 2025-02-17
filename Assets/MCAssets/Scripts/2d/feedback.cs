using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Globalization;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;


public class feedback : MonoBehaviour
{
    //remote
     string posturl = "https://masterchange.today/php_scripts/feedback.php";
    //local
   // readonly string posturl = "http://localhost/php_scripts/feedback.php";


    public Text txtFeedback;
    public Dropdown ddltype;
    private string userInt;
    private int dbuserid;
    private bool doesExisthabits;
    private string ddlText;
    private bool hasuserid;
    private bool doesExistUserInfo;


    public void Start()
    {

        hasuserid = PlayerPrefs.HasKey("dbuserid");
     
        //get the user id
        if (hasuserid)
        {
            dbuserid = PlayerPrefs.GetInt("dbuserid");
            userInt = dbuserid.ToString();
        }
       
        else
        {
            userInt = "0";
        }
    }


   
    public void submitFeedback()
    {
        StartCoroutine(onfeedback());
      //  Debug.Log("in the call register coroutine");
        
    }

    IEnumerator onfeedback()
{
        if (ddltype.value == 0)
        {
            ddlText = "Suggestion";
        }
        else if (ddltype.value == 1)
        {
            ddlText = "Question";
        }
        else if (ddltype.value == 2)
        {
            ddlText = "Other";
        }
        Debug.Log("in the habits function");
        Debug.Log("type of feedback" + ddlText);
        Debug.Log("feedback is: "  + txtFeedback.text);
        Debug.Log("user: " + userInt) ;
        WWWForm form = new WWWForm();
        form.AddField("feedback", txtFeedback.text);
        form.AddField("type", ddlText);
        form.AddField("user", userInt);
        Debug.Log("user id in IEnumerator Habits()" + userInt);

            UnityWebRequest wwwa = UnityWebRequest.Post(posturl, form); // The file location for where my .php file is.
            yield return wwwa.SendWebRequest();
            if (wwwa.isNetworkError || wwwa.isHttpError)
            {

            }
            else
            {


                  SceneManager.LoadScene("feedbackconfirm");
        }
    }


}
