using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;

//do this when submitting the results from the second habits page
public class userBehaviours : MonoBehaviour
{

    //remote
    readonly string insertURL = "https://masterchange.today/php_scripts/behaviourvaluesjson.php";
    readonly string getURL = "https://masterchange.today/php_scripts/getBehaviours.php";

      private int dbuserid;
    private string Switchscene;
    private int behavioursID_U;
    private int behavioursValue_U;

    public Text errormessage;
     private string json;

    //  public globalvariables globaltest;
    private void Start()
    {
        dbuserid = PlayerPrefs.GetInt("dbuserid");
        StartCoroutine(getBehaviours());

    }

    IEnumerator getBehaviours()
    {
        WWWForm forma = new WWWForm();

        forma.AddField("dbuserid", dbuserid);
        UnityWebRequest wwwa = UnityWebRequest.Post(getURL, forma); // The file location for where my .php file is.
        yield return wwwa.SendWebRequest();
        if (wwwa.isNetworkError || wwwa.isHttpError)
        {

        }
        else
        {

            string json = wwwa.downloadHandler.text;
            Debug.Log("json got back for starters: " + json);
            UserBehaviour loadedPlayerData = JsonUtility.FromJson<UserBehaviour>(json);
            var toggles = GameObject.FindObjectsOfType<Toggle>();
           

            //toggles
            foreach (var toggleIs in toggles)
            {
                //these are the inital values on the form
                // var togglevalue = toggleIs.isOn;
                var nameoftoggle = toggleIs.name;
                   int numbernameoftoggle = Convert.ToInt32(nameoftoggle);

                for (int y = 0; y < loadedPlayerData.data.Count; y++)
                {
                    if (loadedPlayerData.data[y].BehaviourType_ID == numbernameoftoggle)
                    {
                      toggleIs.isOn = true;    //     Debug.Log("787878 loadedPlayerData.data[y].yesorno " + loadedPlayerData.data[y].yesorno);
                        //if (loadedPlayerData.data[y].yesorno == 1)
                        //{
                          
                        //}
                    }
                }

            }




        }
    }

    public void SetBehaviours(string nextScene)
    {
        globalvariables.Instance.nextScene = nextScene;
        Switchscene = globalvariables.Instance.nextScene;

        var toggles2 = GameObject.FindObjectsOfType<Toggle>();

        //get the values of each slider
        //get the label for each slider
        //set the obj to loop until count is finished
        //set the the global variable for next scene
        UserBehaviourPut obj = new UserBehaviourPut();
      

        foreach (var toggleIs2 in toggles2)
        {
            var behavioursValue = toggleIs2.isOn;
            int behavioursToggle_U = Convert.ToInt32(behavioursValue);
            var behavioursID = toggleIs2.name;
            Debug.Log("number of behaviour" + behavioursID);

              behavioursValue_U = Convert.ToInt32(behavioursID);
                   Debug.Log("aaaaa. is it on habitValue_U" + behavioursToggle_U);
                   Debug.Log("bbbb. name of slider habitID_U:" + behavioursValue_U);

            //add to JSON
            obj.data1.Add(new behaviourput()
            {
                Behaviour_ID = behavioursValue_U,
                yesorno = behavioursToggle_U

            });

        }
        json = JsonUtility.ToJson(obj);
               Debug.Log("json is: " + json);
        StartCoroutine(pushhabits());
    }

    IEnumerator pushhabits()
    {
        //   Debug.Log("thectarrya: " + json);
        //    Debug.Log("instide posting");
        WWWForm form = new WWWForm();
        form.AddField("user", dbuserid);
        form.AddField("cthearray", json);
        UnityWebRequest www = UnityWebRequest.Post(insertURL, form); // The file location for where my .php file is.

        yield return www.SendWebRequest();
        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
            errormessage.text = "Oops, sorry but that didn't work. Please can you email us at bugs@masterchange.today; tell us what you were trying to do and the error message that follows and if we can replicate you will receive 1000 riros: <b>" + www.error + "</b>";
        }
        else
        {

            string json = www.downloadHandler.text;

                     SceneManager.LoadScene(Switchscene);


        }
    }


   


    public class UserBehaviour
    {
        public List<behaviourinfo> data;
    }
    [System.Serializable]

    public class behaviourinfo
    {
        public int BehaviourType_ID;
     //   public int interested;

    }


    [Serializable]
    public class behaviourput
    {
        public int Behaviour_ID;
         public int yesorno;


    }

    [Serializable]
    public class UserBehaviourPut
    {
        public List<behaviourput> data1 = new List<behaviourput>();
    }


}

