using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;

using UnityEngine.SceneManagement;


public class filmvotes2 : MonoBehaviour
{

    //remote
    string posturllive = "https://masterchange.today/php_scripts/setgetriros.php";
    string recordtip = "https://masterchange.today/php_scripts/filmvote.php";
    //local
    // readonly string posturldev = "http://localhost/php_scripts/filmvote.php";
    // readonly private Boolean live = true;

    public TMP_Text errorMessage;
    public bool mousehover = false;
    public float counter = 0;

    private bool tip1500;
    private bool tip1000;
    private bool tip750;
    private bool tip500;
    private bool tip250;
    private bool tip100;
  
    private bool voteSkip;
     private bool goRegister;

    private int userid;
    private bool userexists;
  
    private string creditURL;
    private int rirosSpent;
    private int rirosBalance;
    private int rirosPaid;
    private int rirosBought;
    //public Text printURL;

    public GameObject problembutton;

   // private showhide3d showhide3d;
  //  private getfav getfav;

    //  public Text setFav;

    private justGetRiros justGetRiros;


    // Start is called before the first frame update
    void Start()
    {

        creditURL = PlayerPrefs.GetString("VideoUrl");
       // printURL.text = creditURL;
       // Debug.LogWarning(string.Format("URL from PlayerPrefs is {0}", creditURL));
        if (PlayerPrefs.HasKey("dbuserid"))
        { 
            userid = PlayerPrefs.GetInt("dbuserid");
   //         setFav.text = "If you want to save this film in your favourites, then choose the heart. This costs Ro$1500";
        Debug.Log("first userid from player prefs" + userid);
         }
        else
        {
    //        setFav.text = "You can save your favourite experiences once you have registered. Take off your headset to register";
            userid = 0;
        }
        rirosBalance = PlayerPrefs.GetInt("rirosBalance");
        rirosBought = PlayerPrefs.GetInt("rirosBought");
        rirosSpent = PlayerPrefs.GetInt("rirosSpent");
     //   showhide3d = FindObjectOfType<showhide3d>();
    }

    




    void Update()
    {
        if (mousehover)
        {
            Debug.Log("in mousehover");
            counter += Time.deltaTime;
            if (counter >= 3)
            {
                mousehover = false;
                counter = 0;
                Debug.Log("in counter");


                if (goRegister)
                {
                    SceneManager.LoadScene("Register");
                    Debug.Log("in goRegister");

                }
                //////////////////////////////////////////
                ///working before i hide/show the error button. uncomment if need to rollback
                ////////////////////////////////////////

                //else if (voteSkip)
                //{
                //    Debug.Log("in voteSkip");

                //    if (PlayerPrefs.HasKey("returntoscene"))
                //    {
                //        //    string newscene = PlayerPrefs.GetString("returntoscene");
                //        string newscene = PlayerPrefs.GetString("next");
                //        PlayerPrefs.SetString("nextscene", newscene);
                //        PlayerPrefs.DeleteKey("returntoscene");

                //    }
                //    else
                //    {
                //        PlayerPrefs.DeleteKey("nextscene");
                //    }
                //    //PlayerPrefs.DeleteKey("returntoscene");
                //    showhide3d = FindObjectOfType<showhide3d>();
                //    showhide3d.ResetScene();
                //}
                else
                {
                    Debug.Log("in StartCoroutine");

                    StartCoroutine(tips());
                }
            }
        }
    }


