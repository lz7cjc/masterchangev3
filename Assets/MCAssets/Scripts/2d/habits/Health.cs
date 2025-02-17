using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using UnityEngine.UI;
using System;

public class Health : MonoBehaviour
{
    public Toggle threemonths;
    public Toggle threeto6months;
    public Toggle sixto12;
    public Toggle morethanyear;
    public Toggle lungcancer;
    public Toggle othercancer;
    public Toggle heartdisease;
    public Toggle asthma;
    public Toggle highblood;
    public Toggle cholesterol;
    public Toggle diabetes;
    public Toggle arthritis;
    public Toggle depression;
    public Toggle anxiety;
    public Toggle insomnia;

    private bool doesExisthealth;

    private int dbuserid;

    private int formthreemonths;
    private int formthreeto6months;
    private int formsixto12;
    private int formmorethanyear;
    private int formlungcancer;
    private int formothercancer;
    private int formheartdisease;
    private int formasthma;
    private int formhighblood;
    private int formcholesterol;
    private int formdiabetes;
    private int formarthritis;
    private int formdepression;
    private int formanxiety;
    private int forminsomnia;


    private int threemonthsID = 94;
    private int threeto6monthsID = 95;
    private int sixto12ID = 96;
    private int morethanyearID = 97;
    private int lungcancerID = 98;
    private int othercancerID = 99;
    private int heartdiseaseID = 103;
    private int asthmaID = 102;
    private int highbloodID = 100;
    private int cholesterolID = 101;
    private int diabetesID = 104;
    private int arthritisID = 105;
    private int depressionID = 106;
    private int anxietyID = 107;
    private int insomniaID = 108;


    private justSetRiros justSetRiros;



    string posturl = "https://masterchange.today/php_scripts/habitvaluesjson.php";
    string gethabits = "https://masterchange.today/php_scripts/gethabits.php";
    //local
    // readonly string posturl = "http://localhost/php_scripts/habitvaluesjson.php";


    public void Start()
    {
        dbuserid = PlayerPrefs.GetInt("dbuserid");
        StartCoroutine(getHabits());

    }

    IEnumerator getHabits()
    {
        Debug.Log("in the habits function");
        WWWForm form = new WWWForm();

        form.AddField("dbuserid", dbuserid);
        UnityWebRequest www1 = UnityWebRequest.Post(gethabits, form); // The file location for where my .php file is.
        yield return www1.SendWebRequest();
        if (www1.isNetworkError || www1.isHttpError)
        {
            Debug.Log(www1.error);
            // errorMessage = www.error;
        }
        else
        {

            Debug.Log("Got the user's habits");

            Debug.Log("this comes back" + www1.downloadHandler.text);
            string json = www1.downloadHandler.text;

            UserHabits loadedPlayerData = JsonUtility.FromJson<UserHabits>(json);
            Debug.Log("record count: " + loadedPlayerData.data.Count);
            for (int i = 0; i < loadedPlayerData.data.Count; i++)
            {
                if (loadedPlayerData.data[i].Habit_ID == threemonthsID)
                {
                    threemonths.isOn = loadedPlayerData.data[i].yesorno;
                }
                if (loadedPlayerData.data[i].Habit_ID == threeto6monthsID)
                {
                    threeto6months.isOn = loadedPlayerData.data[i].yesorno;
                }
                if (loadedPlayerData.data[i].Habit_ID == sixto12ID)
                {
                    sixto12.isOn = loadedPlayerData.data[i].yesorno;
                }
                if (loadedPlayerData.data[i].Habit_ID == morethanyearID)
                {
                    morethanyear.isOn = loadedPlayerData.data[i].yesorno;
                }
                if (loadedPlayerData.data[i].Habit_ID == lungcancerID)
                {
                    lungcancer.isOn = loadedPlayerData.data[i].yesorno;
                }
                if (loadedPlayerData.data[i].Habit_ID == othercancerID)
                {
                    othercancer.isOn = loadedPlayerData.data[i].yesorno;
                }
                if (loadedPlayerData.data[i].Habit_ID == heartdiseaseID)
                {
                    heartdisease.isOn = loadedPlayerData.data[i].yesorno;
                }
                if (loadedPlayerData.data[i].Habit_ID == asthmaID)
                {
                    asthma.isOn = loadedPlayerData.data[i].yesorno;
                }
                if (loadedPlayerData.data[i].Habit_ID == highbloodID)
                {
                    highblood.isOn = loadedPlayerData.data[i].yesorno;

                }
                if (loadedPlayerData.data[i].Habit_ID == cholesterolID)
                {
                    cholesterol.isOn = loadedPlayerData.data[i].yesorno;

                }
                if (loadedPlayerData.data[i].Habit_ID == diabetesID)
                {
                    diabetes.isOn = loadedPlayerData.data[i].yesorno;

                }
                if (loadedPlayerData.data[i].Habit_ID == arthritisID)
                {
                    arthritis.isOn = loadedPlayerData.data[i].yesorno;

                }
                if (loadedPlayerData.data[i].Habit_ID == depressionID)
                {
                    depression.isOn = loadedPlayerData.data[i].yesorno;

                }
                if (loadedPlayerData.data[i].Habit_ID == anxietyID)
                {
                    anxiety.isOn = loadedPlayerData.data[i].yesorno;

                }
                if (loadedPlayerData.data[i].Habit_ID == insomniaID)
                {
                    insomnia.isOn = loadedPlayerData.data[i].yesorno;

                }

            }
        }

    }




    public void OnChangeCheckup()
    {
     }

    public void OnChangeLungCancer()
    {
    
    }

