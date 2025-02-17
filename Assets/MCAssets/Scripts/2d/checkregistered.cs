using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class checkregistered : MonoBehaviour
{
    private int DBuser;
    public GameObject buybuttons;
    public GameObject registernow;
    private bool registered;

    // Start is called before the first frame update
    void Start()
    {
        if (PlayerPrefs.HasKey("dbuserid"))
        {
            DBuser = PlayerPrefs.GetInt("dbuserid");
            registered = true;
            Debug.Log("is registered");
        }
        else
        {
            registered = false;
            Debug.Log("is not registered");
        }
        Validation();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Validation()
    {
        if (registered)
        {
            Debug.Log("is registered fn");
            buybuttons.SetActive(true);
        registernow.SetActive(false);
        }
        else if (!registered)
        {
            Debug.Log("is not registered fn");
            buybuttons.SetActive(false);
            registernow.SetActive(true);

        }
    }
}
