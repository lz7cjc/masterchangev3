using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class showWhichFormat : MonoBehaviour
{

    public GameObject headset;
    public GameObject noHeadset;

    // used on player settings page to change the format to display the app in 2d or VR
    void Start()
    {
        if (PlayerPrefs.GetInt("toggleToVR") == 1)
        {
            headset.SetActive(false);
            noHeadset.SetActive(true);
        }

        else if (PlayerPrefs.GetInt("toggleToVR") == 0)
        {

            headset.SetActive(true);
            noHeadset.SetActive(false);
        }

    }

    // Update is called once per frame
    public void switchVR(int switchTo2DOption)
    {
        Debug.Log("hit script");
        if (switchTo2DOption == 1)
        {
            PlayerPrefs.SetInt("toggleToVR", 1);
            headset.SetActive(false);
            noHeadset.SetActive(true);

        }
        else if (switchTo2DOption == 0)
        {
            PlayerPrefs.SetInt("toggleToVR", 0);

            headset.SetActive(true);
            noHeadset.SetActive(false);

        }

    }
}
