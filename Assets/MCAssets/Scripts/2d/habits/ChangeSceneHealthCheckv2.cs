using UnityEngine;
using UnityEngine.SceneManagement;


public class ChangeSceneHealthCheckv2 : MonoBehaviour
{

    public bool mousehover = false;
    public float counter = 0;
    [SerializeField] private StartUp StartUp;
    
    public string behaviour;
    private bool form;
    private int stage;
    //public int stageAlcohol;
    //public int stageSmoking;
    //public int stageHeights;
    //public int stageSharks;

    private int habitsvalue;
    private floorceilingmove floorceilingmove;
    private float stopCounter;
    private int riros;

    private int stopFilm;
    private bool replaceStage = false;
    private bool tempStop;
   
    // Update is called once per frame
    void Update()
    {
        if (mousehover)
        {
            counter += Time.deltaTime;
            riros = PlayerPrefs.GetInt("rirosBalance");
            if (counter >= 3)
            {
                PlayerPrefs.SetString("returntoscene", "hospital");
                PlayerPrefs.SetString("nextscene", "hospital");
                mousehover = false;
                counter = 0;
                if (riros >= 50)
                {
                    form = false;
                    //stopFilm = 2;
                }
                else
                {
                    form = true;
                }

                if ((!form) && (!PlayerPrefs.HasKey("stopFilm")))
                {
                    PlayerPrefs.SetString("behaviour", behaviour);
                    switch (behaviour)
                    {
                        case "alcohol":
                            Debug.Log("alcohol switch");
                            if (stage > PlayerPrefs.GetInt("stageAlcohol"))
                            {
                                replaceStage = true;
                                PlayerPrefs.SetInt("stageAlcohol", stage);
                            }
                            break;

                        case "smoking":
                            Debug.Log("smoking switch");
                            habitsvalue = PlayerPrefs.GetInt("habitsdone");
                           
                            if ((stage > PlayerPrefs.GetInt("stageSmoking")) || (!PlayerPrefs.HasKey("stageSmoking")))
                            {
                                Debug.Log("update stage");
                                PlayerPrefs.SetInt("stageSmoking", stage);
                            }
                            if (habitsvalue == 1)
                            {
                                form = false;
                                Debug.Log("ooo form value should be able to move forward = " + form);
                            }
                            break;

                        case "heights":
                            if (stage > PlayerPrefs.GetInt("stageHeights"))
                            {
                                PlayerPrefs.SetInt("stageHeights", stage);
                            }
                            Debug.Log("heights switch");

                            break;

                        case "sharks":
                            if (stage > PlayerPrefs.GetInt("stageSharks"))
                            {
                                PlayerPrefs.SetInt("stageSharks", stage);
                            }
                            Debug.Log("sharks switch");

                            break;
                    }
                }

                else
                    {
                        PlayerPrefs.SetInt("stopFilm", stopFilm);
                        Debug.Log("have set stop to film ooo");
                    }

               
                StartUp.ResetScene();
            }
        }
    }

    // mouse Enter event
    public void MouseHoverChangeScene(int stageNew)
    {
        //  Debug.Log("setting scenename");
        // Debug.Log("behaviour to begin with: " + Behaviour);
        stage = stageNew;

        startStopMove(tempStop = true);
        mousehover = true;
    }

    // mouse Exit Event
    public void MouseExit()
    {
        mousehover = false;
        counter = 0;
    }

    private void startStopMove(bool tempStop)
    {
        if (tempStop)
        {
            stopCounter += Time.deltaTime;
            Debug.Log("stopcounter jjj" + stopCounter);
            if (stopCounter >= .01)
            {
             
                floorceilingmove.stopTheCamera();
            }
        }

    }


}
