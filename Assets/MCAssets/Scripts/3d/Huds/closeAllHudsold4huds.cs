using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class closeAllHudsold4huds : MonoBehaviour
{
    private string nextScene;
    public GameObject hud1primary;
    public GameObject hud1PlusOff;
    public GameObject hud1PlusOn;
    public GameObject hud1Zones;
    public GameObject hud1Move;

    public GameObject hud2primary;
    public GameObject hud2PlusOff;
    public GameObject hud2PlusOn;
    public GameObject hud2Zones;
    public GameObject hud2Move;

    public GameObject hud3primary;
    public GameObject hud3PlusOff;
    public GameObject hud3PlusOn;
    public GameObject hud3Zones;
    public GameObject hud3Move;

    public GameObject hud4primary;
    public GameObject hud4PlusOff;
    public GameObject hud4PlusOn;
    public GameObject hud4Zones;
    public GameObject hud4Move;

    public GameObject showWalk1;
    public GameObject showNoWalk1;

    public GameObject showWalk2;
    public GameObject showNoWalk2;

    public GameObject showWalk3;
    public GameObject showNoWalk3;

    public GameObject showWalk4;
    public GameObject showNoWalk4;

    private string behaviour;

    // Start is called before the first frame update
    public void Start()
    {
        CloseTheHuds();
    }
    public void CloseTheHuds()
    {

       hud1primary.SetActive(false);
       hud1PlusOff.SetActive(false);
       hud1PlusOn.SetActive(true);
       hud1Zones.SetActive(false);
       hud1Move.SetActive(false);

        hud2primary.SetActive(false);
        hud2PlusOff.SetActive(false);
        hud2PlusOn.SetActive(true);
        hud2Zones.SetActive(false);
        hud2Move.SetActive(false);

        hud3primary.SetActive(false);
        hud3PlusOff.SetActive(false);
        hud3PlusOn.SetActive(true);
        hud3Zones.SetActive(false);
        hud3Move.SetActive(false);

        hud4primary.SetActive(false);
        hud4PlusOff.SetActive(false);
        hud4PlusOn.SetActive(true);
        hud4Zones.SetActive(false);
        hud4Move.SetActive(false);

        behaviour = PlayerPrefs.GetString("behaviour");
        nextScene = PlayerPrefs.GetString("nextscene");
   
        if ((nextScene == "") && ((behaviour == "space") || (behaviour == "sharks")))

        {
          //  // debug.log("space");
            showWalk1.SetActive(false);
            showNoWalk1.SetActive(true);

            showWalk2.SetActive(false);
            showNoWalk2.SetActive(true);

            showWalk3.SetActive(false);
            showNoWalk3.SetActive(true);

            showWalk4.SetActive(false);
            showNoWalk4.SetActive(true);
        }
        else
        {
           // // debug.log("nospace");
            showWalk1.SetActive(true);
            showNoWalk1.SetActive(false);

            showWalk2.SetActive(true);
            showNoWalk2.SetActive(false);

            showWalk3.SetActive(true);
            showNoWalk3.SetActive(false);

            showWalk4.SetActive(true);
            showNoWalk4.SetActive(false);
        }

    }

    }

