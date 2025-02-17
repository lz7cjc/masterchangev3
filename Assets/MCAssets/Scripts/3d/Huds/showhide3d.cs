//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using System;
//using UnityEngine.SceneManagement;
//using System.Net;

////this is used when coming from a different scene
//public class showhide3d : MonoBehaviour
//{
//    //moving camera
//    //  public GameObject player;
//    private Rigidbody Player;

//    //targets

//    public GameObject targetHome;
//    public GameObject targetSmoking0;
//    public GameObject targetMfn;
//    public GameObject targetIoW;
//    public GameObject targetNC500;
//    public GameObject targetOman;
//    public GameObject targetBrasil;
//    public GameObject interruptFilm;
//    //public GameObject targetSmoking1;
//    //public GameObject targetSmoking2;
//    //public GameObject targetSmoking3;
//    //public GameObject targetSmoking4;
//    //public GameObject targetSmoking5;
//    public GameObject targetactivityCentre;

//    public GameObject targetalcohol1;
//    public GameObject targetalcohol2;
//    public GameObject targetalcohol3;
//    public GameObject targetalcohol4;
//    public GameObject waitingtgt;
//    //  public GameObject targetfilm;
//    // public GameObject targettip;

//    //generic films
//    public GameObject targetTravel;
//    public GameObject targetBeaches;




//    public GameObject preReadyCT;
//    public GameObject readyCT;


//    private riroStopGo riroStopGo;
//    private closeAllHuds closeAllHuds;
//    //variables for playerprefs
//    private int stage;

//    private string nextscene;
//    private string returntoscene;
//    //  private int isLearning;
//    private bool gotnextscene;
//    private string behaviour;
//    private int trainingDone;
//    private bool toggler;

//    //show/hide sections
//    // public GameObject section_tip;
//    public GameObject alcoholintro;
//    public GameObject alcoholfollow;
//    public GameObject alcoholsolution;
//    public GameObject alcoholfinish;
//    public GameObject welcomeSmoking;
//    public GameObject riroCheck;

//    public GameObject xrayResults;
//    public GameObject stopGoCtscan;
//    public GameObject CTresults;
//    public GameObject smokingDone;
//    //public videocontrollerff videocontrollerff;
//    //   public GameObject films;
//    public GameObject notFilms;
//    public GameObject terrain;

//    private justGetRiros justGetRiros;
//    private setCTdate setCTdate;
//    private int starting;

//    private int scriptCounter;
//    private int stopFilm;
//    public GameObject mainCamera;
//    Vector3 m_EulerAngleVelocity;

//    public GameObject hud;
//    public Rigidbody player;

//    //setting VR 
//    //variable for user choice
//    private int headsetOr2D;
//    //run either VR or 2d - split screen or not
//    //public GameObject initVR;
//    //public GameObject init2d;

//    public GameObject mainCameraVR;
//    public GameObject mainCamera2D;

//    public VRViewToggle VRViewToggle;
//    public SpriteRenderer spriterendererVR1;
//    public SpriteRenderer spriterendererVR2;
//    public SpriteRenderer spriterendererVR3;
//    public SpriteRenderer spriterendererVR4;

//    public SpriteRenderer spriterendererNoVR1;
//    public SpriteRenderer spriterendererNoVR2;
//    public SpriteRenderer spriterendererNoVR3;
//    public SpriteRenderer spriterendererNoVR4;

//    public Sprite spritePickedVR;
//    public Sprite spritePickedNoVR;

//    private pickVideoPlayer pickVideoPlayer;

//    private void Start()
//    {

//        Debug.Log("showhide3d starts");
//        Debug.Log("nextscene" + PlayerPrefs.GetString("nextscene"));
//        Debug.Log("returntoscene" + PlayerPrefs.GetString("returntoscene"));

//        ResetScene();


//    }
//    //get player prefs

//    /// <summary>
//    /// check to see if there is a next scene set. This is used when leaving VR and going back in or when playing a film,
//    /// so you are taken back to the right place. 
//    ///     /// </summary>
//    public void ResetScene()
//    {
//        //////////////////////
//        /// check to see if run in 2d or VR
//        //////////////////////
//        ///First make both VR or non Gameobjects which contain the toggle scripts inactive
//        //initVR.SetActive(false);
//        //init2d.SetActive(false);

