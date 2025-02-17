using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Networking;

public class Habits : MonoBehaviour
{
    public Slider smokes;
    public Slider years;
    public Slider attempts;
    public Text textCigValue;
    public Text textQuitsValue;
    public Text textYearsSmoked;
    public Toggle nrt;
    public Toggle noNrt;
    public Toggle TTFCless;
    public Toggle TTFCmore;
    public Toggle prequit;
    public Toggle nodate;
    public Toggle quitting;

    private bool doesExisthabits;



    void Start()
    {
        string habits = Application.persistentDataPath + "/saveFile.json";
        doesExisthabits = File.Exists(habits);

        if (doesExisthabits)
        { 
            string json = File.ReadAllText(Application.persistentDataPath + "/saveFile.json");
        Debug.Log(Application.persistentDataPath + "/saveFile.json");
        PlayerData loadedPlayerData = JsonUtility.FromJson<PlayerData>(json);


        //set the variables for the form
        smokes.value = loadedPlayerData.cigsPerDay;
        years.value = loadedPlayerData.yearsSmoked;
        attempts.value = loadedPlayerData.previousQuits;
        nrt.isOn = loadedPlayerData.nrt;
        prequit.isOn = loadedPlayerData.prequit;
        quitting.isOn = loadedPlayerData.quitting;
        nodate.isOn = loadedPlayerData.nodate;
        TTFCless.isOn = loadedPlayerData.ttfcless;
        TTFCmore.isOn = loadedPlayerData.ttfcmore;
        }
    }

    public void Cigsday(float value)
    {
        Debug.Log("New cig per day Value " + smokes.value);

    }
    public void Quitattempts(float value)
    {
        Debug.Log("New attempts Value " + attempts.value);

    }
    public void YearsSmoked(float value)
    {
        Debug.Log("New years Value " + years.value);

    }

    void Update()
    {

        PlayerData playerData = new PlayerData
        {
            badCTscan = false,
            badge = "Orange",
            badXray = false,
            cigsPerDay = smokes.value,
            ctScan = false,
            goodCTscan = false,
            goodXray = false,
            learning = false,
            level = "learning",
            nrt = nrt.isOn,
            previousQuits = attempts.value,
            prequit = prequit.isOn,
            quitting = quitting.isOn,
            nodate = nodate.isOn,
            ttfcless = TTFCless.isOn,
            ttfcmore = TTFCmore.isOn,
            score = 0,
            stacy = false,
            yearsSmoked = years.value
        };
        textCigValue.text = smokes.value.ToString();
        textQuitsValue.text = attempts.value.ToString();
        textYearsSmoked.text = years.value.ToString();

        string json = JsonUtility.ToJson(playerData);


        File.WriteAllText(Application.persistentDataPath + "/saveFile.json", json);
    }

private class PlayerData
    {
       //Health factors
        public bool ttfcless;
        public bool ttfcmore;
        public bool nrt;
        public bool prequit;
        public bool quitting;
        public bool nodate;
        public float cigsPerDay;
        public float previousQuits;
        public float yearsSmoked;

        //progress
        public bool learning;
        public bool stacy;
        public bool goodXray;
        public bool badXray;
        public bool goodCTscan;
        public bool badCTscan;
        public bool ctScan;

        //scoring
        public string badge;
        public int score;
        public string level;

        //Jeopardy Coefficient
        public float jeopardyCoefficient;
    }
 
   
    
}