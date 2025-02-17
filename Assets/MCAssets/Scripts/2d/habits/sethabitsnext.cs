using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class sethabitsnext : MonoBehaviour
{
    private updateuserdb updateuserdb;

    // Start is called before the first frame update
    void Start()
    {
        PlayerPrefs.SetInt("habitsdone", 1);
        updateuserdb = FindObjectOfType<updateuserdb>();
        updateuserdb.callToUpdate();

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