//        /////
//        ///are they playing in 2D or VR mode
//        ///
//        headsetOr2D = PlayerPrefs.GetInt("toggletovr");
//        // // debug.log("vrornotvalue = " + headsetOr2D);
//        if (headsetOr2D == 1)
//        {
//            //  // debug.log("yyyy in the showide headset script VR =1");
//            //initVR.SetActive(true);
//            //init2d.SetActive(false);
//            //spriterendererNoVR1.sprite = spritePickedNoVR;
//            //spriterendererNoVR2.sprite = spritePickedNoVR;
//            //spriterendererNoVR3.sprite = spritePickedNoVR;
//            //spriterendererNoVR4.sprite = spritePickedNoVR;
//            VRViewToggle = VRViewToggle.FindFirstObjectByType<VRViewToggle>();
//            StartCoroutine(VRViewToggle.EnableVRMode());
//            mainCameraVR.SetActive(true);
//            mainCamera2D.SetActive(false);
//        }

//        else if (headsetOr2D == 0)
//        {
//            //   // debug.log("yyyy in the showide headset script routine V=0");

//            //init2d.SetActive(true);
//            //initVR.SetActive(false);
//            //spriterendererVR1.sprite = spritePickedVR;
//            //spriterendererVR2.sprite = spritePickedVR;
//            //spriterendererVR3.sprite = spritePickedVR;
//            //spriterendererVR4.sprite = spritePickedVR;
//            VRViewToggle = VRViewToggle.FindFirstObjectByType<VRViewToggle>();
//            VRViewToggle.Disable360Mode();
//            mainCameraVR.SetActive(false);
//            mainCamera2D.SetActive(true);
//        }
//        //close all huds to reset
//        closeAllHuds = closeAllHuds.FindFirstObjectByType<closeAllHuds>();
//        closeAllHuds.CloseTheHuds();
//        ////// debug.log("resetscene ^^^");
//        ///
//        /// to main scene or training
//        trainingDone = PlayerPrefs.GetInt("trainingdone");
//        toggler = false;
//        // Player = GameObject.Find("Player").GetComponent<Rigidbody>();

//        if (trainingDone == 0)
//        {

//            player.transform.position = targetHome.transform.position;
//            player.transform.SetParent(targetHome.transform);
//            toggler = true;
//            // PlayerPrefs.SetInt("TrainingDone", 1);
//        }

//        //otherwise run script
//        else if (trainingDone == 1)
//        {
//            whereto();
//        }


//    }

//    private void whereto()
//    {
//        //get player prefs and assign to variables
//        returntoscene = PlayerPrefs.GetString("returntoscene");
//        stage = PlayerPrefs.GetInt("stage");
//        behaviour = PlayerPrefs.GetString("behaviour");
//        nextscene = PlayerPrefs.GetString("nextscene");
//        trainingDone = PlayerPrefs.GetInt("trainingdone");
//        stopFilm = PlayerPrefs.GetInt("stopfilm");


//        {
//            //if we have no behaviour set and we aren't launching a film or going to the tips or intercepting the films
//            // i.e. this is the default behaviour going to activity centre
//            // if (nextscene == "sectors")
//            // {
//            //     goToSectors();
//            // }
//            //else
//            if ((nextscene == "home") || (((nextscene == "") && (nextscene != "film") && (nextscene != "tip") && (!PlayerPrefs.HasKey("stopfilm")) && (trainingDone == 1))))
//            {
//                Debug.Log("in the catch all");
//                notFilms.SetActive(true);
//                hud.SetActive(true);
//                player.transform.position = targetactivityCentre.transform.position;
//                player.transform.SetParent(targetactivityCentre.transform);
//            }
//            else
//            {
//                goToNextScene();
//            }
//        }
//    }

//    private void goToNextScene()
//    {
//        //  //// debug.log("lll next scene is: " + nextscene);
//        if (PlayerPrefs.HasKey("stopfilm"))
//        {
//            stopTheFilm();
//        }

//        else if (nextscene == "sectors")
//        {
//            goToSectors();

//        }
//        else if (nextscene == "hospital")
//        {
//            runVR();
//        }

        

//        else if (nextscene == "film")
//        {
//            pickVideoPlayer = pickVideoPlayer.FindFirstObjectByType<pickVideoPlayer>();
//            pickVideoPlayer.pickVideoFormat();
//        }
//        else if (nextscene == "register")
//        {
//            //      //// debug.log("kkk8 register"); 
//            SceneManager.LoadScene("register");
//            PlayerPrefs.SetString("nextscene", returntoscene);
//            PlayerPrefs.DeleteKey("returntoscene");
//            //      //// debug.log("^^^ register");


//        }

//        else if (nextscene == "dashboard")
//        {
//            //     //// debug.log("kkk9 dashboard");

//            SceneManager.LoadScene("dashboard");
//            PlayerPrefs.SetString("nextscene", returntoscene);
//            PlayerPrefs.DeleteKey("returntoscene");
//            //     //// debug.log("^^^ dashboard");

