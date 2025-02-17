using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Networking;

public class BioDB : MonoBehaviour
{
    public Slider TheAge;
    public Slider TheWeight;
    public Slider TheHeight;
    public Text Textheight;
    public Text Textweight;
    public Text Textage;
    public Text TextBMI;
    private float BMI;

   
    public void Start()
    {
        string bio = Application.persistentDataPath + "/Bio.json";
        bool doesExistbio = File.Exists(bio);

        if (doesExistbio)
        {
            string json = File.ReadAllText(Application.persistentDataPath + "/Bio.json");
            Debug.Log(Application.persistentDataPath + "/Bio.json");
            PlayerData loadedPlayerData = JsonUtility.FromJson<PlayerData>(json);


            //set the variables for the form
            TheAge.value = loadedPlayerData.age;
            TheWeight.value = loadedPlayerData.weight;
            TheHeight.value = loadedPlayerData.height;
        }
    }

    public void WhatAge(float value)
    {
        Debug.Log("New cig per day Value " + TheAge.value);
        BMIcalc();

    }
    public void WhatWeight(float value)
    {
        Debug.Log("New attempts Value " + TheWeight.value);
        BMIcalc();
    }
    public void WhatHeight(float value)
    {
        Debug.Log("New years Value " + TheHeight.value);
        BMIcalc();
    }

    void BMIcalc()
    {

        BMI = (TheWeight.value / Mathf.Pow(TheHeight.value / 100, 2));

        TextBMI.text = BMI.ToString();
        Textheight.text = TheHeight.value.ToString();
        Textage.text = TheAge.value.ToString();
        Textweight.text = TheWeight.value.ToString();


        Debug.Log("Textage.text" + Textage.text);
        Debug.Log("Textheight.text" + Textheight.text);
        Debug.Log("Textweight.text" + Textweight.text);
        Debug.Log("BMI" + BMI);
     
        PlayerData playerData = new PlayerData
        {

            age = TheAge.value,
            weight = TheWeight.value,
            height = TheHeight.value,
            jsonbmi = BMI

        };

        string json = JsonUtility.ToJson(playerData);
        File.WriteAllText(Application.persistentDataPath + "/bio.json", json);
         WWWForm form = new WWWForm();
        
        
    }

    private class PlayerData
    {
        //Health factors
        public float age;
        public float weight;
        public float height;
        public float jsonbmi;

    }



}