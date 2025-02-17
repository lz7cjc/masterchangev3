using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;

//do this when submitting the results from the second habits page
public class userBehavioursfromhabits : MonoBehaviour
{

    //remote
    readonly string insertURL = "https://masterchange.today/php_scripts/behaviourfromhabits.php";
    public bool smoking;
    public bool drinking;
    public bool weightloss;
    public bool diabetes;
    public bool oralhealth;
    public bool stress;
    public bool anxiety;
    public bool depression;
    public bool drugs;
    public bool sexualhealth;
    private int behaviourid;

    private int dbuserid;
  //  public Text errormessage;
    private string json;

    //  public globalvariables globaltest;
    private void Start()
    {
        

    }

 

    public void SetBehaviours()
    {
         dbuserid = PlayerPrefs.GetInt("dbuserid");
  
        if (smoking)
        {
            behaviourid = 1;
        }
        else if (drinking)
        {
            behaviourid = 2;
        }
        else if (weightloss)
        {
            behaviourid = 4;
        }
        else if (diabetes)
        {
            behaviourid = 5;
        }
        else if (oralhealth)
        {
            behaviourid = 6;
        }
        else if (stress)
        {
            behaviourid = 7;
        }
        else if (anxiety)
        {
            behaviourid = 8;
        }
        else if (depression)
        {
            behaviourid = 9;
        }
        else if (drugs)
        {
            behaviourid = 10;
        }
        else if (sexualhealth)
        {
            behaviourid = 11;
        }


        StartCoroutine(pushhabits());
    }

    IEnumerator pushhabits()
    {
        //   Debug.Log("thectarrya: " + json);
           Debug.Log("instide posting");
        WWWForm form = new WWWForm();
        form.AddField("user", dbuserid);
        form.AddField("behaviourid", behaviourid);
        UnityWebRequest www = UnityWebRequest.Post(insertURL, form); // The file location for where my .php file is.

        yield return www.SendWebRequest();
        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
        //    errormessage.text = "Oops, sorry but that didn't work. Please can you email us at bugs@masterchange.today; tell us what you were trying to do and the error message that follows and if we can replicate you will receive 1000 riros: <b>" + www.error + "</b>";
        }
        else
        {

            string json = www.downloadHandler.text;
            Debug.Log("back from php" + json);
        }
    }


   





}

