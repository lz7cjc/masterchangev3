using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Globalization;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;


public class gethabitsfromscene : MonoBehaviour
{
    string inserthabits = "https://masterchange.today/php_scripts/habitvaluesjson.php";
    string gethabits = "https://masterchange.today/php_scripts/gethabits.php";
    public void Start()
    {
        
    }
    public void whatsthescore()
    {

        var toggles = GameObject.FindObjectsOfType<Toggle>();
        var sliders = GameObject.FindObjectsOfType<Slider>();
        var Text = GameObject.FindObjectsOfType<Text>();

        foreach (var toggleIs in toggles)
        {
            Debug.Log("is it on" + toggleIs.isOn);
        }
        foreach (var sliderIs in sliders)
        {
         var slidervalue =  sliderIs.value;
            Debug.Log("is it on" + slidervalue);
            var nameofslider = sliderIs.name;
            Debug.Log("name of slider:" + nameofslider);

        }

        foreach (var textIs in Text)
        {
                var nameoftext= textIs.name;
                var textvalue = textIs.text;
              Debug.Log("New Record <br> name of text:" + nameoftext + "<br>");
                Debug.Log("value of text:" + textvalue);

        }
    }
   
}




