using System.Runtime.CompilerServices;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;
using Unity.Android.Gradle.Manifest;

// called from smoking/alcohol HUD button to determine where to enter the userjourney
public class BCTUserJourneyLaunch : MonoBehaviour
{
    [SerializeField] private Rigidbody player;
    //private int stage;

    [SerializeField] private GameObject returnToZone_tgt;
    [SerializeField] private string lastknownzone;
    [SerializeField] private bool mousehover = false;
    [SerializeField] private float counter = 0;
    // public string videoUrl;

    [SerializeField] private int stageSmoking;

    //smoking stages
    [SerializeField] private GameObject chooseXrayOutcome;
    [SerializeField] private GameObject chooseNoCTScan;
    [SerializeField] private GameObject chooseCTScan;
    [SerializeField] private GameObject getCTScanResult;
    [SerializeField] private GameObject finalWrapUp;

    [SerializeField] private GameObject cameraTarget;
    [SerializeField] private string videoUrl = "https://storage.googleapis.com/masterchange/behaviourchange/smoking/StacyXRayWelcome.mp4";
    [SerializeField] private bool gravity = true;
    [SerializeField] private GameObject stageTgt;
    //////

    /// <summary>
    /// This is to remind that this is the type of bct - lower case
    /// </summary>
    [Tooltip("smoking, alcohol etc lower case")]
    [SerializeField] private string behaviourType;
    [SerializeField] private float Counter = 0;
    [SerializeField] private int delay = 3;
    [SerializeField] private hudCountdown hudCountdown;
    [SerializeField] private LaunchVideo launchVideo;
    [SerializeField] private closeAllHuds closeAllHuds;
    [SerializeField] private ToggleActiveIcons ToggleActiveIcons;
    //[SerializeField] private closeAllHuds closeAllHuds;
    // public MoveCamera moveCamera;

    void Start()
    {
        ToggleActiveIcons.DefaultIcon();
    }

    // Update is called once per frame
    void Update()
    {
        if (mousehover)
        {
            ToggleActiveIcons.HoverIcon();
            hudCountdown.SetCountdown(delay, counter);
            counter += Time.deltaTime;
            if (counter >= delay)
            {
                ToggleActiveIcons.SelectIcon();
                mousehover = false;
                counter = 0;
                lastknownzone = returnToZone_tgt.name;
                PlayerPrefs.SetString("lastknownzone", lastknownzone);
                PlayerPrefs.SetString("bct", behaviourType);
                hudCountdown.resetCountdown();

                // which behaviour
                if (behaviourType == "smoking")
                {
                    // convert last zone to string so can store in pp
                    PlayerPrefs.SetString("lastknownzone", behaviourType);

                    // starting smoking journey
                    if ((stageSmoking == 0) || (!PlayerPrefs.HasKey("stagesmoking")))
                    {
                        Debug.Log("stageSmoking pre: " + stageSmoking);
                        stageSmoking = 1;
                        PlayerPrefs.SetString("videourl", videoUrl);
                        PlayerPrefs.SetInt("stagesmoking", stageSmoking);
                        Debug.Log("stageSmoking post: " + stageSmoking);
                        SceneManager.LoadScene("360VideoApp");
                    }
                    else if ((PlayerPrefs.HasKey("stagesmoking")) && (stageSmoking != 0))
                    {
                        if (stageSmoking == 1)
                        {
                            stageSmoking = 2;
                            chooseXrayOutcome.SetActive(true);
                            chooseNoCTScan.SetActive(false);
                            chooseCTScan.SetActive(false);
                            getCTScanResult.SetActive(false);
                            finalWrapUp.SetActive(false);
                        }

                        if (stageSmoking == 2)
                        {
                            stageTgt = chooseNoCTScan;
                            stageSmoking = 3;
                            chooseXrayOutcome.SetActive(false);
                            chooseNoCTScan.SetActive(true);
                            chooseCTScan.SetActive(false);
                            getCTScanResult.SetActive(false);
                            finalWrapUp.SetActive(false);
                        }

                        if (stageSmoking == 3)
                        {
                            stageTgt = chooseCTScan;
                            stageSmoking = 4;
                            chooseXrayOutcome.SetActive(false);
                            chooseNoCTScan.SetActive(false);
                            chooseCTScan.SetActive(true);
                            getCTScanResult.SetActive(false);
                            finalWrapUp.SetActive(false);
                        }

                        if (stageSmoking == 4)
                        {
                            stageTgt = getCTScanResult;
                            stageSmoking = 5;
                            chooseXrayOutcome.SetActive(false);
                            chooseNoCTScan.SetActive(false);
                            chooseCTScan.SetActive(false);
                            getCTScanResult.SetActive(true);
                            finalWrapUp.SetActive(false);
                        }

                        if (stageSmoking == 5)
                        {
                            stageSmoking = 0;
                            stageTgt = finalWrapUp;
                            chooseXrayOutcome.SetActive(false);
                            chooseNoCTScan.SetActive(false);
                            chooseCTScan.SetActive(false);
                            getCTScanResult.SetActive(false);
                            finalWrapUp.SetActive(true);
                        }
                    }
                }
                PlayerPrefs.SetInt("stagesmoking", stageSmoking);
                MoveCamera();
            }
        }
    }

    private void MoveCamera()
    {
        player.useGravity = gravity;
        closeAllHuds = FindFirstObjectByType<closeAllHuds>();
        closeAllHuds.CloseTheHuds();
        player.transform.position = stageTgt.transform.position;
        player.transform.SetParent(stageTgt.transform);
    }

    // mouse Enter event
    public void MouseHoverLaunchBCT(string _behaviourType)
    {
        behaviourType = _behaviourType;
        stageSmoking = PlayerPrefs.GetInt("stagesmoking");
        Debug.Log("bct value" + behaviourType);
        mousehover = true;
        counter = 0;
    }

    // mouse Exit Event
    public void MouseExit()
    {
        ToggleActiveIcons.DefaultIcon();
        hudCountdown.resetCountdown();
        mousehover = false;
        counter = 0;
    }
}
