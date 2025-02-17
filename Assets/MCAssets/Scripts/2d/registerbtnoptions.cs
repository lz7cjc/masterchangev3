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

public class registerbtnoptions : MonoBehaviour
{
    public Button logout;
    public Button register;
    public Button login;
    private bool dbuserbool;
    
    // Start is called before the first frame update
    void Start()
    {
        if (PlayerPrefs.HasKey("dbuserid"))
        {
            dbuserbool = true;
        }

        if (dbuserbool)
        {
            logout.interactable = true;
            register.interactable = false;
            login.interactable = false;

        }
        else
        {
            logout.interactable = false;
            register.interactable = true;
            login.interactable = true;
        }
    }

    public void fnlogout()
    {
        PlayerPrefs.DeleteKey("dbuserid");
        SceneManager.LoadScene("dashboard");
    }

    public void fnlogin()
    {
        SceneManager.LoadScene("logon");
    }

    public void fnlregister()
    {
        SceneManager.LoadScene("register");
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
