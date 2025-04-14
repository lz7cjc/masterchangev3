using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Runtime.Serialization.Formatters;

public class showhideformat : MonoBehaviour
{
    //public GameObject showText;
    // public Toggle hideScreen;
    private int hidePickFormat;
    private int whichFormat;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("[showhideformat] Start method called.");
        var startTime = Time.realtimeSinceStartup;

        whichFormat = PlayerPrefs.GetInt("toggleToVR");
        Debug.Log($"[showhideformat] toggleToVR: {whichFormat}");

        if (whichFormat == 0)
        {
            Debug.Log("[showhideformat] Enforcing 2D mode.");
            // Add any specific logic to ensure 2D mode is active
        }

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

        Debug.Log($"[showhideformat] Initialization completed in {Time.realtimeSinceStartup - startTime} seconds.");
    }

    public void HidePickFormat(Toggle switchFormat)
    {
        // Update the hidePickFormat value based on the toggle state
        hidePickFormat = switchFormat.isOn ? 1 : 0;

        // Save the updated value to PlayerPrefs
        PlayerPrefs.SetInt("hidePickFormat", hidePickFormat);
        PlayerPrefs.Save();

        // Log the change for debugging purposes
        Debug.Log($"hidePickFormat updated to: {hidePickFormat}");
    }

    // Update is called once per frame
    
}