    IEnumerator tips()
    {
        //write to the filmvotes table via filmvote.php

        creditURL = PlayerPrefs.GetString("VideoUrl");
        Debug.Log("ggg credit url" + creditURL);

        WWWForm formTips = new WWWForm();
        formTips.AddField("voteis", rirosPaid);
        formTips.AddField("filmid", creditURL);
        formTips.AddField("userid", userid);
        UnityWebRequest www = UnityWebRequest.Post(recordtip, formTips); // The file location for where my .php file is.
        yield return www.SendWebRequest();
        if (www.isNetworkError || www.isHttpError)
        {
           Debug.Log(www.error);
            errorMessage.text = "We hit a problem: (" + www.error + "). Please continue";
            problembutton.SetActive(true);
        }
        else
        {
            string json = www.downloadHandler.text;
            Debug.Log("ggg from php for film votes: " + json);
        }
        //////////////////
        ///end of film vote
        ///



        // reconcile riros - deduct payment and write to Riros table via setgetriros.php - only happens if they are registered and there is a dbuserid

        if (PlayerPrefs.HasKey("dbuserid"))
        {
            WWWForm form = new WWWForm();
            form.AddField("userid", userid);
            form.AddField("rirosValue", rirosPaid);
            form.AddField("description", "Film Tip: " + creditURL);
            form.AddField("riroType", "Spent");
            Debug.Log("ggg Sending to php file: rirovalue: " + rirosPaid + " description: " + creditURL + ", Type of entry: " + "Spent" + ", and the userid: " + userid + "ENDS");

            UnityWebRequest www1 = UnityWebRequest.Post(posturllive, form); // The file location for where my .php file is.
            yield return www1.SendWebRequest();
            if (www1.isNetworkError || www1.isHttpError)
            {
       //         Debug.Log(www1.error);
                errorMessage.text = "We hit a problem: (" + www1.error + "). Please continue";

            }
            else
            {
                string json = www1.downloadHandler.text;
        //        Debug.Log("json string for films" + json);
                riros riros = JsonUtility.FromJson<riros>(json);
                int rirosEarntOut = riros.Earnt;
                int rirosBoughtOut = riros.Bought;
                int rirosSpentOut = riros.Spent;
         //       Debug.LogWarning("riros earnt" + rirosEarntOut);
         //       Debug.LogWarning("riros rirosBought" + rirosBought);
         //       Debug.LogWarning("riros rirosSpent" + rirosSpentOut);

                PlayerPrefs.SetInt("rirosEarnt", rirosEarntOut);
                PlayerPrefs.SetInt("rirosBought", rirosBoughtOut);
                PlayerPrefs.SetInt("rirosSpent", rirosSpentOut);
                PlayerPrefs.SetInt("rirosBalance", rirosBoughtOut + rirosEarntOut - rirosSpentOut);

                //        justGetRiros = FindObjectOfType<justGetRiros>();
                //        justGetRiros.getRiros();
                //Debug.Log("This should be the riro count aaaa: " + www.downloadHandler.text);

            }
        }
        else
        {
            PlayerPrefs.SetInt("rirosSpent", rirosSpent + rirosPaid);
            PlayerPrefs.SetInt("rirosBalance", rirosBalance - rirosPaid);

        }

        //  removed 03/02/2020
        //if (PlayerPrefs.HasKey("returntoscene"))
        //{
        //    string newscene = PlayerPrefs.GetString("returntoscene");
        //    PlayerPrefs.SetString("nextscene", newscene);
        //    PlayerPrefs.DeleteKey("returntoscene");

        //}
        //else
        //{
        //    PlayerPrefs.DeleteKey("nextscene");
        //}
        //PlayerPrefs.DeleteKey("returntoscene");

        Debug.Log("ggg go to showhide");
        //showhide3d = FindObjectOfType<showhide3d>();

        //showhide3d.ResetScene();
     //   PlayerPrefs.DeleteKey("VideoUrl");
        //SceneManager.LoadScene("mainVR");
        SceneManager.UnloadSceneAsync("360VideoApp");
      //  SceneManager.LoadScene("mainVR", LoadSceneMode.Additive);

      //  PlayerPrefs.SetString("nextscene", "sectors");

       SceneManager.LoadScene("mainVR");
       // SceneManager.UnloadSceneAsync("videoplayer");

        //////////////////////////////////////////////
        ///
    }



    // mouse Enter event

    public void MouseHoverChangeScene1500()
    {
        Debug.Log("xxsetting" + 1500);
        // Markername = ObjectName;
        mousehover = true;
        tip1500 = true;
        tip1000 = false;
        tip750 = false;
        tip500 = false;
        tip250 = false;
        tip100 = false;
        voteSkip = false;
        goRegister = false;
        rirosPaid = 1500;

        // getfav = FindObjectOfType<getfav>();
        //getfav.favReset();

    }
    public void MouseHoverChangeScene1000()
    {
         Debug.Log("xxsetting" + 1000);
        // Markername = ObjectName;
        mousehover = true;
        tip1500 = false;
        tip1000 = true;
        tip750 = false;
        tip500 = false;
        tip250 = false;
        tip100 = false;
        voteSkip = false;
        goRegister = false;
        rirosPaid = 1000;
    }
    public void MouseHoverChangeScene750()
    {
        Debug.Log("xxsetting" + 750);
        // Markername = ObjectName;
        mousehover = true;
        tip1500 = false;
        tip1000 = false;
        tip750 = true;
        tip500 = false;
        tip250 = false;
        tip100 = false;
        voteSkip = false;
        goRegister = false;
        rirosPaid = 750;
    }
    public void MouseHoverChangeScene500()
    {
        Debug.Log("xxsetting" + 500);
        // Markername = ObjectName;
        mousehover = true;
        tip1500 = false;
        tip1000 = false;
        tip750 = false;
        tip500 = true;
        tip250 = false;
        tip100 = false;
        voteSkip = false;
        goRegister = false;
        rirosPaid = 500;
    }
    public void MouseHoverChangeScene250()
    {
        Debug.Log("xxsetting" + 250);
        // Markername = ObjectName;
        mousehover = true;
        tip1500 = false;
        tip1000 = false;
        tip750 = false;
        tip500 = false;
        tip250 = true;
        tip100 = false;
        voteSkip = false;
        goRegister = false;
        rirosPaid = 250;
    }

    public void MouseHoverChangeScene100()
    {
        Debug.Log("xxsetting 100");
        // Markername = ObjectName;
        mousehover = true;
        tip1500 = false;
        tip1000 = false;
        tip750 = false;
        tip500 = false;
        tip250 = false;
        tip100 = true;
        voteSkip = false;
        goRegister = false;
        rirosPaid = 100;
    }


    public void MouseHoverChangeSceneError()
    {
        // Debug.Log("setting");
        // Markername = ObjectName;
        mousehover = true;
        tip1500 = false;
        tip1000 = false;
        tip750 = false;
        tip500 = false;
        tip250 = false;
        tip100 = false;
        voteSkip = true;
        goRegister = false;
        //rirosPay = 1000;
    }
    public void MouseHoverChangeSceneRegister()
    {
        // Debug.Log("setting");
        // Markername = ObjectName;
        mousehover = true;
        tip1500 = false;
        tip1000 = false;
        tip750 = false;
        tip500 = false;
        tip250 = false;
        tip100 = false;
        voteSkip = false;
        goRegister = true;
        
    }
    
// mouse Exit Event
public void MouseExit()
    {
        Debug.Log("cancelling");
        // Markername = "";
        mousehover = false;
        counter = 0;
    }


private class riros
{
    public int Bought;
    public int Spent;
    public int Earnt;


}}