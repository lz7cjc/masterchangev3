using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;
using TMPro;

public class favouriteinfo : MonoBehaviour
{
    public TMP_Text favInstructions;
    public GameObject heart;
    public GameObject register;
    // Start is called before the first frame update
    void Start()
    {
        if (PlayerPrefs.HasKey("dbuserid"))
        {
            favInstructions.text = "If you want to save this film in your favourites, then choose the heart. This costs Ro$1500";
            heart.SetActive(true);
            register.SetActive(false);
        }
        else
        {
            favInstructions.text = "You can save the film as a favourite but you first need to register. Choose Register then take your headset off to set up your account";
            heart.SetActive(false);
            register.SetActive(true);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