//        }

//        else
//        {
//            //     //// debug.log("kkk10 runVR");
//            //     //// debug.log("^^^ runVR");
//            runVR();
//        }
//    }

//    public void runVR()
//    {
//        //     //// debug.log("kkk11 runVR");
//        notFilms.SetActive(true);
//        hud.SetActive(true);
//        //   PlayerPrefs.DeleteKey("nextscene");
//        goToBehaviourChange();
//    }


//    public void goToBehaviourChange()
//    {
//        //      //// debug.log("kkk12 in hospital function");
//        //   //// debug.log("^^^ behaviour change");
//        terrain.SetActive(true);
//        switch (behaviour)
//        {
//            case "alcohol":
//                stage = PlayerPrefs.GetInt("stagealcohol");
//                if (stage == 0)
//                {

//                    alcoholintro.SetActive(true);
//                    alcoholfollow.SetActive(false);
//                    alcoholsolution.SetActive(false);
//                    alcoholfinish.SetActive(false);
//                    player.transform.position = targetalcohol1.transform.position;
//                    player.transform.SetParent(targetalcohol1.transform);
//                    //          //// debug.log("kkk13 in alcohol stage 0");
//                    //         //// debug.log("^^^ alcohol0");
//                }
//                else if (stage == 1)
//                {
//                    //         //// debug.log("kkk14 in alcohol stage 1");
//                    alcoholintro.SetActive(false);
//                    alcoholfollow.SetActive(true);
//                    alcoholsolution.SetActive(false);
//                    alcoholfinish.SetActive(false);

//                    player.transform.position = targetalcohol2.transform.position;
//                    player.transform.SetParent(targetalcohol2.transform);
//                    //         //// debug.log("^^^ alcohol1");
//                }
//                else if (stage == 2)
//                {
//                    //         //// debug.log("kkk15 in alcohol stage 2");
//                    alcoholintro.SetActive(false);
//                    alcoholfollow.SetActive(false);
//                    alcoholsolution.SetActive(true);
//                    alcoholfinish.SetActive(false);
//                    player.transform.position = targetalcohol3.transform.position;
//                    player.transform.SetParent(targetalcohol3.transform);
//                    //       //// debug.log("^^^ alcohol2");
//                }
//                else if (stage == 3)
//                {
//                    //        //// debug.log("kkk16 in alcohol stage 3");
//                    alcoholintro.SetActive(false);
//                    alcoholfollow.SetActive(false);
//                    alcoholsolution.SetActive(false);
//                    alcoholfinish.SetActive(true);
//                    //        //// debug.log("^^^ alcohol3");

//                    player.transform.position = targetalcohol4.transform.position;
//                    player.transform.SetParent(targetalcohol4.transform);
//                }
//                break;

//            case "smoking":
//                stage = PlayerPrefs.GetInt("stagesmoking");
//                // // debug.log("^^^ smoking");
//                //    keyButtons.SetActive(true);
//                //Welcome to smoking
//                if (stage == 0)
//                {

//                    //// debug.log("^^^ smoking0");

//                    welcomeSmoking.SetActive(true);
//                    xrayResults.SetActive(false);
//                    stopGoCtscan.SetActive(false);
//                    CTresults.SetActive(false);
//                    smokingDone.SetActive(false);
//                    player.transform.position = targetSmoking0.transform.position;
//                    player.transform.SetParent(targetSmoking0.transform);
//                }
//                //Get XRay Results
//                else if (stage == 1)
//                {
//                    //// debug.log("^^^ smoking1");
//                    welcomeSmoking.SetActive(false);
//                    xrayResults.SetActive(true);
//                    stopGoCtscan.SetActive(false);
//                    CTresults.SetActive(false);
//                    smokingDone.SetActive(false);
//                    player.transform.position = targetSmoking0.transform.position;
//                    player.transform.SetParent(targetSmoking0.transform);
//                }

//                //book CT Scan and go for scan when allowed
//                else if (stage == 2)
//                {
//                    //// debug.log("^^^ smoking2");
//                    welcomeSmoking.SetActive(false);
//                    xrayResults.SetActive(false);
//                    stopGoCtscan.SetActive(true);
//                    CTresults.SetActive(false);
//                    smokingDone.SetActive(false);
//                    stopGoCtscan.SetActive(true);
//                    setCTdate = setCTdate.FindFirstObjectByType<setCTdate>();
//                    setCTdate.setReferenceDate();

