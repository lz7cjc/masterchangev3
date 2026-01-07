using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class BCTUserJourneyLaunch : MonoBehaviour
{
    [SerializeField] private CharacterController player;
    [SerializeField] private GameObject returnToZone_tgt;
    [SerializeField] private bool mousehover = false;
    [SerializeField] private float counter = 0;
    private int stageSmoking;

    //smoking stages
    [SerializeField] private GameObject InitialConsultation;
    [SerializeField] private GameObject CTScanDelay;
    [SerializeField] private GameObject CTScan;
    [SerializeField] private GameObject CTScanResult;
    [SerializeField] private GameObject FinalConsultation;

    [SerializeField] private GameObject cameraTarget;
    [SerializeField] private string videoUrl = "https://storage.googleapis.com/masterchange/behaviourchange/smoking/StacyXRayWelcome.mp4";
    [SerializeField] private bool gravity = true;
    [SerializeField]  public string stageTgt;

    [Tooltip("smoking, alcohol etc lower case")]
    [SerializeField] private string behaviourType;
   // [SerializeField] private float Counter = 0;
    [SerializeField] private int delay = 3;
    [SerializeField] private hudCountdown hudCountdown;
    [SerializeField] private LaunchVideo launchVideo;
    [SerializeField] private closeAllHuds closeAllHuds;

    void Start()
    {
        //ToggleActiveIcons.DefaultIcon();
    }

    //private void UpdateStageTgt()
    //{
    //    stageSmoking = PlayerPrefs.GetInt("stagesmoking");
    //    if ((!PlayerPrefs.HasKey("stagesmoking")) || (stageSmoking == 1))
    //    {
    //        stageTgt = "CTScanDelay";
    //    }

    //    if (stageSmoking == 2)
    //    {
    //        stageTgt = "CTScanDelay";
    //    }
    //    else if (stageSmoking == 3)
    //    {
    //        stageTgt = "CTScan";
    //    }
    //    else if (stageSmoking == 4)
    //    {
    //        stageTgt = "CTScanResult";
    //    }
    //    else if (stageSmoking == 5)
    //    {
    //        stageTgt = "FinalConsultation";
    //    }
    //}

    void Update()
    {
        if (mousehover)
        {
            hudCountdown.SetCountdown(delay, counter);
            counter += Time.deltaTime;
            if (counter >= delay)
            {
                mousehover = false;
                counter = 0;
                PlayerPrefs.SetString("bct", behaviourType);
                hudCountdown.resetCountdown();

                if (behaviourType == "smoking")
                {
                    PlayerPrefs.SetString("lastknownzone", cameraTarget.name);
                   // UpdateStageTgt();
                    if (stageSmoking == 1)
                    {
                        PlayerPrefs.SetString("VideoUrl", videoUrl);
                        PlayerPrefs.SetInt("stageSmoking", 2);
                        PlayerPrefs.Save();
                        SceneManager.LoadScene("360VideoApp", LoadSceneMode.Single); 
                      ////  stageSmoking = 2;
                      //  InitialConsultation.SetActive(true);
                      //  CTScanDelay.SetActive(false);
                      //  CTScan.SetActive(false);
                      //  CTScanResult.SetActive(false);
                      //  FinalConsultation.SetActive(false);
                    }
                    else if (stageSmoking == 2)
                    {
                    //    stageSmoking = 3;
                        InitialConsultation.SetActive(false);
                        CTScanDelay.SetActive(true);
                        CTScan.SetActive(false);
                        CTScanResult.SetActive(false);
                        FinalConsultation.SetActive(false);
                    }
                    else if (stageSmoking == 3)
                    {
                      //  stageSmoking = 4;
                        InitialConsultation.SetActive(false);
                        CTScanDelay.SetActive(false);
                        CTScan.SetActive(true);
                        CTScanResult.SetActive(false);
                        FinalConsultation.SetActive(false);
                    }
                    else if (stageSmoking == 4)
                    {
                      //  stageSmoking = 5;
                        InitialConsultation.SetActive(false);
                        CTScanDelay.SetActive(false);
                        CTScan.SetActive(false);
                        CTScanResult.SetActive(true);
                        FinalConsultation.SetActive(false);
                    }
                    else if (stageSmoking == 5)
                    {
                       // stageSmoking = 0;
                        InitialConsultation.SetActive(false);
                        CTScanDelay.SetActive(false);
                        CTScan.SetActive(false);
                        CTScanResult.SetActive(false);
                        FinalConsultation.SetActive(true);
                    }
                }
                PlayerPrefs.SetInt("stageSmoking", stageSmoking);
                MoveCamera();
            }
        }
    }

    private void MoveCamera()
    {
        GameObject target = GameObject.Find(stageTgt);
        if (target == null)
        {
            Debug.LogError("stageTgt is not assigned or not found!");
            return;
        }

     //   player.useGravity = gravity;
        closeAllHuds = FindFirstObjectByType<closeAllHuds>();
        closeAllHuds.CloseTheHuds();
        player.transform.position = target.transform.position;
        player.transform.SetParent(target.transform);
    }

    // mouse Enter event
    public void MouseHoverLaunchBCT(int _stageSmoking)
    {
        stageSmoking = _stageSmoking;
        Debug.Log("stageSmoking" + stageSmoking);
        
        Debug.Log("bct value" + behaviourType);
        mousehover = true;
        counter = 0;
    }

    // mouse Exit Event
    public void MouseExit()
    {
        hudCountdown.resetCountdown();
        mousehover = false;
        counter = 0;
    }
}

