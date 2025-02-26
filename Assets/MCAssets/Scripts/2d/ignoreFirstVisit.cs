using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ignoreFirstVisit : MonoBehaviour
{
    public Toggle hideScreen;
    int HideStartScreen;

    // Start is called before the first frame update
    void Start()
    {
        // Assign the Toggle component in code
        hideScreen = GameObject.Find("HideScreen_tgl").GetComponent<Toggle>();

        if (hideScreen == null)
        {
            Debug.LogError("HideScreen Toggle is not assigned.");
            return;
        }

        // Add listener to the Toggle to call changeToggle method when the state changes
        hideScreen.onValueChanged.AddListener(delegate { changeToggle(); });

        // Get the current value of IntroScreen from PlayerPrefs
        HideStartScreen = PlayerPrefs.GetInt("IntroScreen");
        Debug.Log("gethideScreen in start ()" + HideStartScreen);

        if (HideStartScreen == 1)
        {
            SceneManager.LoadScene("dashboard");
        }
    }

    public void changeToggle()
    {
        if (hideScreen.isOn)
        {
            HideStartScreen = 1;
        }
        else
        {
            HideStartScreen = 0;
        }

        PlayerPrefs.SetInt("IntroScreen", HideStartScreen);
        Debug.Log("HideStartScreen set to " + HideStartScreen);
    }
}
