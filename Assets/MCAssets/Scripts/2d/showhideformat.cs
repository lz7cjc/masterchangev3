using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class showhideformat : MonoBehaviour
{

    public GameObject showText;
    public Toggle hideScreen;
    private int hidePickFormat;
    private int whichFormat;

    // Start is called before the first frame update
    void Start()
    {
        ///////////
        ///hide the text about where to change settings and check if need to skip the screen
        showText.SetActive(false);
        hidePickFormat = PlayerPrefs.GetInt("hidePickFormat");
        whichFormat = PlayerPrefs.GetInt("toggleToVR");
        if (hidePickFormat == 1)
            {
            if (whichFormat == 1)
            {
                SceneManager.LoadScene("FirstVisit");
            }
           else if (whichFormat == 0)
            {
                SceneManager.LoadScene("FirstVisit2d");
            }
        }

    }

    // Update is called once per frame
    public void hide_Screen()
    {
        if (hideScreen.isOn)
        {
            PlayerPrefs.SetInt("hidePickFormat", 1);
            showText.SetActive(true);
        }

        else if (!hideScreen.isOn)
        {
            PlayerPrefs.SetInt("hidePickFormat", 0);
            
        }

    }
}
