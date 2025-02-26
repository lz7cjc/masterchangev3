using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class showhideformat : MonoBehaviour
{
    //public GameObject showText;
   // public Toggle hideScreen;
    private int hidePickFormat;
    private int whichFormat;

    // Start is called before the first frame update
    void Start()
    {
        whichFormat = PlayerPrefs.GetInt("toggleToVR");
        // Hide the text about where to change settings and check if need to skip the screen
        

        if (PlayerPrefs.HasKey("hidePickFormat"))
        {
            hidePickFormat = PlayerPrefs.GetInt("hidePickFormat");
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
        else
        {
            PlayerPrefs.SetInt("hidePickFormat", 1);
        }
    }

    // Update is called once per frame
    
}
