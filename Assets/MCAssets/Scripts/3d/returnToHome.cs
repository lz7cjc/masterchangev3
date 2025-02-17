using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class returnToHome : MonoBehaviour
{

    public bool mousehover = false;

    public float Counter = 0;

    public GameObject player;
    public GameObject cameratarget;

    private int getStage;
    public int delay = 3;
    public string behaviour;
    //private showhide3d showhide3d;
    private int putStage;
    private int trainingDone;
    // public moveHud moveHud;
  //  private FPVMovement FPVMovement;
    private hudCountdown hudCountdown;
    //public SpriteRenderer spriterenderer;
    //public Sprite spriteDefault;
    //public Sprite spriteSwitch;
    public string nextscene = "home";
    Vector3 rot = new Vector3(0, 0, 1);
    Vector3 rotationDirection = new Vector3();

    //////
    /// <summary>
    /// Option 1 - Return to Start
    /// Option 2 - Back one Step
    /// Option 3 - Go to experiences
    /// Option 3 - Go to reception
    /// 
    /// </summary>
    [Tooltip("Option 1 - Return to Start of BC | Option 2 - Back one Step | Option 3 - Go to Main Experiences Room | Option 4 - go back to reception | Option 5 - cancel BCT")]
    public int chooseOption;

    private void Start()
    {
        trainingDone = PlayerPrefs.GetInt("trainingdone");

    }

    void Update()
    {

        if (mousehover)
        {
            // // debug.log("in mousehover uuu");
            //moveHud = FindObjectOfType<moveHud>();
            //moveHud.stopTheCamera();
        //    FPVMovement = FPVMovement.FindFirstObjectByType<FPVMovement>();
        //    FPVMovement.OnMouseEnterStop();

            hudCountdown = hudCountdown.FindFirstObjectByType<hudCountdown>();
            hudCountdown.SetCountdown(delay, Counter);
            Counter += Time.deltaTime;
            if (Counter >= delay)
            {

                mousehover = false;
                Counter = 0;
                //Return to Start of BC
                if (chooseOption == 1)
                //return to start
                {
                    PlayerPrefs.SetInt("trainingdone", 1);
                    PlayerPrefs.DeleteKey("ctstartpoint");
                    PlayerPrefs.DeleteKey("delaynotification");

                }
                else if (chooseOption == 2)
                //back one step
                {
                    PlayerPrefs.SetInt("trainingdone", 1);

                    switch (behaviour)
                    {
                        case "smoking":
                            getStage = PlayerPrefs.GetInt("stagesmoking");
                            putStage = getStage - 1;
                            PlayerPrefs.SetString("nextscene", "hospital");
                            PlayerPrefs.SetInt("stagesmoking", putStage);


                            if (putStage == 2)
                            {
                                // PlayerPrefs.SetFloat("CTstartpoint");
                                PlayerPrefs.SetFloat("delaynotification", -1);
                            }
                            break;

                        case "alcohol":
                            getStage = PlayerPrefs.GetInt("stagealcohol");
                            putStage = getStage - 1;
                            PlayerPrefs.SetString("nextscene", "hospital");
                            PlayerPrefs.SetInt("stagealcohol", putStage);
                            break;

                    }   

                }
                //go to activity centre and cancel behaviour
                else if (chooseOption == 3)
                {
                    // // debug.log("in target function");
                    PlayerPrefs.SetInt("trainingdone", 1);
                    PlayerPrefs.SetString("nextscene", "home");

                }
                // go to reception
                else if (chooseOption == 4)
                {
                    PlayerPrefs.SetInt("trainingdone", 0);
                    PlayerPrefs.SetString("nextscene", nextscene);

                }


                //        // // debug.log("button option : " + trainingDone);

                showandhide();

            }
        }
    }

    // mouse Enter event
    public void MouseHoverChangeScene()
    {
        Debug.Log("returntohome MouseHoverChangeScene");
        // // debug.log("uuuu");
        // Markername = ObjectName;
        mousehover = true;

    }
    public void MouseHoverChangeSceneHud()
    {
        Debug.Log("returntohome MouseHoverChangeSceneHud");
        //     // debug.log("uuuu");
        // Markername = ObjectName;
        mousehover = true;
        //      spriterenderer.sprite = spriteSwitch;
    }


    // mouse Exit Event
    public void MouseExit()
    {
        //   // debug.log("cancelling walk");
        // Markername = "";
        mousehover = false;
        Counter = 0;
        hudCountdown.resetCountdown();
    }

    public void MouseExitHUD()
    {
        //   // debug.log("cancelling walk");
        // Markername = "";
        mousehover = false;
        Counter = 0;
        hudCountdown.resetCountdown();
        //  spriterenderer.sprite = spriteDefault;
    }
    public void showandhide()
    {
        //   // debug.log("calling showhide3d kkk");
        //    TargetObject.SetActive(true);
        player.transform.position = cameratarget.transform.position;
        //set rotation


        //TargetObject.SetActive(true);
        player.transform.SetParent(cameratarget.transform);
        player.transform.rotation = Quaternion.identity;
        //showhide3d = FindFirstObjectByType<showhide3d>();
      
        //showhide3d.ResetScene();


    }



}