//                    if (PlayerPrefs.GetInt("delaynotification") > 0)
//                    {
//                        //// debug.log("^^^ smoking2 wait for CT");
//                        xrayResults.SetActive(false);
//                        stopGoCtscan.SetActive(true);
//                        preReadyCT.SetActive(true);
//                        player.transform.position = targetSmoking0.transform.position;
//                        player.transform.SetParent(targetSmoking0.transform);
//                    }
//                    else
//                    {
//                        //// debug.log("^^^ smoking2 ready for CT");

//                        readyCT.SetActive(true);
//                        xrayResults.SetActive(false);
//                        stopGoCtscan.SetActive(true);
//                        player.transform.position = targetSmoking0.transform.position;
//                        player.transform.SetParent(targetSmoking0.transform);

//                    }

//                }

//                //    PlayerPrefs.DeleteKey("nextscene");
//                //     PlayerPrefs.DeleteKey("stage");


//                // get CT scan results
//                else if (stage == 3)
//                {
//                    //// debug.log("^^^ smoking3 post  CT");
//                    PlayerPrefs.DeleteKey("ctstartpoint");
//                    PlayerPrefs.DeleteKey("delaynotification");

//                    welcomeSmoking.SetActive(false);
//                    xrayResults.SetActive(false);
//                    stopGoCtscan.SetActive(false);
//                    CTresults.SetActive(true);
//                    smokingDone.SetActive(false);
//                    player.transform.position = targetSmoking0.transform.position;
//                    player.transform.SetParent(targetSmoking0.transform);
//                    PlayerPrefs.DeleteKey("delaynotification");
//                    PlayerPrefs.DeleteKey("ctstartpoint");
//                }

//                else if (stage == 4)
//                {
//                    //// debug.log("^^^ smoking4");

//                    welcomeSmoking.SetActive(false);
//                    xrayResults.SetActive(false);
//                    stopGoCtscan.SetActive(false);
//                    CTresults.SetActive(false);
//                    smokingDone.SetActive(true);
//                    player.transform.position = targetSmoking0.transform.position;
//                    player.transform.SetParent(targetSmoking0.transform);
//                }
//                break;

//            default:
//                goToSectors();
//                break;
//        }
//    }

//    public void goToSectors()
//    {
//        Debug.Log("in sectors");
//        notFilms.SetActive(true);
//        hud.SetActive(true);
//        terrain.SetActive(true);
//        if (behaviour == "travel")
//        {
//            //     //// debug.log("^^^ travel");
//            player.useGravity = true;
//            player.transform.position = targetTravel.transform.position;
//            player.transform.SetParent(targetTravel.transform);
//        }

//        else if (behaviour == "oman")
//        {
//            //     //// debug.log("^^^ targetCalm");
//            player.useGravity = true;
//            player.transform.position = targetOman.transform.position;
//            player.transform.SetParent(targetOman.transform);
//        }
//        else if (behaviour == "brasil")
//        {
//            //     //// debug.log("^^^ targetCalm");
//            player.useGravity = true;
//            player.transform.position = targetBrasil.transform.position;
//            player.transform.SetParent(targetBrasil.transform);
//        }

//        else if (behaviour == "beaches")
//        {
//            //     //// debug.log("^^^ targetBeaches");
//            player.useGravity = true;
//            player.transform.position = targetBeaches.transform.position;
//            player.transform.SetParent(targetBeaches.transform);
//        }


//        else if (behaviour == "mindfulness")
//        {
//            //   //// debug.log("^^^ targetSpace");
//            player.useGravity = true;
//            player.transform.position = targetMfn.transform.position;
//            player.transform.SetParent(targetMfn.transform);
//        }
//        else if (behaviour == "nc500")
//        {
//            //   //// debug.log("^^^ targetSpace");
//            player.useGravity = true;
//            player.transform.position = targetNC500.transform.position;
//            player.transform.SetParent(targetNC500.transform);
//        }
//        else if (behaviour == "iow")
//        {
//            //   //// debug.log("^^^ targetSpace");
//            player.useGravity = true;
//            player.transform.position = targetIoW.transform.position;
//            player.transform.SetParent(targetIoW.transform);
//        }

    

//       // PlayerPrefs.DeleteKey("nextscene");
//        notFilms.SetActive(true);


//    }
//    public void stopTheFilm()
//    {
//        // //// debug.log("^^^ stopTheFilm");
//        notFilms.SetActive(true);
//        terrain.SetActive(true);
//        hud.SetActive(true);
//        stopFilm = PlayerPrefs.GetInt("stopfilm");
//        riroStopGo = riroStopGo.FindFirstObjectByType<riroStopGo>();
//        riroStopGo.doNotPass(stopFilm);
//        player.transform.position = interruptFilm.transform.position;
//        player.transform.SetParent(interruptFilm.transform);
//        PlayerPrefs.DeleteKey("stopfilm");
//    }

//}