    public void OnChangeOthercancer()
    {

    }
    public void OnChangeHeartDisease()
    {

    }
    public void OnChangeAsthma()
    {

    }

    public void OnChangeHighBlood()
    {

    }

    public void OnChangeCholesterol()
    {

    }

    public void OnChangeDiabetes()
    {

    }

    public void OnChangeArthritis()
    {
        Debug.Log("arthritis: " + arthritis.isOn);

    }
    public void OnChangeDepression()
    {

    }
    public void OnChangeAnxiety()
    {
   
    }
    public void OnChangeInsomnia()
    {

    }


    public void sethabits()
    {
        formthreemonths = Convert.ToInt32(threemonths.isOn);
        formthreeto6months = Convert.ToInt32(threeto6months.isOn);
        formsixto12 = Convert.ToInt32(sixto12.isOn);
        formmorethanyear = Convert.ToInt32(morethanyear.isOn);
        formlungcancer = Convert.ToInt32(lungcancer.isOn);
        formothercancer = Convert.ToInt32(othercancer.isOn);
        formheartdisease = Convert.ToInt32(heartdisease.isOn);
        formasthma = Convert.ToInt32(asthma.isOn);
        formhighblood = Convert.ToInt32(highblood.isOn);
        formcholesterol = Convert.ToInt32(cholesterol.isOn);
        formdiabetes = Convert.ToInt32(diabetes.isOn);
        formarthritis = Convert.ToInt32(arthritis.isOn);
        formdepression = Convert.ToInt32(depression.isOn);
        formanxiety = Convert.ToInt32(anxiety.isOn);
        forminsomnia = Convert.ToInt32(insomnia.isOn);
        StartCoroutine(pushhabits());
    }



    IEnumerator pushhabits()
    {
        WWWForm form = new WWWForm();

        UserHabitsPut obj = new UserHabitsPut();

        obj.data1.Add(new habitinfoput()
        {
            Habit_ID = threemonthsID,
            amount = formthreemonths,
            label = "binary"
        });

        obj.data1.Add(new habitinfoput()
        {
            Habit_ID = threeto6monthsID,
            amount = formthreeto6months,
            label = "binary"

        });

        obj.data1.Add(new habitinfoput()
        {
            Habit_ID = sixto12ID,
            amount = formsixto12,
            label = "binary"
        });

        obj.data1.Add(new habitinfoput()
        {
            Habit_ID = morethanyearID,
            amount = formmorethanyear,
            label = "binary"
        });

        obj.data1.Add(new habitinfoput()
        {
            Habit_ID = lungcancerID,
            amount = formlungcancer,
            label = "binary"
        });

        obj.data1.Add(new habitinfoput()
        {
            Habit_ID = othercancerID,
            amount = formothercancer,
            label = "binary"
        });

        obj.data1.Add(new habitinfoput()
        {
            Habit_ID = heartdiseaseID,
            amount = formheartdisease,
            label = "binary"
        });

        obj.data1.Add(new habitinfoput()
        {
            Habit_ID = asthmaID,
            amount = formasthma,
            label = "binary"
        });
        obj.data1.Add(new habitinfoput()
        {
            Habit_ID = highbloodID,
            amount = formhighblood,
            label = "binary"
        });
        obj.data1.Add(new habitinfoput()
        {
            Habit_ID = cholesterolID,
            amount = formcholesterol,
            label = "binary"
        });
        obj.data1.Add(new habitinfoput()
        {
            Habit_ID = diabetesID,
            amount = formdiabetes,
            label = "binary"
        });
        obj.data1.Add(new habitinfoput()
        {
            Habit_ID = arthritisID,
            amount = formarthritis,
            label = "binary"
        });
        obj.data1.Add(new habitinfoput()
        {
            Habit_ID = depressionID,
            amount = formdepression,
            label = "binary"
        });
        obj.data1.Add(new habitinfoput()
        {
            Habit_ID = anxietyID,
            amount = formanxiety,
            label = "binary"
        });
        obj.data1.Add(new habitinfoput()
        {
            Habit_ID = insomniaID,
            amount = forminsomnia,
            label = "binary"
        });
        string json = JsonUtility.ToJson(obj);


        Debug.Log("this is the json i created" + json);
        form.AddField("user", dbuserid);
        form.AddField("cthearray", json);

        //UnityWebRequest www1 = UnityWebRequest.Post(posturl, form); // The file location for where my .php file is.
        //yield return www1.SendWebRequest();
        //if (www1.isNetworkError || www1.isHttpError)
        //{
        //    Debug.Log(www1.error);
        //    // errorMessage = www.error;
        //}
        //else
        //{

        //    Debug.Log("Form Upload Complete!");

        //    Debug.Log("this comes back" + www1.downloadHandler.text);


        //}


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
           int payout = Convert.ToInt32(www.downloadHandler.text);

            justSetRiros = FindObjectOfType<justSetRiros>();
            justSetRiros.toPayOut();
            Debug.Log("this comes back" + www.downloadHandler.text);
            
            
            //ChangesceneInc = FindObjectOfType<ChangesceneInc>();
            //ChangesceneInc.ChangeSceneNow(Switchscene);

        }
    }
    [Serializable]
public class UserHabits
{
    public List<habitinfo> data;
}
[System.Serializable]

public class habitinfo
{
    public int Habit_ID;
    //  public string label;
    public int amount;
    public bool yesorno;

}

[Serializable]
public class habitinfoput
{
    public int Habit_ID;
    public string label;
    public int amount;


}

[Serializable]
public class UserHabitsPut
{
    public List<habitinfoput> data1 = new List<habitinfoput>();
}
}
