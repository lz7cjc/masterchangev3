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

public class syncfromdb : MonoBehaviour
{
    //remote
    // string posturl = "https://masterchange.today/php_scripts/updateUser.php";
    //local
    readonly string posturl = "http://localhost/php_scripts/updateUser.php";

    public string ppname;
    public string ppDB;

    private string ppvalueStr;
    private int ppvalueInt;
    private float ppvalueFlt;
    private int dbuserid;
    public bool isNumber;
    public bool isString;
    public bool isFloat;

    public void Start()
    {

        firstFunction();
    }

    public void firstFunction()
    {
        if (PlayerPrefs.HasKey("dbuserid"))
        {
            dbuserid = PlayerPrefs.GetInt("dbuserid");

            StartCoroutine(UpdateProfile());
        }
    }


    IEnumerator UpdateProfile()
    {
        WWWForm form = new WWWForm();
        form.AddField("user", dbuserid);
        form.AddField("fieldName", ppDB);
        if (isNumber)
        {
            Debug.Log("it is a number" + ppname);
            ppvalueInt = PlayerPrefs.GetInt('"' + ppname + '"');
            form.AddField("fieldValue", ppvalueInt);
        }  
        else if (isString)
        {
            Debug.Log("it is a string" + ppDB);

            ppvalueStr = PlayerPrefs.GetString(ppname);
            form.AddField("fieldValue", ppvalueStr);
    
        }
        else if (isFloat)
        {
            Debug.Log("it is a float" + ppDB);


            ppvalueFlt = PlayerPrefs.GetFloat(ppname);
            form.AddField("fieldValue", ppvalueFlt.ToString());
        }

        Debug.Log("111 the DB fieldname is: " + ppDB);
        Debug.Log("111 the PP name is: " + ppname);
        Debug.Log("111 the value  is int: " + ppvalueInt);
        Debug.Log("111 the value  is str: " + ppvalueStr);
        Debug.Log("111 the value  is float: " + ppvalueFlt.ToString());

        UnityWebRequest www = UnityWebRequest.Post(posturl, form); // The file location for where my .php file is.
        yield return www.SendWebRequest();
        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
            //  errorMessage.text = www.error;

        }
        else
        {
            string userString = www.downloadHandler.text;
            Debug.Log("from php: " + userString);
            Debug.Log("Form Upload Complete!");


        }
    }





}
