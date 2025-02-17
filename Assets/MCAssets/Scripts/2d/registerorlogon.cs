using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class registerorlogon : MonoBehaviour
{
    public GameObject logon;
    
    // Start is called before the first frame update
    void Start()
    {
       if (!PlayerPrefs.HasKey("dbuserid"))
        {
            logon.SetActive(true);
        }
       else
        {
            logon.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
